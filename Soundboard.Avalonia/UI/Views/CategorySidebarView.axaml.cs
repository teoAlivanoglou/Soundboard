using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Microsoft.Extensions.DependencyInjection;
using Soundboard.Avalonia.Discovery;
using Soundboard.Avalonia.ViewModels;

namespace Soundboard.Avalonia.UI.Views
{
    public partial class CategorySidebarView : UserControl
    {
        public CategorySidebarView()
        {
            InitializeComponent();
        }

        private SoundboardViewModel? GetViewModel()
        {
            return DataContext as SoundboardViewModel
                   ?? (this.VisualRoot as Control)?.DataContext as SoundboardViewModel
                   ?? App.Host?.Services.GetService<SoundboardViewModel>();
        }

        private void OnSelectAllClick(object? sender, RoutedEventArgs e)
        {
            var vm = GetViewModel();
            if (vm != null)
            {
                foreach (var cat in vm.Categories)
                {
                    cat.IsEnabled = true;
                }
                vm.UpdateSoundVisibility();
            }
        }

        private void OnSelectNoneClick(object? sender, RoutedEventArgs e)
        {
            var vm = GetViewModel();
            if (vm != null)
            {
                foreach (var cat in vm.Categories)
                {
                    cat.IsEnabled = false;
                }
                vm.UpdateSoundVisibility();
            }
        }

        private void OnExpandAllClick(object? sender, RoutedEventArgs e)
        {
            // Ready for hierarchical model expansion
        }

        private void OnCollapseAllClick(object? sender, RoutedEventArgs e)
        {
            // Ready for hierarchical model collapse
        }

        private void OnCategoryRowPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (sender is Control { DataContext: CategoryModel category })
            {
                var vm = GetViewModel();

                if (category.IsAll)
                {
                    var newState = !category.IsEnabled;
                    category.IsEnabled = newState;
                    if (vm != null)
                    {
                        foreach (var cat in vm.Categories)
                        {
                            cat.IsEnabled = newState;
                        }
                    }
                }
                else
                {
                    category.IsEnabled = !category.IsEnabled;
                }

                vm?.UpdateSoundVisibility();
            }
        }
    }
}