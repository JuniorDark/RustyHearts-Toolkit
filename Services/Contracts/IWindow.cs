namespace RHToolkit.Services.Contracts;

public interface IWindow
{
    event RoutedEventHandler Loaded;

    void Show();
}