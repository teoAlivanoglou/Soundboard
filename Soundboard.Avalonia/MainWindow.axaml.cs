using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Microsoft.Extensions.DependencyInjection;
using Soundboard.Avalonia.UI.Controls;
using Soundboard.Avalonia.ViewModels;

namespace Soundboard.Avalonia;

public partial class MainWindow : Window
{
    public MainWindow() : this(App.Host?.Services.GetService<SoundboardViewModel>())
    {
    }

    public MainWindow(SoundboardViewModel? viewModel)
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

        Opacity = 0;

        Opened += async (_, _) =>
        {
            try
            {
                if (viewModel != null)
                {
                    await PickAndLoadFolderAsync(this, viewModel);
                }
            }
            finally
            {
                Opacity = 1;
            }
        };

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

    public static async Task PickAndLoadFolderAsync(TopLevel topLevel, SoundboardViewModel viewModel)
    {
        var storageProvider = topLevel.StorageProvider;
        if (!storageProvider.CanPickFolder) return;

        var folders = await storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select Sounds Folder",
            AllowMultiple = false
        });

        if (folders.Count > 0)
        {
            var folder = folders[0];
            string? localPath = null;
            try
            {
                localPath = folder.TryGetLocalPath();
            }
            catch
            {
                // Fallback to Uri local path
            }

            if (string.IsNullOrWhiteSpace(localPath) && folder.Path != null)
            {
                localPath = folder.Path.IsAbsoluteUri ? folder.Path.LocalPath : folder.Path.ToString();
            }

            if (!string.IsNullOrWhiteSpace(localPath))
            {
                await viewModel.ReadSoundsAsync(localPath);
            }
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
                "IBMPlex" => new FontFamily("avares://Soundboard.Avalonia/UI/Fonts/IBMPlex#IBM Plex Sans"),
                "InterStatic" => new FontFamily("avares://Soundboard.Avalonia/UI/Fonts/InterStatic#Inter"),
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