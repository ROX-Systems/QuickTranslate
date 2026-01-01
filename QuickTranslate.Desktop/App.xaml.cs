using System.IO;
using System.Net.Http;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using QuickTranslate.Core.Factories;
using QuickTranslate.Core.Interfaces;
using QuickTranslate.Core.Services;
using QuickTranslate.Desktop.Services;
using QuickTranslate.Desktop.Services.Interfaces;
using QuickTranslate.Desktop.ViewModels;
using QuickTranslate.Desktop.Views;
using TranslationPopup = QuickTranslate.Desktop.Views.TranslationPopup;
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
        try
        {
            ConfigureLogging();
            ConfigureServices();

            ThemeService.Instance.Initialize();

            Log.Information("QuickTranslate starting...");

            var mainWindow = GetService<MainWindow>();
            MainWindow = mainWindow;
            mainWindow.Show();

            Log.Information("QuickTranslate started successfully");
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "QuickTranslate failed to start");

            // Try to show error message
            try
            {
                MessageBox.Show(
                    $"Application failed to start:\n\n{ex.Message}\n\nDetails:\n{ex}",
                    "QuickTranslate - Startup Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch
            {
                // If we can't show MessageBox, at least log it
            }

            Shutdown(-1);
        }
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
        try
        {
            Log.Information("Configuring services...");

            var services = new ServiceCollection();

            services.AddHttpClient("OpenAI");

            services.AddSingleton<ISettingsStore, SettingsStore>();
            services.AddSingleton<ITranslationHistoryService, TranslationHistoryService>();
            services.AddSingleton<IHealthCheckService, HealthCheckServiceV2>();

            services.AddSingleton<IProviderClient>(sp =>
            {
                var store = sp.GetRequiredService<ISettingsStore>();
                var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
                var settings = store.Load();
                var activeProvider = settings.GetActiveProvider();
                return ProviderClientFactory.CreateClient(activeProvider, httpClientFactory);
            });

            services.AddSingleton<ITranslationService, TranslationService>();
            services.AddSingleton<ITtsService>(sp =>
            {
                var store = sp.GetRequiredService<ISettingsStore>();
                var settings = store.Load();
                return new PiperTtsService(settings.TtsEndpoint);
            });

            services.AddSingleton<IHotkeyService, HotkeyService>();
            services.AddSingleton<IClipboardService, ClipboardService>();
            services.AddSingleton<IAudioPlayerService, AudioPlayerService>();

            services.AddSingleton<MainViewModel>();
            services.AddTransient<SettingsViewModel>();
            services.AddTransient<HistoryViewModel>();
            services.AddTransient<TranslationPopupViewModel>();

            services.AddSingleton<MainWindow>();
            services.AddTransient<SettingsWindow>();
            services.AddTransient<HistoryWindow>();
            services.AddTransient<TranslationPopup>();

            _serviceProvider = services.BuildServiceProvider();

            Log.Information("Services configured successfully");
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Failed to configure services");
            throw new InvalidOperationException("Failed to configure services", ex);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.Information("QuickTranslate shutting down");
        Log.CloseAndFlush();
        base.OnExit(e);
    }
}
