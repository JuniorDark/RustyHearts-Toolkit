using RHToolkit.Models.Database;
using RHToolkit.Models.Localization;
using RHToolkit.Models.SQLite;
using RHToolkit.Services;
using RHToolkit.Services.Contracts;
using RHToolkit.ViewModels.Controls;
using RHToolkit.ViewModels.Pages;
using RHToolkit.ViewModels.Windows;
using RHToolkit.Views.Pages;
using RHToolkit.Views.Windows;
using Wpf.Ui;

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
                _ = services.AddSingleton<IWindowsService, WindowsService>();

                // Top-level pages
                _ = services.AddSingleton<HomePage>();
                _ = services.AddSingleton<DatabaseToolsPage>();
                _ = services.AddSingleton<DatabaseToolsViewModel>();
                _ = services.AddSingleton<DatabasePage>();
                _ = services.AddSingleton<DatabaseViewModel>();
                _ = services.AddSingleton<CharacterEditPage>();
                _ = services.AddSingleton<CharacterEditViewModel>();
                _ = services.AddSingleton<CharacterRestorePage>();
                _ = services.AddSingleton<CharacterRestoreViewModel>();
                _ = services.AddSingleton<SettingsPage>();
                _ = services.AddSingleton<SettingsViewModel>();
                _ = services.AddSingleton<EditToolsPage>();
                _ = services.AddSingleton<EditToolsViewModel>();
                _ = services.AddSingleton<GMDatabaseManagerPage>();
                _ = services.AddSingleton<GMDatabaseManagerViewModel>();
                _ = services.AddSingleton<CouponPage>();
                _ = services.AddSingleton<CouponViewModel>();

                // All other services and viewmodels
                _ = services.AddSingleton<ISqlDatabaseService, SqlDatabaseService>();
                _ = services.AddSingleton<IDatabaseService, DatabaseService>();
                _ = services.AddSingleton<ISqLiteDatabaseService, SqLiteDatabaseService>();
                _ = services.AddSingleton<IGMDatabaseService, GMDatabaseService>();
                _ = services.AddSingleton<IFrameService, FrameService>();
                _ = services.AddSingleton<CachedDataManager>();
                _ = services.AddSingleton<CharacterHelper>();
                _ = services.AddSingleton<MailHelper>();

                _ = services.AddTransient<MailWindow>();
                _ = services.AddTransient<MailWindowViewModel>();
                _ = services.AddTransient<ItemWindow>();
                _ = services.AddTransient<ItemWindowViewModel>();
                _ = services.AddTransient<FrameViewModel>();
                _ = services.AddTransient<CharacterWindow>();
                _ = services.AddTransient<CharacterWindowViewModel>();
                _ = services.AddTransient<TitleWindow>();
                _ = services.AddTransient<TitleWindowViewModel>();
                _ = services.AddTransient<SanctionWindow>();
                _ = services.AddTransient<SanctionWindowViewModel>();
                _ = services.AddTransient<FortuneWindow>();
                _ = services.AddTransient<FortuneWindowViewModel>();
                _ = services.AddTransient<RHEditorWindow>();
                _ = services.AddTransient<RHEditorViewModel>();
                _ = services.AddTransient<EquipmentWindow>();
                _ = services.AddTransient<EquipmentWindowViewModel>();

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
        LocalizationManager.SetCurrentLanguage();

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
