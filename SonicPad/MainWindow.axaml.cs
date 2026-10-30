using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Microsoft.Extensions.DependencyInjection;
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
        TextOptions.SetTextRenderingMode(this, TextRenderingMode.SubpixelAntialias);
        TextOptions.SetTextHintingMode(this, TextHintingMode.Strong);
        TextOptions.SetBaselinePixelAlignment(this, BaselinePixelAlignment.Aligned);
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
}
