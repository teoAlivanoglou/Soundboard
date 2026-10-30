using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Styling;
using Microsoft.Extensions.DependencyInjection;
using SonicPad.ViewModels;

namespace SonicPad.UI.Views
{
    public partial class BottomBarView : UserControl
    {
        public BottomBarView()
        {
            InitializeComponent();
        }

        private void OnStopAllClick(object? sender, RoutedEventArgs e)
        {
            var vm = (this.VisualRoot as Control)?.DataContext as SonicPadViewModel
                     ?? App.Host?.Services.GetService<SonicPadViewModel>();
            vm?.StopAllSounds();
        }

        private async void OnBrowseFolderClick(object? sender, RoutedEventArgs e)
        {
            var topLevel = TopLevel.GetTopLevel(this) ?? (this.VisualRoot as TopLevel);
            if (topLevel != null)
            {
                var vm = (this.DataContext as SonicPadViewModel)
                         ?? (topLevel.DataContext as SonicPadViewModel)
                         ?? App.Host?.Services.GetService<SonicPadViewModel>();
                if (vm != null)
                {
                    await MainWindow.PickAndLoadFolderAsync(topLevel, vm);
                }
            }
        }

        private void OnToggleThemeClick(object? sender, RoutedEventArgs e)
        {
            if (Application.Current is { } app)
            {
                app.RequestedThemeVariant = app.ActualThemeVariant == ThemeVariant.Dark
                    ? ThemeVariant.Light
                    : ThemeVariant.Dark;
            }
        }
    }
}