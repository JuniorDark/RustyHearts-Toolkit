using System.Windows;
using System.Windows.Input;

namespace RHGMTool.ViewModels
{
    public class WindowViewModelBase : ViewModelBase
    {
        protected readonly ICommand _openWindowCommand;

        public WindowViewModelBase()
        {
            _openWindowCommand = new RelayCommand(OpenWindow);
        }

        protected virtual void OpenWindow(object? windowType)
        {
            if (windowType == null || windowType is not Type)
                return;

            var targetType = windowType as Type;

            if (IsWindowOpen(targetType))
            {
                var existingWindow = GetOpenWindow(targetType);
                if (existingWindow.WindowState == WindowState.Minimized)
                {
                    existingWindow.WindowState = WindowState.Normal;
                }
                BringWindowToFront(existingWindow);
            }
            else
            {
                var newWindow = (Window)Activator.CreateInstance(targetType);
                newWindow.Closed += (sender, args) =>
                {
                    IsTextBoxEnabled = true;
                };
                newWindow.Show();
                IsTextBoxEnabled = false;
            }
        }


        protected bool IsWindowOpen(Type windowType)
        {
            return Application.Current.Windows.OfType<Window>().Any(w => w.GetType() == windowType);
        }

        protected Window? GetOpenWindow(Type windowType)
        {
            return Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.GetType() == windowType);
        }

        protected void BringWindowToFront(Window? window)
        {
            window?.Activate();
        }
    }

}
