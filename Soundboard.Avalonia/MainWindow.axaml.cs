using Avalonia.Controls;

namespace Soundboard.Avalonia;

public partial class MainWindow : Window
{
    public TestViewModel _vm = new();

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _vm;
    }
}