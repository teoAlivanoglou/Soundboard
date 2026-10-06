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
    private static readonly HashSet<string> ValidExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp3", ".wav", ".ogg", ".flac"
    };

    private static readonly EnumerationOptions SafeEnumOptions = new()
    {
        IgnoreInaccessible = true,
        AttributesToSkip = FileAttributes.ReparsePoint | FileAttributes.System,
        RecurseSubdirectories = false
    };

    public DiscoveryResult DiscoverSounds(string rootPath, int maxDepth = 2,
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

        var categories = new List<Avalonia.Discovery.CategoryModel>();
        var allSounds = new List<SoundModel>();
        var categoryIndex = 0;

        foreach (var group in categoryGroups)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var categoryBrush = ColorPalette.GetBrush(categoryIndex++);
            var categoryModel =
                new Avalonia.Discovery.CategoryModel(name: group.Key, soundCount: group.Count(), backgroundBrush: categoryBrush);

            categories.Add(categoryModel);

            allSounds.AddRange(group
                .Select(rawSound => new SoundModel(
                    name: rawSound.Name,
                    filePath: rawSound.FilePath,
                    category: categoryModel,
                    subFolder: rawSound.SubFolder,
                    duration: rawSound.Duration,
                    audioData: rawSound.AudioData,
                    waveFormat: rawSound.WaveFormat)
                )
            );
        }

        categories.Insert(0,
            new Avalonia.Discovery.CategoryModel(name: "All", soundCount: allSounds.Count, backgroundBrush: ColorPalette.AllButtonBrush,
                isAll: true));

        return new DiscoveryResult(categories, allSounds);
    }

    public Task<DiscoveryResult> DiscoverSoundsAsync(string rootPath, int maxDepth = 2,
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

                // Should be more accurate than using AudioFileReader.TotalTime
                var duration = TimeSpan.FromSeconds(
                    waveFormat.ExtraSize +
                    (int)reader.Length / sizeof(float) / (waveFormat.Channels * waveFormat.SampleRate));

                var wholeFile = new List<float>((int)(reader.Length / sizeof(float)));
                var readBuffer = new float[waveFormat.SampleRate * waveFormat.Channels]; // 1 second buffer
                int samplesRead;
                while ((samplesRead = reader.Read(readBuffer.AsSpan())) > 0)
                {
                    wholeFile.AddRange(readBuffer.AsSpan(0, samplesRead));
                }

                soundAccumulator.Add(new RawSoundRecord(
                    Name: Path.GetFileNameWithoutExtension(file),
                    FilePath: file,
                    CategoryName: currentCategoryName,
                    SubFolder: relativeFolder == "." ? "" : relativeFolder,
                    Duration: duration,
                    AudioData: [.. wholeFile],
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
        float[] AudioData,
        NAudio.Wave.WaveFormat WaveFormat
    );
}