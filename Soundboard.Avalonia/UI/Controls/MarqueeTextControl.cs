using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Threading;

namespace Soundboard.Avalonia.UI.Controls;

public class MarqueeTextControl : Control
{
    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<MarqueeTextControl, string?>(nameof(Text));

    public static readonly StyledProperty<Orientation> OrientationProperty =
        AvaloniaProperty.Register<MarqueeTextControl, Orientation>(nameof(Orientation), Orientation.Vertical);

    public static readonly StyledProperty<bool> ShowEllipsisProperty =
        AvaloniaProperty.Register<MarqueeTextControl, bool>(nameof(ShowEllipsis), false);

    public static readonly StyledProperty<double> LineHeightProperty =
        AvaloniaProperty.Register<MarqueeTextControl, double>(nameof(LineHeight), double.NaN);

    public static readonly StyledProperty<IBrush?> BackgroundProperty =
        AvaloniaProperty.Register<MarqueeTextControl, IBrush?>(nameof(Background), Brushes.Transparent);

    public static readonly StyledProperty<IBrush?> ForegroundProperty =
        AvaloniaProperty.Register<MarqueeTextControl, IBrush?>(nameof(Foreground), Brushes.White);

    public static readonly StyledProperty<FontFamily> FontFamilyProperty =
        AvaloniaProperty.Register<MarqueeTextControl, FontFamily>(nameof(FontFamily), FontFamily.Default);

    public static readonly StyledProperty<double> FontSizeProperty =
        AvaloniaProperty.Register<MarqueeTextControl, double>(nameof(FontSize), 14.0);

    public static readonly StyledProperty<FontWeight> FontWeightProperty =
        AvaloniaProperty.Register<MarqueeTextControl, FontWeight>(nameof(FontWeight), FontWeight.Normal);

    public static readonly StyledProperty<FontStyle> FontStyleProperty =
        AvaloniaProperty.Register<MarqueeTextControl, FontStyle>(nameof(FontStyle), FontStyle.Normal);

    public static readonly StyledProperty<TextAlignment> TextAlignmentProperty =
        AvaloniaProperty.Register<MarqueeTextControl, TextAlignment>(nameof(TextAlignment), TextAlignment.Left);

    public static readonly StyledProperty<TimeSpan> StartDelayProperty =
        AvaloniaProperty.Register<MarqueeTextControl, TimeSpan>(nameof(StartDelay), TimeSpan.FromMilliseconds(300));

    public static readonly StyledProperty<TimeSpan> EndDelayProperty =
        AvaloniaProperty.Register<MarqueeTextControl, TimeSpan>(nameof(EndDelay), TimeSpan.FromMilliseconds(500));

    public static readonly StyledProperty<double> ScrollSpeedProperty =
        AvaloniaProperty.Register<MarqueeTextControl, double>(nameof(ScrollSpeed), 35.0);

    public static readonly StyledProperty<bool> ResetOnExitProperty =
        AvaloniaProperty.Register<MarqueeTextControl, bool>(nameof(ResetOnExit), true);

