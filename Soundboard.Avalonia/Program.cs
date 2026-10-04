using Avalonia;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Soundboard.AudioEngine;
using Soundboard.Discovery;
using Soundboard.Settings;
using System;

namespace Soundboard.Avalonia;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        // 1. Setup the Generic Host ApplicationBuilder with DI
        var builder = Host.CreateApplicationBuilder(args);

        // Register core services from referenced Soundboard project
        builder.Services.AddSingleton(SettingsService.Load());
        builder.Services.AddSingleton<AudioPlaybackEngine>();
        builder.Services.AddSingleton<SoundDiscoveryService>();

        // Register Avalonia Views / ViewModels
        builder.Services.AddSingleton<MainWindow>();

        var host = builder.Build();
        App.Host = host;
        host.Start();

        // 2. Start Avalonia Desktop Application
        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}
