using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Soundboard.Avalonia.Discovery;
using Soundboard.Avalonia.Utils;
using System;
using System.Collections.Generic;

namespace Soundboard.Avalonia.UI.Controls;

public partial class FastSoundPad
{
    #region Static Rendering Caches (Zero Allocation)

    // Pre-warmed & lazy caches for zero-allocation rendering across light & dark themes
    private static readonly Dictionary<Color, BoxShadows> IdleAccentShadowCache = new();
    private static readonly Dictionary<(Color, int), BoxShadows> AnimatedAccentShadowCache = new();
    private static readonly Dictionary<Color, ImmutableSolidColorBrush> ProgressBrushCache = new();
    private static readonly Dictionary<(Color, bool), BoxShadows> SteadyGlowShadowCache = new();
    private static readonly Dictionary<(Color, bool, int, int), BoxShadows> AnimatedGlowShadowCache = new();
    private static readonly Dictionary<(Color, bool), BoxShadows> PlayheadShadowCache = new();

    static FastSoundPad()
    {
        PrewarmCaches();
    }

    private static void PrewarmCaches()
    {
        foreach (var primary in ColorPalette.PrimaryColors)
        {
            var (dark, light) = HelmlabColorConversion.GetDualModeColors(primary);
            PrewarmColor(dark, isLight: false);
            PrewarmColor(light, isLight: true);
        }
    }

    private static void PrewarmColor(Color color, bool isLight)
    {
        // 1. Idle Accent Shadow (6.0px inset)
        if (!IdleAccentShadowCache.ContainsKey(color))
        {
            IdleAccentShadowCache[color] = new BoxShadows(new BoxShadow
            {
                IsInset = true,
                OffsetX = PadMetrics.Default.AccentThickness,
                Color = color
            });
        }

        // 2. Playback Progress Fill Brush (alpha = 124)
        if (!ProgressBrushCache.ContainsKey(color))
        {
            ProgressBrushCache[color] = new ImmutableSolidColorBrush(new Color(124, color.R, color.G, color.B));
        }

        // 3. Steady-State Playing Glow Shadow (blur = 24.0, opacity = 1.0)
        var glowKey = (color, isLight);
        if (!SteadyGlowShadowCache.ContainsKey(glowKey))
        {
            SteadyGlowShadowCache[glowKey] = CreateGlowShadow(color, isLight, 24.0, 1.0);
        }

        // 4. Playhead Dual Glow Shadows
        var playheadKey = (color, isLight);
        if (!PlayheadShadowCache.ContainsKey(playheadKey))
        {
            PlayheadShadowCache[playheadKey] = CreatePlayheadShadow(color, isLight);
        }
    }

    private static BoxShadows CreateGlowShadow(Color categoryColor, bool isLightTheme, double blur, double opacity)
    {
        var maxOpacity = isLightTheme ? 0.7 : 0.55;
        var glowAlpha = (byte)Math.Clamp(categoryColor.A * maxOpacity * opacity, 0, 255);
        var glowColor = Color.FromArgb(glowAlpha, categoryColor.R, categoryColor.G, categoryColor.B);

        var glowShadow = new BoxShadow
        {
            Blur = isLightTheme ? blur * (20.0 / 24.0) : blur,
            Spread = 0.1,
            Color = glowColor
        };

        return new BoxShadows(glowShadow);
    }

    private static BoxShadows CreatePlayheadShadow(Color categoryColor, bool isLight)
    {
        var innerAlpha = (byte)((isLight ? 0.95 : 1.00) * 255);
        var outerAlpha = (byte)((isLight ? 0.90 : 0.85) * 255);

        return new BoxShadows(
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
    }

    private static BoxShadows GetAccentShadow(Color color, double offset)
    {
        if (offset <= 0.01) return default;

        // Idle steady state (matches default AccentThickness 6.0)
        if (Math.Abs(offset - PadMetrics.Default.AccentThickness) < 0.01)
        {
            if (IdleAccentShadowCache.TryGetValue(color, out var cached))
                return cached;

            var shadow = new BoxShadows(new BoxShadow
            {
                IsInset = true,
                OffsetX = PadMetrics.Default.AccentThickness,
                Color = color
            });
            IdleAccentShadowCache[color] = shadow;
            return shadow;
        }

        // Animated or custom thickness: quantize to 0.1px resolution to avoid cache bloat
        var offsetTenths = (int)Math.Round(offset * 10);
        if (offsetTenths <= 0) return default;

        var key = (color, offsetTenths);
        if (AnimatedAccentShadowCache.TryGetValue(key, out var animCached))
            return animCached;

        var animShadow = new BoxShadows(new BoxShadow
        {
            IsInset = true,
            OffsetX = offsetTenths / 10.0,
            Color = color
        });
        AnimatedAccentShadowCache[key] = animShadow;
        return animShadow;
    }

    private static ImmutableSolidColorBrush GetProgressBrush(Color color)
    {
        if (ProgressBrushCache.TryGetValue(color, out var brush))
            return brush;

        var created = new ImmutableSolidColorBrush(new Color(124, color.R, color.G, color.B));
        ProgressBrushCache[color] = created;
        return created;
    }

    private static BoxShadows GetGlowShadow(Color categoryColor, bool isLightTheme, double blur, double opacity)
    {
        if (opacity <= 0.01 || blur <= 0.01) return default;

        // Steady state playing: opacity == 1.0, blur == 24.0
        if (Math.Abs(opacity - 1.0) < 0.01 && Math.Abs(blur - 24.0) < 0.01)
        {
            var steadyKey = (categoryColor, isLightTheme);
            if (SteadyGlowShadowCache.TryGetValue(steadyKey, out var steady))
                return steady;

            var steadyShadow = CreateGlowShadow(categoryColor, isLightTheme, 24.0, 1.0);
            SteadyGlowShadowCache[steadyKey] = steadyShadow;
            return steadyShadow;
        }

        // Animating: quantize opacity to tenths and blur to whole pixels
        var opStep = (int)Math.Round(opacity * 10);
        var blurStep = (int)Math.Round(blur);
        if (opStep <= 0 || blurStep <= 0) return default;

        var animKey = (categoryColor, isLightTheme, opStep, blurStep);
        if (AnimatedGlowShadowCache.TryGetValue(animKey, out var anim))
            return anim;

        var shadow = CreateGlowShadow(categoryColor, isLightTheme, blurStep, opStep / 10.0);
        AnimatedGlowShadowCache[animKey] = shadow;
        return shadow;
    }

    private static BoxShadows GetPlayheadShadow(Color categoryColor, bool isLight)
    {
        var key = (categoryColor, isLight);
        if (PlayheadShadowCache.TryGetValue(key, out var cached))
            return cached;

        var shadow = CreatePlayheadShadow(categoryColor, isLight);
        PlayheadShadowCache[key] = shadow;
        return shadow;
    }

    #endregion
}
