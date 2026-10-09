using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Remote.Protocol.Input;
using Avalonia.Styling;
using Microsoft.Extensions.DependencyInjection;
using Soundboard.Avalonia.Discovery;
using Soundboard.Avalonia.ViewModels;
using System;
using System.ComponentModel;
using Avalonia.Media.Immutable;
using Avalonia.Threading;
using MouseButton = Avalonia.Input.MouseButton;

namespace Soundboard.Avalonia.UI.Controls;

public class FastSoundPad : Control
{
    private static readonly CornerRadius PadCornerRadius = new(16);
    private const double PaddingLeft = 14;
    private const double PaddingTop = 10;
    private const double PaddingRight = 10;
    private const double PaddingBottom = 10;
    private const double AccentThickness = 6.0;

    private SoundModel? _boundSound;
    private bool _pointerDown;

    private IBrush _surfaceBrush = Brushes.White;
    private IBrush _borderBrush = Brushes.Gray;
    private IBrush _textPrimaryBrush = Brushes.Black;
    private IBrush _textMutedBrush = Brushes.Gray;
    private Pen _borderPen = new(Brushes.Gray, 2);
    private bool _isLightTheme;

    private double _glowProgress = 0.0; // 0.0 = off, 1.0 = fully glowing                                                  
    private double _accentProgress = 0.0; // 0.0 = off, 1.0 = fully grown                                                  
    private TimeSpan _lastFrameTime = TimeSpan.Zero;
    private bool _isAnimating = false;

    // Pre-allocated buffer for shadow layers (1 primary + 9 extra = 10 layers, zero GC allocations per frame)
    private readonly BoxShadow[] _extraShadows = new BoxShadow[9];

    // Glow fade durations in seconds                                                                                           
    private const double GlowFadeInDuration = 0.15; // 150ms                                                                  
    private const double GlowFadeOutDuration = 0.20; // 200ms       

    // Accent grow durations in seconds                                                                                           
    private const double AccentGrowDuration =  0.25; // 250ms                                                                  
    private const double AccentShrinkDuration =  0.25; // 250ms       

