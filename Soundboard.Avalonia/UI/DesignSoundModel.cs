using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using Soundboard.Discovery;

namespace Soundboard.Avalonia.UI;

public partial class DesignSoundModel : ObservableObject
{
    [ObservableProperty] public partial string Name { get; set; } = "Cleanest 808";

    [ObservableProperty]
    public partial string LongName { get; set; } =
        "0000000000111111111122222222223333333333444444444455555555556666666666777777777788888888889999999999";

    [ObservableProperty] public partial string Duration { get; set; } = "0:04";
    [ObservableProperty] public partial string PadNumber { get; set; } = "#01";
    [ObservableProperty] public partial bool IsPlaying { get; set; } = true;
    [ObservableProperty] public partial double Progress { get; set; } = 0.33;


    // BUTTON PROPS:
    [ObservableProperty] public partial double Size { get; set; } = 136;


    [ObservableProperty] public partial DesignCategoryModel Category { get; set; } = new();

    private global::Avalonia.Threading.DispatcherTimer? _playbackTimer;
    private global::System.Diagnostics.Stopwatch? _stopwatch;
    private global::System.TimeSpan _totalDuration;

    public void ToggleSimulation()
    {
        if (IsPlaying)
        {
            StopSimulation();
        }
        else
        {
            StartSimulation();
        }
    }

    public void StartSimulation()
    {
        StopPlaybackTimer();

        if (global::System.TimeSpan.TryParseExact(Duration, @"m\:ss", global::System.Globalization.CultureInfo.InvariantCulture, out var parsed) ||
            global::System.TimeSpan.TryParse(Duration, global::System.Globalization.CultureInfo.InvariantCulture, out parsed))
        {
            _totalDuration = parsed > global::System.TimeSpan.Zero ? parsed : global::System.TimeSpan.FromSeconds(3);
        }
        else
        {
            _totalDuration = global::System.TimeSpan.FromSeconds(3);
        }

        Progress = 0.0;
        IsPlaying = true;

        _stopwatch = global::System.Diagnostics.Stopwatch.StartNew();
        _playbackTimer = new global::Avalonia.Threading.DispatcherTimer
        {
            Interval = global::System.TimeSpan.FromMilliseconds(16)
        };
        _playbackTimer.Tick += OnPlaybackTimerTick;
        _playbackTimer.Start();
    }

    public void StopSimulation()
    {
        StopPlaybackTimer();
        IsPlaying = false;
    }

    private void StopPlaybackTimer()
    {
        if (_playbackTimer != null)
        {
            _playbackTimer.Stop();
            _playbackTimer.Tick -= OnPlaybackTimerTick;
            _playbackTimer = null;
        }

        _stopwatch?.Stop();
        _stopwatch = null;
    }

    private void OnPlaybackTimerTick(object? sender, global::System.EventArgs e)
    {
        if (_stopwatch == null)
        {
            StopSimulation();
            return;
        }

        var elapsedMs = _stopwatch.Elapsed.TotalMilliseconds;
        var totalMs = _totalDuration.TotalMilliseconds;

        if (totalMs <= 0 || elapsedMs >= totalMs)
        {
            Progress = 1.0;
            StopSimulation();
        }
        else
        {
            Progress = global::System.Math.Clamp(elapsedMs / totalMs, 0.0, 1.0);
        }
    }
}

public partial class DesignCategoryModel : ObservableObject
{
    [ObservableProperty] public partial string Name { get; set; } = "Bass";
    [ObservableProperty] public partial int SoundCount { get; set; } = 12;
    [ObservableProperty] public partial bool IsEnabled { get; set; } = true;

    [ObservableProperty]
    public partial SolidColorBrush BackgroundBrush { get; set; } = new(Color.FromRgb(0xF8, 0xBD, 0x28));

    [ObservableProperty] public partial bool IsAll { get; set; } = false;
}