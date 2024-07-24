using Microsoft.Win32;
using RHToolkit.Models.MessageBox;
using RHToolkit.Properties;
using RHToolkit.Views.Windows;
using System.Data;
using System.Windows.Controls;
using static RHToolkit.Models.MIP.MIPCoder;

namespace RHToolkit.Models.Editor;

public partial class DataTableManager : ObservableObject
{
    private SearchDialog? searchDialog;
    private readonly Stack<EditHistory> _undoStack = new();
    private readonly Stack<EditHistory> _redoStack = new();

    private int? lastFoundRow = null;
    private DataGridSelectionUnit? _selectionUnit = null;
    private Point? lastFoundCell = null;

    private readonly FileManager _fileManager = new();

    #region Search

    public void OpenSearchDialog(Window owner, string? parameter, DataGridSelectionUnit selectionUnit)
    {
        if (searchDialog == null || !searchDialog.IsVisible)
        {
            searchDialog = new SearchDialog
            {
                Owner = owner
            };
            searchDialog.FindNext += Search;
            searchDialog.ReplaceFindNext += Search;
            searchDialog.Replace += Replace;
            searchDialog.ReplaceAll += ReplaceAll;
            searchDialog.CountMatches += CountMatches;
            searchDialog.Show();
        }
        else
        {
            searchDialog.Focus();
        }
        _selectionUnit = selectionUnit;
        searchDialog.SearchTabControl.SelectedIndex = parameter == "Find" ? 0 : 1;
    }

    private void Search(string searchText, bool matchCase)
    {
        if (string.IsNullOrWhiteSpace(searchText) || DataTable == null)
            return;

        StringComparison comparison = matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

        bool found = false;
        bool wrappedAround = false;

        // Clear any existing message at the start of a new search
        searchDialog?.ShowMessage(string.Empty, Brushes.Transparent);

        int startRowIndex = 0;
        int startColIndex = 0;

        if (lastFoundCell != null)
        {
            // Start the search from the next cell after the last found cell
            startRowIndex = (int)lastFoundCell.Value.X;
            startColIndex = (int)lastFoundCell.Value.Y + 1;

            if (startColIndex >= DataTable.Columns.Count)
            {
                // If we've reached the end of the columns, wrap around to the next row
                startRowIndex++;
                startColIndex = 0;

                if (startRowIndex >= DataTable.Rows.Count)
                {
                    // If we've reached the end of the rows, wrap around to the beginning
                    startRowIndex = 0;
                }
            }
        }

        // Iterate through rows starting from the startRowIndex
        for (int rowIndex = startRowIndex; rowIndex < DataTable.Rows.Count + startRowIndex; rowIndex++)
        {
            int currentRowIndex = rowIndex % DataTable.Rows.Count; // Wrap around using modulus operator

            int colStartIndex = (rowIndex == startRowIndex) ? startColIndex : 0; // Start from startColIndex if it's the starting row, otherwise start from the first column

            // Iterate through columns starting from the colStartIndex
            for (int colIndex = colStartIndex; colIndex < DataTable.Columns.Count; colIndex++)
            {
                var cellValue = DataTable.Rows[currentRowIndex][colIndex];
                if (cellValue?.ToString()?.Contains(searchText, comparison) == true)
                {
                    // Found the value
                    found = true;

                    if (_selectionUnit == DataGridSelectionUnit.FullRow)
                    {
                        SelectedRow = currentRowIndex;
                    }
                    else
                    {
                        SelectedCell = new Point(currentRowIndex, colIndex);
                    }

                    lastFoundCell = new Point(currentRowIndex, colIndex); // Update last found cell

                    // Show message if wrapped around
                    if (wrappedAround)
                    {
                        searchDialog?.ShowMessage($"Found the 1st occurrence from the top. The end of the table has been reached.", Brushes.Green);
                    }

                    break;
                }
            }

            if (found)
                break;

            // Mark that we have wrapped around after reaching the end of the rows
            if (rowIndex >= DataTable.Rows.Count - 1)
            {
                wrappedAround = true;
            }
        }

        if (!found)
        {
            if (wrappedAround)
            {
                searchDialog?.ShowMessage($"End of data reached. Search text '{searchText}' not found.", Brushes.Red);
            }
            else
            {
                searchDialog?.ShowMessage($"Search text '{searchText}' not found.", Brushes.Red);
            }
            lastFoundCell = null;
        }
    }

