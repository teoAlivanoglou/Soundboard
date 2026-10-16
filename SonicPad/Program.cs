using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Dialogs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SonicPad.AudioEngine;
using SonicPad.Discovery;
using SonicPad.Settings;
using SonicPad.UI.Fonts;
using SonicPad.ViewModels;
using System;


namespace SonicPad;

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

        // Register core services
        builder.Services.AddSingleton(SettingsService.Load());
        builder.Services.AddSingleton<AudioDataCache>();
        builder.Services.AddSingleton<AudioPlaybackEngine>();
        builder.Services.AddSingleton<SoundDiscoveryService>();
        builder.Services.AddSingleton<SonicPadViewModel>();

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
            .With(new SkiaOptions
            {
                MaxGpuResourceSizeBytes = 256 * 1024 * 1024, // 256mb
                UseStencilBuffers = true
            })
#if DEBUG
            .WithDeveloperTools()
            .LogToTrace()
#endif
            .ConfigureFonts(fontManager =>
            {
                fontManager.AddFontCollection(new UIFontCollection());
            });
}
