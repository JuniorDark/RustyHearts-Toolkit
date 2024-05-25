﻿using Microsoft.Win32;
using RHToolkit.Models.MessageBox;
using RHToolkit.Models.SQLite;
using RHToolkit.Services;

namespace RHToolkit.ViewModels.Pages
{
    public partial class GMDatabaseManagerViewModel(ISqLiteDatabaseService sqLiteDatabaseService, IGMDatabaseService gmDatabaseService, CachedDataManager cachedDataManager) : ObservableObject
    {
        private readonly ISqLiteDatabaseService _sqLiteDatabaseService = sqLiteDatabaseService;
        private readonly IGMDatabaseService _gmDatabaseService = gmDatabaseService;
        private readonly CachedDataManager _cachedDataManager = cachedDataManager;
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
                if (RHMessageBox.ConfirmMessage("Create a new gmdb?"))
                {
                    _cancellationTokenSource = new CancellationTokenSource();

                    CancelOperationCommand.NotifyCanExecuteChanged();

                    await GMDatabaseManager.CreateGMDatabase(SelectedFolder, ReportProgress, _cancellationTokenSource.Token);

                    await Task.Delay(100);

                    _cachedDataManager.InitializeCachedLists();

                    RHMessageBox.ShowOKMessage("Database created successfully!", "Success");
                }
            }
            catch (OperationCanceledException)
            {
                RHMessageBox.ShowOKMessage("Operation was canceled.", "Canceled");
            }
            catch (Exception ex)
            {
                RHMessageBox.ShowOKMessage($"Error creating database file: {ex.Message}", "Error");
            }
            finally
            {
                _cancellationTokenSource = null;
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
        private string? _selectedFolder;
        partial void OnSelectedFolderChanged(string? value)
        {
            CreateDatabaseCommand.NotifyCanExecuteChanged();
        }

        [ObservableProperty]
        private string? _progressMessage;

        #endregion
    }
}
