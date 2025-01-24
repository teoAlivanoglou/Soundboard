using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using NAudio.CoreAudioApi;
using NAudio.Dsp;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using Soundboard.Models;
using Soundboard.Utils;

// ReSharper disable UseObjectOrCollectionInitializer

namespace Soundboard.AudioEngine
{
    internal class AudioPlaybackEngine : IDisposable
    {
        private readonly IWavePlayer _outputDevice;
        private readonly MixingSampleProvider _mixer;

        private readonly Dictionary<Sound, List<ISampleProvider>> _soundsAndSampleProviders;
        private readonly Dictionary<Sound, DateTime> _soundsLastPlayed;

        private CancellationTokenSource _cancellationTokenSource;

        private AudioPlaybackEngine(AudioDriverSettings audioDriverSettings)
            : this((int)audioDriverSettings.SampleRate, 2, audioDriverSettings.DriverType,
                (int)audioDriverSettings.Latency)
        {
        }

        public AudioPlaybackEngine(int sampleRate = (int)SampleRate.R48000, int channelCount = 2,
            DriverType outputType = DriverType.Wasapi, int latency = 20)
        {
            _outputDevice = outputType switch
            {
                DriverType.WaveOutEvent => new WaveOutEvent() { DesiredLatency = latency },
                DriverType.Wasapi => new WasapiOut(AudioClientShareMode.Shared, true, latency),
                DriverType.DirectSound => new DirectSoundOut(latency),
                _ => throw new ArgumentOutOfRangeException(nameof(outputType), outputType, null)
            };

            _mixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channelCount));
            _mixer.ReadFully = true;

            _mixer.MixerInputEnded += MixerOnMixerInputEnded;

            _outputDevice.Init(_mixer);
            _outputDevice.Play();

            _soundsAndSampleProviders = new Dictionary<Sound, List<ISampleProvider>>();
            _cancellationTokenSource = new CancellationTokenSource();

            _soundsLastPlayed = new Dictionary<Sound, DateTime>();
            _ = UpdateTimers(_cancellationTokenSource.Token);
        }

        public void PlaySound(Sound sound, int fadeDuration = 0)
        {
            if ((sound.AudioData != null && sound.AudioData.Any()) || (sound.ByteData != null && sound.ByteData.Any()))
            {
                var provider =
                    ConvertToRightChannelCount(new CachedSoundSampleProvider(sound));
                if (!_soundsAndSampleProviders.TryAdd(sound, [provider]))
                {
                    _soundsAndSampleProviders[sound].Add(provider);
                }

                fadeDuration = Math.Min(fadeDuration, (int)sound.duration.TotalMilliseconds / 2);
                provider.SetFadeIn(fadeDuration);
                provider.FadeEnding(TimeSpan.FromMilliseconds(fadeDuration), sound.Duration);

                _soundsLastPlayed[sound] = DateTime.UtcNow;
                sound.IsPlaying = true;

                AddMixerInput(provider);
            }

            else
            {
                var provider =
                    ConvertToRightChannelCount(new AutoDisposeFileReader(new AudioFileReader(sound.FilePath)));
                if (!_soundsAndSampleProviders.TryAdd(sound, [provider]))
                {
                    _soundsAndSampleProviders[sound].Add(provider);
                }

                fadeDuration = Math.Min(fadeDuration, (int)sound.duration.TotalMilliseconds / 2);
                provider.SetFadeIn(fadeDuration);
                provider.FadeEnding(TimeSpan.FromMilliseconds(fadeDuration), sound.Duration);

                _soundsLastPlayed[sound] = DateTime.UtcNow;
                sound.IsPlaying = true;

                AddMixerInput(provider);
            }
        }

        private void AddMixerInput(ISampleProvider input)
        {
            if (input.WaveFormat.SampleRate == _outputDevice.OutputWaveFormat.SampleRate)
                _mixer.AddMixerInput(input);
        }

        private DelayFadeOutSampleProvider ConvertToRightChannelCount(ISampleProvider input)
        {
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

            return new DelayFadeOutSampleProvider(result);
        }

        public void StopSound(Sound sound)
        {
            if (!_soundsAndSampleProviders.TryGetValue(sound, out var soundSampleProviders)) return;

            foreach (var sampleProvider in soundSampleProviders)
            {
                _mixer.RemoveMixerInput(sampleProvider);
            }

            sound.IsPlaying = false;
            sound.Progress = 0;

            soundSampleProviders.Clear();
        }

        public void StopAllSounds()
        {
            foreach (var sound in _soundsAndSampleProviders.Keys)
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
                if (_soundsLastPlayed.Keys.Count > 0)
                {
                    foreach (var (sound, beginTime) in _soundsLastPlayed)
                    {
                        if (!sound.isPlaying) continue;

                        var elapsed = (DateTime.UtcNow - beginTime);
                        if (elapsed <= sound.duration)
                        {
                            sound.Progress = elapsed.TotalMilliseconds / sound.duration.TotalMilliseconds;
                        }
                        else
                        {
                            sound.Progress = 0;
                            sound.isPlaying = false;
                        }
                    }
                }

                if (_soundsLastPlayed.Count > 0)
                {
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

        private static AudioPlaybackEngine? _instance = null;


        public static AudioPlaybackEngine Instance
        {
            get
            {
                if (_instance is null)
                    Initialize();

                Debug.Assert(_instance != null, nameof(_instance) + " != null");

                return _instance;
            }
        }

        public static void Initialize(int sampleRate = (int)SampleRate.R48000, int channelCount = 2,
            DriverType outputType = DriverType.Wasapi, int latency = 20)
        {
            _instance = new AudioPlaybackEngine(sampleRate, channelCount, outputType, latency);
        }

        public static void Initialize(AudioDriverSettings audioDriverSettings)
        {
            _instance = new AudioPlaybackEngine(audioDriverSettings);
        }
    }
}