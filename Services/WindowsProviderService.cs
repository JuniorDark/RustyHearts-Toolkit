namespace RHToolkit.Services;

public class WindowsProviderService(IServiceProvider serviceProvider)
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    public void Show<T>(bool setOwner = false)
        where T : class
    {
        if (!typeof(Window).IsAssignableFrom(typeof(T)))
        {
            throw new InvalidOperationException(string.Format(Resources.WindowsServiceErrorMessage, typeof(Window)));
        }

        Window windowInstance =
            _serviceProvider.GetService<T>() as Window
            ?? throw new InvalidOperationException(Resources.WindowsServiceServiceErrorMessage);

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

    public T? ShowInstance<T>(bool setOwner = false) where T : class
    {
        if (!typeof(Window).IsAssignableFrom(typeof(T)))
        {
            throw new InvalidOperationException(string.Format(Resources.WindowsServiceErrorMessage, typeof(Window)));
        }

        if (_serviceProvider.GetService<T>() is not Window windowInstance)
        {
            throw new InvalidOperationException(Resources.WindowsServiceServiceErrorMessage);
        }

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
        return windowInstance as T;
    }

}

