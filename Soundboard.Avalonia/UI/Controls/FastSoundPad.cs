using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Styling;
using Microsoft.Extensions.DependencyInjection;
using Soundboard.Avalonia.Discovery;
using Soundboard.Avalonia.ViewModels;
using System;
using System.ComponentModel;
using MouseButton = Avalonia.Input.MouseButton;

namespace Soundboard.Avalonia.UI.Controls;

public enum BorderEffect
{
    AnimatedGradient,
    Solid,
    None
}

public record struct PadMetrics()
{
    public CornerRadius CornerRadius { get; set; } = new(16);
    public double PaddingLeft { get; set; } = 14;
    public double PaddingTop { get; set; } = 10;
    public double PaddingRight { get; set; } = 10;
    public double PaddingBottom { get; set; } = 10;
    public double AccentThickness { get; set; } = 6.0;
    public double BorderThickness { get; set; } = 2.0;

    public static readonly PadMetrics Default = new();
}

public readonly record struct PadPalette(
    ImmutableSolidColorBrush Surface,
    ImmutableSolidColorBrush Border,
    ImmutableSolidColorBrush TextPrimary,
    ImmutableSolidColorBrush TextMuted,
    bool IsLightTheme)
{
    private static PadPalette? _dark;
    private static PadPalette? _light;

    public static PadPalette Get(ThemeVariant theme)
    {
        if (theme == ThemeVariant.Light && _light is not null) return _light.Value;
        if (theme == ThemeVariant.Dark && _dark is not null) return _dark.Value;

        var palette = Create(theme);
        if (theme == ThemeVariant.Light) _light = palette;
        else _dark = palette;

        return palette;
    }

    private static readonly ImmutableSolidColorBrush ErrorBrush = new(Colors.Magenta);

    private static ImmutableSolidColorBrush ResolveBrush(Application? app, string resourceKey, ThemeVariant theme)
    {
        return app?.TryFindResource(resourceKey, theme, out var res) is true && res switch
        {
            ImmutableSolidColorBrush immutable => immutable,
            ISolidColorBrush solid => new ImmutableSolidColorBrush(solid.Color),
            Color color => new ImmutableSolidColorBrush(color),
            _ => null
        } is { } brush
            ? brush
            : ErrorBrush;
    }

    private static PadPalette Create(ThemeVariant theme)
    {
        var app = Application.Current;

        return new PadPalette(
            Surface: ResolveBrush(app, "PadSurfaceBrush", theme),
            Border: ResolveBrush(app, "PadBorderBrush", theme),
            TextPrimary: ResolveBrush(app, "TextPrimaryBrush", theme),
            TextMuted: ResolveBrush(app, "TextMutedBrush", theme),
            IsLightTheme: theme == ThemeVariant.Light);
    }
}

public partial class FastSoundPad : Control
{
    // Feature Flags & Effects
    private bool _enableShadows = true;
    private BorderEffect _borderEffect = BorderEffect.AnimatedGradient;
    private bool _enableProgress = true;
    private bool _enableText = true;
    private bool _showCategory = true;
    private bool _showTitle = true;
    private bool _showFooter = true;

    // Cache dirtiness
    private bool _titleDirty = true;
    private bool _titleWrapped;
    private double _titleUnwrappedWidth;

    // Runtime state
    private SoundModel? _boundSound;
    private bool _pointerDown;

    private PadMetrics _metrics = PadMetrics.Default;
    private PadPalette _palette;

    private static readonly Cursor HandCursor = new(StandardCursorType.Hand);

