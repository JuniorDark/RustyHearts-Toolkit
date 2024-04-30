using RHToolkit.Models;
using RHToolkit.Services;
using Wpf.Ui;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using Wpf.Ui.Extensions;

namespace RHToolkit.ViewModels.Pages;

public sealed partial class SettingsViewModel(ISqlDatabaseService databaseService, INavigationService navigationService, ISnackbarService snackbarService) : ObservableObject, INavigationAware
{
    private readonly ISqlDatabaseService _databaseService = databaseService;
    private readonly INavigationService _navigationService = navigationService;
    private readonly ISnackbarService _snackbarService = snackbarService;

    private bool _isInitialized = false;

    [ObservableProperty]
    private string _appVersion = string.Empty;

    [ObservableProperty]
    private ApplicationTheme _currentApplicationTheme = ApplicationTheme.Unknown;

    [ObservableProperty]
    private NavigationViewPaneDisplayMode _currentApplicationNavigationStyle =
        NavigationViewPaneDisplayMode.Left;

    public void OnNavigatedTo()
    {
        if (!_isInitialized)
        {
            InitializeViewModel();
        }
    }

    public void OnNavigatedFrom() { }

    partial void OnCurrentApplicationThemeChanged(ApplicationTheme oldValue, ApplicationTheme newValue)
    {
        ApplicationThemeManager.Apply(newValue);
    }

    partial void OnCurrentApplicationNavigationStyleChanged(
        NavigationViewPaneDisplayMode oldValue,
        NavigationViewPaneDisplayMode newValue
    )
    {
        _ = _navigationService.SetPaneDisplayMode(newValue);
    }

    private void InitializeViewModel()
    {
        CurrentApplicationTheme = ApplicationThemeManager.GetAppTheme();
        AppVersion = $"{GetAssemblyVersion()}";

        SqlCredentials.SQLServer = SQLServer;
        SqlCredentials.SQLUser = SQLUser;
        SqlCredentials.SQLPwd = SQLPwd;

        ApplicationThemeManager.Changed += OnThemeChanged;

        _isInitialized = true;
    }

    private void OnThemeChanged(ApplicationTheme currentApplicationTheme, Color systemAccent)
    {
        // Update the theme if it has been changed elsewhere than in the settings.
        if (CurrentApplicationTheme != currentApplicationTheme)
        {
            CurrentApplicationTheme = currentApplicationTheme;
        }
    }

    private static string GetAssemblyVersion()
    {
        return Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? string.Empty;
    }

    [RelayCommand]
    private async Task OnOpenSqlConnection()
    {
        if (string.IsNullOrEmpty(SQLServer) || string.IsNullOrEmpty(SQLUser) || string.IsNullOrEmpty(SQLPwd))
        {
            _snackbarService.Show(
             "SQL Info",
             "Server address, SQL account, and SQL password cannot be empty!",
             ControlAppearance.Caution,
             new SymbolIcon(SymbolRegular.DatabaseWarning20),
             TimeSpan.FromSeconds(5)
         );
            return;
        }

        IsTextBoxEnabled = false;
        TestButton = "Connecting...";
        (bool connectionTestResult, string errorMessage) = await _databaseService.TestDatabaseConnectionAsync();

        IsTextBoxEnabled = true;
        TestButton = "Test Connection";

        if (!connectionTestResult)
        {
            _snackbarService.Show(
            "SQL Error",
            $"Failed to establish a connection with the SQL server. Error: {errorMessage}",
            ControlAppearance.Danger,
            new SymbolIcon(SymbolRegular.DatabaseWarning20),
            TimeSpan.FromSeconds(5)
        );
            return;
        }
        else
        {
            _snackbarService.Show(
            "Connection Test.",
            "Connection success!",
            ControlAppearance.Success,
            new SymbolIcon(SymbolRegular.DatabasePlugConnected20),
            TimeSpan.FromSeconds(5)
        );
        }
    }

    [ObservableProperty]
    private bool _isTextBoxEnabled = true;

    [ObservableProperty]
    private string _testButton = "Test Connection";

    [ObservableProperty]
    private string? _sQLServer = "192.168.100.3";
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
}
