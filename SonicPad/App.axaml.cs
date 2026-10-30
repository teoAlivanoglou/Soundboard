using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SonicPad.ViewModels;

namespace SonicPad;

public partial class App : Application
{
    public static IHost? Host { get; set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var splash = new SplashWindow();
            var transition = new StartupWindowTransition();
            SonicPadViewModel? viewModel = null;
            desktop.MainWindow = splash;

            void StopLoading()
            {
                transition.Stop();
                viewModel?.CancelSoundLoading();
            }

            splash.Closed += (_, _) =>
            {
                if (!transition.IsRevealed) StopLoading();
            };
            desktop.ShutdownRequested += (_, _) => StopLoading();

            // Give the splash a render opportunity before resolving audio services/picking a folder.
            splash.Opened += (_, _) => Dispatcher.UIThread.Post(async () =>
            {
                if (transition.IsStopped) return;
                viewModel = Host?.Services.GetService<SonicPadViewModel>() ?? new SonicPadViewModel();
                splash.DataContext = viewModel;
                viewModel.LoadingStatusText = "Opening folder picker...";

                void RevealMainWindow()
                {
                    if (!transition.TryReveal()) return;
                    var mainWindow = Host?.Services.GetService<MainWindow>() ?? new MainWindow(viewModel);
                    mainWindow.Closed += (_, _) => StopLoading();
                    desktop.MainWindow = mainWindow;
                    mainWindow.Show();
                    splash.Close();
                }

                await MainWindow.PickAndLoadFolderAsync(splash, viewModel, RevealMainWindow,
                    () => !transition.IsStopped);
            }, DispatcherPriority.Background);

            desktop.Exit += async (_, _) =>
            {
                StopLoading();
                Host?.Services.GetService<Settings.SettingsService>()?.Save();
                if (Host != null)
                {
                    await Host.StopAsync();
                    Host.Dispose();
                }
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
