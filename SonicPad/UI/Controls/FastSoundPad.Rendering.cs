using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Media.TextFormatting;
using Microsoft.Win32.SafeHandles;
using SkiaSharp;
using SonicPad.Discovery;
using SonicPad.Utils;
using System;
using System.Collections.Generic;

namespace SonicPad.UI.Controls;

public readonly record struct CategoryCacheKey(string Name, Color Color);
public sealed class CategoryCacheEntry
{
    public TextLayout NaturalLayout { get; init; } = null!;
    public double NaturalWidth { get; init; }

    // Stores ONLY the single most-recent elided layout for the current grid width
    public TextLayout? ElidedLayout { get; set; }
    public int ElidedWidth { get; set; }
}
public partial class FastSoundPad
{
    private Pen _borderPen = new(Brushes.Gray, 2);
    private LinearGradientBrush _animatedBorderBrush = null!;

    // Static immutable typefaces (updated globally when font family or weight offset changes)
    private static FontFamily _globalFontFamily = FontFamily.Default;
    private static int _fontWeightDelta = 0;

    private static Typeface CategoryTypeface = new(FontFamily.Default, FontStyle.Normal, FontWeight.DemiBold);
    private static Typeface NormalTypeface = new(FontFamily.Default, FontStyle.Normal, FontWeight.Normal);

    public static event Action? GlobalFontFamilyChanged;

    public static int FontWeightDelta
    {
        get => _fontWeightDelta;
        set
        {
            if (_fontWeightDelta != value)
            {
                _fontWeightDelta = value;
                UpdateGlobalTypefaces();
                CategoryCache.Clear();
                GlobalFontFamilyChanged?.Invoke();
            }
        }
    }

    private static double _fontSizeDelta = 0.0;
    public static double FontSizeDelta
    {
        get => _fontSizeDelta;
        set
        {
            if (Math.Abs(_fontSizeDelta - value) > 0.01)
            {
                _fontSizeDelta = value;
                CategoryCache.Clear();
                GlobalFontFamilyChanged?.Invoke();
            }
        }
    }

    private static bool _showHashPrefix = true;
    public static bool ShowHashPrefix
    {
        get => _showHashPrefix;
        set
        {
            if (_showHashPrefix != value)
            {
                _showHashPrefix = value;
                GlobalFontFamilyChanged?.Invoke();
            }
        }
    }

    public static void SetGlobalFontFamily(FontFamily fontFamily)
    {
        _globalFontFamily = fontFamily;
        UpdateGlobalTypefaces();
        CategoryCache.Clear();
        GlobalFontFamilyChanged?.Invoke();
    }

    private static void UpdateGlobalTypefaces()
    {
        var normalWeight = (FontWeight)Math.Clamp(400 + _fontWeightDelta, 100, 900);
        var categoryWeight = (FontWeight)Math.Clamp(600 + _fontWeightDelta, 100, 900);

        NormalTypeface = new Typeface(_globalFontFamily, FontStyle.Normal, normalWeight);
        CategoryTypeface = new Typeface(_globalFontFamily, FontStyle.Normal, categoryWeight);
    }

    public static void RefreshAllPads()
    {
        CategoryCache.Clear();
        GlobalFontFamilyChanged?.Invoke();
    }

    // Immutable text layouts (built strictly when DataContext changes, zero dirty flags or rebuilds during render/resize)
    private TextLayout? _durationLayout;
    private TextLayout? _indexLayout;

    // Cache holding both natural layouts and elided layouts
    private static readonly Dictionary<CategoryCacheKey, CategoryCacheEntry> CategoryCache = new();




    // Granularly dirty text layouts
    private TextLayout? _titleLayout;
    private TextLayout? _idleTitleLayout;


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
        var categoryBrush = sound.Category.BackgroundBrush;
        var categoryColor = categoryBrush.Color;

