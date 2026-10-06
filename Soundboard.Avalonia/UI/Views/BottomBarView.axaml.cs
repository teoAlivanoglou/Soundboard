using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Microsoft.Extensions.DependencyInjection;
using Soundboard.Avalonia.ViewModels;

namespace Soundboard.Avalonia.UI.Views
{
    public partial class BottomBarView : UserControl
    {
        public BottomBarView()
        {
            InitializeComponent();
        }

        private void OnStopAllClick(object? sender, RoutedEventArgs e)
        {
            var vm = (this.VisualRoot as Control)?.DataContext as SoundboardViewModel
                     ?? App.Host?.Services.GetService<SoundboardViewModel>();
            vm?.StopAllSounds();
        }

        private async void OnBrowseFolderClick(object? sender, RoutedEventArgs e)
        {
            var topLevel = TopLevel.GetTopLevel(this) ?? (this.VisualRoot as TopLevel);
            if (topLevel != null)
            {
                var vm = (this.DataContext as SoundboardViewModel)
                         ?? (topLevel.DataContext as SoundboardViewModel)
                         ?? App.Host?.Services.GetService<SoundboardViewModel>();
                if (vm != null)
                {
                    await MainWindow.PickAndLoadFolderAsync(topLevel, vm);
                }
            }
        }

        // TEMPORARY: Toggles between Light and Dark mode. Remove when real refresh logic is wired.
        private void OnRefreshClick(object? sender, RoutedEventArgs e)
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