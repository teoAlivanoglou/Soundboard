using System;
using System.Collections.Generic;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using NAudio.Wave;

namespace Soundboard.Avalonia.Discovery;

public partial class SoundModel : ObservableObject
{
    [ObservableProperty] public partial string Name { get; set; }
    [ObservableProperty] public partial bool IsPlaying { get; set; }
    [ObservableProperty] public partial bool IsVisible { get; set; } = true;
    [ObservableProperty] public partial double Progress { get; set; }
    [ObservableProperty] public partial TimeSpan Duration { get; set; }

    [ObservableProperty] public partial int Index { get; set; } = 1;

    public CategoryModel Category { get; init; }
    public string SubFolder { get; init; }
    public string FilePath { get; init; }
    public IBrush BackgroundBrush => Category.BackgroundBrush;
    public float[]? AudioData { get; set; }
    public WaveFormat? WaveFormat { get; set; }

    public SoundModel(
        string name,
        string filePath,
        CategoryModel category,
        string subFolder,
        TimeSpan duration,
        float[]? audioData = null,
        WaveFormat? waveFormat = null,
        int index = 1)
    {
        Name = name;
        FilePath = filePath;
        Category = category;
        SubFolder = subFolder;
        Duration = duration;
        AudioData = audioData;
        WaveFormat = waveFormat;
        Index = index;
    }

    /// <summary>
    /// Design-time constructor with sample sound data.
    /// </summary>
    public SoundModel() : this(
        name: "Cleanest 808",
        filePath: string.Empty,
        category: new CategoryModel(),
        subFolder: string.Empty,
        duration: TimeSpan.FromSeconds(2.4),
        index: 1)
    {
        IsPlaying = true;
        Progress = 0.35;
    }

    public static SoundModel FromStream(
        string name,
        System.IO.Stream memoryStream,
        CategoryModel category,
        string subFolder = "",
        int index = 1)
    {
        using var reader = new NAudio.SoundFile.SoundFileReader(memoryStream);
        var sampleProvider = reader.ToSampleProvider();
        var waveFormat = sampleProvider.WaveFormat;

        var duration = reader.TotalTime;
        var samples = new List<float>((int)(reader.Length / sizeof(float)));
        var buffer = new float[16384];
        int read;

        while ((read = sampleProvider.Read(buffer.AsSpan())) > 0)
        {
            samples.AddRange(buffer.AsSpan(0, read));
        }

        return new SoundModel(
            name: name,
            filePath: string.Empty,
            category: category,
            subFolder: subFolder,
            duration: duration,
            audioData: samples.ToArray(),
            waveFormat: waveFormat,
            index: index);
    }
}