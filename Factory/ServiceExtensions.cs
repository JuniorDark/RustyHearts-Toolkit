using Microsoft.Extensions.DependencyInjection;

namespace RHToolkit.Factory;

public static class ServiceExtensions
{
    public static void AddWindowFactory<TWindow>(this IServiceCollection services)
        where TWindow : class
    {
        services.AddTransient<TWindow>();
        services.AddSingleton<Func<TWindow>>(x => () => x.GetService<TWindow>()!);
        services.AddSingleton<IAbstractFactory<TWindow>, AbstractFactory<TWindow>>();
    }
}
