using System;
using Avalonia;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;

namespace SonicPad.UI.Controls;

public enum MarqueePhase
{
    Idle,
    StartDelay,
    Scrolling,
    EndDelay,
    Rewinding
}

public struct MarqueeScroller
{
    public Orientation Orientation { get; }
    public MarqueePhase Phase { get; private set; } = MarqueePhase.Idle;
    public double Offset { get; private set; } = 0.0;
    public double Overflow { get; set; } = 0.0;
    public bool IsHovered { get; private set; } = false;
    private double _phaseTimer = 0.0;

    public const double DefaultSpeed = 35.0; // px/s
    public const double StartDelay = 0.10;   // 100ms pause before scrolling
    public const double EndDelay = 1.00;     // 1000ms pause at end
    public const double RewindMultiplier = 4.0;

    public bool IsActive => Phase != MarqueePhase.Idle || Offset > 0.0;

    public MarqueeScroller(Orientation orientation)
    {
        Orientation = orientation;
    }

    public void SetHover(bool isHovered)
    {
        if (IsHovered == isHovered) return;
        IsHovered = isHovered;

        if (IsHovered)
        {
            if (Overflow > 1.0)
            {
                Phase = MarqueePhase.StartDelay;
                _phaseTimer = 0.0;
            }
        }
        else
        {
            Phase = MarqueePhase.Idle;
            Offset = 0.0;
            _phaseTimer = 0.0;
        }
    }

    public void Update(double dt)
    {
        if (Overflow <= 1.0)
        {
            Phase = MarqueePhase.Idle;
            Offset = 0.0;
            return;
        }

        switch (Phase)
        {
            case MarqueePhase.StartDelay:
                _phaseTimer += dt;
                if (_phaseTimer >= StartDelay)
                {
                    Phase = MarqueePhase.Scrolling;
                    _phaseTimer = 0.0;
                }
                break;

            case MarqueePhase.Scrolling:
                Offset += DefaultSpeed * dt;
                if (Offset >= Overflow)
                {
                    Offset = Overflow;
                    Phase = MarqueePhase.EndDelay;
                    _phaseTimer = 0.0;
                }
                break;

            case MarqueePhase.EndDelay:
                _phaseTimer += dt;
                if (_phaseTimer >= EndDelay)
                {
                    Phase = MarqueePhase.Rewinding;
                    _phaseTimer = 0.0;
                }
                break;

            case MarqueePhase.Rewinding:
                Offset -= DefaultSpeed * RewindMultiplier * dt;
                if (Offset <= 0.0)
                {
                    Offset = 0.0;
                    if (IsHovered)
                    {
                        Phase = MarqueePhase.StartDelay;
                        _phaseTimer = 0.0;
                    }
                    else
                    {
                        Phase = MarqueePhase.Idle;
                    }
                }
                break;

            case MarqueePhase.Idle:
            default:
                Offset = 0.0;
                break;
        }
    }

    public void Draw(DrawingContext context, TextLayout activeLayout, TextLayout? idleLayout, Rect zone)
    {
        if (Orientation == Orientation.Horizontal)
        {
            Overflow = Math.Max(0.0, activeLayout.Width - zone.Width);

            if (IsActive)
            {
                using (context.PushClip(zone))
                {
                    activeLayout.Draw(context, new Point(zone.X - Offset, zone.Y));
                }
            }
            else
            {
                var layoutToDraw = idleLayout ?? activeLayout;
                using (context.PushClip(zone))
                {
                    layoutToDraw.Draw(context, zone.Position);
                }
            }
        }
        else // Orientation.Vertical
        {
            Overflow = Math.Max(0.0, activeLayout.Height - zone.Height);

            if (IsActive)
            {
                using (context.PushClip(zone))
                {
                    activeLayout.Draw(context, new Point(zone.X, zone.Y - Offset));
                }
            }
            else
            {
                var layoutToDraw = idleLayout ?? activeLayout;
                var yOffset = Math.Max(0.0, (zone.Height - layoutToDraw.Height) / 2.0);
                using (context.PushClip(zone))
                {
                    layoutToDraw.Draw(context, new Point(zone.X, zone.Y + yOffset));
                }
            }
        }
    }

    public void Reset()
    {
        Phase = MarqueePhase.Idle;
        Offset = 0.0;
        Overflow = 0.0;
        _phaseTimer = 0.0;
        IsHovered = false;
    }
}