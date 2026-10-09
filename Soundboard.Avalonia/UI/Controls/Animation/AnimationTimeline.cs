using System;
using System.Collections.Generic;

namespace Soundboard.Avalonia.UI.Controls;

public class AnimationTrack
{
    public Action<double> Setter { get; }
    public double From { get; }
    public double To { get; }
    public double DurationSeconds { get; }
    public double DelaySeconds { get; }
    public EasingFunction Easing { get; }

    public AnimationTrack(
        Action<double> setter,
        double from,
        double to,
        TimeSpan duration,
        EasingFunction? easing = null,
        TimeSpan delay = default)
    {
        Setter = setter;
        From = from;
        To = to;
        DurationSeconds = duration.TotalSeconds;
        DelaySeconds = delay.TotalSeconds;
        Easing = easing ?? Soundboard.Avalonia.UI.Controls.Easing.Linear;
    }

    public void Evaluate(double elapsedSeconds)
    {
        if (elapsedSeconds < DelaySeconds)
        {
            Setter(From);
            return;
        }

        if (DurationSeconds <= 0.0)
        {
            Setter(To);
            return;
        }

        var t = Math.Clamp((elapsedSeconds - DelaySeconds) / DurationSeconds, 0.0, 1.0);
        var easedT = Easing(t);
        var value = From + (To - From) * easedT;
        Setter(value);
    }

    public bool IsComplete(double elapsedSeconds)
    {
        return elapsedSeconds >= DelaySeconds + DurationSeconds;
    }
}

public class AnimationStateBuilder<TState> where TState : struct, Enum
{
    private readonly List<AnimationTrack> _tracks = [];
    public IReadOnlyList<AnimationTrack> Tracks => _tracks;

    public AnimationStateBuilder<TState> Animate(
        Action<double> setter,
        double from,
        double to,
        TimeSpan duration,
        EasingFunction? easing = null,
        TimeSpan delay = default)
    {
        _tracks.Add(new AnimationTrack(setter, from, to, duration, easing, delay));
        return this;
    }
}

public class AnimationTimeline<TState> where TState : struct, Enum
{
    private readonly Dictionary<TState, AnimationTrack[]> _stateTracks = [];
    private AnimationTrack[]? _activeTracks;
    private double _elapsedSeconds;
    private bool _isRunning;

    public bool IsRunning => _isRunning;

    public virtual AnimationTimeline<TState> OnState(TState state, Action<AnimationStateBuilder<TState>> configure)
    {
        var builder = new AnimationStateBuilder<TState>();
        configure(builder);
        _stateTracks[state] = [.. builder.Tracks];
        return this;
    }

    public void Play(TState state)
    {
        if (!_stateTracks.TryGetValue(state, out var tracks))
        {
            _activeTracks = null;
            _isRunning = false;
            return;
        }

        _activeTracks = tracks;
        _elapsedSeconds = 0.0;
        _isRunning = true;

        // Apply initial 'From' values immediately on start frame
        for (int i = 0; i < tracks.Length; i++)
        {
            tracks[i].Evaluate(0.0);
        }
    }

    /// <summary>
    /// Steps the timeline forward. Returns true if animation is still running, false when complete.
    /// </summary>
    public bool Update(double dt)
    {
        if (!_isRunning || _activeTracks == null) return false;

        _elapsedSeconds += dt;
        var allComplete = true;

        for (int i = 0; i < _activeTracks.Length; i++)
        {
            _activeTracks[i].Evaluate(_elapsedSeconds);
            if (!_activeTracks[i].IsComplete(_elapsedSeconds))
            {
                allComplete = false;
            }
        }

        if (allComplete)
        {
            _isRunning = false;
            return false;
        }

        return true;
    }
}

public class AnimationTimeline : AnimationTimeline<PadState>
{
    public new AnimationTimeline OnState(PadState state, Action<AnimationStateBuilder<PadState>> configure)
    {
        base.OnState(state, configure);
        return this;
    }
}
