using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using NAudio.SoundFile;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using Soundboard.Avalonia.Discovery;
using Soundboard.Avalonia.Settings;

// ReSharper disable UseObjectOrCollectionInitializer

namespace Soundboard.Avalonia.AudioEngine;

public class AudioPlaybackEngine : IDisposable
{
    private IWavePlayer _outputDevice;
    private MixingSampleProvider _mixer;

    private readonly SettingsService _settingsService;
    private readonly object _lock = new();
    private readonly Dictionary<SoundModel, List<ISampleProvider>> _soundsAndSampleProviders = new();
    private readonly Dictionary<SoundModel, DateTime> _soundsLastPlayed = new();
    private CancellationTokenSource _cancellationTokenSource = new();


    public AudioPlaybackEngine(SettingsService settingsService)
    {
        _settingsService = settingsService;

        InitializeDriver();

        _ = UpdateTimers(_cancellationTokenSource.Token);
    }
    
    public static IReadOnlyList<DriverType> GetAvailableDrivers()
    {
        var drivers = new List<DriverType>();

#if WINDOWS
        try
        {
            using var enumerator = new NAudio.CoreAudioApi.MMDeviceEnumerator();
            var devices = enumerator.EnumerateAudioEndPoints(
                NAudio.CoreAudioApi.DataFlow.Render,
                NAudio.CoreAudioApi.DeviceState.Active);

            if (devices.Count > 0)
            {
                drivers.Add(DriverType.Wasapi);
            }
        }
        catch
        {
            // WASAPI unavailable
        }

        try
        {
            if (WaveOut.DeviceCount > 0)
            {
                drivers.Add(DriverType.WaveOutEvent);
            }
        }
        catch
        {
            // WaveOut unavailable
        }

        try
        {
            if (DirectSoundOut.Devices.Any())
            {
                drivers.Add(DriverType.DirectSound);
            }
        }
        catch
        {
            // DirectSound unavailable
        }
#else
        if (OperatingSystem.IsMacOS())
        {
            drivers.Add(DriverType.CoreAudio);
        }
#endif

        return drivers;
    }
    
    [MemberNotNull(nameof(_outputDevice), nameof(_mixer))]
    private void InitializeDriver()
    {
        var audioSettings = _settingsService.AudioPlayerSettings;


#if WINDOWS
        _outputDevice = audioSettings.DriverType switch
        {
            DriverType.WaveOutEvent => new WaveOut() { BufferMilliseconds = (int)audioSettings.Latency },
            DriverType.Wasapi => (new WasapiPlayerBuilder()).WithSharedMode()
                .WithLatency((int)audioSettings.Latency).Build(),
            DriverType.DirectSound => new DirectSoundOut((int)audioSettings.Latency),
            _ => throw new ArgumentOutOfRangeException(nameof(audioSettings.DriverType), audioSettings.DriverType,
                null)
        };
#else
#pragma warning disable CA1416
        _outputDevice = (OperatingSystem.IsMacOS() && audioSettings.DriverType == DriverType.CoreAudio)
            ? new CoreAudioPlayer()
            : throw new PlatformNotSupportedException($"Driver {audioSettings.DriverType} is not supported on this platform.");
#pragma warning restore CA1416
#endif
        
        _mixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat((int)audioSettings.SampleRate, 2));
        _mixer.ReadFully = true;

        _mixer.MixerInputEnded += MixerOnMixerInputEnded;

