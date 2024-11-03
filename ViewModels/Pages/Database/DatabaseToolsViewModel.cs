using RHToolkit.Models;
using RHToolkit.Models.Database;
using RHToolkit.Models.MessageBox;
using RHToolkit.Services;
using RHToolkit.Views.Windows;
using Wpf.Ui.Controls;

namespace RHToolkit.ViewModels.Pages;

public partial class DatabaseToolsViewModel(ISqLiteDatabaseService sqLiteDatabaseService, WindowsProviderService windowsProviderService) : ObservableObject
{
    private readonly ISqLiteDatabaseService _sqLiteDatabaseService = sqLiteDatabaseService;

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

        try
        {
            if (!SqlCredentialValidator.ValidateCredentials())
            {
                return;
            }

            if (!_sqLiteDatabaseService.ValidateDatabase())
            {
                return;
            }

            switch (value)
            {
                case "mail":
                    windowsProviderService.Show<MailWindow>(true);
                    break;
            }
        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
        }
    }
}
