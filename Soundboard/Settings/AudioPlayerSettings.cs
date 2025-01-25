using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using Soundboard.AudioEngine;
using Soundboard.Settings;

namespace Soundboard.Settings;

public partial class AudioPlayerSettings : ObservableObject
{
    // ReSharper disable InconsistentNaming
    [ObservableProperty] private SampleRate sampleRate = SampleRate.R48000;
    [ObservableProperty] private DriverType driverType = DriverType.Wasapi;
    [ObservableProperty] private double latency = 20;
    [ObservableProperty] private int fadeInTime = 200;
    // ReSharper restore InconsistentNaming

}