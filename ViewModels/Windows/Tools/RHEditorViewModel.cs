using RHToolkit.Models.Editor;
using RHToolkit.Models.MessageBox;
using RHToolkit.Views.Windows;
using System.Windows.Controls;

namespace RHToolkit.ViewModels.Windows
{
    public partial class RHEditorViewModel : ObservableObject
    {
        private readonly Guid _token;

        public RHEditorViewModel()
        {
            _token = Guid.NewGuid();
            DataTableManager = new()
            {
                Token = _token
            };
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
                RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
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
                RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
            }
        }

        private void IsLoaded()
        {
            Title = string.Format(Resources.EditorTitleFileName, "RH Table", DataTableManager.CurrentFileName);
            OpenMessage = "";
            OnCanExecuteFileCommandChanged();
            IsVisible = Visibility.Visible;
        }

        [RelayCommand(CanExecute = nameof(CanExecuteFileCommand))]
        private void OpenSearchDialog(string? parameter)
        {
            try
            {
                Window? window = Application.Current.Windows.OfType<RHEditorWindow>().FirstOrDefault();
                Window owner = window ?? Application.Current.MainWindow;
                DataTableManager.OpenSearchDialog(owner, parameter, DataGridSelectionUnit.CellOrRowHeader);

            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
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
            Title = string.Format(Resources.EditorTitle, "RH Table");
            OpenMessage = Resources.OpenFile;
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
                RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
            }
        }
        #endregion

        #endregion

        #region Properties
        [ObservableProperty]
        private string _title = string.Format(Resources.EditorTitle, "RH Table");

        [ObservableProperty]
        private string? _openMessage = Resources.OpenFile;

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
