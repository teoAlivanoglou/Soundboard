using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NAudio.SoundFile;

namespace Soundboard.Avalonia.Discovery;

public class SoundDiscoveryService
{
    /// <summary>
    /// When true, audio files are fully decoded into memory as float[] arrays for playback.
    /// When false, audio is streamed on-demand from disk with near-zero RAM usage.
    /// </summary>
    public static bool EnableAudioCaching { get; set; } = false;

    private static readonly HashSet<string> ValidExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp3",
        ".wav",
        ".ogg",
        ".flac"
    };

    private static readonly EnumerationOptions SafeEnumOptions = new()
    {
        IgnoreInaccessible = true,
        AttributesToSkip = FileAttributes.ReparsePoint | FileAttributes.System,
        RecurseSubdirectories = false
    };

    public DiscoveryResult DiscoverSounds(string rootPath, int maxDepth = 6,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rootPath) || !Directory.Exists(rootPath))
        {
            return new DiscoveryResult([], []);
        }

        var rootDirName =
            Path.GetFileName(rootPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        if (string.IsNullOrWhiteSpace(rootDirName))
        {
            rootDirName = "Root";
        }

        // Intermediate item to hold sound metadata before category models are created
        var rawSounds = new List<RawSoundRecord>();

        ScanDirectory(rootPath, rootPath, rootDirName, 0, maxDepth, rawSounds, cancellationToken);
        var categoryGroups = rawSounds
            .GroupBy(s => s.CategoryName)
            .Where(g => g.Any())
            .ToList();

        var categories = new List<CategoryModel>();
        var allSounds = new List<SoundModel>();
        var categoryIndex = 0;

        foreach (var group in categoryGroups)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var categoryColor = ColorPalette.GetColor(categoryIndex++);
            var categoryModel =
                new CategoryModel(name: group.Key, soundCount: group.Count(), color: categoryColor);

            categories.Add(categoryModel);

            foreach (var rawSound in group)
            {
                allSounds.Add(new SoundModel(
                    name: rawSound.Name,
                    filePath: rawSound.FilePath,
                    category: categoryModel,
                    subFolder: rawSound.SubFolder,
                    duration: rawSound.Duration,
                    audioData: rawSound.AudioData,
                    waveFormat: rawSound.WaveFormat,
                    index: allSounds.Count + 1));
            }
        }

        categories.Insert(0, CategoryModel.GetAll(allSounds.Count));

        return new DiscoveryResult(categories, allSounds);
    }

    public Task<DiscoveryResult> DiscoverSoundsAsync(string rootPath, int maxDepth = 6,
        CancellationToken cancellationToken = default) =>
        Task.Run(() => DiscoverSounds(rootPath, maxDepth, cancellationToken), cancellationToken);


    private void ScanDirectory(
        string rootPath,
        string currentPath,
        string currentCategoryName,
        int depth,
        int maxDepth,
        List<RawSoundRecord> soundAccumulator,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IEnumerable<string> files;

        try
        {
            files = Directory.EnumerateFiles(currentPath, "*", SafeEnumOptions);
        }
        catch
        {
            return;
        }

        var relativeFolder = Path.GetRelativePath(rootPath, currentPath);

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var extension = Path.GetExtension(file);
            if (!ValidExtensions.Contains(extension))
                continue;

            try
            {
                using var reader = new SoundFileReader(file);
                var waveFormat = reader.WaveFormat;
                var duration = reader.TotalTime;

                float[]? audioData = null;
                if (EnableAudioCaching)
                {
                    var wholeFile = new List<float>((int)(reader.Length / sizeof(float)));
                    var readBuffer = new float[waveFormat.SampleRate * waveFormat.Channels]; // 1 second buffer
                    int samplesRead;
                    while ((samplesRead = reader.Read(readBuffer.AsSpan())) > 0)
                    {
                        wholeFile.AddRange(readBuffer.AsSpan(0, samplesRead));
                    }
                    audioData = [.. wholeFile];

                    if (waveFormat.SampleRate > 0 && waveFormat.Channels > 0)
                    {
                        duration = TimeSpan.FromSeconds((double)wholeFile.Count / (waveFormat.Channels * waveFormat.SampleRate));
                    }
                }

                soundAccumulator.Add(new RawSoundRecord(
                    Name: Path.GetFileNameWithoutExtension(file),
                    FilePath: file,
                    CategoryName: currentCategoryName,
                    SubFolder: relativeFolder == "." ? "" : relativeFolder,
                    Duration: duration,
                    AudioData: audioData,
                    WaveFormat: waveFormat
                ));
            }
            catch
            {
                // Discard. Corrupted or unreadable sound file
                continue;
            }
        }


        // Subdirectories
        IEnumerable<string> subDirectories;
        try
        {
            subDirectories = Directory.EnumerateDirectories(currentPath, "*", SafeEnumOptions);
        }
        catch
        {
            return;
        }

        foreach (var subDir in subDirectories)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var relSubPath = Path.GetRelativePath(rootPath, subDir);

            // Clamp to max depth
            var nextCategory = (depth < maxDepth)
                ? relSubPath
                : currentCategoryName;

            ScanDirectory(rootPath, subDir, nextCategory, depth + 1, maxDepth, soundAccumulator, cancellationToken);
        }
    }


    private record RawSoundRecord(
        string Name,
        string FilePath,
        string CategoryName,
        string SubFolder,
        TimeSpan Duration,
        float[]? AudioData,
        NAudio.Wave.WaveFormat WaveFormat
    );
}