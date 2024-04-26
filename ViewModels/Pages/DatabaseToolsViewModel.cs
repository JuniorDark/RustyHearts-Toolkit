using RHToolkit.Services;
using RHToolkit.Models;
using RHToolkit.Views.Windows;
using Wpf.Ui.Controls;

namespace RHToolkit.ViewModels.Pages
{
    public partial class DatabaseToolsViewModel(WindowsProviderService windowsProviderService) : ObservableObject
    {
        [ObservableProperty]
        private WindowCard[] _windowCards =
        [
            new("Mail", "Send Mail to characters", SymbolRegular.Mail24, "mail"),
        new("Character Editor", "Character editor.", SymbolRegular.PersonEdit24, "charactereditor"),
    ];

        [RelayCommand]
        public void OnOpenWindow(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            switch (value)
            {
                case "mail":
                    windowsProviderService.Show<MailWindow>();
                    break;
            }
        }
    }
}
