using Microsoft.Win32;
using RHToolkit.Models.Editor;
using RHToolkit.Models.MessageBox;
using RHToolkit.Properties;
using RHToolkit.Views.Windows;
using System.Windows.Controls;

namespace RHToolkit.ViewModels.Windows
{
    public partial class RHEditorViewModel : ObservableObject
    {
        private readonly FileManager _fileManager = new();
        private readonly Stack<EditHistory> _undoStack = new();
        private readonly Stack<EditHistory> _redoStack = new();

        public RHEditorViewModel()
        {
            DataTableManager = new DataTableManager();
        }

        #region Commands 
        [RelayCommand]
        private async Task CloseWindow(Window window)
        {
            try
            {
                await CloseFile();

                window?.Close();

            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"Error: {ex.Message}", Resources.Error);
            }
        }

        #region File

        [RelayCommand]
        private async Task LoadFile()
        {
            try
            {
                await CloseFile();

                OpenFileDialog openFileDialog = new()
                {
                    Filter = "Rusty Hearts Table Files (*.rh)|*.rh|All Files (*.*)|*.*",
                    FilterIndex = 1
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    var cashShopTable = await _fileManager.FileToDataTableAsync(openFileDialog.FileName);

                    if (cashShopTable != null)
                    {

                        ClearFile();

                        CurrentFile = openFileDialog.FileName;
                        CurrentFileName = Path.GetFileName(CurrentFile);
                        if (DataTableManager != null)
                        {
                            DataTableManager.LoadFile(cashShopTable);
                            DataTableManager.CurrentFile = openFileDialog.FileName;
                            DataTableManager.CurrentFileName = Path.GetFileName(CurrentFile);
                        }

                        Title = $"RH Table Editor ({CurrentFileName})";
                        OpenMessage = "";
                        OnCanExecuteFileCommandChanged();
                        IsVisible = Visibility.Visible;
                    }
                }

            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"Error: {ex.Message}", Resources.Error);
            }
        }

        [RelayCommand(CanExecute = nameof(CanExecuteFileCommand))]
        private void OpenSearchDialog(string? parameter)
        {
            try
            {
                if (DataTableManager != null)
                {
                    Window? rhEditorWindow = Application.Current.Windows.OfType<RHEditorWindow>().FirstOrDefault();
                    Window owner = rhEditorWindow ?? Application.Current.MainWindow;
                    DataTableManager.OpenSearchDialog(owner, parameter, DataGridSelectionUnit.CellOrRowHeader);
                }

            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"Error: {ex.Message}", Resources.Error);
            }

        }

        [RelayCommand(CanExecute = nameof(CanExecuteFileCommand))]
        public async Task<bool> CloseFile()
        {
            if (DataTableManager == null) return true;

            var close = await DataTableManager.CloseFile();

            if (close)
            {
                ClearFile();
                return true;
            }

            return false;
        }

        private void ClearFile()
        {
            CurrentFile = null;
            CurrentFileName = null;
            Title = $"RH Table Editor";
            OpenMessage = "Open a file";
            IsVisible = Visibility.Hidden;
            OnCanExecuteFileCommandChanged();
        }

        private bool CanExecuteFileCommand()
        {
            return DataTableManager != null && DataTableManager.DataTable != null;
        }

        private void OnCanExecuteFileCommandChanged()
        {
            CloseFileCommand.NotifyCanExecuteChanged();
            OpenSearchDialogCommand.NotifyCanExecuteChanged();
        }

        #endregion

        #endregion

        #region Properties
        [ObservableProperty]
        private string _title = $"RH Table Editor";

        [ObservableProperty]
        private string? _openMessage = "Open a file";

        [ObservableProperty]
        private Visibility _isVisible = Visibility.Hidden;

        [ObservableProperty]
        private string? _currentFile;

        [ObservableProperty]
        private string? _currentFileName;

        [ObservableProperty]
        private DataTableManager? _dataTableManager;
        partial void OnDataTableManagerChanged(DataTableManager? value)
        {
            OnCanExecuteFileCommandChanged();
        }

        #endregion
    }
}
