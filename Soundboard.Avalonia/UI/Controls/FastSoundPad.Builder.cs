using Avalonia;
using Avalonia.Media;

namespace Soundboard.Avalonia.UI.Controls;

public partial class FastSoundPad
{
    public FastSoundPad WithCornerRadius(CornerRadius radius)
    {
        _metrics.CornerRadius = radius;
        InvalidateVisual();
        return this;
    }

    public FastSoundPad WithAccentThickness(double thickness)
    {
        _metrics.AccentThickness = thickness;
        _accentOffset = thickness;
        if (_timeline is not null)
        {
            InitializeAnimations();
        }
        InvalidateVisual();
        return this;
    }

    public FastSoundPad WithBorderThickness(double thickness)
    {
        _metrics.BorderThickness = thickness;
        _borderPen = new Pen(_borderPen?.Brush ?? _palette.Border, _metrics.BorderThickness);
        InvalidateVisual();
        return this;
    }

    public FastSoundPad WithPadding(double left, double top, double right, double bottom)
    {
        _metrics.PaddingLeft = left;
        _metrics.PaddingTop = top;
        _metrics.PaddingRight = right;
        _metrics.PaddingBottom = bottom;
        _titleDirty = true;
        InvalidateVisual();
        return this;
    }

    public FastSoundPad WithShadows(bool enabled = true)
    {
        _enableShadows = enabled;
        InvalidateVisual();
        return this;
    }

    public FastSoundPad WithoutShadows() => WithShadows(false);

    public FastSoundPad WithBorderEffect(BorderEffect effect)
    {
        _borderEffect = effect;
        InvalidateVisual();
        return this;
    }

    public FastSoundPad WithProgress(bool enabled = true)
    {
        _enableProgress = enabled;
        InvalidateVisual();
        return this;
    }

    public FastSoundPad WithoutProgress() => WithProgress(false);

    public FastSoundPad WithText(bool enabled = true)
    {
        _enableText = enabled;
        InvalidateVisual();
        return this;
    }

    public FastSoundPad WithoutText() => WithText(false);

    public FastSoundPad WithCategory(bool enabled = true)
    {
        _showCategory = enabled;
        _titleDirty = true;
        InvalidateVisual();
        return this;
    }

    public FastSoundPad WithoutCategory() => WithCategory(false);

    public FastSoundPad WithTitle(bool enabled = true)
    {
        _showTitle = enabled;
        _titleDirty = true;
        InvalidateVisual();
        return this;
    }

    public FastSoundPad WithoutTitle() => WithTitle(false);

    public FastSoundPad WithFooter(bool enabled = true)
    {
        _showFooter = enabled;
        _titleDirty = true;
        InvalidateVisual();
        return this;
    }

    public FastSoundPad WithoutFooter() => WithFooter(false);
}
