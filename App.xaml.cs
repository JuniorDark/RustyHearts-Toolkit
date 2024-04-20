using Microsoft.Extensions.DependencyInjection;
using RHToolkit.Services;
using RHToolkit.ViewModels;
using RHToolkit.Views;
using System.Windows;

namespace RHToolkit;

public partial class App : Application
{
    // Service collection to register services
    public static readonly ServiceCollection Services = new();

    // ServiceProvider to resolve services
    private readonly ServiceProvider _serviceProvider;

    public App()
    {
        ConfigureServices(Services);
        _serviceProvider = Services.BuildServiceProvider();
    }

    // Method to register services
    private static void ConfigureServices(IServiceCollection services)
    {
        // Register your services
        services.AddSingleton<ISqlDatabaseService, SqlDatabaseService>();
        services.AddSingleton<IDatabaseService, DatabaseService>();
        services.AddSingleton<ISqLiteDatabaseService, SqLiteDatabaseService>();
        services.AddSingleton<IGMDatabaseService, GMDatabaseService>();
        services.AddSingleton<IFrameService, FrameService>();

        services.AddSingleton<MainWindow>();
        services.AddSingleton<MainWindowViewModel>();

        services.AddTransient<MailWindow>();
        services.AddTransient<MailWindowViewModel>();
        services.AddTransient<ItemWindow>();
        services.AddTransient<ItemWindowViewModel>();

        services.AddTransient<FrameViewModel>();
    }

    // Method to retrieve service provider
    public static T GetService<T>() where T : class
    {
        return Services.BuildServiceProvider().GetRequiredService<T>();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var mainWindow = new MainWindow
        {
            DataContext = _serviceProvider.GetRequiredService<MainWindowViewModel>()
        };
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        base.OnExit(e);
        _serviceProvider.Dispose();
    }
}
