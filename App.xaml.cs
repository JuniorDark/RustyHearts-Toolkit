using RHToolkit.Models.Database;
using RHToolkit.Models.Localization;
using RHToolkit.Models.SQLite;
using RHToolkit.Services;
using RHToolkit.Services.Contracts;
using RHToolkit.ViewModels.Controls;
using RHToolkit.ViewModels.Pages;
using RHToolkit.ViewModels.Windows;
using RHToolkit.ViewModels.Windows.Database.VM;
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
                _ = services.AddSingleton<HomePageViewModel>();
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
                _ = services.AddSingleton<CharacterDataManager>();
                _ = services.AddSingleton<MailDataManager>();
                // Database services and viewmodels
                _ = services.AddTransient<ItemDataManager>();
                _ = services.AddTransient<ItemDataViewModel>();
                _ = services.AddTransient<CharacterWindow>();
                _ = services.AddTransient<CharacterWindowViewModel>();
                _ = services.AddTransient<CharacterDataViewModel>();
                _ = services.AddTransient<MailWindow>();
                _ = services.AddTransient<MailWindowViewModel>();
                _ = services.AddTransient<TitleWindow>();
                _ = services.AddTransient<TitleWindowViewModel>();
                _ = services.AddTransient<SanctionWindow>();
                _ = services.AddTransient<SanctionWindowViewModel>();
                _ = services.AddTransient<FortuneWindow>();
                _ = services.AddTransient<FortuneWindowViewModel>();
                _ = services.AddTransient<EquipmentWindow>();
                _ = services.AddTransient<EquipmentWindowViewModel>();
                _ = services.AddTransient<InventoryWindow>();
                _ = services.AddTransient<InventoryWindowViewModel>();
                _ = services.AddTransient<StorageWindow>();
                _ = services.AddTransient<StorageWindowViewModel>();
                // Editor services and viewmodels
                _ = services.AddTransient<RHEditorWindow>();
                _ = services.AddTransient<RHEditorViewModel>();
                _ = services.AddTransient<CashShopEditorWindow>();
                _ = services.AddTransient<CashShopEditorViewModel>();
                _ = services.AddTransient<SetItemEditorWindow>();
                _ = services.AddTransient<SetItemEditorViewModel>();
                _ = services.AddTransient<PackageEditorWindow>();
                _ = services.AddTransient<PackageEditorViewModel>();
                _ = services.AddTransient<RandomRuneEditorWindow>();
                _ = services.AddTransient<RandomRuneEditorViewModel>();
                _ = services.AddTransient<DropGroupEditorWindow>();
                _ = services.AddTransient<DropGroupEditorViewModel>();
                _ = services.AddTransient<NpcEditorWindow>();
                _ = services.AddTransient<NpcEditorViewModel>();
                _ = services.AddTransient<NPCShopEditorWindow>();
                _ = services.AddTransient<NPCShopEditorViewModel>();
                _ = services.AddTransient<TitleEditorWindow>();
                _ = services.AddTransient<TitleEditorViewModel>();
                _ = services.AddTransient<ItemEditorWindow>();
                _ = services.AddTransient<ItemEditorViewModel>();
                _ = services.AddTransient<PetEditorWindow>();
                _ = services.AddTransient<PetEditorViewModel>();
                _ = services.AddTransient<QuestEditorWindow>();
                _ = services.AddTransient<QuestEditorViewModel>();
                _ = services.AddTransient<EnemyEditorWindow>();
                _ = services.AddTransient<EnemyEditorViewModel>();
                // SQLite services and viewmodels
                _ = services.AddTransient<ItemWindow>();
                _ = services.AddTransient<ItemWindowViewModel>();
                _ = services.AddTransient<NpcShopWindow>();
                _ = services.AddTransient<NPCShopViewModel>();
                _ = services.AddTransient<ItemMixWindow>();
                _ = services.AddTransient<ItemMixViewModel>();
                _ = services.AddTransient<RareCardRewardWindow>();
                _ = services.AddTransient<RareCardRewardViewModel>();
                _ = services.AddTransient<DropGroupListWindow>();
                _ = services.AddTransient<DropGroupListViewModel>();

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
