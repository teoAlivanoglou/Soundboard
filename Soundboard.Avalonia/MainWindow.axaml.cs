using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Microsoft.Extensions.DependencyInjection;
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
        if (viewModel != null)
        {
            DataContext = viewModel;
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
}