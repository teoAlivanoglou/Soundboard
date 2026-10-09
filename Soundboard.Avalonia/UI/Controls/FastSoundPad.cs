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

public partial class FastSoundPad : Control
{
    // Geometry & Layout Configuration
    private CornerRadius _cornerRadius = new(16);
    private double _paddingLeft = 14;
    private double _paddingTop = 10;
    private double _paddingRight = 10;
    private double _paddingBottom = 10;
    private double _accentThickness = 6.0;
    private double _borderThickness = 2.0;

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

    // Theme brushes
    private IBrush _surfaceBrush = Brushes.White;
    private IBrush _borderBrush = Brushes.Gray;
    private IBrush _textPrimaryBrush = Brushes.Black;
    private IBrush _textMutedBrush = Brushes.Gray;
    private bool _isLightTheme;

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
        _categoryDirty = true;
        _titleDirty = true;
        RebuildImmutableText(e.NewSize, DataContext as SoundModel);
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

        RebuildImmutableText(Bounds.Size, _boundSound);
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
        RebuildImmutableText(Bounds.Size, _boundSound);
        InvalidateVisual();
    }

    private void UpdateThemeBrushes()
    {
        var theme = Application.Current?.ActualThemeVariant ?? ThemeVariant.Dark;
        _isLightTheme = theme == ThemeVariant.Light;

        // 1. Surface Brush (#1A1D26 Dark / #FFFFFF Light)
        if (Application.Current is not null &&
            Application.Current.TryFindResource("PadSurfaceBrush", theme, out var s) && s is IBrush sb)
            _surfaceBrush = sb;
        else
            _surfaceBrush = theme == ThemeVariant.Light ? Brushes.White : new SolidColorBrush(Color.Parse("#FF1A1D26"));

        // 2. Border Brush (#2B3142 Dark / #D8DCE6 Light)
        if (Application.Current is not null &&
            Application.Current.TryFindResource("PadBorderBrush", theme, out var b) && b is IBrush bb)
            _borderBrush = bb;
        else
            _borderBrush = theme == ThemeVariant.Light
                ? new SolidColorBrush(Color.Parse("#FFD8DCE6"))
                : new SolidColorBrush(Color.Parse("#FF2B3142"));

        _borderPen = new Pen(_borderBrush, _borderThickness);

        // 3. Text Primary Brush (#F1F3F7 Dark / #181B22 Light)
        if (Application.Current is not null &&
            Application.Current.TryFindResource("TextPrimaryBrush", theme, out var tp) && tp is IBrush tpb)
            _textPrimaryBrush = tpb;
        else
            _textPrimaryBrush = theme == ThemeVariant.Light
                ? new SolidColorBrush(Color.Parse("#FF181B22"))
                : new SolidColorBrush(Color.Parse("#FFF1F3F7"));

        // 4. Text Muted Brush (#828A9C Dark / #64748B Light)
        if (Application.Current is not null &&
            Application.Current.TryFindResource("TextMutedBrush", theme, out var tm) && tm is IBrush tmb)
            _textMutedBrush = tmb;
        else
            _textMutedBrush = theme == ThemeVariant.Light
                ? new SolidColorBrush(Color.Parse("#FF64748B"))
                : new SolidColorBrush(Color.Parse("#FF828A9C"));
    }

    #endregion
}