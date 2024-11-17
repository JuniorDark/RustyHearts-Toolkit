namespace RHToolkit.Services;

/// <summary>
/// Provides methods to show WPF windows using a service provider.
/// </summary>
public class WindowsProviderService(IServiceProvider serviceProvider)
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    /// <summary>
    /// Shows a window of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of the window to show.</typeparam>
    /// <param name="setOwner">Whether to set the owner of the window to the currently active window.</param>
    /// <exception cref="InvalidOperationException">Thrown if the specified type is not a window or if the window instance cannot be retrieved from the service provider.</exception>
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

    /// <summary>
    /// Shows a window of the specified type and returns the instance of the window.
    /// </summary>
    /// <typeparam name="T">The type of the window to show.</typeparam>
    /// <param name="setOwner">Whether to set the owner of the window to the currently active window.</param>
    /// <returns>The instance of the window.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the specified type is not a window or if the window instance cannot be retrieved from the service provider.</exception>
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