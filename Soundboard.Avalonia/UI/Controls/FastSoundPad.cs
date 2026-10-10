using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using Microsoft.Extensions.DependencyInjection;
using Soundboard.Avalonia.Discovery;
using Soundboard.Avalonia.ViewModels;
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
    IBrush Surface,
    IBrush Border,
    IBrush TextPrimary,
    IBrush TextMuted,
    bool IsLightTheme)
{
    private static PadPalette? s_dark;
    private static PadPalette? s_light;

    public static PadPalette Get(ThemeVariant theme)
    {
        if (theme == ThemeVariant.Light && s_light is not null) return s_light.Value;
        if (theme == ThemeVariant.Dark && s_dark is not null) return s_dark.Value;

        var palette = Create(theme);
        if (theme == ThemeVariant.Light) s_light = palette;
        else s_dark = palette;

        return palette;
    }

    private static PadPalette Create(ThemeVariant theme)
    {
        var isLight = theme == ThemeVariant.Light;
        var app = Application.Current;

        var surface = app?.TryFindResource("PadSurfaceBrush", theme, out var s) is true && s is IBrush sb
            ? sb
            : (isLight ? Brushes.White : new SolidColorBrush(Color.Parse("#FF1A1D26")));

        var border = app?.TryFindResource("PadBorderBrush", theme, out var b) == true && b is IBrush bb
            ? bb
            : (isLight ? new SolidColorBrush(Color.Parse("#FFD8DCE6")) : new SolidColorBrush(Color.Parse("#FF2B3142")));

        var textPrimary = app?.TryFindResource("TextPrimaryBrush", theme, out var tp) == true && tp is IBrush tpb
            ? tpb
            : (isLight ? new SolidColorBrush(Color.Parse("#FF181B22")) : new SolidColorBrush(Color.Parse("#FFF1F3F7")));

        var textMuted = app?.TryFindResource("TextMutedBrush", theme, out var tm) == true && tm is IBrush tmb
            ? tmb
            : (isLight ? new SolidColorBrush(Color.Parse("#FF64748B")) : new SolidColorBrush(Color.Parse("#FF828A9C")));

        return new PadPalette(surface, border, textPrimary, textMuted, isLight);
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
    private bool _categoryDirty = true;
    private bool _titleDirty = true;

    // Runtime state
    private SoundModel? _boundSound;
    private bool _pointerDown;

    private PadMetrics _metrics = PadMetrics.Default;
    private PadPalette _palette;


    public FastSoundPad()
    {
        ClipToBounds = false;
        Cursor = new Cursor(StandardCursorType.Hand);

        InitializeRendering();
        InitializeAnimations();

        AddHandler(PointerPressedEvent, OnPressed, RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(PointerReleasedEvent, OnReleased, RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(PointerMovedEvent, OnMoved, RoutingStrategies.Tunnel, handledEventsToo: true);
    }

    #region Layout & Sizing Observation

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);

        // Only dirty text layouts if the width actually changed (horizontal resizing / column adjustments)
        if (e.WidthChanged)
        {
            _categoryDirty = true;
            _titleDirty = true;
        }
        else if (e.HeightChanged)
        {
            // Vertical resize only affects title vertical boundary
            _titleDirty = true;
        }
    }

    #endregion

    #region Data Context & Property Observation

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        _pointerDown = false;
        _categoryDirty = true;
        _titleDirty = true;

        if (_boundSound is not null)
            _boundSound.PropertyChanged -= OnSoundPropertyChanged;

        _boundSound = DataContext as SoundModel;

        if (_boundSound is not null)
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

        UpdateThemeBrushes();
        RequestFrameIfNeeded();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (Application.Current is not null)
        {
            Application.Current.ActualThemeVariantChanged -= OnAppThemeChanged;
        }
    }

    private void OnAppThemeChanged(object? sender, EventArgs e)
    {
        UpdateThemeBrushes();
        _categoryDirty = true;
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