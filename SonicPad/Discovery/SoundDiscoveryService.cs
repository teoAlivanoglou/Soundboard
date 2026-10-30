using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media;
using NAudio.SoundFile;
using SonicPad.AudioEngine;

namespace SonicPad.Discovery;

public class SoundDiscoveryService
{
    /// <summary>Decode full audio into memory when enabled; otherwise resolve metadata only.</summary>
    public static bool EnableAudioCaching { get; set; }

    private const int ChunkSize = 30;
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
        IReadOnlyList<CategoryModel> categories = [];
        var sounds = new List<SoundModel>();
        DiscoverSoundsStreamingAsync(rootPath, update => {
            if (update.Categories is not null) categories = update.Categories;
            sounds.AddRange(update.Sounds);
            return Task.CompletedTask;
        }, maxDepth, cancellationToken).GetAwaiter().GetResult();

        // This batch API has not published its models to a UI and can reconcile them here.
        int Reconcile(CategoryModel category)
        {
            var ownCount = sounds.Count(sound => ReferenceEquals(sound.Category, category));
            category.SoundCount = ownCount + category.Children.Sum(Reconcile);
            category.DirectSoundCount = ownCount + category.Children.Where(child => child.IsRootLabel)
                .Sum(child => child.SoundCount);
            foreach (var empty in category.Children.Where(child => child.SoundCount == 0).ToArray())
                category.Children.Remove(empty);
            return category.SoundCount;
        }

