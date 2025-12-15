using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using QuickTranslate.Core.Interfaces;
using QuickTranslate.Core.Services;
using QuickTranslate.Desktop.Services;
using QuickTranslate.Desktop.Services.Interfaces;
using QuickTranslate.Desktop.ViewModels;
using QuickTranslate.Desktop.Views;
using Serilog;

namespace QuickTranslate.Desktop;

public partial class App : Application
{
    private static IServiceProvider? _serviceProvider;

    public static T GetService<T>() where T : class
    {
        return _serviceProvider?.GetRequiredService<T>() 
            ?? throw new InvalidOperationException("Service provider not initialized");
    }

    private void Application_Startup(object sender, StartupEventArgs e)
    {
        ConfigureLogging();
        ConfigureServices();
        
        ThemeService.Instance.Initialize();
        
        Log.Information("QuickTranslate starting...");

        var mainWindow = GetService<MainWindow>();
        MainWindow = mainWindow;
        mainWindow.Show();
    }

    private void ConfigureLogging()
    {
        var logPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "QuickTranslate",
            "logs",
            "log-.txt");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(logPath, 
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();
    }

    private void ConfigureServices()
    {
        var services = new ServiceCollection();

        services.AddSingleton<ISettingsStore, SettingsStore>();
        
        services.AddSingleton<IProviderClient>(sp =>
        {
            var store = sp.GetRequiredService<ISettingsStore>();
            var settings = store.Load();
            var activeProvider = settings.GetActiveProvider();
            return new OpenAiProviderClient(activeProvider);
        });

        services.AddSingleton<ITranslationService, TranslationService>();
        services.AddSingleton<ITtsService, PiperTtsService>();

        services.AddSingleton<IHotkeyService, HotkeyService>();
        services.AddSingleton<IClipboardService, ClipboardService>();
        services.AddSingleton<IAudioPlayerService, AudioPlayerService>();

        services.AddSingleton<MainViewModel>();
        services.AddTransient<SettingsViewModel>();

        services.AddSingleton<MainWindow>();
        services.AddTransient<SettingsWindow>();

        _serviceProvider = services.BuildServiceProvider();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.Information("QuickTranslate shutting down");
        Log.CloseAndFlush();
        base.OnExit(e);
    }
}
