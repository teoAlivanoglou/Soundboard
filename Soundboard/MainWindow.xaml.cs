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
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Soundboard.Models;
using static Soundboard.ViewModels.SoundboardViewModel;
using ListViewItem = System.Windows.Controls.ListViewItem;

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

            mainWindowStaticRef = this;


            var dialog = new FolderBrowserDialog();
            var res = dialog.ShowDialog();

            viewModel.ReadSounds(res == System.Windows.Forms.DialogResult.OK
                ? dialog.SelectedPath
                : @"C:\Users\teoal\Documents\Audacity");

            soundboardButtonsScrollViewer = FindVisualChild<ScrollViewer>(SoundboardButtons);
        }

        private void UniformGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (sender is not WrapPanel wrapPanel) return;
            if (wrapPanel.Children.Count == 0) return;

            ListViewItem? element0 = wrapPanel.Children[0] as ListViewItem;
            var templ = element0.Template;
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

            int i = 1;

            while (i * viewModel.MinButtonSize < maxWidth)
            {
                i++;
            }

            viewModel.Columns = i - 1;
            viewModel.ItemWidth = (int)(maxWidth / viewModel.Columns) - 2 * viewModel.ButtonGap - 1;
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
            }
            else
            {
                filter.Enabled = !filter.Enabled;
                bool allEnabled = true;
                for (var i = 1; i < viewModel.Categories.Count; i++)
                {
                    // if all are enabled set allEnabled to true,
                    // if all are disabled set allEnabled to false
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

        private childItem FindVisualChild<childItem>(DependencyObject obj)
            where childItem : DependencyObject

        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child != null && child is childItem)
                    return (childItem)child;
                else
                {
                    childItem childOfChild = FindVisualChild<childItem>(child);
                    if (childOfChild != null)
                        return childOfChild;
                }
            }

            return null;
        }
    }
}