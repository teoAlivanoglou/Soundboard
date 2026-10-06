using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;

namespace Soundboard.Avalonia.UI.Views
{
    public partial class BottomBarView : UserControl
    {
        public BottomBarView()
        {
            InitializeComponent();
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