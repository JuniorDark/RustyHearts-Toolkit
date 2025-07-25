﻿using RHToolkit.DependencyInjection;
using RHToolkit.Models.Database;
using RHToolkit.Models.Localization;
using RHToolkit.Models.MessageBox;
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
            var basePath =
                Path.GetDirectoryName(AppContext.BaseDirectory)
                ?? throw new DirectoryNotFoundException(
                    "Unable to find the base directory of the application."
                );
            _ = c.SetBasePath(basePath);
        })
        .ConfigureServices(
            (context, services) =>
            {
                _ = services.AddNavigationViewPageProvider();

                /// App Host
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
                _ = services.AddSingleton<PCKToolPage>();
                _ = services.AddSingleton<PCKToolViewModel>();
                // All other services and viewmodels
                _ = services.AddSingleton<ISqlDatabaseService, SqlDatabaseService>();
                _ = services.AddSingleton<IDatabaseService, DatabaseService>();
                _ = services.AddSingleton<ISqLiteDatabaseService, SqLiteDatabaseService>();
                _ = services.AddSingleton<IGMDatabaseService, GMDatabaseService>();
                _ = services.AddSingleton<IFrameService, FrameService>();
                _ = services.AddSingleton<CachedDataManager>();
                _ = services.AddSingleton<CharacterDataManager>();
                _ = services.AddSingleton<MailDataManager>();
                _ = services.AddSingleton<SkillDataManager>();
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
                _ = services.AddTransient<SkillDataViewModel>();
                _ = services.AddTransient<SkillDataManager>();
                // Editor services and viewmodels
                _ = services.AddTransient<RHEditorWindow>();
                _ = services.AddTransient<RHEditorViewModel>();
                _ = services.AddTransient<WDataEditorWindow>();
                _ = services.AddTransient<WDataEditorViewModel>();
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
                _ = services.AddTransient<AddEffectEditorWindow>();
                _ = services.AddTransient<AddEffectEditorViewModel>();
                _ = services.AddTransient<WorldEditorWindow>();
                _ = services.AddTransient<WorldEditorViewModel>();
                _ = services.AddTransient<SkillEditorWindow>();
                _ = services.AddTransient<SkillEditorViewModel>();
                // SQLite services and viewmodels
                _ = services.AddTransient<DropGroupListWindow>();
                _ = services.AddTransient<DropGroupListViewModel>();
                _ = services.AddTransient<ItemMixWindow>();
                _ = services.AddTransient<ItemMixViewModel>();
                _ = services.AddTransient<ItemWindow>();
                _ = services.AddTransient<ItemWindowViewModel>();
                _ = services.AddTransient<NpcShopWindow>();
                _ = services.AddTransient<NPCShopViewModel>();
                _ = services.AddTransient<RareCardRewardWindow>();
                _ = services.AddTransient<RareCardRewardViewModel>();
                _ = services.AddTransient<SkillWindow>();
                _ = services.AddTransient<SkillWindowViewModel>();

            }
    )
        .Build();

    /// <summary>
    /// Gets services.
    /// </summary>
    public static IServiceProvider Services
    {
        get { return _host.Services; }
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
        // Log the exception details
        LogException(e.Exception);

        RHMessageBoxHelper.ShowOKMessage("An unexpected error occurred. Check the error log for more details.", "Error");

        // Prevent default unhandled exception processing
        e.Handled = true;
    }

    /// <summary>
    /// Logs the exception details to a file.
    /// </summary>
    /// <param name="exception">The exception to log.</param>
    private static void LogException(Exception exception)
    {
        try
        {
            string logDirectory = Path.Combine(AppContext.BaseDirectory, "log");
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            string logFilePath = Path.Combine(logDirectory, "error.log");
            string logMessage = $"{DateTime.Now}: {exception}\n";

            File.AppendAllText(logFilePath, logMessage);
        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"Failed to log exception: {ex.Message}", "Logging Error");
        }
    }
}