    private void Replace(string searchText, string replaceText, bool matchCase)
    {
        if (string.IsNullOrEmpty(searchText) || DataTable == null)
            return;

        StringComparison comparison = matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

        if (lastFoundCell != null)
        {
            int rowIndex = (int)lastFoundCell.Value.X;
            int colIndex = (int)lastFoundCell.Value.Y;

            object cellValue = DataTable.Rows[rowIndex][colIndex];
            string oldValue = cellValue?.ToString() ?? string.Empty;
            string newValue = oldValue.Replace(searchText, replaceText, comparison);

            if (!string.IsNullOrEmpty(oldValue) && oldValue.Contains(searchText, comparison))
            {
                DataTable.Rows[rowIndex][colIndex] = newValue;
                _undoStack.Push(new EditHistory
                {
                    Row = rowIndex,
                    Column = colIndex,
                    OldValue = oldValue,
                    NewValue = newValue,
                    Action = EditAction.CellEdit
                });
                _redoStack.Clear();
                OnCanExecuteChangesChanged();

                searchDialog?.ShowMessage($"Replaced text in row {rowIndex + 1}, column {colIndex + 1}.", Brushes.Green);
                lastFoundCell = null;
                return;
            }
        }

        Search(searchText, matchCase);
    }

    public void ReplaceAll(string searchText, string replaceText, bool matchCase)
    {
        if (string.IsNullOrEmpty(searchText) || DataTable == null)
            return;

        StringComparison comparison = matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

        int replaceCount = 0;
        List<EditHistory> groupedEdits = [];

        for (int rowIndex = 0; rowIndex < DataTable.Rows.Count; rowIndex++)
        {
            for (int colIndex = 0; colIndex < DataTable.Columns.Count; colIndex++)
            {
                string? oldValue = DataTable.Rows[rowIndex][colIndex].ToString();
                if (!string.IsNullOrEmpty(oldValue) && oldValue.Contains(searchText, comparison))
                {
                    string newValue = oldValue.Replace(searchText, replaceText, comparison);
                    DataTable.Rows[rowIndex][colIndex] = newValue;
                    replaceCount++;

                    groupedEdits.Add(new EditHistory
                    {
                        Row = rowIndex,
                        Column = colIndex,
                        OldValue = oldValue,
                        NewValue = newValue,
                        Action = EditAction.CellEdit
                    });
                }
            }
        }

        if (replaceCount > 0)
        {
            _undoStack.Push(new EditHistory
            {
                Action = EditAction.CellEdit,
                GroupedEdits = groupedEdits
            });
            _redoStack.Clear();
            OnCanExecuteChangesChanged();
        }

        searchDialog?.ShowMessage($"Replaced {replaceCount} occurrences.", Brushes.Green);
    }

    public void CountMatches(string searchText, bool matchCase)
    {
        if (string.IsNullOrEmpty(searchText) || DataTable == null)
            return;

        StringComparison comparison = matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

        int count = DataTable.Rows.Cast<DataRow>().Sum(row => row.ItemArray.Count(item => item != null && item.ToString()?.Contains(searchText, comparison) == true));

        searchDialog?.ShowMessage($"Count: {count} matches in entire table", Brushes.LightBlue);
    }

    #endregion

    #region Row

    [RelayCommand(CanExecute = nameof(CanExecuteFileCommand))]
    public void AddNewRow()
    {
        if (DataTable != null)
        {
            DataRow newRow = DataTable.NewRow();

            // Initialize the new row with default values
            foreach (DataColumn column in DataTable.Columns)
            {
                newRow[column] = GetDefaultValue(column.DataType);
            }

            DataTable.Rows.Add(newRow);

            int rowIndex = DataTable.Rows.IndexOf(newRow);

            _undoStack.Push(new EditHistory
            {
                Row = rowIndex,
                AffectedRow = newRow,
                Action = EditAction.RowInsert,
                NewValue = newRow.ItemArray
            });
            _redoStack.Clear();
            OnCanExecuteChangesChanged();


            SelectedItem = DataTable.DefaultView[rowIndex];
            OnPropertyChanged(nameof(DataTable));
        }
    }

    [RelayCommand(CanExecute = nameof(CanExecuteSelectedRowCommand))]
    public void DuplicateSelectedRow()
    {
        if (SelectedItem != null && DataTable != null)
        {
            DataRow originalRow = SelectedItem.Row;
            DataRow duplicate = DataTable.NewRow();

            for (int i = 0; i < DataTable.Columns.Count; i++)
            {
                duplicate[i] = originalRow[i];
            }

            int selectedIndex = DataTable.Rows.IndexOf(originalRow);
            DataTable.Rows.InsertAt(duplicate, selectedIndex + 1);

            _undoStack.Push(new EditHistory
            {
                Row = selectedIndex + 1,
                AffectedRow = duplicate,
                Action = EditAction.RowInsert,
                NewValue = duplicate.ItemArray
            });
            _redoStack.Clear();
            OnCanExecuteChangesChanged();
            OnPropertyChanged(nameof(DataTable));
        }
    }

