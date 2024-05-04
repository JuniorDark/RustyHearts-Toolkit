using RHToolkit.Services;
using RHToolkit.Models;
using RHToolkit.Views.Windows;
using Wpf.Ui.Controls;
using RHToolkit.Properties;

namespace RHToolkit.ViewModels.Pages
{
    public partial class DatabaseToolsViewModel(WindowsProviderService windowsProviderService) : ObservableObject
    {
        [ObservableProperty]
        private WindowCard[] _windowCards =
        [
            new(Resources.Mail, Resources.MailDesc, SymbolRegular.Mail24, "mail"),
        new(Resources.CharacterEditor, Resources.CharacterEditorDesc, SymbolRegular.PersonEdit24, "charactereditor"),
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
