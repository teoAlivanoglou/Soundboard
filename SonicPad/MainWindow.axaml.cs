using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Microsoft.Extensions.DependencyInjection;
using SonicPad.UI.Controls;
using SonicPad.ViewModels;

namespace SonicPad;

public partial class MainWindow : Window
{
    public MainWindow() : this(App.Host?.Services.GetService<SonicPadViewModel>())
    {
    }

    public MainWindow(SonicPadViewModel? viewModel)
    {
        InitializeComponent();
        var initialFont = new FontFamily("Segoe UI, SF Pro Text, -apple-system, Helvetica Neue, Inter, sans-serif");
        FontFamily = initialFont;
        FastSoundPad.SetGlobalFontFamily(initialFont);
        FastSoundPad.FontWeightDelta = 0;
        FastSoundPad.FontSizeDelta = 0;
        FastSoundPad.ShowHashPrefix = true;
        TextOptions.SetTextRenderingMode(this, TextRenderingMode.SubpixelAntialias);
        TextOptions.SetTextHintingMode(this, TextHintingMode.Strong);
        TextOptions.SetBaselinePixelAlignment(this, BaselinePixelAlignment.Aligned);
        UpdateFontStatusText("SystemStack");
        if (viewModel != null)
        {
            DataContext = viewModel;
            var savedSidebarWidth = Math.Clamp(viewModel.Settings.ApplicationUiSettings.SidebarWidth, 160, 500);
            MainWorkspaceGrid.ColumnDefinitions[0].Width = new GridLength(savedSidebarWidth);
        }

        Closed += (_, _) => viewModel?.CancelSoundLoading();

        SizeChanged += (_, e) =>
        {
            if (WindowState == WindowState.Normal && viewModel != null)
            {
                viewModel.Settings.ApplicationUiSettings.WindowWidth = (int)e.NewSize.Width;
                viewModel.Settings.ApplicationUiSettings.WindowHeight = (int)e.NewSize.Height;
            }
        };

        Closing += (_, _) =>
        {
            if (viewModel != null)
            {
                if (WindowState == WindowState.Normal)
                {
                    viewModel.Settings.ApplicationUiSettings.WindowWidth = (int)Bounds.Width;
                    viewModel.Settings.ApplicationUiSettings.WindowHeight = (int)Bounds.Height;
                }

                var sidebarCol = MainWorkspaceGrid.ColumnDefinitions[0];
                var currentSidebarWidth = sidebarCol.ActualWidth > 0 ? sidebarCol.ActualWidth : sidebarCol.Width.Value;
                viewModel.Settings.ApplicationUiSettings.SidebarWidth = (int)Math.Clamp(Math.Round(currentSidebarWidth), 160, 500);
                viewModel.Settings.Save();
            }
        };
    }

    public static async Task PickAndLoadFolderAsync(TopLevel topLevel, SonicPadViewModel viewModel,
        Action? firstChunkReady = null, Func<bool>? isActive = null)
    {
        var closed = false;
        var window = topLevel as Window;
        void OnClosed(object? sender, EventArgs e) => closed = true;
        if (window != null) window.Closed += OnClosed;
        bool CanApply() => !closed && (isActive?.Invoke() ?? true);

        void LoadWarning()
        {
            if (!CanApply()) return;
            viewModel.CancelSoundLoading();
            viewModel.StopAllSounds();
            viewModel.LoadDefaultSounds();
            viewModel.LoadingStatusText = "Please Select a Folder";
            firstChunkReady?.Invoke();
        }

        try
        {
            var storageProvider = topLevel.StorageProvider;
            if (!storageProvider.CanPickFolder)
            {
                LoadWarning();
                return;
            }

            var folders = await storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select Sounds Folder",
                AllowMultiple = false
            });
            if (!CanApply()) return;
            if (folders.Count == 0)
            {
                // Canceling a later Browse keeps the current library. Startup still needs a window.
                if (firstChunkReady != null) LoadWarning();
                return;
            }

            using var folder = folders[0];
            string? localPath = null;
            try
            {
                localPath = folder.TryGetLocalPath();
            }
            catch
            {
                // Some providers expose the local path only through a file URI.
            }

            if (string.IsNullOrWhiteSpace(localPath) && folder.Path is { IsAbsoluteUri: true, IsFile: true })
                localPath = folder.Path.LocalPath;

            if (string.IsNullOrWhiteSpace(localPath))
            {
                LoadWarning();
                return;
            }

            await viewModel.StreamSoundsAsync(localPath, firstChunkReady);
        }
        catch (OperationCanceledException)
        {
            // Shutdown or a newer load owns the outcome.
        }
        catch (Exception)
        {
            LoadWarning();
        }
        finally
        {
            if (window != null) window.Closed -= OnClosed;
        }
    }

    private string _currentFontTag = "SystemStack";

    private void OnFontFamilyChanged(object? sender, RoutedEventArgs e)
    {
        if (sender is RadioButton { IsChecked: true, Tag: string tag })
        {
            _currentFontTag = tag;
            FontFamily = tag switch
            {
                "SystemStack" => new FontFamily("Segoe UI, SF Pro Text, -apple-system, Helvetica Neue, Inter, sans-serif"),
                "SegoeUI" => new FontFamily("Segoe UI"),
                "IBMPlex" => new FontFamily("avares://SonicPad/UI/Fonts/IBMPlex#IBM Plex Sans"),
                "InterStatic" => new FontFamily("avares://SonicPad/UI/Fonts/InterStatic#Inter"),
                _ => FontFamily.Default
            };
            FastSoundPad.SetGlobalFontFamily(FontFamily);
            UpdateFontStatusText();
            InvalidateVisual();
        }
    }

    private void OnWeightOffsetChanged(object? sender, RoutedEventArgs e)
    {
        if (sender is RadioButton { IsChecked: true, Tag: string tag } &&
            int.TryParse(tag, out var delta))
        {
            FastSoundPad.FontWeightDelta = delta;
            var sidebarWeight = (FontWeight)Math.Clamp(400 + delta, 100, 900);
            if (Application.Current?.Resources is { } appRes)
            {
                appRes["SidebarCategoryFontWeight"] = sidebarWeight;
            }
            Resources["SidebarCategoryFontWeight"] = sidebarWeight;

            UpdateFontStatusText();
            InvalidateVisual();
        }
    }

    private void UpdateFontStatusText(string? fontTag = null)
    {
        if (fontTag != null) _currentFontTag = fontTag;
        if (FontStatusTextBlock != null)
        {
            var weightStr = FastSoundPad.FontWeightDelta switch
            {
                < 0 => $"{FastSoundPad.FontWeightDelta} (Light)",
                > 0 => $"+{FastSoundPad.FontWeightDelta} (Medium/Bold)",
                _ => "Default (400/600)"
            };
            FontStatusTextBlock.Text = $"Font: {_currentFontTag} | Weight: {weightStr} | Subpixel AA | Strong Hint | Aligned | tnum";
        }
    }
}