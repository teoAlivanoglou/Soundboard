using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Microsoft.Extensions.DependencyInjection;
using SonicPad.Discovery;
using SonicPad.ViewModels;

namespace SonicPad.UI.Views
{
    public partial class CategorySidebarView : UserControl
    {
        public CategorySidebarView()
        {
            InitializeComponent();
        }

        private SonicPadViewModel? GetViewModel()
        {
            return DataContext as SonicPadViewModel
                   ?? (this.VisualRoot as Control)?.DataContext as SonicPadViewModel
                   ?? App.Host?.Services.GetService<SonicPadViewModel>();
        }

        private static void SetEnabledRecursive(CategoryModel category, bool isEnabled)
        {
            category.IsEnabled = isEnabled;
            foreach (var child in category.Children)
            {
                SetEnabledRecursive(child, isEnabled);
            }
        }

        private static void SetExpandedRecursive(CategoryModel category, bool isExpanded)
        {
            category.IsExpanded = isExpanded;
            foreach (var child in category.Children)
            {
                SetExpandedRecursive(child, isExpanded);
            }
        }

        private void OnSelectAllClick(object? sender, RoutedEventArgs e)
        {
            var vm = GetViewModel();
            if (vm != null)
            {
                foreach (var cat in vm.Categories)
                {
                    SetEnabledRecursive(cat, true);
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
                    SetEnabledRecursive(cat, false);
                }
                vm.UpdateSoundVisibility();
            }
        }

        private void OnExpandAllClick(object? sender, RoutedEventArgs e)
        {
            var vm = GetViewModel();
            if (vm != null)
            {
                foreach (var cat in vm.Categories)
                {
                    SetExpandedRecursive(cat, true);
                }
            }
        }

        private void OnCollapseAllClick(object? sender, RoutedEventArgs e)
        {
            var vm = GetViewModel();
            if (vm != null)
            {
                foreach (var cat in vm.Categories)
                {
                    SetExpandedRecursive(cat, false);
                }
            }
        }

        private void OnLeftZonePointerPressed(object? sender, PointerPressedEventArgs e)
        {
            var pointerPoint = e.GetCurrentPoint(sender as Visual);
            if (!pointerPoint.Properties.IsLeftButtonPressed)
                return;

            if (sender is Control { DataContext: CategoryModel category })
            {
                if (category.Children.Count > 0)
                {
                    category.IsExpanded = !category.IsExpanded;
                }
                else
                {
                    var vm = GetViewModel();
                    var newState = !category.IsEnabled;
                    SetEnabledRecursive(category, newState);
                    vm?.UpdateSoundVisibility();
                }
                e.Handled = true;
            }
        }

        private void OnCategoryRowPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            var pointerPoint = e.GetCurrentPoint(sender as Visual);
            if (!pointerPoint.Properties.IsLeftButtonPressed)
                return;

            if (sender is Control { DataContext: CategoryModel category })
            {
                var vm = GetViewModel();
                var newState = !category.IsEnabled;
                SetEnabledRecursive(category, newState);
                vm?.UpdateSoundVisibility();
                e.Handled = true;
            }
        }
    }
}