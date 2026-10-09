using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Soundboard.Avalonia.Discovery;

namespace Soundboard.Avalonia.UI.Controls;

public partial class FastSoundPad
{
    private Pen _borderPen = new(Brushes.Gray, 2);
    private LinearGradientBrush _animatedBorderBrush = null!;

    // Static immutable typefaces (never reallocated)
    private readonly Typeface _categoryTypeface = new(FontFamily.Default, FontStyle.Normal, FontWeight.DemiBold);
    private readonly Typeface _normalTypeface = new(FontFamily.Default, FontStyle.Normal, FontWeight.Normal);

    // Immutable text layouts (built strictly when DataContext/size changes, zero dirty flags during render)
    private TextLayout? _durationLayout;
    private TextLayout? _indexLayout;
    private Point _footerPos;

    // Granularly dirty text layouts
    private TextLayout? _categoryLayout;
    private TextLayout? _titleLayout;
    private Point _categoryPos;
    private Point _titlePos;

    private void InitializeRendering()
    {
        _animatedBorderBrush = new LinearGradientBrush
        {
            GradientStops =
            [
                new GradientStop(Colors.Transparent, 0),
                new GradientStop(Colors.Transparent, 1)
            ]
        };
    }

    public override void Render(DrawingContext context)
    {
        if (Bounds.Width <= 0 || Bounds.Height <= 0 || DataContext is not SoundModel sound || !IsVisible) 
            return;

        var size = Bounds.Size;
        var roundedRect = new RoundedRect(new Rect(size), _cornerRadius);
        var categoryBrush = sound.Category.BackgroundBrush as ISolidColorBrush;
        var categoryColor = categoryBrush?.Color ?? Colors.Transparent;

        DrawShadows(context, roundedRect, categoryColor);
        DrawSurface(context, roundedRect, categoryColor);
        DrawProgress(context, size, sound);
        DrawText(context, size, sound, categoryBrush);
        DrawBorder(context, roundedRect, size, categoryColor);
    }

    #region Pipeline Step 1: Shadows

    private void DrawShadows(DrawingContext context, RoundedRect roundedRect, Color categoryColor)
    {
        if (!_enableShadows || _glowOpacity <= 0.0) return;

        var maxOpacity = _isLightTheme ? 0.7 : 0.55;
        var glowAlpha = (byte)Math.Clamp(categoryColor.A * maxOpacity * _glowOpacity, 0, 255);
        var glowColor = Color.FromArgb(glowAlpha, categoryColor.R, categoryColor.G, categoryColor.B);

        var glowShadow = new BoxShadow
        {
            Blur = _isLightTheme ? _glowBlur * (20.0 / 24.0) : _glowBlur,
            Spread = 0.1,
            Color = glowColor
        };

        context.DrawRectangle(null, null, roundedRect, new BoxShadows(glowShadow));
    }

    #endregion

    #region Pipeline Step 2: Surface & Accent

    private void DrawSurface(DrawingContext context, RoundedRect roundedRect, Color categoryColor)
    {
        var accentShadow = new BoxShadow
        {
            IsInset = true,
            OffsetX = _accentOffset,
            Color = categoryColor
        };

        context.DrawRectangle(_surfaceBrush, null, roundedRect, new BoxShadows(accentShadow));
    }

    #endregion

    #region Pipeline Step 3: Playback Progress

    private void DrawProgress(DrawingContext context, Size size, SoundModel sound)
    {
        if (!_enableProgress || !sound.IsPlaying) return;

        // Ready for progress bar and laser playhead rendering (Pass 2)
    }

    #endregion

    #region Pipeline Step 4: Text Elements

    private void DrawText(DrawingContext context, Size size, SoundModel sound, IBrush? categoryBrush)
    {
        if (!_enableText) return;

        var contentWidth = Math.Max(0, size.Width - _paddingLeft - _paddingRight);
        var footerY = size.Height - _paddingBottom - 16;

        // 1. Category Name (Top-Left, Granular Dirty)
        if (_showCategory)
        {
            if (_categoryDirty)
            {
                _categoryLayout = new TextLayout(
                    sound.Category.Name,
                    _categoryTypeface,
                    fontSize: 14,
                    foreground: categoryBrush,
                    textAlignment: TextAlignment.Left,
                    maxWidth: contentWidth,
                    maxHeight: 20,
                    textTrimming: TextTrimming.CharacterEllipsis);

                _categoryPos = new Point(_paddingLeft, _paddingTop);
                _categoryDirty = false;
            }

            _categoryLayout?.Draw(context, _categoryPos);
        }

        // 2. Footer: Duration & Index (Immutable Text - No Dirty Flags)
        if (_showFooter)
        {
            DrawImmutableText(context);
        }

        // 3. Sound Title (Middle, Granular Dirty)
        if (_showTitle)
        {
            if (_titleDirty)
            {
                var titleTop = _showCategory ? _paddingTop + 24 : _paddingTop;
                var bottomLimit = _showFooter ? footerY - 4 : size.Height - _paddingBottom;
                var titleHeight = Math.Max(10, bottomLimit - titleTop);

                _titleLayout = new TextLayout(
                    sound.Name,
                    _normalTypeface,
                    fontSize: 14,
                    foreground: _textPrimaryBrush,
                    textAlignment: TextAlignment.Left,
                    textWrapping: TextWrapping.Wrap,
                    textTrimming: TextTrimming.CharacterEllipsis,
                    maxWidth: contentWidth,
                    maxHeight: titleHeight,
                    lineHeight: 18);

                var titleOffsetY = titleTop + Math.Max(0, (titleHeight - _titleLayout.Height) / 2);
                _titlePos = new Point(_paddingLeft, titleOffsetY);
                _titleDirty = false;
            }

            _titleLayout?.Draw(context, _titlePos);
        }
    }

