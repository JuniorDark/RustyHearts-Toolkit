using Microsoft.Win32;
using RHToolkit.Models.MessageBox;
using RHToolkit.Models.SQLite;
using RHToolkit.Models.UISettings;
using RHToolkit.Services;

namespace RHToolkit.ViewModels.Pages
{
    public partial class GMDatabaseManagerViewModel(ISqLiteDatabaseService sqLiteDatabaseService, IGMDatabaseService gmDatabaseService, CachedDataManager cachedDataManager) : ObservableObject
    {
        private readonly ISqLiteDatabaseService _sqLiteDatabaseService = sqLiteDatabaseService;
        private readonly IGMDatabaseService _gmDatabaseService = gmDatabaseService;
        private readonly CachedDataManager _cachedDataManager = cachedDataManager;
        private readonly GMDatabaseManager _gmDatabaseManager = new();
        private CancellationTokenSource? _cancellationTokenSource;

        #region Commands

        [RelayCommand]
        private void SelectFolder()
        {
            OpenFolderDialog openFolderDialog = new();

            if (openFolderDialog.ShowDialog() == true)
            {
                SelectedFolder = openFolderDialog.FolderName;
            }
        }

        [RelayCommand(CanExecute = nameof(CanExecuteCreateDatabaseCommand))]
        private async Task CreateDatabase()
        {
            if (SelectedFolder == null) return;

            try
            {
                if (RHMessageBoxHelper.ConfirmMessage("Create a new gmdb database?"))
                {
                    _cancellationTokenSource = new CancellationTokenSource();

                    CancelOperationCommand.NotifyCanExecuteChanged();
                    IsVisible = Visibility.Visible;
                    await _gmDatabaseManager.CreateGMDatabase(SelectedFolder, ReportProgress, _cancellationTokenSource.Token);

                    await Task.Delay(200);

                    _cachedDataManager.InitializeCachedLists();

                    RHMessageBoxHelper.ShowOKMessage("Database created successfully!", "Success");
                }
            }
            catch (OperationCanceledException)
            {
                RHMessageBoxHelper.ShowOKMessage("Operation was canceled.", "Canceled");
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"Error creating database file: {ex.Message}", "Error");
            }
            finally
            {
                _cancellationTokenSource = null;
                IsVisible = Visibility.Hidden;
                CancelOperationCommand.NotifyCanExecuteChanged();
                CreateDatabaseCommand.NotifyCanExecuteChanged();
            }
        }

        [RelayCommand(CanExecute = nameof(CanExecuteCancelOperationCommand))]
        private void CancelOperation()
        {
            _cancellationTokenSource?.Cancel();
        }

        private bool CanExecuteCancelOperationCommand() => _cancellationTokenSource != null;

        private void ReportProgress(string message)
        {
            ProgressMessage = message;
        }

        private bool CanExecuteCreateDatabaseCommand()
        {
            return !string.IsNullOrWhiteSpace(SelectedFolder) && _cancellationTokenSource == null;
        }

        #endregion

        #region Properties

        [ObservableProperty]
        private Visibility _isVisible = Visibility.Hidden;

        [ObservableProperty]
        private string? _selectedFolder = RegistrySettingsHelper.GetTableFolder();
        partial void OnSelectedFolderChanged(string? value)
        {
            CreateDatabaseCommand.NotifyCanExecuteChanged();
        }

        [ObservableProperty]
        private string? _progressMessage;

        #endregion
    }
}
