using RHToolkit.Models;
using RHToolkit.Models.Database;
using RHToolkit.Properties;
using RHToolkit.Services;
using RHToolkit.Views.Windows;
using Wpf.Ui.Controls;

namespace RHToolkit.ViewModels.Pages;

public partial class DatabaseToolsViewModel(WindowsProviderService windowsProviderService) : ObservableObject
{
    [ObservableProperty]
    private WindowCard[] _windowCards =
    [
        new(Resources.Mail, Resources.MailDesc, SymbolRegular.Mail24, "mail"),
];

    [RelayCommand]
    public void OnOpenWindow(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        if (!SqlCredentialValidator.ValidateCredentials())
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