    [RelayCommand(CanExecute = nameof(CanExecuteSelectedRowCommand))]
    public void DeleteSelectedRow()
    {
        if (SelectedItem != null && DataTable != null)
        {
            DataRow deletedRow = SelectedItem.Row;
            int rowIndex = DataTable.Rows.IndexOf(deletedRow);

            object?[] deletedRowValues = deletedRow.ItemArray;

            DataTable.Rows.Remove(deletedRow);

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
                SelectedItem = DataTable.DefaultView[rowIndex - 1];
            }
            else if (DataTable.Rows.Count > 0)
            {
                SelectedItem = DataTable.DefaultView[rowIndex];
            }

            OnCanExecuteChangesChanged();
            OnPropertyChanged(nameof(DataTable));
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

    private static object GetDefaultValue(Type type)
    {
        if (type == typeof(int))
            return 0;
        if (type == typeof(float))
            return 0;
        if (type == typeof(string))
            return string.Empty;
        if (type == typeof(long))
            return 0;
        else return 0;
    }

    #endregion

    #region Edit History

    [RelayCommand(CanExecute = nameof(CanUndo))]
    public void UndoChanges()
    {
        if (_undoStack.Count > 0)
        {
            var edit = _undoStack.Pop();
            _redoStack.Push(edit);

            if (edit.GroupedEdits != null)
            {
                foreach (var groupedEdit in edit.GroupedEdits)
                {
                    DataTable!.Rows[groupedEdit.Row][groupedEdit.Column] = groupedEdit.OldValue;
                }
            }
            else
            {
                switch (edit.Action)
                {
                    case EditAction.CellEdit:
                        if (edit.Row >= 0 && edit.Row < DataTable!.Rows.Count)
                        {
                            DataTable.Rows[edit.Row][edit.Column] = edit.OldValue;
                        }
                        break;
                    case EditAction.RowInsert:
                        if (edit.Row >= 0 && edit.Row < DataTable!.Rows.Count)
                        {
                            DataTable.Rows.RemoveAt(edit.Row);
                        }
                        break;
                    case EditAction.RowDelete:
                        if (edit.Row >= 0)
                        {
                            DataRow newRow = DataTable!.NewRow();
                            newRow.ItemArray = (object?[])edit.OldValue!;
                            DataTable.Rows.InsertAt(newRow, edit.Row);
                        }
                        break;
                }
            }

            OnCanExecuteChangesChanged();
        }
    }

    [RelayCommand(CanExecute = nameof(CanRedo))]
    public void RedoChanges()
    {
        if (_redoStack.Count > 0)
        {
            var edit = _redoStack.Pop();
            _undoStack.Push(edit);

            if (edit.GroupedEdits != null)
            {
                foreach (var groupedEdit in edit.GroupedEdits)
                {
                    DataTable!.Rows[groupedEdit.Row][groupedEdit.Column] = groupedEdit.NewValue;
                }
            }
            else
            {
                switch (edit.Action)
                {
                    case EditAction.CellEdit:
                        if (edit.Row >= 0 && edit.Row < DataTable!.Rows.Count)
                        {
                            DataTable.Rows[edit.Row][edit.Column] = edit.NewValue;
                        }
                        break;
                    case EditAction.RowInsert:
                        if (edit.Row >= 0)
                        {
                            DataRow insertRow = DataTable!.NewRow();
                            insertRow.ItemArray = (object?[])edit.NewValue!;
                            if (edit.Row < DataTable.Rows.Count)
                            {
                                DataTable.Rows.InsertAt(insertRow, edit.Row);
                            }
                            else
                            {
                                DataTable.Rows.Add(insertRow);
                            }
                        }
                        break;
                    case EditAction.RowDelete:
                        if (edit.Row >= 0 && edit.Row < DataTable!.Rows.Count)
                        {
                            DataTable.Rows.RemoveAt(edit.Row);
                        }
                        break;
                }
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

    public void OnCanExecuteChangesChanged()
    {
        UndoChangesCommand.NotifyCanExecuteChanged();
        RedoChangesCommand.NotifyCanExecuteChanged();
    }
    #endregion

    #region Commands

    #region Save File

    public void LoadFile(DataTable dataTable)
    {
        try
        {
            if (dataTable != null)
            {
                DataTable = dataTable;

                DataTable.TableNewRow += DataTableChanged;
                DataTable.RowChanged += DataTableChanged;
                DataTable.RowDeleted += DataTableChanged;
            }

            HasChanges = false;
            OnCanExecuteFileCommandChanged();
        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"Error loading rh file: {ex.Message}", Resources.Error);
        }
    }

    private int _changesCounter = 0;
    private const int ChangesBeforeSave = 10;

    private async void DataTableChanged(object sender, EventArgs e)
    {
        HasChanges = true;

        _changesCounter++;
        if (_changesCounter >= ChangesBeforeSave)
        {
            try
            {
                _changesCounter = 0;
                await _fileManager.SaveTempFile(CurrentFileName!, DataTable!);
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"Error saving backup file: {ex.Message}", "Save File Error");
            }
        }
    }

    [RelayCommand(CanExecute = nameof(HasChanges))]
    public async Task SaveFile()
    {
        if (DataTable == null) return;
        if (CurrentFile == null) return;
        try
        {
            await _fileManager.DataTableToFileAsync(CurrentFile, DataTable);
            FileManager.ClearTempFile(CurrentFileName!);

            HasChanges = false;
        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"Error saving file: {ex.Message}", "Save File Error");
        }
    }

    [RelayCommand(CanExecute = nameof(CanExecuteFileCommand))]
    public async Task SaveFileAs()
    {
        if (DataTable == null || CurrentFileName == null) return;

        SaveFileDialog saveFileDialog = new()
        {
            Filter = "Rusty Hearts Table File (*.rh)|*.rh|All Files (*.*)|*.*",
            FilterIndex = 1,
            FileName = CurrentFileName
        };

        if (saveFileDialog.ShowDialog() == true)
        {
            try
            {
                string file = saveFileDialog.FileName;
                await _fileManager.DataTableToFileAsync(file, DataTable);
                FileManager.ClearTempFile(CurrentFileName);
                HasChanges = false;
                CurrentFile = file;
                CurrentFileName = Path.GetFileName(file);
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"Error saving file: {ex.Message}", "Save File Error");
            }
        }
    }

