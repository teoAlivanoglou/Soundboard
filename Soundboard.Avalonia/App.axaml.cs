using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Soundboard.Avalonia;

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
            desktop.MainWindow = Host?.Services.GetService<MainWindow>() ?? new MainWindow();
            desktop.Exit += async (_, _) =>
            {
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