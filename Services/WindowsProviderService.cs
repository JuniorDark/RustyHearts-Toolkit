namespace RHToolkit.Services;

public class WindowsProviderService(IServiceProvider serviceProvider)
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    public void Show<T>(bool setOwner = false)
        where T : class
    {
        if (!typeof(Window).IsAssignableFrom(typeof(T)))
        {
            throw new InvalidOperationException($"The window class should be derived from {typeof(Window)}.");
        }

        Window windowInstance =
            _serviceProvider.GetService<T>() as Window
            ?? throw new InvalidOperationException("Window is not registered as service.");

        if (setOwner)
        {
            // Automatically set the owner to the currently active window
            Window? owner = Application.Current.Windows.OfType<Window>().SingleOrDefault(w => w.IsActive);
            if (owner != null)
            {
                windowInstance.Owner = owner;
            }
        }

        windowInstance.Show();
    }

}

