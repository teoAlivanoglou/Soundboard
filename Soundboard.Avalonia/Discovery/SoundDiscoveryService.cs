using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media;
using NAudio.SoundFile;
using Soundboard.Avalonia.AudioEngine;

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

        var rootScan = ScanDirectoryNode(rootPath, 0, maxDepth, cancellationToken);
        if (rootScan == null)
        {
            return new DiscoveryResult([], []);
        }

        var categories = new List<CategoryModel>();
        var allSounds = new List<SoundModel>();
        var colorIndex = 0;

        // Direct loose files in the selected root folder become a top-level (root) category
        if (rootScan.DirectSounds.Count > 0)
        {
            var rootColor = ColorPalette.GetColor(colorIndex++);
            var rootCategory = new CategoryModel("(root)", rootScan.DirectSounds.Count, rootColor)
            {
                DirectSoundCount = rootScan.DirectSounds.Count,
                RelativePath = ""
            };
            categories.Add(rootCategory);

            foreach (var raw in rootScan.DirectSounds)
            {
                allSounds.Add(new SoundModel(
                    name: raw.Name,
                    filePath: raw.FilePath,
                    category: rootCategory,
                    subFolder: "",
                    duration: raw.Duration,
                    audioData: raw.AudioData,
                    waveFormat: raw.WaveFormat,
                    index: allSounds.Count + 1));
            }
        }

        // Subdirectories of the root folder become top-level categories
        foreach (var subNode in rootScan.SubDirectories)
        {
            var category = BuildCategory(subNode, null, rootPath, ref colorIndex, allSounds);
            categories.Add(category);
        }

        return new DiscoveryResult(categories, allSounds);
    }

    public Task<DiscoveryResult> DiscoverSoundsAsync(string rootPath, int maxDepth = 6,
        CancellationToken cancellationToken = default) =>
        Task.Run(() => DiscoverSounds(rootPath, maxDepth, cancellationToken), cancellationToken);

    private DirectoryScanNode? ScanDirectoryNode(string currentPath, int depth, int maxDepth,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var directSounds = new List<RawSoundRecord>();

        IEnumerable<string> files;

        try
        {
            files = Directory.EnumerateFiles(currentPath, "*", SafeEnumOptions);
        }
        catch
        {
            return null;
        }

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var extension = Path.GetExtension(file);
            if (!ValidExtensions.Contains(extension)) continue;

            try
            {
                using var fileStream = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.Read);
                using var stream = VorbisWavHelper.WrapIfVorbisWav(fileStream);
                using var reader = new SoundFileReader(stream);
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

                    if (waveFormat is { SampleRate: > 0, Channels: > 0 })
                    {
                        duration = TimeSpan.FromSeconds((double)wholeFile.Count /
                                                        (waveFormat.Channels * waveFormat.SampleRate));
                    }
                }

                directSounds.Add(new RawSoundRecord(
                    Name: Path.GetFileNameWithoutExtension(file),
                    FilePath: file,
                    CategoryName: string.Empty,
                    SubFolder: string.Empty,
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
        var subNodes = new List<DirectoryScanNode>();
        if (depth < maxDepth)
        {
            IEnumerable<string> subDirectories;
            try
            {
                subDirectories = Directory.EnumerateDirectories(currentPath, "*", SafeEnumOptions);
            }
            catch
            {
                subDirectories = [];
            }

            foreach (var subDir in subDirectories)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var childNode = ScanDirectoryNode(subDir, depth + 1, maxDepth, cancellationToken);
                if (childNode != null)
                    subNodes.Add(childNode);
            }
        }

        // Prune empty
        if (directSounds.Count == 0 && subNodes.Count == 0)
        {
            return null;
        }

        return new DirectoryScanNode(
            currentPath,
            Path.GetFileName(currentPath),
            directSounds,
            subNodes);
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

    private record DirectoryScanNode(
        string Path,
        string Name,
        List<RawSoundRecord> DirectSounds,
        List<DirectoryScanNode> SubDirectories
    );

    private static int GetTotalSoundCount(DirectoryScanNode node) =>
        node.DirectSounds.Count + node.SubDirectories.Sum(GetTotalSoundCount);

    private static CategoryModel BuildCategory(
        DirectoryScanNode node,
        CategoryModel? parent,
        string rootPath,
        ref int colorIndex,
        List<SoundModel> allSounds)
    {
        // Compact linear chains of single-child folders with 0 direct sounds
        var prefix = string.Empty;
        var current = node;
        while (current.DirectSounds.Count == 0 && current.SubDirectories.Count == 1)
        {
            prefix += current.Name + " / ";
            current = current.SubDirectories[0];
        }

        Color? color = null;
        if (current.DirectSounds.Count > 0)
        {
            color = ColorPalette.GetColor(colorIndex++);
        }

        var totalCount = GetTotalSoundCount(current);
        var relPath = Path.GetRelativePath(rootPath, current.Path);

        var category = color.HasValue
            ? new CategoryModel(current.Name, totalCount, color.Value)
            : new CategoryModel(current.Name, totalCount);

        category.Parent = parent;
        category.Prefix = prefix;
        category.DirectSoundCount = current.DirectSounds.Count;
        category.RelativePath = relPath;

        // If directory has both direct sounds and subfolders, we need a logical (root) directory
        var soundTargetCategory = category;
        if (current.DirectSounds.Count > 0 && current.SubDirectories.Count > 0)
        {
            var rootChild = new CategoryModel("(root)", current.DirectSounds.Count, color!.Value)
            {
                Parent = category,
                DirectSoundCount = current.DirectSounds.Count,
                RelativePath = relPath
            };
            category.Children.Add(rootChild);
            soundTargetCategory = rootChild;
        }

        // Now sounds
        foreach (var raw in current.DirectSounds)
        {
            allSounds.Add(new SoundModel(
                name: raw.Name,
                filePath: raw.FilePath,
                category: soundTargetCategory,
                subFolder: relPath == "." ? "" : relPath,
                duration: raw.Duration,
                audioData: raw.AudioData,
                waveFormat: raw.WaveFormat,
                index: allSounds.Count + 1
            ){IsPlaying = false});
        }

        foreach (var sub in current.SubDirectories)
        {
            var childCat = BuildCategory(sub, category, rootPath, ref colorIndex, allSounds);
            category.Children.Add(childCat);
        }

        return category;
    }
}