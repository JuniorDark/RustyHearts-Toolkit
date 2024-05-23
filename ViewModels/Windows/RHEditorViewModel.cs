using Microsoft.Win32;
using RHToolkit.Models.Editor;
using RHToolkit.Models.MessageBox;
using RHToolkit.Models.RH;
using RHToolkit.Properties;
using System.Data;

namespace RHToolkit.ViewModels.Windows
{
    public partial class RHEditorViewModel : ObservableObject
    {
        private Stack<EditHistory> _undoStack = new();
        private Stack<EditHistory> _redoStack = new();

        #region Read

        [RelayCommand]
        private async Task LoadFile()
        {
            OpenFileDialog openFileDialog = new()
            {
                Filter = "Rusty Hearts Table Files (*.rh)|*.rh|All Files (*.*)|*.*",
                FilterIndex = 1
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    RhData = null;
                    OriginalRhData = null;

                    CurrentFile = openFileDialog.FileName;
                    CurrentFileName = Path.GetFileName(CurrentFile);
                    Title = $"RH Table Editor ({CurrentFileName})";

                    RhData = await ProcessFileAsync(CurrentFile);
                    OriginalRhData = RhData?.Clone();

                }
                catch (Exception ex)
                {
                    RHMessageBox.ShowOKMessage($"Error loading rh file: {ex.Message}", Resources.Error);
                }
                
            }
        }

        private async Task<DataTable?> ProcessFileAsync(string sourceFile)
        {
            using FileStream sourceFileStream = File.OpenRead(sourceFile);
            byte[] buffer = new byte[4096];
            int bytesRead;

            using MemoryStream memoryStream = new();
            while ((bytesRead = await sourceFileStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await memoryStream.WriteAsync(buffer, 0, bytesRead);
            }

            byte[] sourceBytes = memoryStream.ToArray();

            return DataTableCryptor.RhToDataTable(sourceBytes);
        }

        [RelayCommand(CanExecute = nameof(CanExecuteSaveCommand))]
        private async Task SaveFile()
        {
            if (RhData == null) return;
            if (CurrentFile == null) return;
            try
            {
                string file = CurrentFile;
                byte[] encryptedData = DataTableCryptor.DataTableToRh(RhData);
                await File.WriteAllBytesAsync(file, encryptedData);
                OriginalRhData = RhData;
            }
            catch (Exception ex)
            {
                RHMessageBox.ShowOKMessage($"Error saving file: {ex.Message}", "Save File Error");
            }
        }

        [RelayCommand(CanExecute = nameof(CanExecuteSaveAsCommand))]
        private async Task SaveFileAs()
        {
            if (RhData == null) return;
            if (CurrentFileName == null) return;

            SaveFileDialog saveFileDialog = new()
            {
                Filter = "Rusty Hearts Table Files (*.rh)|*.rh|All Files (*.*)|*.*",
                FilterIndex = 1,
                FileName = CurrentFileName
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    string file = saveFileDialog.FileName;
                    byte[] encryptedData = DataTableCryptor.DataTableToRh(RhData);
                    await File.WriteAllBytesAsync(file, encryptedData);
                    OriginalRhData = RhData;
                    RHMessageBox.ShowOKMessage("File saved successfully.", "Save Successful");
                }
                catch (Exception ex)
                {
                    RHMessageBox.ShowOKMessage($"Error saving file: {ex.Message}", "Save File Error");
                }
            }
        }

        [RelayCommand(CanExecute = nameof(CanExecuteCloseCommand))]
        private async Task CloseFile()
        {
            if (RhData == null) return;
            try
            {
                if (!RhData.Equals(OriginalRhData))
                {
                    var result = RHMessageBox.ConfirmMessageYesNoCancel($"Save file '{CurrentFileName}' ?");
                    if (result == MessageBoxResult.Yes)
                    {
                        await SaveFile();
                        RhData = null;
                        OriginalRhData = null;
                        CurrentFile = null;
                        CurrentFileName = null;
                        Title = $"RH Table Editor";
                    }
                    else if (result == MessageBoxResult.No)
                    {
                        RhData = null;
                        OriginalRhData = null;
                        CurrentFile = null;
                        CurrentFileName = null;
                        Title = $"RH Table Editor";
                    }
                    else if (result == MessageBoxResult.Cancel)
                    {
                        return;
                    }
                }
                else
                {
                    RhData = null;
                    OriginalRhData = null;
                    CurrentFile = null;
                    CurrentFileName = null;
                    Title = $"RH Table Editor";
                }
            }
            catch (Exception ex)
            {
                RHMessageBox.ShowOKMessage($"Error loading rh file: {ex.Message}", Resources.Error);
            }
        }

        private bool CanExecuteSaveCommand()
        {
            if (RhData != null)
            {
                return !RhData.Equals(OriginalRhData);
            }

            return false;
        }

        private bool CanExecuteSaveAsCommand()
        {
            return RhData != null;
        }

        private bool CanExecuteCloseCommand()
        {
            return RhData != null;
        }

        [RelayCommand(CanExecute = nameof(CanExecuteSelectedRowCommand))]
        private void DuplicateSelectedRow()
        {
            if (SelectedItem != null && RhData != null)
            {
                DataRow originalRow = SelectedItem.Row;
                DataRow duplicate = RhData.NewRow();

                for (int i = 0; i < RhData.Columns.Count; i++)
                {
                    duplicate[i] = originalRow[i];
                }

                int selectedIndex = RhData.Rows.IndexOf(originalRow);
                RhData.Rows.InsertAt(duplicate, selectedIndex + 1);

                _undoStack.Push(new EditHistory
                {
                    Row = selectedIndex + 1,
                    AffectedRow = duplicate,
                    Action = EditAction.RowInsert,
                    OldValue = originalRow.ItemArray
                });
                _redoStack.Clear();
                OnCanExecuteChangesChanged();
            }
        }

