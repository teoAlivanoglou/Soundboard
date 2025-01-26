using System.ComponentModel;
using System.Diagnostics;
using Soundboard.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using Soundboard.Models;
using Soundboard.Settings;
using static Soundboard.ViewModels.SoundboardViewModel;


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
        SoundboardViewModel viewModel = new SoundboardViewModel();
        ScrollViewer? soundboardButtonsScrollViewer;


        public MainWindow()
        {
            InitializeComponent();
            DataContext = viewModel;

            var folder = OpenFolder();
            if (folder is not null)
                viewModel.ReadSounds(folder);

            soundboardButtonsScrollViewer = FindVisualChild<ScrollViewer>(SoundboardButtons);

            var settings = ApplicationSettingsManager.Settings;
        }

        private void WindowResized(object? sender, SizeChangedEventArgs? e)
        {
            var maxWidth = SoundboardButtons.ActualWidth;

            if (soundboardButtonsScrollViewer is null)
            {
                soundboardButtonsScrollViewer = FindVisualChild<ScrollViewer>(SoundboardButtons);

                if (soundboardButtonsScrollViewer is not null)

                    maxWidth -= soundboardButtonsScrollViewer.ComputedVerticalScrollBarVisibility == Visibility.Visible
                        ? SystemParameters.VerticalScrollBarWidth
                        : 0;
            }
            else
            {
                maxWidth -= soundboardButtonsScrollViewer.ComputedVerticalScrollBarVisibility == Visibility.Visible
                    ? SystemParameters.VerticalScrollBarWidth
                    : 0;
            }

            var i = 1;

            while (i * ApplicationSettingsManager.Settings.ApplicationUiSettings.MinButtonSize < maxWidth)
            {
                i++;
            }

            ApplicationSettingsManager.Settings.ApplicationUiSettings.ItemWidth =
                (int)(maxWidth / (i - 1))
                - 2 * ApplicationSettingsManager.Settings.ApplicationUiSettings.ButtonGap
                - 1;
        }

        private void ButtonSizeChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            WindowResized(null, null);
        }

        private void ButtonGapChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            WindowResized(null, null);
        }

        private void SoundButtonClicked(object sender, MouseButtonEventArgs e)
        {
            var buttonClicked = sender as FrameworkElement;
            var sound = buttonClicked.DataContext as Sound;

            if (e.ChangedButton == MouseButton.Left)
            {
                viewModel.PlaySound(sound);
            }
            else if (e.ChangedButton == MouseButton.Right)
            {
                viewModel.StopSound(sound);
            }
        }

        private void StopAllClicked(object sender, MouseButtonEventArgs e)
        {
            viewModel.StopAllSounds();
        }

        private void SettingsClicked(object sender, MouseButtonEventArgs e)
        {
            if (!e.RightButton.HasFlag(MouseButtonState.Pressed))
                return;

            viewModel.ToggleSettings();
        }

        private void TabButtonClick(object sender, MouseButtonEventArgs e)
        {
            var buttonClicked = sender as FrameworkElement;
            var filter = buttonClicked?.DataContext as CategoryFilter;

            Debug.Assert(filter != null, nameof(filter) + " != null");

            if (string.IsNullOrWhiteSpace(filter.category) || filter.Category == "All")
            {
                filter.Enabled = !filter.Enabled;
                foreach (var categoryFilter in viewModel.Categories)
                {
                    if (categoryFilter.Category != "All")
                        categoryFilter.Enabled = filter.Enabled;
                }
            }
            else
            {
                filter.Enabled = !filter.Enabled;
                var allEnabled = true;
                for (var i = 1; i < viewModel.Categories.Count; i++)
                {
                    allEnabled &= viewModel.Categories[i].Enabled;
                }

                viewModel.Categories[0].Enabled = allEnabled;
            }

            foreach (var sound in viewModel.SoundItems)
            {
                var enabled = viewModel.Categories.FirstOrDefault(c => c.Category == sound.Category)?.Enabled ?? true;
                sound.IsVisible = enabled;
            }
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

        private void BrowseFolderClicked(object sender, MouseButtonEventArgs e)
        {
            var folder = OpenFolder();
            if (folder is not null)
                viewModel.ReadSounds(folder);
        }

        private void RefreshClicked(object sender, MouseButtonEventArgs e)
        {
            viewModel.RefreshSounds();
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
            viewModel.ResetAudioDriver();
        }

        private void WindowClosing(object? sender, CancelEventArgs e)
        {
            ApplicationSettingsManager.SaveSettings();
        }

    }
}