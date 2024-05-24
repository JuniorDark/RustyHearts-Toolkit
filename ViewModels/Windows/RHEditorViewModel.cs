using Microsoft.Win32;
using RHToolkit.Models.Editor;
using RHToolkit.Models.MessageBox;
using RHToolkit.Properties;
using System.Data;

namespace RHToolkit.ViewModels.Windows
{
    public partial class RHEditorViewModel : ObservableObject
    {
        private readonly Stack<EditHistory> _undoStack = new();
        private readonly Stack<EditHistory> _redoStack = new();

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
                    ClearFile();

                    CurrentFile = openFileDialog.FileName;
                    CurrentFileName = Path.GetFileName(CurrentFile);
                    FileData = await FileManager.FileToDataTableAsync(CurrentFile);
                    Title = $"RH Table Editor ({CurrentFileName})";

                    if (FileData != null)
                    {
                        FileData.TableNewRow += DataTableChanged;
                        FileData.RowChanged += DataTableChanged;
                        FileData.RowDeleted += DataTableChanged;
                        FileData.ColumnChanged += DataTableChanged;
                    }

                    HasChanges = false;
                    SaveFileCommand.NotifyCanExecuteChanged();
                }
                catch (Exception ex)
                {
                    RHMessageBox.ShowOKMessage($"Error loading rh file: {ex.Message}", Resources.Error);
                }
            }
        }

        private int _changesCounter = 0;
        private const int ChangesBeforeSave = 5;

        private async void DataTableChanged(object sender, EventArgs e)
        {
            HasChanges = true;
            Title = $"RH Table Editor ({CurrentFileName})*";
            SaveFileCommand.NotifyCanExecuteChanged();

            _changesCounter++;
            if (_changesCounter >= ChangesBeforeSave)
            {
                _changesCounter = 0;
                await FileManager.SaveTempFile(CurrentFileName!, FileData!);
            }
        }

        [RelayCommand(CanExecute = nameof(CanExecuteSaveCommand))]
        private async Task SaveFile()
        {
            if (FileData == null) return;
            if (CurrentFile == null) return;
            try
            {
                await FileManager.DataTableToFileAsync(CurrentFile, FileData);
                FileManager.ClearTempFile(CurrentFileName!);

                HasChanges = false;
                SaveFileCommand.NotifyCanExecuteChanged();
            }
            catch (Exception ex)
            {
                RHMessageBox.ShowOKMessage($"Error saving file: {ex.Message}", "Save File Error");
            }
        }

        [RelayCommand(CanExecute = nameof(CanExecuteFileCommand))]
        private async Task SaveFileAs()
        {
            if (FileData == null || CurrentFileName == null) return;

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
                    await FileManager.DataTableToFileAsync(file, FileData);
                    FileManager.ClearTempFile(CurrentFileName);
                    HasChanges = false;
                    SaveFileCommand.NotifyCanExecuteChanged();

                    CurrentFile = file;
                    CurrentFileName = Path.GetFileName(file);
                }
                catch (Exception ex)
                {
                    RHMessageBox.ShowOKMessage($"Error saving file: {ex.Message}", "Save File Error");
                }
            }
        }

        [RelayCommand(CanExecute = nameof(CanExecuteFileCommand))]
        private async Task CloseFile()
        {
            if (FileData == null) return;

            try
            {
                if (HasChanges)
                {
                    var result = RHMessageBox.ConfirmMessageYesNoCancel($"Save file '{CurrentFileName}' ?");
                    if (result == MessageBoxResult.Yes)
                    {
                        await SaveFile();
                        ClearFile();
                    }
                    else if (result == MessageBoxResult.No)
                    {
                        ClearFile();
                    }
                    else if (result == MessageBoxResult.Cancel)
                    {
                        return;
                    }
                }
                else
                {
                    ClearFile();
                }
            }
            catch (Exception ex)
            {
                RHMessageBox.ShowOKMessage($"Error loading rh file: {ex.Message}", Resources.Error);
            }
        }

        private void ClearFile()
        {
            if (FileData != null)
            {
                FileData.TableNewRow -= DataTableChanged;
                FileData.RowChanged -= DataTableChanged;
                FileData.RowDeleted -= DataTableChanged;
                FileData.ColumnChanged -= DataTableChanged;
            }

            FileManager.ClearTempFile(CurrentFileName);
            FileData = null;
            OriginalFileData = null;
            CurrentFile = null;
            CurrentFileName = null;
            Title = $"RH Table Editor";
            _undoStack.Clear();
            _redoStack.Clear();
            HasChanges = false;
            SaveFileCommand.NotifyCanExecuteChanged();
            OnCanExecuteChangesChanged();
        }

        private bool CanExecuteSaveCommand()
        {
            return HasChanges;
        }

        private bool CanExecuteFileCommand()
        {
            return FileData != null;
        }

        private void OnCanExecuteFileCommandChanged()
        {
            SaveFileCommand.NotifyCanExecuteChanged();
            SaveFileAsCommand.NotifyCanExecuteChanged();
            CloseFileCommand.NotifyCanExecuteChanged();
            AddNewRowCommand.NotifyCanExecuteChanged();
        }

        [RelayCommand(CanExecute = nameof(CanExecuteFileCommand))]
        private void AddNewRow()
        {
            if (FileData != null)
            {
                DataRow newRow = FileData.NewRow();

                // Initialize the new row with default values
                foreach (DataColumn column in FileData.Columns)
                {
                    newRow[column] = GetDefaultValue(column.DataType);
                }

                FileData.Rows.Add(newRow);

                int rowIndex = FileData.Rows.IndexOf(newRow);

                _undoStack.Push(new EditHistory
                {
                    Row = rowIndex,
                    AffectedRow = newRow,
                    Action = EditAction.RowInsert,
                    NewValue = newRow.ItemArray
                });
                _redoStack.Clear();
                OnCanExecuteChangesChanged();
                SelectedItem = FileData.DefaultView[rowIndex];
            }
        }

        private static object GetDefaultValue(Type type)
        {
            if (type == typeof(int))
                return 0;
            if (type == typeof(float))
                return 0.0;
            if (type == typeof(string))
                return string.Empty;
            if (type == typeof(long))
                return 0;
            else return 0;
        }


        [RelayCommand(CanExecute = nameof(CanExecuteSelectedRowCommand))]
        private void DuplicateSelectedRow()
        {
            if (SelectedItem != null && FileData != null)
            {
                DataRow originalRow = SelectedItem.Row;
                DataRow duplicate = FileData.NewRow();

                for (int i = 0; i < FileData.Columns.Count; i++)
                {
                    duplicate[i] = originalRow[i];
                }

                int selectedIndex = FileData.Rows.IndexOf(originalRow);
                FileData.Rows.InsertAt(duplicate, selectedIndex + 1);

                _undoStack.Push(new EditHistory
                {
                    Row = selectedIndex + 1,
                    AffectedRow = duplicate,
                    Action = EditAction.RowInsert,
                    NewValue = duplicate.ItemArray
                });
                _redoStack.Clear();
                OnCanExecuteChangesChanged();
            }
        }


        [RelayCommand(CanExecute = nameof(CanExecuteSelectedRowCommand))]
        private void DeleteSelectedRow()
        {
            if (SelectedItem != null && FileData != null)
            {
                DataRow deletedRow = SelectedItem.Row;
                int rowIndex = FileData.Rows.IndexOf(deletedRow);

                object?[] deletedRowValues = deletedRow.ItemArray;

                FileData.Rows.Remove(deletedRow);

                _undoStack.Push(new EditHistory
                {
                    Row = rowIndex,
                    AffectedRow = deletedRow,
                    Action = EditAction.RowDelete,
                    OldValue = deletedRowValues
                });
                _redoStack.Clear();
                
                if (rowIndex > 0)
                {
                    SelectedItem = FileData.DefaultView[rowIndex - 1];
                }
                else if (FileData.Rows.Count > 0)
                {
                    SelectedItem = FileData.DefaultView[rowIndex];
                }

                OnCanExecuteChangesChanged();
            }
        }

        private bool CanExecuteSelectedRowCommand()
        {
            return SelectedItem != null;
        }

        private void OnCanExecuteSelectedRowCommandChanged()
        {
            DuplicateSelectedRowCommand.NotifyCanExecuteChanged();
            DeleteSelectedRowCommand.NotifyCanExecuteChanged();
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
                        if (edit.Row >= 0 && edit.Row < FileData!.Rows.Count)
                        {
                            FileData.Rows[edit.Row][edit.Column] = edit.OldValue;
                        }
                        break;
                    case EditAction.RowInsert:
                        if (edit.Row >= 0 && edit.Row < FileData!.Rows.Count)
                        {
                            FileData.Rows.RemoveAt(edit.Row);
                        }
                        break;
                    case EditAction.RowDelete:
                        if (edit.Row >= 0)
                        {
                            DataRow newRow = FileData!.NewRow();
                            newRow.ItemArray = (object?[])edit.OldValue!;
                            FileData.Rows.InsertAt(newRow, edit.Row);
                        }
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
                        if (edit.Row >= 0 && edit.Row < FileData!.Rows.Count)
                        {
                            FileData.Rows[edit.Row][edit.Column] = edit.NewValue;
                        }
                        break;
                    case EditAction.RowInsert:
                        if (edit.Row >= 0)
                        {
                            DataRow insertRow = FileData!.NewRow();
                            insertRow.ItemArray = (object?[])edit.NewValue!;
                            if (edit.Row < FileData.Rows.Count)
                            {
                                FileData.Rows.InsertAt(insertRow, edit.Row);
                            }
                            else
                            {
                                FileData.Rows.Add(insertRow);
                            }
                        }
                        break;
                    case EditAction.RowDelete:
                        if (edit.Row >= 0 && edit.Row < FileData!.Rows.Count)
                        {
                            FileData.Rows.RemoveAt(edit.Row);
                        }
                        break;
                }

                OnCanExecuteChangesChanged();
            }
        }

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
            _undoStack.Push(new EditHistory
            {
                Row = rowIndex,
                Action = EditAction.RowInsert,
                NewValue = newRowValues
            });
            _redoStack.Clear();
            OnCanExecuteChangesChanged();
        }

        private bool CanUndo() => _undoStack.Count > 0;
        private bool CanRedo() => _redoStack.Count > 0;

        private void OnCanExecuteChangesChanged()
        {
            UndoChangesCommand.NotifyCanExecuteChanged();
            RedoChangesCommand.NotifyCanExecuteChanged();
        }

        #endregion

        #region Properties
        [ObservableProperty]
        private string _title = $"RH Table Editor";

        [ObservableProperty]
        private string? _openMessage = "Open a file";

        [ObservableProperty]
        private DataTable? _fileData;
        partial void OnFileDataChanged(DataTable? value)
        {
            OpenMessage = value == null ? "Open a file" : "";
            OnCanExecuteFileCommandChanged();
        }

        [ObservableProperty]
        private DataRowView? _selectedItem;
        partial void OnSelectedItemChanged(DataRowView? value)
        {
            OnCanExecuteSelectedRowCommandChanged();
        }

        [ObservableProperty]
        private DataTable? _originalFileData;

        [ObservableProperty]
        private string? _currentFile;

        [ObservableProperty]
        private string? _currentFileName;

        [ObservableProperty]
        private bool _hasChanges = false;

        #endregion
    }
}
