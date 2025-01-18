using System.Collections.ObjectModel;
using System.Diagnostics;
using Soundboard.ViewModels;
using System.Media;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Soundboard.Models;
using static Soundboard.ViewModels.SoundboardViewModel;

namespace Soundboard
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        SoundboardViewModel viewModel = new SoundboardViewModel();

        public MainWindow()
        {
            InitializeComponent();
            DataContext = viewModel;

            mainWindowStaticRef = this;

            viewModel.ReadSounds("C:\\Users\\teoal\\Documents\\Audacity");
        }

        private void UniformGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (sender is not UniformGrid grid) return;
            if (grid.Children.Count == 0) return;

            ListViewItem? element0 = grid.Children[0] as ListViewItem;
            var templ = element0.Template;
        }

        private void WindowResized(object? sender, SizeChangedEventArgs? e)
        {
            var maxWidth = SoundboardButtons.ActualWidth - 10;
            var maxCols = SoundboardButtons.Items.Count;

            int i = 1;

            while (i * viewModel.MinButtonSize /* + (i-1) * viewModel.ExtraGap */ < maxWidth && i <= (maxCols + 1))
            {
                i++;
            }

            viewModel.Columns = i - 1;
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

            viewModel.PlaySound(sound);
        }

        private void StopAllClicked(object sender, MouseButtonEventArgs e)
        {
            viewModel.StopAllSounds();
        }

        private void SettingsClicked(object sender, MouseButtonEventArgs e)
        {
            if (!e.RightButton.HasFlag(MouseButtonState.Pressed))
                return;

            viewModel.SettingsVisible = !viewModel.SettingsVisible;
        }

        public static MainWindow mainWindowStaticRef;

        public static void SetTitle(string s)
        {
            mainWindowStaticRef.Dispatcher.Invoke(() => mainWindowStaticRef.Title = s);
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
                if (filter.Enabled)
                {
                    viewModel.VisibleSoundItems.Clear();
                    for (int i = 0; i < viewModel.SoundItems.Count; i++)
                    {
                        viewModel.VisibleSoundItems.Add(viewModel.SoundItems[i]);
                    }
                }
            }


            return;
            filter!.Enabled = !filter.Enabled;

            foreach (var sound in viewModel.SoundItems)
            {
                if (filter.Category == "All")
                    sound.IsVisible = filter.Enabled;
                else
                {
                    if (sound.Category == filter.Category)
                    {
                        sound.IsVisible = filter.Enabled;
                    }
                    else
                    {
                        sound.IsVisible = !filter.Enabled;
                    }
                }

                if (!sound.IsVisible)
                {
                    if (viewModel.VisibleSoundItems.Contains(sound))
                        viewModel.VisibleSoundItems.Remove(sound);
                }
                else
                {
                    if (!viewModel.VisibleSoundItems.Contains(sound))
                        viewModel.VisibleSoundItems.Add(sound);
                }
            }
        }
    }
}