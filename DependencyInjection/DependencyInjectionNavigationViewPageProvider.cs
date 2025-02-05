using Wpf.Ui.Abstractions;

namespace RHToolkit.DependencyInjection;

public class DependencyInjectionNavigationViewPageProvider(IServiceProvider serviceProvider)
: INavigationViewPageProvider
{
    public object? GetPage(Type pageType)
    {
        return serviceProvider.GetService(pageType);
    }
}