        _outputDevice.Init(_mixer);
        _outputDevice.Play();
    }

    public void PlaySound(SoundModel sound, int fadeDuration = 0)
    {
        ISampleProvider rawProvider;
        if (sound.AudioData is { Length: > 0 })
        {
            rawProvider = new CachedSoundSampleProvider(sound);
        }
        else
        {
            var stream = File.Open(sound.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            rawProvider = new AutoDisposeFileReader(new SoundFileReader(stream), stream);
        }

        var provider = ConvertToRightChannelCount(rawProvider);

        fadeDuration = Math.Min(fadeDuration, (int)sound.Duration.TotalMilliseconds / 2);
        if (fadeDuration > 0)
        {
            var fadeTimeSpan = TimeSpan.FromMilliseconds(fadeDuration);
            provider.SetFadeIn(fadeTimeSpan);
            provider.FadeEnding(fadeTimeSpan, sound.Duration);
        }

        lock (_lock)
        {
            if (!_soundsAndSampleProviders.TryAdd(sound, [provider]))
            {
                _soundsAndSampleProviders[sound].Add(provider);
            }

            _soundsLastPlayed[sound] = DateTime.UtcNow;
        }

        if (Dispatcher.UIThread.CheckAccess())
        {
            sound.IsPlaying = true;
        }
        else
        {
            Dispatcher.UIThread.Post(() => sound.IsPlaying = true);
        }

        AddMixerInput(provider);
    }

    private void AddMixerInput(ISampleProvider input)
    {
        if (input.WaveFormat.SampleRate == _mixer.WaveFormat.SampleRate)
            _mixer.AddMixerInput(input);
    }

    private DelayFadeOutSampleProvider ConvertToRightChannelCount(ISampleProvider input)
    {
        var underlyingDisposable = input as IDisposable;
        ISampleProvider result;
        if (input.WaveFormat.Channels == _mixer.WaveFormat.Channels)
        {
            result = input;
        }
        else if (input.WaveFormat.Channels == 1 && _mixer.WaveFormat.Channels == 2)
        {
            result = new MonoToStereoSampleProvider(input);
        }
        else
        {
            throw new NotImplementedException("Not yet implemented this channel count conversion");
        }

        if (input.WaveFormat.SampleRate != _mixer.WaveFormat.SampleRate)
        {
            result = new WdlResamplingSampleProvider(result, _mixer.WaveFormat.SampleRate);
        }

        return new DelayFadeOutSampleProvider(result, underlyingDisposable);
    }

    public void StopSound(SoundModel sound)
    {
        List<ISampleProvider>? soundSampleProviders = null;
        var fadeDuration = _settingsService.AudioPlayerSettings.FadeInTime;

        lock (_lock)
        {
            if (_soundsAndSampleProviders.Remove(sound, out var providers))
            {
                soundSampleProviders = providers;
            }

            _soundsLastPlayed.Remove(sound);
        }

        if (soundSampleProviders != null)
        {
            foreach (var sampleProvider in soundSampleProviders)
            {
                if (fadeDuration > 0 && sampleProvider is DelayFadeOutSampleProvider fadeProvider)
                {
                    fadeProvider.BeginFadeOut(TimeSpan.FromMilliseconds(fadeDuration));
                }
                else
                {
                    _mixer.RemoveMixerInput(sampleProvider);
                    if (sampleProvider is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
            }
        }

        if (Dispatcher.UIThread.CheckAccess())
        {
            sound.IsPlaying = false;
            sound.Progress = 0;
        }
        else
        {
            Dispatcher.UIThread.Post(() =>
            {
                sound.IsPlaying = false;
                sound.Progress = 0;
            });
        }
    }

    public void StopAllSounds()
    {
        List<SoundModel> sounds;
        lock (_lock)
        {
            sounds = [.. _soundsAndSampleProviders.Keys];
        }

        foreach (var sound in sounds)
        {
            StopSound(sound);
        }
    }

    private void MixerOnMixerInputEnded(object? sender, SampleProviderEventArgs e)
    {
        // Task.Run(() => MessageBox.Show($"Sound stopped at exactly {DateTime.UtcNow}"));
    }

    private async Task UpdateTimers(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            List<KeyValuePair<SoundModel, DateTime>>? activeSounds = null;
            lock (_lock)
            {
                if (_soundsLastPlayed.Count > 0)
                {
                    activeSounds = [.. _soundsLastPlayed];
                }
            }

            if (activeSounds != null && activeSounds.Count > 0)
            {
                var now = DateTime.UtcNow;
                var updates =
                    new List<(SoundModel Sound, DateTime BeginTime, double Progress, bool IsPlaying)>(
                        activeSounds.Count);
                var finishedSounds = new List<SoundModel>();

                foreach (var (sound, beginTime) in activeSounds)
                {
                    var elapsed = now - beginTime;
                    var visualDurationMs = Math.Max(sound.Duration.TotalMilliseconds, 250.0);
                    if (elapsed.TotalMilliseconds <= visualDurationMs)
                    {
                        var progress = visualDurationMs > 0
                            ? elapsed.TotalMilliseconds / visualDurationMs
                            : 0;
                        updates.Add((sound, beginTime, progress, true));
                    }
                    else
                    {
                        updates.Add((sound, beginTime, 0, false));
                        finishedSounds.Add(sound);
                    }
                }

                if (finishedSounds.Count > 0)
                {
                    lock (_lock)
                    {
                        foreach (var sound in finishedSounds)
                        {
                            var visualDurationMs = Math.Max(sound.Duration.TotalMilliseconds, 250.0);
                            if (_soundsLastPlayed.TryGetValue(sound, out var lastPlayed) &&
                                (now - lastPlayed).TotalMilliseconds >= visualDurationMs)
                            {
                                _soundsLastPlayed.Remove(sound);
                                if (_soundsAndSampleProviders.Remove(sound, out var providers))
                                {
                                    foreach (var p in providers)
                                    {
                                        _mixer.RemoveMixerInput(p);
                                        if (p is IDisposable d) d.Dispose();
                                    }
                                }
                            }
                        }
                    }
                }

                Dispatcher.UIThread.Post(() =>
                {
                    foreach (var (sound, beginTime, progress, isPlaying) in updates)
                    {
                        lock (_lock)
                        {
                            if (_soundsLastPlayed.TryGetValue(sound, out var currentBeginTime) &&
                                currentBeginTime > beginTime)
                            {
                                continue;
                            }
                        }

                        sound.Progress = progress;
                        sound.IsPlaying = isPlaying;
                    }
                });

                await Task.Delay(20, cancellationToken);
            }
            else
            {
                await Task.Delay(100, cancellationToken);
            }
        }
    }

    public void Dispose()
    {
        _cancellationTokenSource.Cancel();
        _outputDevice.Dispose();
    }

    public void Reset()
    {
        StopAllSounds();
        lock (_lock)
        {
            _soundsAndSampleProviders.Clear();
            _soundsLastPlayed.Clear();
        }

        _outputDevice.Dispose();

        if (_cancellationTokenSource.IsCancellationRequested)
        {
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();
            _ = UpdateTimers(_cancellationTokenSource.Token);
        }

        InitializeDriver();
    }
}