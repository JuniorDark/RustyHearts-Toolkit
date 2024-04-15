using Microsoft.Extensions.DependencyInjection;
using RHGMTool.Services;
using System.Windows;

namespace RHGMTool
{
    public partial class App : Application
    {
        private readonly ServiceProvider _serviceProvider;

        public App()
        {
            // Set up DI container
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Register your services
            services.AddSingleton<ISqLiteDatabaseService, SqLiteDatabaseService>();
            services.AddSingleton<IGMDatabaseService, GMDatabaseService>();
            services.AddSingleton<MainWindow>();
            // Register your view models, services, etc. here
            // services.AddTransient<IMyService, MyService>();
            // services.AddScoped<IMyViewModel, MyViewModel>();
            // ...
        }

        // Override OnStartup method to resolve and set main window
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Resolve your main window and show it
            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }
    }

}
