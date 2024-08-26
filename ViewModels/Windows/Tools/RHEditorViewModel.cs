using RHToolkit.Models.Editor;
using RHToolkit.Models.MessageBox;
using RHToolkit.Properties;
using RHToolkit.Views.Windows;
using System.Windows.Controls;

namespace RHToolkit.ViewModels.Windows
{
    public partial class RHEditorViewModel : ObservableObject
    {
        public RHEditorViewModel()
        {
            DataTableManager = new DataTableManager();
        }

        #region Commands 

        #region File

        [RelayCommand]
        private async Task LoadFile()
        {
            try
            {
                await CloseFile();

                string filter = "Rusty Hearts Table Files (*.rh)|*.rh|All Files (*.*)|*.*";
                bool isLoaded = await DataTableManager.LoadFile(filter);

                if (isLoaded)
                {
                    IsLoaded();
                }
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"Error: {ex.Message}", Resources.Error);
            }
        }

        [RelayCommand]
        private async Task LoadFileAs()
        {
            try
            {
                await CloseFile();

                string filter = "Rusty Hearts Table Files (*.rh)|*.rh|All Files (*.*)|*.*";
                bool isLoaded = await DataTableManager.LoadFile(filter);

                if (isLoaded)
                {
                    IsLoaded();
                }
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"Error: {ex.Message}", Resources.Error);
            }
        }

        private void IsLoaded()
        {
            Title = $"RH Table Editor ({DataTableManager.CurrentFileName})";
            OpenMessage = "";
            OnCanExecuteFileCommandChanged();
            IsVisible = Visibility.Visible;
        }

        [RelayCommand(CanExecute = nameof(CanExecuteFileCommand))]
        private void OpenSearchDialog(string? parameter)
        {
            try
            {
                Window? rhEditorWindow = Application.Current.Windows.OfType<RHEditorWindow>().FirstOrDefault();
                Window owner = rhEditorWindow ?? Application.Current.MainWindow;
                DataTableManager.OpenSearchDialog(owner, parameter, DataGridSelectionUnit.CellOrRowHeader);

            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"Error: {ex.Message}", Resources.Error);
            }

        }

        [RelayCommand(CanExecute = nameof(CanExecuteFileCommand))]
        public async Task<bool> CloseFile()
        {
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
            Title = $"RH Table Editor";
            OpenMessage = "Open a file";
            IsVisible = Visibility.Hidden;
            OnCanExecuteFileCommandChanged();
        }

        private bool CanExecuteFileCommand()
        {
            return DataTableManager.DataTable != null;
        }

        private void OnCanExecuteFileCommandChanged()
        {
            AddRowCommand.NotifyCanExecuteChanged();
            CloseFileCommand.NotifyCanExecuteChanged();
            OpenSearchDialogCommand.NotifyCanExecuteChanged();
        }

        #endregion

        #region Add Row

        [RelayCommand(CanExecute = nameof(CanExecuteFileCommand))]
        private void AddRow()
        {
            try
            {
                DataTableManager.AddNewRow();
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"Error: {ex.Message}", "Error");
            }
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
        private DataTableManager _dataTableManager;
        partial void OnDataTableManagerChanged(DataTableManager value)
        {
            OnCanExecuteFileCommandChanged();
        }

        #endregion
    }
}
