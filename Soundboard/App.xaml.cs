using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Soundboard.AudioEngine;
using Soundboard.Discovery;
using Soundboard.Settings;
using Soundboard.ViewModels;
using System.Windows;

namespace Soundboard;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public static IHost Host { get; private set; } = null!;
    private MainWindow? _mainWindow;


    protected override async void OnStartup(StartupEventArgs args)
    {
        try
        {
            base.OnStartup(args);

            var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder(args.Args);

            builder.Services.AddSingleton(SettingsService.Load());
            builder.Services.AddSingleton<AudioPlaybackEngine>();
            builder.Services.AddSingleton<SoundDiscoveryService>();
            builder.Services.AddSingleton<SoundboardViewModel>();
            builder.Services.AddSingleton<MainWindow>();

            Host = builder.Build();
            await Host.StartAsync();

            _mainWindow = Host.Services.GetRequiredService<MainWindow>();
            _mainWindow.Show();
        }
        catch (Exception e)
        {
            if (_mainWindow is null)
            {
                MessageBox.Show(e.Message, nameof(e), MessageBoxButton.OK);
                Environment.Exit(1);
            }

            MessageBox.Show(_mainWindow, e.Message, nameof(e), MessageBoxButton.OK);
            Environment.Exit(1);
        }
    }

    protected override async void OnExit(ExitEventArgs args)
    {
        try
        {
            await Host.StopAsync();
            base.OnExit(args);
        }
        catch (Exception e)
        {
            MessageBox.Show(_mainWindow!, e.Message, nameof(e), MessageBoxButton.OK);
            Environment.Exit(1);
        }
    }
}