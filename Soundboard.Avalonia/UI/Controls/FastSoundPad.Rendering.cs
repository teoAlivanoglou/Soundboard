using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Media.TextFormatting;
using Soundboard.Avalonia.Discovery;
using System;

namespace Soundboard.Avalonia.UI.Controls;

public partial class FastSoundPad
{
    private Pen _borderPen = new(Brushes.Gray, 2);
    private LinearGradientBrush _animatedBorderBrush = null!;

    // Static immutable typefaces (never reallocated)
    private static readonly Typeface CategoryTypeface = new(FontFamily.Default, FontStyle.Normal, FontWeight.DemiBold);
    private static readonly Typeface NormalTypeface = new(FontFamily.Default, FontStyle.Normal, FontWeight.Normal);

    // Immutable text layouts (built strictly when DataContext changes, zero dirty flags or rebuilds during render/resize)
    private TextLayout? _durationLayout;
    private TextLayout? _indexLayout;

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
        var roundedRect = new RoundedRect(new Rect(size), _metrics.CornerRadius);
        var categoryBrush = sound.Category.BackgroundBrush as ISolidColorBrush;
        var categoryColor = categoryBrush?.Color ?? Colors.Transparent;

        // DrawShadows(context, roundedRect, categoryColor);
        DrawSurface(context, roundedRect, categoryColor);
        DrawText(context, size, sound, categoryBrush);
        DrawProgress(context, size, sound);
        // DrawProgressPlayhead(context, size, sound);
        DrawBorder(context, roundedRect, size, categoryColor);
    }

    #region Pipeline Step 1: Shadows

    private void DrawShadows(DrawingContext context, RoundedRect roundedRect, Color categoryColor)
    {
        if (!_enableShadows || _glowOpacity <= 0.0) return;

        var maxOpacity = _palette.IsLightTheme ? 0.7 : 0.55;
        var glowAlpha = (byte)Math.Clamp(categoryColor.A * maxOpacity * _glowOpacity, 0, 255);
        var glowColor = Color.FromArgb(glowAlpha, categoryColor.R, categoryColor.G, categoryColor.B);

        var glowShadow = new BoxShadow
        {
            Blur = _palette.IsLightTheme ? _glowBlur * (20.0 / 24.0) : _glowBlur,
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

        context.DrawRectangle(_palette.Surface, null, roundedRect, new BoxShadows(accentShadow));
    }

    #endregion

    #region Pipeline Step 3: Playback Progress

    private void DrawProgress(DrawingContext context, Size size, SoundModel sound)
    {
        if (!_enableProgress || !sound.IsPlaying) return;

        // Will use it for clipping
        var roundedRect = new RoundedRect(new Rect(size), _metrics.CornerRadius);

        var progressToSize = new Size(size.Width * sound.Progress, size.Height);
        var newRect = new Rect(progressToSize);


        var categoryColor = ((ImmutableSolidColorBrush)sound.BackgroundBrush).Color;
        var newBrush = new SolidColorBrush(new Color(124, categoryColor.R, categoryColor.G, categoryColor.B));
        using (context.PushClip(roundedRect))
        {
            context.DrawRectangle(newBrush, null, newRect);
        }
    }

    #endregion

    #region Pipeline Step 3b: Playback Progress Playhead

    // Static cached warm-white laser filament from Colors.axaml (#FFFBE8)
    private static readonly IBrush LaserCoreBrush = new SolidColorBrush(Color.Parse("#FFFBE8"));

    private void DrawProgressPlayhead(DrawingContext context, Size size, SoundModel sound)
    {
        if (!_enableProgress || !sound.IsPlaying) return;

        var categoryBrush = sound.Category.BackgroundBrush as ISolidColorBrush;
        var categoryColor = categoryBrush?.Color ?? Colors.White;
        var isLight = _palette.IsLightTheme;

        const double playheadWidth = 2.0;

        // Pixel-snap X to eliminate subpixel shimmering/jittering
        var playheadX = Math.Round(size.Width * sound.Progress);
        var playheadRect = new Rect(0, 0, playheadWidth, size.Height);

        var innerAlpha = (byte)((isLight ? 0.95 : 1.00) * 255);
        var outerAlpha = (byte)((isLight ? 0.90 : 0.85) * 255);

        var boxShadows = new BoxShadows(
            new BoxShadow
            {
                Color = Color.FromArgb(outerAlpha, categoryColor.R, categoryColor.G, categoryColor.B),
                Blur = 9,
                Spread = 0,
                IsInset = false
            },
            [
                new BoxShadow
                {
                    Color = Color.FromArgb(innerAlpha, categoryColor.R, categoryColor.G, categoryColor.B),
                    Blur = 4,
                    Spread = 0,
                    IsInset = false
                }
            ]);

        var roundedRect = new RoundedRect(new Rect(size), _metrics.CornerRadius);

        using (context.PushClip(roundedRect))
        using (context.PushTransform(Matrix.CreateTranslation(playheadX, 0)))
        {
            context.DrawRectangle(LaserCoreBrush, null, playheadRect, boxShadows);
        }
    }

    #endregion

    #region Pipeline Step 4: Text Elements

    private void DrawText(DrawingContext context, Size size, SoundModel sound, IBrush? categoryBrush)
    {
        if (!_enableText) return;

        var contentWidth = Math.Max(0, size.Width - _metrics.PaddingLeft - _metrics.PaddingRight);
        var footerY = size.Height - _metrics.PaddingBottom - 16;

        // 1. Category Name (Top-Left, Granular Dirty)
        if (_showCategory)
        {
            if (_categoryDirty)
            {
                _categoryLayout = new TextLayout(
                    sound.Category.Name,
                    CategoryTypeface,
                    fontSize: 14,
                    foreground: categoryBrush,
                    textAlignment: TextAlignment.Left,
                    maxWidth: contentWidth,
                    maxHeight: 20,
                    textTrimming: TextTrimming.CharacterEllipsis);

                _categoryPos = new Point(_metrics.PaddingLeft, _metrics.PaddingTop);
                _categoryDirty = false;
            }

            _categoryLayout?.Draw(context, _categoryPos);
        }

        // 2. Footer: Duration & Index (Immutable Text - No Dirty Flags)
        if (_showFooter)
        {
            DrawImmutableText(context, size);
        }

        // 3. Sound Title (Middle, Granular Dirty)
        if (_showTitle)
        {
            var titleTop = _showCategory ? _metrics.PaddingTop + 24 : _metrics.PaddingTop;
            var bottomLimit = _showFooter ? footerY - 4 : size.Height - _metrics.PaddingBottom;
            var titleHeight = Math.Max(10, bottomLimit - titleTop);

            if (_titleDirty || _titleLayout is null)
            {
                _titleLayout = new TextLayout(
                    sound.Name,
                    NormalTypeface,
                    fontSize: 14,
                    foreground: _palette.TextPrimary,
                    textAlignment: TextAlignment.Left,
                    textWrapping: TextWrapping.Wrap,
                    textTrimming: TextTrimming.CharacterEllipsis,
                    maxWidth: contentWidth,
                    maxHeight: titleHeight,
                    lineHeight: 18);

                _titleDirty = false;
            }

            var titleOffsetY = titleTop + Math.Max(0, (titleHeight - _titleLayout.Height) / 2);
            _titlePos = new Point(_metrics.PaddingLeft, titleOffsetY);

            _titleLayout.Draw(context, _titlePos);
        }
    }

    /// <summary>
    /// Draws static, immutable footer text (Duration & Index). Position calculated on the fly without reallocating layouts.
    /// </summary>
    private void DrawImmutableText(DrawingContext context, Size size)
    {
        if (_durationLayout is null && _indexLayout is null) return;

        var footerY = size.Height - _metrics.PaddingBottom - 16;
        var durationX = _metrics.PaddingLeft;

        _durationLayout?.Draw(context, new Point(durationX, footerY));

        if (_indexLayout is not null)
        {
            var indexX = Math.Max(durationX, size.Width - _metrics.PaddingRight - _indexLayout.Width);
            _indexLayout.Draw(context, new Point(indexX, footerY));
        }
    }

    private void BuildImmutableText(SoundModel? sound)
    {
        if (sound is null)
        {
            _durationLayout = null;
            _indexLayout = null;
            return;
        }

        var durationStr = sound.Duration.ToString(@"m\:ss\.ff");
        var indexStr = $"#{sound.Index:D2}";

        _durationLayout = new TextLayout(
            durationStr,
            NormalTypeface,
            fontSize: 14,
            foreground: _palette.TextMuted,
            textAlignment: TextAlignment.Left);

        _indexLayout = new TextLayout(
            indexStr,
            NormalTypeface,
            fontSize: 14,
            foreground: _palette.TextMuted,
            textAlignment: TextAlignment.Left);
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
                _borderPen.Brush = _palette.Border;
                context.DrawRectangle(null, _borderPen, roundedRect);
                break;

            case BorderEffect.AnimatedGradient:
            default:
                if (_borderFade <= 0.0)
                {
                    _borderPen.Brush = _palette.Border;
                }
                else
                {
                    var borderCol = (_palette.Border as ISolidColorBrush)?.Color ?? Colors.Gray;
                    var fade = _borderFade;
                    var a = (byte)(borderCol.A + (categoryColor.A - borderCol.A) * fade);
                    var r = (byte)(borderCol.R + (categoryColor.R - borderCol.R) * fade);
                    var g = (byte)(borderCol.G + (categoryColor.G - borderCol.G) * fade);
                    var b = (byte)(borderCol.B + (categoryColor.B - borderCol.B) * fade);

                    _animatedBorderBrush.GradientStops[0].Color = Color.FromArgb(a, r, g, b);
                    _animatedBorderBrush.GradientStops[1].Color = borderCol;

                    var borderThickness = _borderPen.Thickness;
                    var shadowEdgePx = _metrics.AccentThickness + borderThickness;

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


    static Color ColorWithAlpha(Color color, byte alpha)
    {
        return Color.FromArgb(alpha, color.R, color.G, color.B);
    }
    static Color ColorWithAlpha(Color color, double alpha)
    {
        return Color.FromArgb((byte)(alpha*255), color.R, color.G, color.B);
    }
}