    public FastSoundPad()
    {
        ClipToBounds = false;
        Cursor = HandCursor;

        UpdateThemeBrushes();
        InitializeRendering();
        InitializeAnimations();

        AddHandler(PointerPressedEvent, OnPressed, RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(PointerReleasedEvent, OnReleased, RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(PointerMovedEvent, OnMoved, RoutingStrategies.Tunnel, handledEventsToo: true);
    }

    #region Layout & Sizing Observation

    private const double TitleCharWidthThreshold = 14.0 * 0.55;
    private double _lastTitleWidth;
    private double _lastTitleHeight;

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);

        var contentWidth = Math.Max(0, e.NewSize.Width - _metrics.PaddingLeft - _metrics.PaddingRight);
        var contentHeight = Math.Max(0, e.NewSize.Height - _metrics.PaddingTop - _metrics.PaddingBottom);

        if (!_titleWrapped && _titleLayout is not null && contentWidth >= _titleUnwrappedWidth)
        {
            _lastTitleWidth = contentWidth;
            return;
        }

        if (Math.Abs(_lastTitleWidth - contentWidth) >= TitleCharWidthThreshold)
        {
            _lastTitleWidth = contentWidth;
            _titleDirty = true;
        }
        else if (Math.Abs(_lastTitleHeight - contentHeight) >= 18.0)
        {
            _lastTitleHeight = contentHeight;
            _titleDirty = true;
        }
    }

    #endregion

    #region Data Context & Property Observation

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        _pointerDown = false;
        _titleDirty = true;
        _titleWrapped = true; // force first layout
        _titleLayout = null;

        if (_boundSound is not null)
            _boundSound.PropertyChanged -= OnSoundPropertyChanged;

        _boundSound = DataContext as SoundModel;

        if (_boundSound is not null && VisualRoot is not null)
        {
            _boundSound.PropertyChanged += OnSoundPropertyChanged;
            if (_boundSound.IsPlaying)
            {
                StartAnimation(true);
            }
        }

        BuildImmutableText(_boundSound);
        InvalidateVisual();
    }

    private void OnSoundPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SoundModel.IsPlaying))
        {
            StartAnimation(_boundSound?.IsPlaying == true);
        }
        else if (e.PropertyName == nameof(SoundModel.Name))
        {
            _titleDirty = true;
        }

        InvalidateVisual();
    }

    #endregion

    #region Pointer & Click Handling

    private void OnPressed(object? s, PointerPressedEventArgs e) => _pointerDown = true;

    private void OnReleased(object? s, PointerReleasedEventArgs e)
    {
        var wasDown = _pointerDown;
        _pointerDown = false;

        if (DataContext is not SoundModel sound) return;
        if (!wasDown || !new Rect(Bounds.Size).Contains(e.GetPosition(this))) return;

        var vm = App.Host?.Services.GetService<SoundboardViewModel>();

        switch (e.InitialPressMouseButton)
        {
            case MouseButton.Left:
                vm?.PlaySound(sound);
                e.Handled = true;
                break;
            case MouseButton.Right:
                vm?.StopSound(sound);
                e.Handled = true;
                break;
            case MouseButton.None:
            case MouseButton.Middle:
            case MouseButton.XButton1:
            case MouseButton.XButton2:
            default:
                break;
        }
    }

    private void OnMoved(object? s, PointerEventArgs e)
    {
        var p = e.GetCurrentPoint(null).Properties;
        if (!_pointerDown || p.IsLeftButtonPressed || p.IsRightButtonPressed) return;

        _pointerDown = false;
    }

    #endregion

    #region Theming

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (Application.Current is not null)
        {
            Application.Current.ActualThemeVariantChanged += OnAppThemeChanged;
        }

        if (_boundSound is not null)
        {
            _boundSound.PropertyChanged -= OnSoundPropertyChanged;
            _boundSound.PropertyChanged += OnSoundPropertyChanged;
        }

        UpdateThemeBrushes();
        // BuildImmutableText(_boundSound);
        RequestFrameIfNeeded();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (Application.Current is not null)
        {
            Application.Current.ActualThemeVariantChanged -= OnAppThemeChanged;

        }

        if (_boundSound is not null)
            _boundSound.PropertyChanged -= OnSoundPropertyChanged;
    }

    private void OnAppThemeChanged(object? sender, EventArgs e)
    {
        UpdateThemeBrushes();
        CategoryCache.Clear();

        _titleDirty = true;
        BuildImmutableText(_boundSound);
        InvalidateVisual();
    }

    private void UpdateThemeBrushes()
    {
        var theme = Application.Current?.ActualThemeVariant ?? ThemeVariant.Dark;
        _palette = PadPalette.Get(theme);
        _borderPen = new Pen(_palette.Border, _metrics.BorderThickness);
    }

    #endregion
}