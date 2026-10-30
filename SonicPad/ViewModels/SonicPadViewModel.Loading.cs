using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using SonicPad.Discovery;

namespace SonicPad.ViewModels;

public partial class SonicPadViewModel
{
    [ObservableProperty] public partial bool IsLoadingSounds { get; set; }
    [ObservableProperty] public partial bool IsScanningSounds { get; set; }
    [ObservableProperty] public partial double LoadingProgress { get; set; }
    [ObservableProperty] public partial string LoadingStatusText { get; set; } = "Opening folder picker...";

    private CancellationTokenSource? _loadCancellation;
    private int _loadGeneration;
    private bool _enableNewCategories = true;
    private bool _expandNewCategories = true;

    public void CancelSoundLoading()
    {
        Dispatcher.UIThread.VerifyAccess();
        ++_loadGeneration;
        _loadCancellation?.Cancel();
        _loadCancellation = null;
        IsScanningSounds = false;
        IsLoadingSounds = false;
    }

    public async Task StreamSoundsAsync(string folderPath, Action? firstChunkReady = null)
    {
        Dispatcher.UIThread.VerifyAccess();
        CancelSoundLoading();
        var generation = _loadGeneration;
        using var cancellation = new CancellationTokenSource();
        _loadCancellation = cancellation;
        var token = cancellation.Token;
        _lastChosenDirectory = folderPath;
        RefreshSoundsCommand.NotifyCanExecuteChanged();
        StopAllSounds();
        Categories.Clear();
        SoundItems.Clear();
        VisibleSoundItems.Clear();
        LoadingProgress = 0;
        LoadingStatusText = "Scanning folders...";
        IsScanningSounds = true;
        IsLoadingSounds = true;
        _enableNewCategories = true;
        _expandNewCategories = true;
        var publishedCategories = new HashSet<CategoryModel>();
        var revealed = false;
        var width = 2;
        var clock = Stopwatch.StartNew();

        bool IsCurrent() => generation == _loadGeneration && !token.IsCancellationRequested;
        void Reveal()
        {
            if (revealed || !IsCurrent()) return;
            revealed = true;
            Debug.WriteLine($"[Discovery] First display ready after {clock.Elapsed.TotalMilliseconds:F1}ms");
            firstChunkReady?.Invoke();
        }

        void PublishCategory(CategoryModel category)
        {
            if (!publishedCategories.Add(category)) return;
            if (category.Parent is { } parent)
            {
                PublishCategory(parent);
                category.IsEnabled = parent.IsEnabled;
                category.IsExpanded = _expandNewCategories;
                parent.Children.Add(category);
            }
            else
            {
                category.IsEnabled = _enableNewCategories;
                category.IsExpanded = _expandNewCategories;
                Categories.Add(category);
            }
        }

        try
        {
            await _soundDiscoveryService.DiscoverSoundsStreamingAsync(folderPath, async update =>
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (!IsCurrent()) return;
                    if (update.Categories is not null)
                    {
                        // The manifest describes candidates, not verified sounds. Keep it
                        // detached from the sidebar until successful decoding validates a branch.
                        foreach (var category in update.Categories) PrepareCategory(category);
                        width = DisplayWidth(update.TotalCandidates);
                        IsScanningSounds = false;
                        Debug.WriteLine($"[Discovery] Structural scan: {clock.Elapsed.TotalMilliseconds:F1}ms, {update.TotalCandidates} candidates");
                    }
                    foreach (var sound in update.Sounds)
                    {
                        CountSuccessfulSound(sound.Category);
                        PublishCategory(sound.Category);
                        sound.IndexDisplayWidth = width;
                        sound.IsVisible = sound.Category.IsEnabled;
                        SoundItems.Add(sound);
                        if (sound.Category.IsEnabled) VisibleSoundItems.Add(sound);
                    }
                    LoadingProgress = update.TotalCandidates == 0 ? 1 :
                        (double)update.ProcessedCandidates / update.TotalCandidates;
                    LoadingStatusText = $"Loaded {update.SuccessfulCount} sounds — {update.ProcessedCandidates} of {update.TotalCandidates} files checked";
                    if (update.IsComplete)
                    {
                        if (update.SuccessfulCount == 0)
                        {
                            LoadDefaultSounds();
                            LoadingStatusText = "Please Select a Folder";
                        }
                        else
                        {
                            var finalWidth = DisplayWidth(update.SuccessfulCount);
                            foreach (var sound in SoundItems) sound.IndexDisplayWidth = finalWidth;
                            LoadingStatusText = $"Loaded {update.SuccessfulCount} sounds";
                        }
                        LoadingProgress = 1;
                    }
                    if (update.Sounds.Count > 0 || update.IsComplete) Reveal();
                });
            }, Settings.ApplicationUiSettings.MaxFolderLevel, token);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            // Replacement and shutdown own the next visible state.
        }
        catch (Exception exception)
        {
            Debug.WriteLine($"[Discovery] Loading failed: {exception}");
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (!IsCurrent()) return;
                StopAllSounds();
                LoadDefaultSounds();
                LoadingStatusText = "Please Select a Folder";
                Reveal();
            });
        }
        finally
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (generation != _loadGeneration) return;
                _loadCancellation = null;
                IsScanningSounds = false;
                IsLoadingSounds = false;
            });
        }
    }

    private static int DisplayWidth(int count) => Math.Max(2, count.ToString(CultureInfo.InvariantCulture).Length);

    private static void PrepareCategory(CategoryModel category)
    {
        // Parent links preserve the manifest ancestry after unpublished child lists
        // are detached; the producer has finished constructing them before publishing.
        foreach (var child in category.Children) PrepareCategory(child);
        category.Children.Clear();
        category.SoundCount = 0;
        category.DirectSoundCount = 0;
    }

    private static void CountSuccessfulSound(CategoryModel category)
    {
        category.DirectSoundCount++;
        if (category.IsRootLabel && category.Parent is { } parent) parent.DirectSoundCount++;
        for (CategoryModel? ancestor = category; ancestor != null; ancestor = ancestor.Parent)
            ancestor.SoundCount++;
    }

    public void SetAllCategoriesEnabled(bool enabled)
    {
        _enableNewCategories = enabled;
        foreach (var category in Categories) VisitCategory(category, item => item.IsEnabled = enabled);
        UpdateSoundVisibility();
    }

    public void SetAllCategoriesExpanded(bool expanded)
    {
        _expandNewCategories = expanded;
        foreach (var category in Categories) VisitCategory(category, item => item.IsExpanded = expanded);
    }

    private static void VisitCategory(CategoryModel category, Action<CategoryModel> action)
    {
        action(category);
        foreach (var child in category.Children) VisitCategory(child, action);
    }
}
