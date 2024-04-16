using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RHGMTool.Views;

namespace RHGMTool.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {

        public MainWindowViewModel()
        {

        }

        [ObservableProperty]
        private bool _isTextBoxEnabled = true;

        [RelayCommand]
        private static void OpenItemDatabaseWindow(object obj)
        {
            WindowManager.Instance.OpenOrActivateWindow<ItemWindow>();
        }

        [RelayCommand]
        private static void OpenMailWindow(object obj)
        {
            WindowManager.Instance.OpenOrActivateWindow<MailWindow>();
        }
    }
}