        // DrawShadows(context, roundedRect, categoryColor);
        DrawSurface(context, roundedRect, categoryColor);
        DrawText(context, size, sound);
        DrawProgress(context, size, sound);
        // DrawProgressPlayhead(context, size, sound);
        DrawBorder(context, roundedRect, size, categoryColor);
    }

    #region Pipeline Step 1: Shadows

    private void DrawShadows(DrawingContext context, RoundedRect roundedRect, Color categoryColor)
    {
        if (_glowOpacity <= 0.0) return;

        var shadow = GetGlowShadow(categoryColor, _palette.IsLightTheme, _glowBlur, _glowOpacity);
        if (shadow.Count > 0)
        {
            context.DrawRectangle(null, null, roundedRect, shadow);
        }
    }

    #endregion

    #region Pipeline Step 2: Surface & Accent

    private void DrawSurface(DrawingContext context, RoundedRect roundedRect, Color categoryColor)
    {
        if (_accentOffset <= 0.01)
        {
            context.DrawRectangle(_palette.Surface, null, roundedRect);
            return;
        }

        var accentShadow = GetAccentShadow(categoryColor, _accentOffset);
        context.DrawRectangle(_palette.Surface, null, roundedRect, accentShadow);
    }

    #endregion

    #region Pipeline Step 3: Playback Progress

    private void DrawProgress(DrawingContext context, Size size, SoundModel sound)
    {
        if (!sound.IsPlaying) return;

        // Will use it for clipping
        var roundedRect = new RoundedRect(new Rect(size), _metrics.CornerRadius);

        var progress = sound.PlaybackStartTime.HasValue
            ? Math.Clamp((DateTime.UtcNow - sound.PlaybackStartTime.Value).TotalMilliseconds / Math.Max(sound.Duration.TotalMilliseconds, 250.0), 0.0, 1.0)
            : sound.Progress;

        var progressToSize = new Size(size.Width * progress, size.Height);
        var newRect = new Rect(progressToSize);

        var brush = GetProgressBrush(sound.BackgroundBrush.Color);
        using (context.PushClip(roundedRect))
        {
            context.DrawRectangle(brush, null, newRect);
        }
    }

    #endregion

    #region Pipeline Step 3b: Playback Progress Playhead

    // Static cached warm-white laser filament from Colors.axaml (#FFFBE8)
    private static readonly ImmutableSolidColorBrush LaserCoreBrush = new(Color.Parse("#FFFBE8"));

    private void DrawProgressPlayhead(DrawingContext context, Size size, SoundModel sound)
    {
        if (!sound.IsPlaying) return;

        var categoryColor = sound.BackgroundBrush.Color;
        var isLight = _palette.IsLightTheme;

        const double playheadWidth = 2.0;

        var progress = sound.PlaybackStartTime.HasValue
            ? Math.Clamp((DateTime.UtcNow - sound.PlaybackStartTime.Value).TotalMilliseconds / Math.Max(sound.Duration.TotalMilliseconds, 250.0), 0.0, 1.0)
            : sound.Progress;

        // Pixel-snap X to eliminate subpixel shimmering/jittering
        var playheadX = Math.Round(size.Width * progress);
        var playheadRect = new Rect(0, 0, playheadWidth, size.Height);

        var boxShadows = GetPlayheadShadow(categoryColor, isLight);
        var roundedRect = new RoundedRect(new Rect(size), _metrics.CornerRadius);

        using (context.PushClip(roundedRect))
        using (context.PushTransform(Matrix.CreateTranslation(playheadX, 0)))
        {
            context.DrawRectangle(LaserCoreBrush, null, playheadRect, boxShadows);
        }
    }

    #endregion

    #region Pipeline Step 4: Text Elements

    private void DrawText(DrawingContext context, Size size, SoundModel sound)
    {
        var contentWidth = Math.Max(0, size.Width - _metrics.PaddingLeft - _metrics.PaddingRight);

        var (categoryRect, titleRect) = GetTextHitZones();

        // 1. Category Name (Top-Left, Granular Dirty)
        var (naturalLayout, idleLayout) = GetCategoryLayouts(sound, contentWidth);
        var categoryMarqueeStarted = _categoryMarquee.UpdateOverflow(naturalLayout.Width, categoryRect.Width);
        _categoryMarquee.Draw(context, naturalLayout, idleLayout, categoryRect);

        // 2. Footer: Duration & Index (Immutable Text - No Dirty Flags)
        DrawImmutableText(context, size);

        // 3. Sound Title (Middle, Granular Dirty)
        var lineHeight = 18.0 + _fontSizeDelta;
        var maxVisibleLines = Math.Max(1, (int)Math.Round(titleRect.Height / lineHeight));

        if (_titleDirty || _titleLayout is null)
        {
            _titleLayout = new TextLayout(
                sound.Name,
                NormalTypeface,
                fontSize: 14 + _fontSizeDelta,
                foreground: _palette.TextPrimary,
                textAlignment: TextAlignment.Left,
                textWrapping: TextWrapping.Wrap,
                textTrimming: null,
                maxWidth: contentWidth,
                maxHeight: double.PositiveInfinity,
                lineHeight: lineHeight);

            _idleTitleLayout = _titleLayout.Height > titleRect.Height
                ? new TextLayout(
                    sound.Name,
                    NormalTypeface,
                    fontSize: 14 + _fontSizeDelta,
                    foreground: _palette.TextPrimary,
                    textAlignment: TextAlignment.Left,
                    textWrapping: TextWrapping.Wrap,
                    textTrimming: TextTrimming.CharacterEllipsis,
                    maxWidth: contentWidth,
                    maxHeight: titleRect.Height,
                    maxLines: maxVisibleLines,
                    lineHeight: lineHeight)
                : null;

            _titleWrapped = _titleLayout.TextLines.Count > 1;
            _titleUnwrappedWidth = _titleLayout.Width;
            _titleDirty = false;
        }

        var titleMarqueeStarted = _titleMarquee.UpdateOverflow(_titleLayout.Height, titleRect.Height);
        _titleMarquee.Draw(context, _titleLayout, _idleTitleLayout, titleRect);

        // Hover may have arrived before this render established overflow.
        if (categoryMarqueeStarted || titleMarqueeStarted)
            RequestFrameIfNeeded();
    }

    /// <summary>
    /// Draws static, immutable footer text (Duration & Index). Position calculated on the fly without reallocating layouts.
    /// </summary>
    private void DrawImmutableText(DrawingContext context, Size size)
    {
        if ((_durationLayout is null || _indexLayout is null) && _boundSound is not null)
        {
            BuildImmutableText(_boundSound);
        }

        if (_durationLayout is null && _indexLayout is null) return;

        var footerY = size.Height - _metrics.PaddingBottom - 16 - _fontSizeDelta;
        var durationX = _metrics.PaddingLeft;

        _durationLayout?.Draw(context, new Point(durationX, footerY));

        if (_indexLayout is not null)
        {
            var indexX = Math.Max(durationX, size.Width - _metrics.PaddingRight - _indexLayout.Width);
            _indexLayout.Draw(context, new Point(indexX, footerY));
        }
    }

    private static readonly FontFeatureCollection TabularNumbersFeatures = FontFeatureCollection.Parse("tnum");

    private void BuildImmutableText(SoundModel? sound)
    {
        if (sound is null)
        {
            _durationLayout = null;
            _indexLayout = null;
            return;
        }

        var durationStr = sound.Duration.ToString(@"m\:ss\.ff");
        var indexNumber = sound.Index.ToString($"D{sound.IndexDisplayWidth}", System.Globalization.CultureInfo.InvariantCulture);
        var indexStr = _showHashPrefix ? $"#{indexNumber}" : indexNumber;

        _durationLayout = new TextLayout(
            durationStr,
            NormalTypeface,
            fontSize: 14 + _fontSizeDelta,
            foreground: _palette.TextMuted,
            textAlignment: TextAlignment.Left,
            fontFeatures: TabularNumbersFeatures);

        _indexLayout = new TextLayout(
            indexStr,
            NormalTypeface,
            fontSize: 14 + _fontSizeDelta,
            foreground: _palette.TextMuted,
            textAlignment: TextAlignment.Left,
            fontFeatures: TabularNumbersFeatures);
    }

    #endregion

    #region Pipeline Step 5: Border

    private void DrawBorder(DrawingContext context, RoundedRect roundedRect, Size size, Color categoryColor)
    {
        if (_borderFade <= 0.0)
        {
            _borderPen.Brush = _palette.Border;
        }
        else
        {
            var borderCol = _palette.Border.Color;
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
    }

    #endregion


    static Color ColorWithAlpha(Color color, byte alpha)
    {
        return Color.FromArgb(alpha, color.R, color.G, color.B);
    }

    static Color ColorWithAlpha(Color color, double alpha)
    {
        return Color.FromArgb((byte)(alpha * 255), color.R, color.G, color.B);
    }


    private (TextLayout NaturalLayout, TextLayout IdleLayout) GetCategoryLayouts(SoundModel sound, double contentWidth)
    {
        var key = new CategoryCacheKey(sound.Category.Name, sound.Category.BackgroundBrush.Color);

        if (!CategoryCache.TryGetValue(key, out var entry))
        {
            // Not cached yet
            var naturalLayout = new TextLayout(
                sound.Category.Name,
                CategoryTypeface,
                fontSize: 14 + _fontSizeDelta,
                foreground: sound.Category.BackgroundBrush,
                textAlignment: TextAlignment.Left,
                maxHeight: 20 + _fontSizeDelta);

            entry = new CategoryCacheEntry
            {
                NaturalLayout = naturalLayout,
                NaturalWidth = naturalLayout.Width
            };
            CategoryCache[key] = entry;
        }


        // Case 1, fits inside the content area
        if (contentWidth >= entry.NaturalWidth)
        {
            return (entry.NaturalLayout, entry.NaturalLayout);
        }

        // Case 2, overflows
        var intWidth = (int)contentWidth;
        if (entry.ElidedLayout is null || entry.ElidedWidth != intWidth)
        {
            entry.ElidedLayout = new TextLayout(
                sound.Category.Name,
                CategoryTypeface,
                fontSize: 14 + _fontSizeDelta,
                foreground: sound.Category.BackgroundBrush,
                textAlignment: TextAlignment.Left,
                maxWidth: contentWidth,
                maxHeight: 20 + _fontSizeDelta,
                textTrimming: TextTrimming.CharacterEllipsis);

            entry.ElidedWidth = intWidth;
        }

        return (entry.NaturalLayout, entry.ElidedLayout!);
    }

    private (Rect categoryRect, Rect titleRect) GetTextHitZones()
    {
        var size = Bounds.Size;
        var contentWidth = Math.Max(0, size.Width - _metrics.PaddingLeft - _metrics.PaddingRight);
        var categoryHeight = 20.0 + _fontSizeDelta;
        var categoryRect = new Rect(_metrics.PaddingLeft, _metrics.PaddingTop, contentWidth, categoryHeight);

        var titleTop = _metrics.PaddingTop + 24.0 + _fontSizeDelta;
        var footerY = size.Height - _metrics.PaddingBottom - 16.0 - _fontSizeDelta;
        var bottomLimit = footerY - 4.0;
        var rawTitleHeight = Math.Max(10.0, bottomLimit - titleTop);

        var lineHeight = 18.0 + _fontSizeDelta;
        var maxLines = Math.Max(1, (int)(rawTitleHeight / lineHeight));
        var snappedTitleHeight = maxLines * lineHeight;
        var centeredTitleTop = titleTop + (rawTitleHeight - snappedTitleHeight) / 2.0;
        var titleRect = new Rect(_metrics.PaddingLeft, centeredTitleTop, contentWidth, snappedTitleHeight);

        return (categoryRect, titleRect);
    }
}
