using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using Soundboard.AudioEngine;

namespace Soundboard.Utils
{
    public partial class AudioDriverSettings(
        SampleRate sampleRate = SampleRate.R48000,
        DriverType driverType = DriverType.Wasapi,
        double latency = 20) : ObservableObject
    {
        // ReSharper disable InconsistentNaming
        [ObservableProperty] public SampleRate sampleRate = sampleRate;
        [ObservableProperty] public DriverType driverType = driverType;
        [ObservableProperty] public double latency = latency;
        // ReSharper restore InconsistentNaming

        public IWavePlayer CreatePlayer()
        {
            return driverType switch
            {
                DriverType.WaveOutEvent => new WaveOutEvent() { DesiredLatency = (int)latency },
                DriverType.Wasapi => new WasapiOut(AudioClientShareMode.Shared, true, (int)latency),
                DriverType.DirectSound => new DirectSoundOut((int)latency),
                _ => throw new ArgumentOutOfRangeException(nameof(driverType), driverType, null)
            };
        }

        // public static IWavePlayer CreatePlayer(AudioDriverSettings audioDriverSettings)
        // {
        //     return audioDriverSettings.CreatePlayer();
        // }
    }
}