    /// <summary>
    /// Draws static, immutable footer text (Duration & Index). Never checks dirty flags during render loop.
    /// </summary>
    private void DrawImmutableText(DrawingContext context)
    {
        _durationLayout?.Draw(context, _footerPos);
        _indexLayout?.Draw(context, _footerPos);
    }

    private void RebuildImmutableText(Size size, SoundModel? sound)
    {
        if (sound is null || size.Width <= 0 || size.Height <= 0)
        {
            _durationLayout = null;
            _indexLayout = null;
            return;
        }

        var contentWidth = Math.Max(0, size.Width - _paddingLeft - _paddingRight);
        var footerY = size.Height - _paddingBottom - 16;
        _footerPos = new Point(_paddingLeft, footerY);

        var durationStr = sound.Duration.ToString(@"m\:ss\.ff");
        var indexStr = $"#{sound.Index:D2}";

        _durationLayout = new TextLayout(
            durationStr,
            _normalTypeface,
            fontSize: 14,
            foreground: _textMutedBrush,
            textAlignment: TextAlignment.Left);

        _indexLayout = new TextLayout(
            indexStr,
            _normalTypeface,
            fontSize: 14,
            foreground: _textMutedBrush,
            textAlignment: TextAlignment.Right,
            maxWidth: contentWidth);
    }

    #endregion

    #region Pipeline Step 5: Border

    private void DrawBorder(DrawingContext context, RoundedRect roundedRect, Size size, Color categoryColor)
    {
        switch (_borderEffect)
        {
            case BorderEffect.None:
                return;

            case BorderEffect.Solid:
                _borderPen.Brush = _borderBrush;
                context.DrawRectangle(null, _borderPen, roundedRect);
                break;

            case BorderEffect.AnimatedGradient:
            default:
                if (_borderFade <= 0.0)
                {
                    _borderPen.Brush = _borderBrush;
                }
                else
                {
                    var borderCol = (_borderBrush as ISolidColorBrush)?.Color ?? Colors.Gray;
                    var fade = _borderFade;
                    var a = (byte)(borderCol.A + (categoryColor.A - borderCol.A) * fade);
                    var r = (byte)(borderCol.R + (categoryColor.R - borderCol.R) * fade);
                    var g = (byte)(borderCol.G + (categoryColor.G - borderCol.G) * fade);
                    var b = (byte)(borderCol.B + (categoryColor.B - borderCol.B) * fade);

                    _animatedBorderBrush.GradientStops[0].Color = Color.FromArgb(a, r, g, b);
                    _animatedBorderBrush.GradientStops[1].Color = borderCol;

                    var borderThickness = _borderPen.Thickness;
                    var shadowEdgePx = _accentThickness + borderThickness;

                    const double initGradientWidthPx = 11.0;

                    var initStartX = shadowEdgePx / size.Width;
                    var initEndX = (shadowEdgePx + initGradientWidthPx) / size.Width;

                    var sweep = _borderSweep;
                    var startX = initStartX + (0.0 - initStartX) * sweep;
                    var startY = 0.0 + (0.8 - 0.0) * sweep;

                    var endX = initEndX + (0.7 - initEndX) * sweep;
                    var endY = 0.0 + (0.2 - 0.0) * sweep;

                    _animatedBorderBrush.StartPoint = new RelativePoint(startX, startY, RelativeUnit.Relative);
                    _animatedBorderBrush.EndPoint = new RelativePoint(endX, endY, RelativeUnit.Relative);
                    _borderPen.Brush = _animatedBorderBrush;
                }

                context.DrawRectangle(null, _borderPen, roundedRect);
                break;
        }
    }

    #endregion
}