        [RelayCommand(CanExecute = nameof(CanExecuteSelectedRowCommand))]
        private void DeleteSelectedRow()
        {
            if (SelectedItem != null && RhData != null)
            {
                DataRow deletedRow = SelectedItem.Row;
                int rowIndex = RhData.Rows.IndexOf(deletedRow);

                object?[] deletedRowValues = deletedRow.ItemArray;

                RhData.Rows.Remove(deletedRow);

                _undoStack.Push(new EditHistory
                {
                    Row = rowIndex,
                    AffectedRow = deletedRow,
                    Action = EditAction.RowDelete,
                    OldValue = deletedRowValues
                });
                _redoStack.Clear();
                OnCanExecuteChangesChanged();

                Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() =>
                {
                    if (rowIndex > 0)
                    {
                        SelectedItem = RhData.DefaultView[rowIndex - 1];
                    }
                    else if (RhData.Rows.Count > 0)
                    {
                        SelectedItem = RhData.DefaultView[rowIndex];
                    }
                }), DispatcherPriority.ContextIdle);
            }
        }

        private bool CanExecuteSelectedRowCommand()
        {
            return SelectedItem != null;
        }

        [RelayCommand(CanExecute = nameof(CanUndo))]
        private void UndoChanges()
        {
            if (_undoStack.Count > 0)
            {
                var edit = _undoStack.Pop();
                _redoStack.Push(edit);

                switch (edit.Action)
                {
                    case EditAction.CellEdit:
                        RhData!.Rows[edit.Row][edit.Column] = edit.OldValue;
                        break;
                    case EditAction.RowInsert:
                        RhData!.Rows.RemoveAt(edit.Row);
                        break;
                    case EditAction.RowDelete:
                        DataRow newRow = RhData!.NewRow();
                        newRow.ItemArray = (object?[])edit.OldValue!;
                        RhData.Rows.InsertAt(newRow, edit.Row);
                        break;
                }

                OnCanExecuteChangesChanged();
            }
        }

        [RelayCommand(CanExecute = nameof(CanRedo))]
        private void RedoChanges()
        {
            if (_redoStack.Count > 0)
            {
                var edit = _redoStack.Pop();
                _undoStack.Push(edit);

                switch (edit.Action)
                {
                    case EditAction.CellEdit:
                        RhData!.Rows[edit.Row][edit.Column] = edit.NewValue;
                        break;
                    case EditAction.RowInsert:
                        if (edit.AffectedRow != null)
                        {
                            DataRow duplicatedRow = RhData!.NewRow();
                            duplicatedRow.ItemArray = ((object?[])edit.OldValue)!;
                            RhData.Rows.InsertAt(duplicatedRow, edit.Row);
                        }
                        break;
                    case EditAction.RowDelete:
                        RhData!.Rows.RemoveAt(edit.Row);
                        break;
                }

                OnCanExecuteChangesChanged();
            }
        }

        private bool CanUndo() => _undoStack.Count > 0;
        private bool CanRedo() => _redoStack.Count > 0;

        public void RecordEdit(int row, int column, object? oldValue, object? newValue)
        {
            _undoStack.Push(new EditHistory
            {
                Row = row,
                Column = column,
                OldValue = oldValue,
                NewValue = newValue,
                Action = EditAction.CellEdit
            });
            _redoStack.Clear();
            OnCanExecuteChangesChanged();
        }

        public void RecordRowAddition(int rowIndex, object?[] newRowValues)
        {
            // Record the addition of the row for undo/redo
            _undoStack.Push(new EditHistory
            {
                Row = rowIndex,
                Action = EditAction.RowInsert,
                NewValue = newRowValues
            });
            _redoStack.Clear();
            OnCanExecuteChangesChanged();
        }


        private void OnCanExecuteChangesChanged()
        {
            ((RelayCommand)UndoChangesCommand).NotifyCanExecuteChanged();
            ((RelayCommand)RedoChangesCommand).NotifyCanExecuteChanged();
        }

        #endregion

        #region Properties
        [ObservableProperty]
        private string _title = $"RH Table Editor";

        [ObservableProperty]
        private string? _openMessage = "Open a file";

        [ObservableProperty]
        private DataTable? _rhData;
        partial void OnRhDataChanged(DataTable? value)
        {
            OpenMessage = value == null ? "Open a file" : "";
            IsButtonEnabled = value == null ? false : true;
            ((AsyncRelayCommand)SaveFileCommand).NotifyCanExecuteChanged();
            ((AsyncRelayCommand)SaveFileAsCommand).NotifyCanExecuteChanged();
            ((AsyncRelayCommand)CloseFileCommand).NotifyCanExecuteChanged();
        }

        [ObservableProperty]
        private DataRowView? _selectedItem;

        [ObservableProperty]
        private DataTable? _originalRhData;
        partial void OnOriginalRhDataChanged(DataTable? value)
        {
            ((AsyncRelayCommand)SaveFileCommand).NotifyCanExecuteChanged();
        }

        [ObservableProperty]
        private string? _currentFile;

        [ObservableProperty]
        private string? _currentFileName;

        [ObservableProperty]
        private bool _isButtonEnabled = false;

        #endregion
    }
}
