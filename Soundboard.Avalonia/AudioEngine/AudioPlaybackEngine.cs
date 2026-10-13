using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
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
    private VolumeSampleProvider _volumeProvider;

    private readonly AudioDataCache _audioCache;
    private readonly SettingsService _settingsService;
    private readonly Lock _lock = new();
    private readonly Dictionary<SoundModel, List<ISampleProvider>> _soundsAndSampleProviders = [];
    private readonly Dictionary<ISampleProvider, SoundModel> _providerToSound = [];

    public AudioPlaybackEngine(SettingsService settingsService, AudioDataCache audioCache)
    {
        _settingsService = settingsService;
        _audioCache = audioCache;

        InitializeDriver();
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

    [MemberNotNull(nameof(_outputDevice), nameof(_mixer), nameof(_volumeProvider))]
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
        _outputDevice = (OperatingSystem.IsMacOSVersionAtLeast(10, 5) && audioSettings.DriverType == DriverType.CoreAudio)
            ? new CoreAudioPlayer()
            : throw new PlatformNotSupportedException($"Driver {audioSettings.DriverType} is not supported on this platform.");
#endif

        _mixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat((int)audioSettings.SampleRate, 2))
        {
            ReadFully = true
        };

        _mixer.MixerInputEnded += MixerOnMixerInputEnded;

        _volumeProvider = new VolumeSampleProvider(_mixer)
        {
            Volume = (float)Math.Clamp(audioSettings.Volume / 100.0, 0.0, 1.0)
        };

        _outputDevice.Init(_volumeProvider);
        _outputDevice.Play();
    }

    public void SetVolume(double volume)
    {
        _volumeProvider.Volume = (float)Math.Clamp(volume / 100.0, 0.0, 1.0);
    }

    public void PlaySound(SoundModel sound, int fadeDuration = 0)
    {
        ISampleProvider rawProvider;
        if (sound.AudioData is { Length: > 0 })
        {
            _audioCache.Touch(sound);
            rawProvider = new CachedSoundSampleProvider(sound);
        }
        else if (_audioCache.CanCache(sound) && AudioDecoder.TryDecode(sound.FilePath, out var data, out var format))
        {
            sound.WaveFormat = format;
            _audioCache.Add(sound, data!);
            rawProvider = new CachedSoundSampleProvider(sound);
        }
        else
        {
            var fileStream = File.Open(sound.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var stream = VorbisWavHelper.WrapIfVorbisWav(fileStream);
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

            _providerToSound[provider] = sound;
        }

        if (Dispatcher.UIThread.CheckAccess())
        {
            sound.PlaybackStartTime = DateTime.UtcNow;
            sound.IsPlaying = true;
        }
        else
        {
            Dispatcher.UIThread.Post(() =>
            {
                sound.PlaybackStartTime = DateTime.UtcNow;
                sound.IsPlaying = true;
            });
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
                if (fadeDuration <= 0)
                {
                    foreach (var provider in providers)
                    {
                        _providerToSound.Remove(provider);
                    }
                }
            }
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
            sound.PlaybackStartTime = null;
            sound.IsPlaying = false;
            sound.Progress = 0;
        }
        else
        {
            Dispatcher.UIThread.Post(() =>
            {
                sound.PlaybackStartTime = null;
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
        // Offload lock acquisition and disposable cleanup off the real-time audio thread
        ThreadPool.QueueUserWorkItem(_ =>
        {
            SoundModel? finishedSound = null;
            lock (_lock)
            {
                if (_providerToSound.Remove(e.SampleProvider, out var sound))
                {
                    if (_soundsAndSampleProviders.TryGetValue(sound, out var list))
                    {
                        list.Remove(e.SampleProvider);
                        if (list.Count == 0)
                        {
                            _soundsAndSampleProviders.Remove(sound);
                            finishedSound = sound;
                        }
                    }
                    else
                    {
                        // Sound was already removed from _soundsAndSampleProviders by StopSound (during fade-out)
                        finishedSound = sound;
                    }
                }
            }

            if (e.SampleProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }

            if (finishedSound != null)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    finishedSound.IsPlaying = false;
                    finishedSound.Progress = 0;
                    finishedSound.PlaybackStartTime = null;
                });
            }
        });
    }

    public void Dispose()
    {
        _outputDevice.Dispose();
    }

    public void Reset()
    {
        StopAllSounds();
        lock (_lock)
        {
            _soundsAndSampleProviders.Clear();
            _providerToSound.Clear();
        }

        _outputDevice.Dispose();
        InitializeDriver();
    }
}