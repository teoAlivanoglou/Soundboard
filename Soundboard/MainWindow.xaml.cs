using Soundboard.ViewModels;
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
        }

        private void UniformGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (sender is not UniformGrid grid) return;
            if (grid.Children.Count == 0) return;

            ListViewItem? element0 = grid.Children[0] as ListViewItem;
            var templ = element0.Template;

        }

        private void WindowResized(object sender, SizeChangedEventArgs e)
        {
            var maxWidth = SoundboardButtons.ActualWidth;

            int i = 1;

            while (i * viewModel.MinButtonSize /* + (i-1) * viewModel.ExtraGap */ < maxWidth ) {
                i++;
            }

            viewModel.Columns = i - 1;
        }
    }
}