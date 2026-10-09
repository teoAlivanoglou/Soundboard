using System;

namespace Soundboard.Avalonia.UI.Controls;

public delegate double EasingFunction(double t);

public static class Easing
{
    public static readonly EasingFunction Linear = t => t;
    public static readonly EasingFunction EaseInQuad = t => t * t;
    public static readonly EasingFunction EaseOutQuad = t => 1.0 - (1.0 - t) * (1.0 - t);
    public static readonly EasingFunction EaseInOutQuad = t => t < 0.5 ? 2.0 * t * t : 1.0 - Math.Pow(-2.0 * t + 2.0, 2) / 2.0;
    public static readonly EasingFunction EaseInCubic = t => t * t * t;
    public static readonly EasingFunction EaseOutCubic = t => 1.0 - Math.Pow(1.0 - t, 3);
    public static readonly EasingFunction EaseInOutCubic = t => t < 0.5 ? 4.0 * t * t * t : 1.0 - Math.Pow(-2.0 * t + 2.0, 3) / 2.0;
}
