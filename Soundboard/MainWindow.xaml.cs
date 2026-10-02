using System.ComponentModel;
using System.Diagnostics;
using Soundboard.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using Soundboard.Settings;
using static Soundboard.ViewModels.SoundboardViewModel;
using System.IO;
using Soundboard.Discovery;


// TODO: Massive refactoring needed, split shit up boy!
// TODO: Cleanup xaml files, extract styles etc.
// TODO: Actually get v1.0 out
// TODO: Maybe get the source code of the libs used and integrate them into my code in order to reduce size by removing redundancies.
//      -- I can either import them as separate projects to get separate DLLS just copy the code I need into my project for a monolithic application
//      -- Negligibly faster compilation vs negligibly better program performance and smaller size
//      -- We'll see


namespace Soundboard
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly SoundboardViewModel _viewModel;
        private readonly SettingsService _settings;
        ScrollViewer? soundboardButtonsScrollViewer;

        public MainWindow(SoundboardViewModel viewModel, SettingsService settings)
        {
            _viewModel = viewModel;
            _settings = settings;
            DataContext = _viewModel;

            InitializeComponent();


            var folder = OpenFolder();
            if (folder is not null)
                _viewModel.ReadSounds(folder);

            soundboardButtonsScrollViewer = FindVisualChild<ScrollViewer>(SoundboardButtonsPanel);
        }

        private void WindowResized(object? sender, SizeChangedEventArgs? e)
        {
            if (e is not null)
            {
               _settings.ApplicationUiSettings.WindowWidth = (int)e.NewSize.Width;
               _settings.ApplicationUiSettings.WindowHeight = (int)e.NewSize.Height;
            }
        }

        private void SoundButtonClicked(object sender, MouseButtonEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is not SoundModel sound) return;

            switch (e.ChangedButton)
            {
                case MouseButton.Left:
                    _viewModel.PlaySound(sound);
                    break;
                case MouseButton.Right:
                    _viewModel.StopSound(sound);
                    break;
            }
        }

        private void StopAllClicked(object sender, MouseButtonEventArgs e)
        {
            _viewModel.StopAllSounds();
        }

        private void SettingsClicked(object sender, MouseButtonEventArgs e)
        {
            _viewModel.ToggleSettings();
        }

        private void TabButtonClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is not FrameworkElement { DataContext: CategoryModel filter })
                return;

            if (filter.IsAll)
            {
                var newState = !filter.IsEnabled;
                foreach (var cat in _viewModel.Categories)
                {
                    cat.IsEnabled = newState;
                }
            }
            else
            {
                filter.IsEnabled = !filter.IsEnabled;
                var allCategory = _viewModel.Categories.FirstOrDefault(c => c.IsAll);
                if (allCategory != null)
                {
                    allCategory.IsEnabled = _viewModel.Categories
                        .Where(c => !c.IsAll)
                        .All(c => c.IsEnabled);
                }
            }

            _viewModel.UpdateSoundVisibility();
        }


        private static TChildItem? FindVisualChild<TChildItem>(DependencyObject obj)
            where TChildItem : DependencyObject

        {
            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                var child = VisualTreeHelper.GetChild(obj, i);
                if (child is TChildItem item)
                    return item;
                else
                {
                    var childOfChild = FindVisualChild<TChildItem>(child);
                    if (childOfChild != null)
                        return childOfChild;
                }
            }

            return null;
        }

        private async void BrowseFolderClicked(object sender, MouseButtonEventArgs e)
        {
            var folder = OpenFolder();
            if (folder is not null)
                await _viewModel.ReadSoundsAsync(folder);
        }

        private async void RefreshClicked(object sender, MouseButtonEventArgs e)
        {
            await _viewModel.RefreshSoundsAsync();
        }


        public string? OpenFolder()
        {
            try
            {
                var dialog = new FolderBrowserDialog();
                var res = dialog.ShowDialog();


                return res == System.Windows.Forms.DialogResult.OK
                    ? dialog.SelectedPath
                    : null;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private void ResetAudioDriverButtonClicked(object sender, RoutedEventArgs e)
        {
            _viewModel.ResetAudioDriver();
        }

        private void WindowClosing(object? sender, CancelEventArgs e)
        {
            _settings.Save();
        }


        protected byte[] GenerateCode(string inputFileName, string inputFileContent)
        {
            string fxcPath = @"C:\Program Files (x86)\Windows Kits\10\bin\10.0.22621.0\x64\fxc.exe";

            Process cmdProcess = new Process();

            ProcessStartInfo cmdStartInfo = new ProcessStartInfo
            {
                FileName = fxcPath,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                RedirectStandardInput = false,
                UseShellExecute = false,
                CreateNoWindow = true,
                // Arguments = $"/T ps_2_0 /Fo CON \"{inputFileName}\""
                Arguments = $"/T ps_2_0 /Fo temp.bin \"{inputFileName}\""
            };


            cmdProcess.StartInfo = cmdStartInfo;
            cmdProcess.Start();

            cmdProcess.WaitForExit();

            // var bbbb = File.ReadAllBytes(@"\\.\CON");


            var b = File.ReadAllBytes("temp.bin");
            File.Delete("temp.bin");
            return b;
        }
    }
}