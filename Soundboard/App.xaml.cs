using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Soundboard.AudioEngine;
using Soundboard.ViewModels;
using System.Configuration;
using System.Data;
using System.Runtime.InteropServices;
using System.Windows;

namespace Soundboard
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        public static IHost Host { get; private set; } = null!;

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder(e.Args);

            builder.Services.AddSingleton<AudioPlaybackEngine>();
            builder.Services.AddSingleton<SoundboardViewModel>();
            builder.Services.AddSingleton<MainWindow>();

            Host = builder.Build();
            await Host.StartAsync();

            var mainWindow = Host.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            await Host.StopAsync();
            base.OnExit(e);
        }
    }

}
