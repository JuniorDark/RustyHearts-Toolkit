using System.Windows;

namespace RHGMTool.ViewModels
{
    public class WindowManager
    {
        private static WindowManager? _instance;
        private readonly Dictionary<Type, Window> _windows;

        public static WindowManager Instance => _instance ??= new WindowManager();

        private WindowManager()
        {
            _windows = [];
        }

        public void OpenOrActivateWindow<T>() where T : Window, new()
        {
            var windowType = typeof(T);
            if (_windows.TryGetValue(windowType, out Window? value))
            {
                var existingWindow = value;
                existingWindow.WindowState = WindowState.Normal; // Ensure it's not minimized
                existingWindow.Activate();
            }
            else
            {
                var newWindow = new T();
                _windows.Add(windowType, newWindow);
                newWindow.Closed += (sender, args) => _windows.Remove(windowType);
                newWindow.Show();
            }
        }
    }

}
