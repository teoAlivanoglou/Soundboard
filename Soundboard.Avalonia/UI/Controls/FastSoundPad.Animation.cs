using System;
using Avalonia.Controls;

namespace Soundboard.Avalonia.UI.Controls;

public enum PadState
{
    Playing,
    Stopped
}

public partial class FastSoundPad
{
    // Animated values (actual numbers, not normalized 0-1)
    private double _accentOffset = 6.0;   // 6.0px (idle) to 0.0px (playing)
    private double _borderSweep = 0.0;    // 0.0 (idle corner) to 1.0 (playing full spread)
    private double _borderFade = 0.0;     // 0.0 (idle plain border) to 1.0 (playing visible gradient)
    private double _glowBlur = 0.0;       // 0.0px (idle) to 24.0px (playing)
    private double _glowOpacity = 0.0;    // 0.0 (idle) to 1.0 (playing)

    private AnimationTimeline _timeline = null!;
    private TimeSpan _lastFrameTime = TimeSpan.Zero;
    private bool _isFrameScheduled;

    private void InitializeAnimations()
    {
        _accentOffset = _accentThickness;

        _timeline = new AnimationTimeline()
            // When playback starts:
            .OnState(PadState.Playing, state => state
                .Animate(v => _accentOffset = v, from: _accentThickness, to: 0.0, duration: 150.Ms(), easing: Easing.EaseOutCubic)
                .Animate(v => _borderSweep = v, from: 0.0, to: 1.0, duration: 150.Ms(), easing: Easing.EaseOutQuad)
                .Animate(v => _borderFade = v, from: 0.0, to: 1.0, duration: 150.Ms())
                .Animate(v => _glowBlur = v, from: 0.0, to: 24.0, duration: 150.Ms(), easing: Easing.EaseOutQuad)
                .Animate(v => _glowOpacity = v, from: 0.0, to: 1.0, duration: 150.Ms(), easing: Easing.EaseOutQuad))
            // When playback stops:
            .OnState(PadState.Stopped, state => state
                // Stage 1: Retract accent & border sweep immediately
                .Animate(v => _accentOffset = v, from: 0.0, to: _accentThickness, duration: 200.Ms(), easing: Easing.EaseInQuad)
                .Animate(v => _borderSweep = v, from: 1.0, to: 0.0, duration: 200.Ms(), easing: Easing.EaseInQuad)
                // Stage 2: Fade out glow & border color starting with delay
                .Animate(v => _glowBlur = v, from: 24.0, to: 0.0, duration: 180.Ms(), easing: Easing.EaseOutQuad, delay: 80.Ms())
                .Animate(v => _glowOpacity = v, from: 1.0, to: 0.0, duration: 180.Ms(), easing: Easing.EaseOutQuad, delay: 80.Ms())
                .Animate(v => _borderFade = v, from: 1.0, to: 0.0, duration: 150.Ms(), delay: 100.Ms()));
    }

    private void StartAnimation(bool isPlaying)
    {
        _lastFrameTime = TimeSpan.Zero;
        _timeline.Play(isPlaying ? PadState.Playing : PadState.Stopped);
        RequestFrameIfNeeded();
    }

    private void RequestFrameIfNeeded()
    {
        if (!_isFrameScheduled && _timeline.IsRunning)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel != null)
            {
                _isFrameScheduled = true;
                topLevel.RequestAnimationFrame(OnAnimationFrame);
            }
        }
    }

    private void OnAnimationFrame(TimeSpan currentTime)
    {
        _isFrameScheduled = false;
        if (!_timeline.IsRunning) return;

        if (_lastFrameTime == TimeSpan.Zero)
        {
            _lastFrameTime = currentTime;
            RequestFrameIfNeeded();
            return;
        }

        var dt = (currentTime - _lastFrameTime).TotalSeconds;
        _lastFrameTime = currentTime;

        var isRunning = _timeline.Update(dt);
        InvalidateVisual();

        if (isRunning)
        {
            RequestFrameIfNeeded();
        }
    }
}