        foreach (var category in categories) Reconcile(category);
        return new DiscoveryResult(categories.Where(category => category.SoundCount > 0).ToArray(), sounds);
    }

    public Task<DiscoveryResult> DiscoverSoundsAsync(string rootPath, int maxDepth = 6,
        CancellationToken cancellationToken = default) =>
        Task.Run(() => DiscoverSounds(rootPath, maxDepth, cancellationToken), cancellationToken);

    /// <summary>
    /// Scans and decodes off-thread, awaiting every publication to bound pending work.
    /// Category models are never mutated by the producer after initialization is published.
    /// </summary>
    public Task DiscoverSoundsStreamingAsync(string rootPath, Func<DiscoveryUpdate, Task> publish,
        int maxDepth = 6, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(publish);
        return Task.Run(async () => {
            cancellationToken.ThrowIfCancellationRequested();
            var root = string.IsNullOrWhiteSpace(rootPath) || !Directory.Exists(rootPath)
                ? null : ScanDirectoryNode(rootPath, 0, maxDepth, cancellationToken);
            var categories = new List<CategoryModel>();
            var candidates = new List<SoundCandidate>();
            var colorIndex = 0;
            if (root is not null)
            {
                if (root.DirectSounds.Count > 0)
                {
                    var category = new CategoryModel("(root)", root.DirectSounds.Count, ColorPalette.GetColor(colorIndex++))
                    {
                        DirectSoundCount = root.DirectSounds.Count,
                        RelativePath = ""
                    };
                    categories.Add(category);
                    candidates.AddRange(root.DirectSounds.Select(file => CreateCandidate(file, category, rootPath)));
                }
                foreach (var child in root.SubDirectories)
                    categories.Add(BuildCategory(child, null, rootPath, ref colorIndex, candidates));
            }

            await publish(new DiscoveryUpdate(categories, candidates.Count, [], 0, 0, false)).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            var processed = 0;
            var successful = 0;
            var chunk = new List<SoundModel>(ChunkSize);
            var cacheAudio = EnableAudioCaching;
            foreach (var candidate in candidates)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var sound = ResolveSound(candidate, successful + 1, cacheAudio, cancellationToken);
                if (sound is not null)
                {
                    chunk.Add(sound);
                    successful++;
                }
                processed++;
                if (processed % ChunkSize == 0)
                {
                    await publish(new DiscoveryUpdate(null, candidates.Count, chunk.ToArray(), processed, successful, false))
                        .ConfigureAwait(false);
                    cancellationToken.ThrowIfCancellationRequested();
                    chunk.Clear();
                }
            }
            if (processed % ChunkSize != 0)
            {
                await publish(new DiscoveryUpdate(null, candidates.Count, chunk.ToArray(), processed, successful, false))
                    .ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
            }
            await publish(new DiscoveryUpdate(null, candidates.Count, [], processed, successful, true)).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
        }, cancellationToken);
    }

    private static SoundModel? ResolveSound(SoundCandidate candidate, int index, bool cacheAudio,
        CancellationToken cancellationToken)
    {
        try
        {
            using var fileStream = File.Open(candidate.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var stream = VorbisWavHelper.WrapIfVorbisWav(fileStream);
            using var reader = new SoundFileReader(stream);
            var waveFormat = reader.WaveFormat;
            var duration = reader.TotalTime;
            float[]? audioData = null;
            if (cacheAudio)
            {
                var samples = new List<float>();
                var buffer = new float[waveFormat.SampleRate * waveFormat.Channels];
                int count;
                while ((count = reader.Read(buffer.AsSpan())) > 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    samples.AddRange(buffer.AsSpan(0, count));
                }
                audioData = samples.ToArray();
                if (waveFormat is { SampleRate: > 0, Channels: > 0 })
                    duration = TimeSpan.FromSeconds((double)samples.Count / (waveFormat.SampleRate * waveFormat.Channels));
            }
            cancellationToken.ThrowIfCancellationRequested();
            return new SoundModel(Path.GetFileNameWithoutExtension(candidate.FilePath), candidate.FilePath,
                candidate.Category, candidate.SubFolder, duration, audioData, waveFormat, index);
        }
        catch (OperationCanceledException) { throw; }
        catch
        {
            // A corrupt, missing, or unreadable candidate does not consume a successful index.
            return null;
        }
    }

    private static DirectoryScanNode? ScanDirectoryNode(string currentPath, int depth, int maxDepth,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var files = EnumerateSorted(currentPath, directories: false, cancellationToken)
            .Where(file => ValidExtensions.Contains(Path.GetExtension(file))).ToList();
        var children = new List<DirectoryScanNode>();
        foreach (var directory in EnumerateSorted(currentPath, directories: true, cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var child = ScanDirectoryNode(directory, depth + 1, maxDepth, cancellationToken);
            if (child is null) continue;

            // Depth limits the category hierarchy, not which sounds are discovered.
            // Beyond the boundary each child has already flattened its descendants.
            if (depth < maxDepth) children.Add(child);
            else files.AddRange(child.DirectSounds);
        }
        cancellationToken.ThrowIfCancellationRequested();
        return files.Count == 0 && children.Count == 0 ? null
            : new DirectoryScanNode(currentPath, Path.GetFileName(currentPath), files, children);
    }

    private static string[] EnumerateSorted(string path, bool directories, CancellationToken cancellationToken)
    {
        try
        {
            // Materialize here: lazy enumeration can fail during iteration, not just creation.
            var entries = directories
                ? Directory.EnumerateDirectories(path, "*", SafeEnumOptions)
                : Directory.EnumerateFiles(path, "*", SafeEnumOptions);
            var materialized = new List<string>();
            foreach (var entry in entries)
            {
                cancellationToken.ThrowIfCancellationRequested();
                materialized.Add(entry);
            }
            return materialized.OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(Path.GetFileName, StringComparer.Ordinal).ToArray();
        }
        catch (IOException) { return []; }
        catch (UnauthorizedAccessException) { return []; }
    }

    private static SoundCandidate CreateCandidate(string filePath, CategoryModel category, string rootPath)
    {
        var subFolder = Path.GetRelativePath(rootPath, Path.GetDirectoryName(filePath)!);
        return new SoundCandidate(filePath, category, subFolder == "." ? "" : subFolder);
    }

    private sealed record SoundCandidate(string FilePath, CategoryModel Category, string SubFolder);
    private sealed record DirectoryScanNode(
        string Path,
        string Name,
        List<string> DirectSounds,
        List<DirectoryScanNode> SubDirectories);

    private static int GetTotalSoundCount(DirectoryScanNode node) =>
        node.DirectSounds.Count + node.SubDirectories.Sum(GetTotalSoundCount);

    private static CategoryModel BuildCategory(
        DirectoryScanNode node,
        CategoryModel? parent,
        string rootPath,
        ref int colorIndex,
        List<SoundCandidate> candidates)
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
        foreach (var file in current.DirectSounds)
        {
            candidates.Add(CreateCandidate(file, soundTargetCategory, rootPath));
        }

        foreach (var sub in current.SubDirectories)
        {
            var childCat = BuildCategory(sub, category, rootPath, ref colorIndex, candidates);
            category.Children.Add(childCat);
        }

        return category;
    }
}