    public static readonly StyledProperty<bool?> IsActiveProperty =
        AvaloniaProperty.Register<MarqueeTextControl, bool?>(nameof(IsActive), null);

    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public Orientation Orientation
    {
        get => GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    public bool ShowEllipsis
    {
        get => GetValue(ShowEllipsisProperty);
        set => SetValue(ShowEllipsisProperty, value);
    }

    public double LineHeight
    {
        get => GetValue(LineHeightProperty);
        set => SetValue(LineHeightProperty, value);
    }

    public IBrush? Background
    {
        get => GetValue(BackgroundProperty);
        set => SetValue(BackgroundProperty, value);
    }

    public IBrush? Foreground
    {
        get => GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
    }

    public FontFamily FontFamily
    {
        get => GetValue(FontFamilyProperty);
        set => SetValue(FontFamilyProperty, value);
    }

    public double FontSize
    {
        get => GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    public FontWeight FontWeight
    {
        get => GetValue(FontWeightProperty);
        set => SetValue(FontWeightProperty, value);
    }

    public FontStyle FontStyle
    {
        get => GetValue(FontStyleProperty);
        set => SetValue(FontStyleProperty, value);
    }

    public TextAlignment TextAlignment
    {
        get => GetValue(TextAlignmentProperty);
        set => SetValue(TextAlignmentProperty, value);
    }

    public TimeSpan StartDelay
    {
        get => GetValue(StartDelayProperty);
        set => SetValue(StartDelayProperty, value);
    }

    public TimeSpan EndDelay
    {
        get => GetValue(EndDelayProperty);
        set => SetValue(EndDelayProperty, value);
    }

    public double ScrollSpeed
    {
        get => GetValue(ScrollSpeedProperty);
        set => SetValue(ScrollSpeedProperty, value);
    }

    public bool ResetOnExit
    {
        get => GetValue(ResetOnExitProperty);
        set => SetValue(ResetOnExitProperty, value);
    }

    public bool? IsActive
    {
        get => GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }

    static MarqueeTextControl()
    {
        AffectsMeasure<MarqueeTextControl>(
            TextProperty,
            OrientationProperty,
            FontSizeProperty,
            FontFamilyProperty,
            FontWeightProperty,
            FontStyleProperty,
            LineHeightProperty);

        AffectsRender<MarqueeTextControl>(
            BackgroundProperty,
            ForegroundProperty,
            TextAlignmentProperty,
            ShowEllipsisProperty);
    }

    private enum ScrollPhase
    {
        Idle,
        StartDelay,
        Scrolling,
        EndDelay,
        Rewinding
    }

    private TextLayout? _textLayout;
    private double _lastLayoutWidth = -1;
    private double _overflow;
    private double _effectiveLineHeight = 18.0;

    private bool _isHovered;
    private double _scrollOffset;
    private ScrollPhase _phase = ScrollPhase.Idle;
    private DateTime _phaseStartTime;
    private DateTime _lastFrameTime;
    private DispatcherTimer? _animationTimer;

    public MarqueeTextControl()
    {
        ClipToBounds = true;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == TextProperty ||
            change.Property == FontSizeProperty ||
            change.Property == FontFamilyProperty ||
            change.Property == FontWeightProperty ||
            change.Property == FontStyleProperty ||
            change.Property == LineHeightProperty ||
            change.Property == OrientationProperty ||
            change.Property == ForegroundProperty ||
            change.Property == TextAlignmentProperty)
        {
            _textLayout = null;
            _scrollOffset = 0;
            StopAnimation();
        }
        else if (change.Property == IsActiveProperty)
        {
            var active = change.GetNewValue<bool?>();
            if (active.HasValue)
            {
                SetHoverState(active.Value);
            }
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        EnsureTextLayout(availableSize.Width);

        if (_textLayout == null)
            return new Size(0, 0);

        if (Orientation == Orientation.Vertical)
        {
            var desiredWidth = Math.Min(availableSize.Width, _textLayout.Width);

            // Snap vertical height to integer lines if available height is finite and LineHeight is known
            if (!double.IsInfinity(availableSize.Height) && availableSize.Height > 0)
            {
                var lh = _effectiveLineHeight > 0 ? _effectiveLineHeight : 18.0;
                var lines = Math.Max(1, (int)(availableSize.Height / lh));
                var snappedHeight = lines * lh;
                var desiredHeight = Math.Min(snappedHeight, _textLayout.Height);
                return new Size(desiredWidth, desiredHeight);
            }

            return new Size(desiredWidth, _textLayout.Height);
        }
        else
        {
            // Horizontal orientation: single-line height, flexible width
            var desiredHeight = _effectiveLineHeight > 0 ? _effectiveLineHeight : _textLayout.Height;
            if (!double.IsInfinity(availableSize.Height) && availableSize.Height > 0)
            {
                desiredHeight = Math.Min(availableSize.Height, desiredHeight);
            }

            var desiredWidth = double.IsInfinity(availableSize.Width)
                ? _textLayout.Width
                : Math.Min(availableSize.Width, _textLayout.Width);

            return new Size(desiredWidth, desiredHeight);
        }
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Orientation == Orientation.Vertical && Math.Abs(_lastLayoutWidth - finalSize.Width) >= 1.0)
        {
            _textLayout = null;
        }

        EnsureTextLayout(finalSize.Width);
        UpdateOverflow(finalSize);

        return finalSize;
    }

    private void EnsureTextLayout(double constraintWidth)
    {
        var text = Text ?? string.Empty;
        var isVertical = Orientation == Orientation.Vertical;
        var width = !isVertical || double.IsInfinity(constraintWidth) || double.IsNaN(constraintWidth) ||
                    constraintWidth <= 0
            ? 10000.0
            : constraintWidth;

        if (_textLayout != null)
        {
            if (!isVertical || Math.Abs(_lastLayoutWidth - width) < 1.0)
                return;
        }

        _lastLayoutWidth = width;

        var typeface = new Typeface(FontFamily, FontStyle, FontWeight);
        var fontSize = Math.Max(1.0, FontSize);
        var lh = double.IsNaN(LineHeight) || LineHeight <= 0 ? fontSize * 1.25 : LineHeight;
        _effectiveLineHeight = lh;

        if (Orientation == Orientation.Vertical)
        {
            _textLayout = new TextLayout(
                text,
                typeface,
                fontSize,
                Foreground,
                TextAlignment,
                TextWrapping.Wrap,
                textTrimming: null,
                textDecorations: null,
                maxWidth: width,
                maxHeight: double.PositiveInfinity,
                maxLines: 0,
                lineHeight: lh);
        }
        else
        {
            _textLayout = new TextLayout(
                text,
                typeface,
                fontSize,
                Foreground,
                TextAlignment,
                TextWrapping.NoWrap,
                textTrimming: null,
                textDecorations: null,
                maxWidth: double.PositiveInfinity,
                maxHeight: double.PositiveInfinity,
                maxLines: 1,
                lineHeight: lh);
        }
    }

    private void UpdateOverflow(Size bounds)
    {
        if (_textLayout == null)
        {
            _overflow = 0;
            return;
        }

        if (Orientation == Orientation.Vertical)
        {
            _overflow = Math.Max(0.0, _textLayout.Height - bounds.Height);
        }
        else
        {
            _overflow = Math.Max(0.0, _textLayout.Width - bounds.Width);
        }

        if (_overflow <= 1.0)
        {
            _overflow = 0;
            _scrollOffset = 0;
            StopAnimation();
        }
    }

    protected override void OnPointerEntered(PointerEventArgs e)
    {
        base.OnPointerEntered(e);
        if (!IsActive.HasValue)
        {
            SetHoverState(true);
        }
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        if (!IsActive.HasValue)
        {
            SetHoverState(false);
        }
    }

    private void SetHoverState(bool hovered)
    {
        if (_isHovered == hovered) return;
        _isHovered = hovered;

        if (_isHovered)
        {
            if (_overflow > 1.0)
            {
                StartAnimation();
            }
        }
        else
        {
            if (ResetOnExit)
            {
                _scrollOffset = 0;
                StopAnimation();
                InvalidateVisual();
            }
            else if (_phase != ScrollPhase.Idle)
            {
                _phase = ScrollPhase.Rewinding;
            }
        }
    }

    private void StartAnimation()
    {
        _phase = ScrollPhase.StartDelay;
        _phaseStartTime = DateTime.UtcNow;
        _lastFrameTime = DateTime.UtcNow;

        _animationTimer ??= new DispatcherTimer(
            TimeSpan.FromMilliseconds(16),
            DispatcherPriority.Render,
            OnAnimationTick);

        if (!_animationTimer.IsEnabled)
        {
            _animationTimer.Start();
        }
    }

    private void StopAnimation()
    {
        _phase = ScrollPhase.Idle;
        _animationTimer?.Stop();
    }

    private void OnAnimationTick(object? sender, EventArgs e)
    {
        if (_overflow <= 1.0)
        {
            StopAnimation();
            return;
        }

        var now = DateTime.UtcNow;
        var dt = (now - _lastFrameTime).TotalSeconds;
        _lastFrameTime = now;

        if (dt > 0.1) dt = 0.1;

        switch (_phase)
        {
            case ScrollPhase.StartDelay:
                if (now - _phaseStartTime >= StartDelay)
                {
                    _phase = ScrollPhase.Scrolling;
                }

                break;

            case ScrollPhase.Scrolling:
                var speed = Math.Max(1.0, ScrollSpeed);
                _scrollOffset += speed * dt;

                if (_scrollOffset >= _overflow)
                {
                    _scrollOffset = _overflow;
                    _phase = ScrollPhase.EndDelay;
                    _phaseStartTime = now;
                }

                InvalidateVisual();
                break;

            case ScrollPhase.EndDelay:
                if (now - _phaseStartTime >= EndDelay)
                {
                    _phase = ScrollPhase.Rewinding;
                }

                break;

            case ScrollPhase.Rewinding:
                var rewindSpeed = Math.Max(1.0, ScrollSpeed * 4.0);
                _scrollOffset -= rewindSpeed * dt;

                if (_scrollOffset <= 0)
                {
                    _scrollOffset = 0;
                    if (_isHovered)
                    {
                        _phase = ScrollPhase.StartDelay;
                        _phaseStartTime = now;
                    }
                    else
                    {
                        StopAnimation();
                    }
                }

                InvalidateVisual();
                break;
        }
    }

    public override void Render(DrawingContext context)
    {
        if (Bounds.Width <= 0 || Bounds.Height <= 0)
            return;

        if (Background != null)
        {
            context.DrawRectangle(Background, null, new Rect(Bounds.Size));
        }

        if (_textLayout == null)
        {
            EnsureTextLayout(Bounds.Width);
            UpdateOverflow(Bounds.Size);
        }

        if (_textLayout == null)
            return;

        using (context.PushClip(new Rect(Bounds.Size)))
        {
            if (Orientation == Orientation.Vertical)
            {
                var drawPoint = new Point(0, -_scrollOffset);
                _textLayout.Draw(context, drawPoint);
            }
            else
            {
                var yCenter = Math.Max(0.0, (Bounds.Height - _effectiveLineHeight) / 2.0);
                var drawPoint = new Point(-_scrollOffset, yCenter);
                _textLayout.Draw(context, drawPoint);
            }
        }
    }
}