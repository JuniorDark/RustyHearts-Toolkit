using RHToolkit.Models;
using RHToolkit.Models.Localization;
using RHToolkit.Models.UISettings;
using RHToolkit.Services;
using Wpf.Ui;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using Wpf.Ui.Extensions;

namespace RHToolkit.ViewModels.Pages;

public sealed partial class SettingsViewModel(ISqlDatabaseService databaseService, INavigationService navigationService, ISnackbarService snackbarService) : ObservableObject
{
    private readonly ISqlDatabaseService _databaseService = databaseService;
    private readonly INavigationService _navigationService = navigationService;
    private readonly ISnackbarService _snackbarService = snackbarService;

    private bool _isInitialized = false;

    [ObservableProperty]
    private string _appVersion = string.Empty;

    [ObservableProperty]
    private ApplicationTheme _currentApplicationTheme = ApplicationTheme.Unknown;
    partial void OnCurrentApplicationThemeChanged(ApplicationTheme value)
    {
        RegistrySettingsHelper.SetAppTheme(value);
    }

    [ObservableProperty]
    private bool _isUserLanguageChange = false;

    public void HandleLanguageSelectionChange()
    {
        IsUserLanguageChange = true;
    }

    [ObservableProperty]
    private string _currentApplicationLanguage = "English";
    partial void OnCurrentApplicationLanguageChanged(string? oldValue, string newValue)
    {
        string languageCode;
        switch (newValue)
        {
            case "English":
                languageCode = "en-US";
                break;
            case "한국어":
                languageCode = "ko-KR";
                break;
            default:
                languageCode = "en-US";
                break;
        }

        RegistrySettingsHelper.SetAppLanguage(languageCode);
        LocalizationManager.LoadLocalizedStrings(languageCode);

        if (IsUserLanguageChange)
        {
            _snackbarService.Show(
                        Resources.LanguageChanged,
                        Resources.LanguageChangedDesc,
                        ControlAppearance.Success,
                        new SymbolIcon(SymbolRegular.LocalLanguage24),
                        TimeSpan.FromSeconds(5)
                    );
        }
        
    }

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

    [ObservableProperty]
    private string[] _themes = [Resources.ThemeDark, Resources.ThemeLight];

    [ObservableProperty]
    private string[] _languages = ["English", "한국어"];

    public void LoadSettings()
    {
        IsUserLanguageChange = false;

        CurrentApplicationTheme = RegistrySettingsHelper.GetAppTheme();
        CurrentApplicationLanguage = GetLanguageDisplayName(RegistrySettingsHelper.GetAppLanguage());
        SQLServer = RegistrySettingsHelper.GetSQLServer();
        SQLUser = RegistrySettingsHelper.GetSQLUser();
        SQLPwd = RegistrySettingsHelper.GetSQLPassword();
        SqlCredentials.SQLServer = SQLServer;
        SqlCredentials.SQLUser = SQLUser;
        SqlCredentials.SQLPwd = SQLPwd;
    }

    private static string GetLanguageDisplayName(string languageCode)
    {
        return languageCode switch
        {
            "en-US" => "English",
            "ko-KR" => "한국어",
            _ => "English",
        };
    }

    private void InitializeViewModel()
    {
        AppVersion = $"{GetAssemblyVersion()}";

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
             Resources.SQLInfo,
             Resources.SQLEmptyDesc,
             ControlAppearance.Caution,
             new SymbolIcon(SymbolRegular.DatabaseWarning20),
             TimeSpan.FromSeconds(5)
         );
            return;
        }

        IsTextBoxEnabled = false;
        TestButton = Resources.Connecting;
        (bool connectionTestResult, string errorMessage) = await _databaseService.TestDatabaseConnectionAsync();

        IsTextBoxEnabled = true;
        TestButton = Resources.TestConnection;

        if (!connectionTestResult)
        {
            _snackbarService.Show(
            Resources.SQLError,
            $"{Resources.SQLConnectionError}. {Resources.Error}: {errorMessage}",
            ControlAppearance.Danger,
            new SymbolIcon(SymbolRegular.DatabaseWarning20),
            TimeSpan.FromSeconds(5)
        );
            return;
        }
        else
        {
            _snackbarService.Show(
            Resources.TestConnection,
            Resources.ConnectionSuccess,
            ControlAppearance.Success,
            new SymbolIcon(SymbolRegular.DatabasePlugConnected20),
            TimeSpan.FromSeconds(5)
        );
        }
    }

    [ObservableProperty]
    private bool _isTextBoxEnabled = true;

    [ObservableProperty]
    private string _testButton = Resources.TestConnection;

    [ObservableProperty]
    private string _sQLServer = "127.0.0.1";
    partial void OnSQLServerChanged(string value)
    {
        RegistrySettingsHelper.SetSQLServer(value);
        SqlCredentials.SQLServer = value;
    }

    [ObservableProperty]
    private string _sQLUser = "sa";
    partial void OnSQLUserChanged(string value)
    {
        RegistrySettingsHelper.SetSQLUser(value);
        SqlCredentials.SQLUser = value;
    }

    [ObservableProperty]
    private string _sQLPwd = string.Empty;
    partial void OnSQLPwdChanged(string value)
    {
        RegistrySettingsHelper.SetSQLPassword(value);
        SqlCredentials.SQLPwd = value;
    }
}