    [RelayCommand(CanExecute = nameof(CanExecuteFileCommand))]
    public async Task SaveFileAsMIP()
    {
        if (DataTable == null || CurrentFileName == null) return;

        SaveFileDialog saveFileDialog = new()
        {
            Filter = "Rusty Hearts Patch File (*.mip)|*.mip|All Files (*.*)|*.*",
            FilterIndex = 1,
            FileName = CurrentFileName
        };

        if (saveFileDialog.ShowDialog() == true)
        {
            try
            {
                string file = saveFileDialog.FileName;
                await _fileManager.CompressToMipAsync(DataTable, file, MIPCompressionMode.Compress);
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"Error saving MIP file: {ex.Message}", "Save File Error");
            }
        }
    }

    [RelayCommand(CanExecute = nameof(CanExecuteFileCommand))]
    public async Task<bool> CloseFile()
    {
        if (DataTable == null) return true;

        if (HasChanges)
        {
            var result = RHMessageBoxHelper.ConfirmMessageYesNoCancel($"Save changes to file '{CurrentFileName}' ?");
            if (result == MessageBoxResult.Yes)
            {
                await SaveFile();
                ClearFile();
                return true;
            }
            else if (result == MessageBoxResult.No)
            {
                ClearFile();
                return true;
            }
            else if (result == MessageBoxResult.Cancel)
            {
                return false;
            }
        }
        else
        {
            ClearFile();
            return true;
        }

        return true;
    }

    public void ClearFile()
    {
        if (DataTable != null)
        {
            DataTable.TableNewRow -= DataTableChanged;
            DataTable.RowChanged -= DataTableChanged;
            DataTable.RowDeleted -= DataTableChanged;
        }

        FileManager.ClearTempFile(CurrentFileName);
        DataTable = null;
        CurrentFile = null;
        CurrentFileName = null;
        SelectedItem = null;
        HasChanges = false;
        _undoStack.Clear();
        _redoStack.Clear();
        OnCanExecuteFileCommandChanged();
    }

    private bool CanExecuteFileCommand()
    {
        return DataTable != null;
    }

    private void OnCanExecuteFileCommandChanged()
    {
        SaveFileCommand.NotifyCanExecuteChanged();
        SaveFileAsCommand.NotifyCanExecuteChanged();
        SaveFileAsMIPCommand.NotifyCanExecuteChanged();
        UndoChangesCommand.NotifyCanExecuteChanged();
        RedoChangesCommand.NotifyCanExecuteChanged();
        CloseFileCommand.NotifyCanExecuteChanged();
    }

    #endregion

    #endregion

    #region Properties

    [ObservableProperty]
    private string? _currentFile;

    [ObservableProperty]
    private string? _currentFileName;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveFileCommand))]
    private bool _hasChanges = false;

    [ObservableProperty]
    private DataTable? _dataTable;
    partial void OnDataTableChanged(DataTable? value)
    {
        OnCanExecuteFileCommandChanged();
    }

    [ObservableProperty]
    private int _selectedRow;

    [ObservableProperty]
    private Point _selectedCell;

    [ObservableProperty]
    private DataRowView? _selectedItem;
    partial void OnSelectedItemChanged(DataRowView? value)
    {
        OnCanExecuteSelectedRowCommandChanged();
    }

    #endregion

}
