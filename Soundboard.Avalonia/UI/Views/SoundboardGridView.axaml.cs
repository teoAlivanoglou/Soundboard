using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Soundboard.Avalonia.UI.Views
{
    public partial class SoundboardGridView : UserControl
    {
        public SoundboardGridView()
        {
            InitializeComponent();
        }

        private void OnItemsControlSizeChanged(object? sender, SizeChangedEventArgs e)
        {
            // Crucial: Ignore height changes (e.g. from row virtualization or items loading)
            if (!e.WidthChanged) return;

            // Ignore sub-pixel jitter
            if (Math.Abs(e.PreviousSize.Width - e.NewSize.Width) < 0.5) return;

            if (DataContext is ViewModels.SoundboardViewModel vm && e.NewSize.Width > 0)
            {
                vm.UpdateLayoutAndRows(e.NewSize.Width, forceRebuild: false);
            }
        }
    }
}