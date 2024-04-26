using RHToolkit.Models;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace RHToolkit.ViewModels.Pages;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISnackbarService _snackbarService;

    public SettingsViewModel(ISnackbarService snackbarService)
    {
        _snackbarService = snackbarService;
        AppVersion = $"{GetAssemblyVersion()}";

        SqlCredentials.SQLServer = SQLServer;
        SqlCredentials.SQLUser = SQLUser;
        SqlCredentials.SQLPwd = SQLPwd;
    }

    [ObservableProperty]
    private string _appVersion = string.Empty;

    [RelayCommand]
    private void OnOpenSnackbar(object sender)
    {
        _snackbarService.Show(
            "Don't Blame Yourself.",
            "No Witcher's Ever Died In His Bed.",
            ControlAppearance.Success,
            new SymbolIcon(SymbolRegular.Fluent24),
            TimeSpan.FromSeconds(5)
        );
    }

    [ObservableProperty]
    private bool _isTextBoxEnabled = true;

    [ObservableProperty]
    private string? _sQLServer = "192.168.44.208";
    partial void OnSQLServerChanged(string? value)
    {
        SqlCredentials.SQLServer = value;
    }

    [ObservableProperty]
    private string? _sQLUser = "sa";
    partial void OnSQLUserChanged(string? value)
    {
        SqlCredentials.SQLUser = value;
    }

    [ObservableProperty]
    private string? _sQLPwd = "RustyHearts";
    partial void OnSQLPwdChanged(string? value)
    {
        SqlCredentials.SQLPwd = value;
    }

    private static string GetAssemblyVersion()
    {
        return Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? string.Empty;
    }
}
