using RHGMTool.Views;

namespace RHGMTool.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        public RelayCommand OpenItemDatabaseCommand { get; }
        public RelayCommand OpenMailWindowCommand { get; }

        public MainViewModel()
        {
            OpenItemDatabaseCommand = new RelayCommand(OpenItemDatabaseWindow);
            OpenMailWindowCommand = new RelayCommand(OpenMailWindow);
        }

        private void OpenItemDatabaseWindow(object obj)
        {
            WindowManager.Instance.OpenOrActivateWindow<ItemWindow>();
        }

        private void OpenMailWindow(object obj)
        {
            WindowManager.Instance.OpenOrActivateWindow<MailWindow>();
        }
    }
}