    public FastSoundPad()
    {
        ClipToBounds = false;
        Cursor = new Cursor(StandardCursorType.Hand);

        AddHandler(PointerPressedEvent, OnPressed, RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(PointerReleasedEvent, OnReleased, RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(PointerMovedEvent, OnMoved, RoutingStrategies.Tunnel, handledEventsToo: true);
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        _pointerDown = false;

        if (_boundSound is not null)
            _boundSound.PropertyChanged -= OnSoundPropertyChanged;

        _boundSound = DataContext as SoundModel;

        if (_boundSound is not null)
            _boundSound.PropertyChanged += OnSoundPropertyChanged;

        InvalidateVisual();
    }

    private void OnSoundPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Re-render when playback state or text/category changes
        if (e.PropertyName == nameof(SoundModel.IsPlaying))
        {
            StartGlowAnimation();
        }
        InvalidateVisual();
    }
    private void StartGlowAnimation()
    {
        if (_isAnimating) return;
        _isAnimating = true;
        _lastFrameTime = TimeSpan.Zero;
        TopLevel.GetTopLevel(this)?.RequestAnimationFrame(OnAnimationFrame);
    }
    private void OnAnimationFrame(TimeSpan currentTime)
    {
        if (!_isAnimating) return;

        if (_lastFrameTime == TimeSpan.Zero)
        {
            _lastFrameTime = currentTime;
            TopLevel.GetTopLevel(this)?.RequestAnimationFrame(OnAnimationFrame);
            return;
        }

        var dt = (currentTime - _lastFrameTime).TotalSeconds;
        _lastFrameTime = currentTime;

        var isPlaying = _boundSound?.IsPlaying == true;

        // 1. Advance glow                                                                                                 
        var glowSpeed = isPlaying ? 1.0 / GlowFadeInDuration : -1.0 / GlowFadeOutDuration;
        _glowProgress = Math.Clamp(_glowProgress + glowSpeed * dt, 0.0, 1.0);

        // 2. Advance accent (half speed)                                                                                  
        var accentSpeed = isPlaying ? 1.0 / AccentGrowDuration : -1.0 / AccentShrinkDuration;
        _accentProgress = Math.Clamp(_accentProgress + accentSpeed * dt, 0.0, 1.0);


        // Redraw
        InvalidateVisual();

        // Check if target reached
        var glowDone = isPlaying ? _glowProgress >= 1.0 : _glowProgress <= 0.0;
        var accentDone = isPlaying ? _accentProgress >= 1.0 : _accentProgress <= 0.0;
        if (!glowDone || !accentDone)
        {
            // Still transitioning
            TopLevel.GetTopLevel(this)?.RequestAnimationFrame(OnAnimationFrame);
        }
        else
        {
            // Animation Complete
            _isAnimating = false;
        }
    }

    #region Pointer & Click Handlind

    private void OnPressed(object? s, PointerPressedEventArgs e) => _pointerDown = true;

    private void OnReleased(object? s, PointerReleasedEventArgs e)
    {
        var wasDown = _pointerDown;
        _pointerDown = false;

        if (DataContext is not SoundModel sound) return;
        if (!wasDown || !new Rect(Bounds.Size).Contains(e.GetPosition(this))) return;

        var vm = // (VisualRoot as Control)?.DataContext as SoundboardViewModel ??
            App.Host?.Services.GetService<SoundboardViewModel>();

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

        _borderPen = new Pen(_borderBrush, 2);

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

    private readonly LinearGradientBrush _animatedBorderBrush = new()
    {
        GradientStops =
        [
            new GradientStop(Colors.Transparent, 0),
            new GradientStop(Colors.Transparent, 1)
        ]
    };

    public override void Render(DrawingContext context)
    {
        if (Bounds.Width <= 0 || Bounds.Height <= 0 || DataContext is not SoundModel sound || !IsVisible) return;

        var size = Bounds.Size;
        var fullRect = new Rect(size);

        var categoryBrush = (sound.Category.BackgroundBrush as ISolidColorBrush);
        var categoryColor = categoryBrush?.Color ?? Colors.Transparent;

        var roundedRect = new RoundedRect(fullRect, PadCornerRadius);

        //// Border animation
        var ease = 1 - Math.Sqrt(1 - Math.Pow(_glowProgress, 2));

        _animatedBorderBrush.GradientStops[0].Color = categoryColor;
        _animatedBorderBrush.GradientStops[1].Color = ((SolidColorBrush)_borderBrush).Color;

        var borderThickness = _borderPen.Thickness;
        var shadowEdgePx = AccentThickness + borderThickness;
        
        const double initGradientWidthPx = 11.0;

        var initStartX = shadowEdgePx / size.Width;
        var initEndX = (shadowEdgePx + initGradientWidthPx) / size.Width;

        var startX = initStartX + (0.0 - initStartX) * ease;
        var startY = 0.0 + (0.8 - 0.0) * ease;

        var endX = initEndX + (0.7 - initEndX) * ease;
        var endY = 0.0 + (0.2 - 0.0) * ease;

        _animatedBorderBrush.StartPoint = new RelativePoint(startX, startY, RelativeUnit.Relative);
        _animatedBorderBrush.EndPoint = new RelativePoint(endX, endY, RelativeUnit.Relative);
        _borderPen.Brush = _animatedBorderBrush;

        // Accent
        var accentShadow = new BoxShadow
        {
            IsInset = true,
            OffsetX = AccentThickness * (1.0 - _accentProgress),
            Color = categoryColor
        };

        // Glow
        var maxBlur = _isLightTheme ? 20.0 : 24.0;
        var maxOpacity = _isLightTheme ? 0.7 : 0.55;
        var glowAlpha = (byte)Math.Clamp(categoryColor.A * maxOpacity * _glowProgress, 0, 255);
        var glowColor = Color.FromArgb(glowAlpha, categoryColor.R, categoryColor.G, categoryColor.B);
        
        _extraShadows[0] = new BoxShadow
        {
            Blur = maxBlur * _glowProgress,
            Spread = 0.1,
            Color = glowColor
        };

        var boxShadows = new BoxShadows(accentShadow, _extraShadows);
        context.DrawRectangle(_surfaceBrush, _borderPen, roundedRect, boxShadows);

        // Content area dimensions
        var contentWidth = Math.Max(0, size.Width - PaddingLeft - PaddingRight);
        var contentHeight = Math.Max(0, size.Height - PaddingTop - PaddingBottom);

        // Category Name - not (yet) using our marquee textbox
        var categoryTypeface = new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.DemiBold);
        var categoryLayout = new TextLayout(
            sound.Category.Name,
            categoryTypeface,
            fontSize: 14,
            foreground: categoryBrush,
            textAlignment: TextAlignment.Left,
            maxWidth: contentWidth,
            maxHeight: 20,
            textTrimming: TextTrimming.CharacterEllipsis);

        categoryLayout.Draw(context, new Point(PaddingLeft, PaddingTop));

        // Draw Footer: Duration (Bottom-Left) & #Index (Bottom-Right)
        var footerTypeface = new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Normal);
        var durationStr = sound.Duration.ToString(@"m\:ss\.ff");
        var indexStr = $"#{sound.Index:D2}";

        var durationLayout = new TextLayout(
            durationStr,
            footerTypeface,
            fontSize: 14,
            foreground: _textMutedBrush,
            textAlignment: TextAlignment.Left);

        var indexLayout = new TextLayout(
            indexStr,
            footerTypeface,
            fontSize: 14,
            foreground: _textMutedBrush,
            textAlignment: TextAlignment.Right,
            maxWidth: contentWidth);

        var footerY = size.Height - PaddingBottom - 16;
        durationLayout.Draw(context, new Point(PaddingLeft, footerY));
        indexLayout.Draw(context, new Point(PaddingLeft, footerY));

        // Draw Sound Title (Middle, Centered Vertically)
        var titleTypeface = new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Normal);
        var titleTop = PaddingTop + 24;
        var titleHeight = Math.Max(10, footerY - titleTop - 4);

        var titleLayout = new TextLayout(
            sound.Name,
            titleTypeface,
            fontSize: 14,
            foreground: _textPrimaryBrush,
            textAlignment: TextAlignment.Left,
            textWrapping: TextWrapping.Wrap,
            textTrimming: TextTrimming.CharacterEllipsis,
            maxWidth: contentWidth,
            maxHeight: titleHeight,
            lineHeight: 18);

        // Vertically center title in middle area
        var titleOffsetY = titleTop + Math.Max(0, (titleHeight - titleLayout.Height) / 2);
        titleLayout.Draw(context, new Point(PaddingLeft, titleOffsetY));
    }
}