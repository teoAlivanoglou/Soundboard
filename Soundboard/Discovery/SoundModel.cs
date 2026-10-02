using CommunityToolkit.Mvvm.ComponentModel;
using NAudio.Wave;
using System.Windows.Media;

namespace Soundboard.Discovery;

public partial class SoundModel : ObservableObject
{
    [ObservableProperty] public partial string Name { get; set; }
    [ObservableProperty] public partial bool IsPlaying { get; set; }
    [ObservableProperty] public partial bool IsVisible { get; set; } = true;
    [ObservableProperty] public partial double Progress { get; set; }
    [ObservableProperty] public partial TimeSpan Duration { get; set; }

    public CategoryModel Category { get; init; }
    public string SubFolder { get; init; }
    public string FilePath { get; init; }
    public Brush BackgroundBrush => Category.BackgroundBrush;
    public float[]? AudioData { get; set; }
    public WaveFormat? WaveFormat { get; set; }

    public SoundModel(
        string name,
        string filePath,
        CategoryModel category,
        string subFolder,
        TimeSpan duration,
        float[]? audioData = null,
        WaveFormat? waveFormat = null)
    {
        Name = name;
        FilePath = filePath;
        Category = category;
        SubFolder = subFolder;
        Duration = duration;
        AudioData = audioData;
        WaveFormat = waveFormat;
    }

    public static SoundModel FromStream(
        string name,
        System.IO.Stream memoryStream,
        CategoryModel category,
        string subFolder = "")
    {
        var settings = new MediaFoundationReader.MediaFoundationReaderSettings { RequestFloatOutput = true, };

        using var reader = new StreamMediaFoundationReader(memoryStream, settings);
        var sampleProvider = reader.ToSampleProvider();
        var waveFormat = sampleProvider.WaveFormat;

        var duration = TimeSpan.FromSeconds(waveFormat.ExtraSize +
                                            (reader.Length / 4.0) /
                                            (double)(waveFormat.Channels * waveFormat.SampleRate));

        var samples = new List<float>((int)(reader.Length / 4));
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
            waveFormat: waveFormat);
    }
}