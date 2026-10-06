using Avalonia.Controls;
using Avalonia.Input;
using Microsoft.Extensions.DependencyInjection;
using Soundboard.Avalonia.Discovery;
using Soundboard.Avalonia.ViewModels;

namespace Soundboard.Avalonia.UI.Views
{
    public partial class CategoryBarView : UserControl
    {
        public CategoryBarView()
        {
            InitializeComponent();
        }

        private void InputElement_OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (sender is Control { DataContext: CategoryModel category })
            {
                var vm = (this.VisualRoot as Control)?.DataContext as SoundboardViewModel
                         ?? App.Host?.Services.GetService<SoundboardViewModel>();

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