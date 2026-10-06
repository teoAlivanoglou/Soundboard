using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Microsoft.Extensions.DependencyInjection;
using Soundboard.Avalonia.ViewModels;

namespace Soundboard.Avalonia.UI.Views
{
    public partial class SettingsPanelView : UserControl
    {
        public SettingsPanelView()
        {
            InitializeComponent();
        }

        private void ResetAudioDriverButtonClicked(object? sender, RoutedEventArgs e)
        {
            var vm = (DataContext as SoundboardViewModel)
                     ?? ((VisualRoot as Control)?.DataContext as SoundboardViewModel)
                     ?? App.Host?.Services.GetService<SoundboardViewModel>();
            vm?.ResetAudioDriver();
        }
    }
}