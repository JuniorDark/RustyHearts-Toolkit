using RHToolkit.Services;
using RHToolkit.Services.Contracts;
using RHToolkit.ViewModels.Controls;
using RHToolkit.ViewModels.Pages;
using RHToolkit.ViewModels.Windows;
using RHToolkit.Views.Windows;
using RHToolkit.Views.Pages;
using Wpf.Ui;
using RHToolkit.Models.Localization;
using RHToolkit.Models.UISettings;

namespace RHToolkit;

public partial class App : Application
{
    private static readonly IHost _host = Host.CreateDefaultBuilder()
        .ConfigureAppConfiguration(c =>
        {
            _ = c.SetBasePath(AppContext.BaseDirectory);
        })
        .ConfigureServices(
            (_1, services) =>
            {
                // App Host
                _ = services.AddHostedService<ApplicationHostService>();

                // Main window container with navigation
                _ = services.AddSingleton<IWindow, MainWindow>();
                _ = services.AddSingleton<MainWindowViewModel>();
                _ = services.AddSingleton<INavigationService, NavigationService>();
                _ = services.AddSingleton<ISnackbarService, SnackbarService>();
                _ = services.AddSingleton<IContentDialogService, ContentDialogService>();
                _ = services.AddSingleton<WindowsProviderService>();

                // Top-level pages
                _ = services.AddSingleton<HomePage>();
                _ = services.AddSingleton<DatabaseToolsPage>();
                _ = services.AddSingleton<DatabaseToolsViewModel>();
                _ = services.AddSingleton<DatabasePage>();
                _ = services.AddSingleton<DatabaseViewModel>();
                _ = services.AddSingleton<CharacterEditPage>();
                _ = services.AddSingleton<CharacterEditViewModel>();
                _ = services.AddSingleton<SettingsPage>();
                _ = services.AddSingleton<SettingsViewModel>();

                // All other pages and view models
                _ = services.AddSingleton<ISqlDatabaseService, SqlDatabaseService>();
                _ = services.AddSingleton<IDatabaseService, DatabaseService>();
                _ = services.AddSingleton<ISqLiteDatabaseService, SqLiteDatabaseService>();
                _ = services.AddSingleton<IGMDatabaseService, GMDatabaseService>();
                _ = services.AddSingleton<IFrameService, FrameService>();

                _ = services.AddTransient<MailWindow>();
                _ = services.AddTransient<MailWindowViewModel>();
                _ = services.AddTransient<ItemWindow>();
                _ = services.AddTransient<ItemWindowViewModel>();
                _ = services.AddTransient<FrameViewModel>();
                _ = services.AddTransient<CharacterWindow>();
                _ = services.AddTransient<CharacterWindowViewModel>();

            }
        )
        .Build();

    /// <summary>
    /// Gets registered service.
    /// </summary>
    /// <typeparam name="T">Type of the service to get.</typeparam>
    /// <returns>Instance of the service or <see langword="null"/>.</returns>
    public static T GetRequiredService<T>()
        where T : class
    {
        return _host.Services.GetRequiredService<T>();
    }

    /// <summary>
    /// Occurs when the application is loading.
    /// </summary>
    private void OnStartup(object sender, StartupEventArgs e)
    {
        var languageCode = RegistrySettingsHelper.GetAppLanguage();

        if (languageCode != null)
        {
            LocalizationManager.LoadLocalizedStrings(languageCode);
        }
        else
        {
            LocalizationManager.LoadLocalizedStrings("en-US");
        }

        _host.Start();
    }

    /// <summary>
    /// Occurs when the application is closing.
    /// </summary>
    private void OnExit(object sender, ExitEventArgs e)
    {
        _host.StopAsync().Wait();

        _host.Dispose();
    }

    /// <summary>
    /// Occurs when an exception is thrown by an application but not handled.
    /// </summary>
    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        // For more info see https://learn.microsoft.com/en-us/dotnet/api/system.windows.application.dispatcherunhandledexception?view=windowsdesktop-8.0
    }
}
