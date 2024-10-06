using Microsoft.Win32;
using RHToolkit.Messages;
using RHToolkit.Models.MessageBox;
using RHToolkit.Models.RH;
using RHToolkit.Models.UISettings;
using RHToolkit.Properties;
using RHToolkit.Views.Windows;
using System.Data;
using System.Windows.Controls;
using static RHToolkit.Models.MIP.MIPCoder;

namespace RHToolkit.Models.Editor;

public partial class DataTableManager : ObservableObject
{
    private SearchDialog? _searchDialog;
    private readonly Stack<EditHistory> _undoStack = new();
    private readonly Stack<EditHistory> _redoStack = new();
    private readonly FileManager _fileManager = new();
    private readonly DataTableCryptor _dataTableCryptor = new();
    private DataGridSelectionUnit? _selectionUnit = null;
    private Point? lastFoundCell = null;

    #region Commands

    #region File

    public bool CreateTable(string tableName, List<KeyValuePair<string, int>> columns, string? stringTableName = null, List<KeyValuePair<string, int>>? stringColumns = null)
    {
        try
        {
            string? tableFolder = GetTableFolderPath();

            if (tableFolder != null)
            {
                string newFileName = tableName + "(new).rh";
                string filePath = Path.Combine(tableFolder, newFileName);

                string? stringFilePath = null;
                DataTable? stringTable = null;
                string? newStringFileName = null;

                if (stringTableName != null && stringColumns != null)
                {
                    stringTable = DataTableCryptor.CreateDataTable(stringColumns);
                    newStringFileName = stringTableName + "(new).rh";
                    stringFilePath = Path.Combine(tableFolder, newStringFileName);
                }

                var table = DataTableCryptor.CreateDataTable(columns);

                if (table != null)
                {
                    LoadTable(table, stringTable);
                    CurrentFile = filePath;
                    CurrentFileName = newFileName;

                    if (stringTable != null)
                    {
                        CurrentStringFile = stringFilePath;
                        CurrentStringFileName = newStringFileName;
                    }

                    OnCanExecuteFileCommandChanged();
                    return true;
                }
            }
            
            return false;
        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"Error creating table: {ex.Message}", Resources.Error);
            return false;
        }
    }

    public async Task<bool> LoadFile(string filter, string? stringTableName = null, string? tableColumnName = null, string? fileType = null)
    {
        OpenFileDialog openFileDialog = new()
        {
            Filter = filter,
            FilterIndex = 1
        };

        if (openFileDialog.ShowDialog() == true)
        {
            return await LoadFileAs(openFileDialog.FileName, stringTableName, tableColumnName, fileType);
        }

        return false;
    }

    public async Task<bool> LoadFileFromPath(string fileName, string? stringTableName = null, string? tableColumnName = null, string? fileType = null)
    {
        string tableFolder = GetTableFolderPath();

        if (string.IsNullOrEmpty(tableFolder))
        {
            return false;
        }

        string? filePath = FindFileInSubdirectories(tableFolder, fileName);

        if (string.IsNullOrEmpty(filePath))
        {
            RHMessageBoxHelper.ShowOKMessage($"The file '{fileName}' does not exist in the folder '{tableFolder}' or its subdirectories.", Resources.Error);
            return false;
        }

        return await LoadFileAs(filePath, stringTableName, tableColumnName, fileType, tableFolder);
    }

    public async Task<bool> LoadFileAs(string file, string? stringTableName = null, string? tableColumnName = null, string? fileType = null, string? baseFolder = null)
    {
        try
        {
            string fileName = Path.GetFileName(file);
            var table = await _fileManager.RHFileToDataTableAsync(file);

            if (!IsValidTable(table, tableColumnName, fileName, fileType))
            {
                return false;
            }

            string? stringFilePath = GetStringFilePath(file, stringTableName, baseFolder);
            DataTable? stringTable = null;

            if (stringFilePath != null)
            {
                stringTable = await _fileManager.RHFileToDataTableAsync(stringFilePath);
            }

            if (table != null)
            {
                LoadTable(table, stringTable);
                SetCurrentFile(file, fileName, stringFilePath);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"Error loading rh file: {ex.Message}", Resources.Error);
            return false;
        }
    }

    private static string GetTableFolderPath()
    {
        string tableFolder = RegistrySettingsHelper.GetTableFolder();

        if (string.IsNullOrEmpty(tableFolder) || !Directory.Exists(tableFolder))
        {
            var openFolderDialog = new OpenFolderDialog();

            if (openFolderDialog.ShowDialog() == true)
            {
                tableFolder = openFolderDialog.FolderName;
                RegistrySettingsHelper.SetTableFolder(tableFolder);
            }
            else
            {
                RHMessageBoxHelper.ShowOKMessage("No folder selected. Operation cancelled.", Resources.Error);
                return string.Empty;
            }
        }

        return tableFolder;
    }

    private static bool IsValidTable(DataTable? table, string? tableColumnName, string fileName, string? fileType)
    {
        if (table != null && tableColumnName != null && !table.Columns.Contains(tableColumnName))
        {
            RHMessageBoxHelper.ShowOKMessage($"The file '{fileName}' is not a valid {fileType} file.", Resources.Error);
            return false;
        }
        return true;
    }

    private static string? GetStringFilePath(string file, string? stringTableName, string? baseFolder)
    {
        if (stringTableName == null)
        {
            return null;
        }

        string? directory = baseFolder ?? Path.GetDirectoryName(file);

        
        if (directory != null)
        {
            string? stringFilePath = FindFileInSubdirectories(directory, stringTableName);

            if (!File.Exists(stringFilePath))
            {
                RHMessageBoxHelper.ShowOKMessage($"The file '{stringTableName}' does not exist in the same directory as {Path.GetFileName(file)}.", Resources.Error);
                return null;
            }

            return stringFilePath;
        }

        return null;
    }

    private void SetCurrentFile(string file, string fileName, string? stringFilePath)
    {
        CurrentFile = file;
        CurrentFileName = fileName;

        if (stringFilePath != null)
        {
            CurrentStringFile = stringFilePath;
            CurrentStringFileName = Path.GetFileName(stringFilePath);
        }

        OnCanExecuteFileCommandChanged();
    }


    private static string? FindFileInSubdirectories(string directory, string fileName)
    {
        foreach (var file in Directory.EnumerateFiles(directory, fileName, SearchOption.AllDirectories))
        {
            return file;
        }

        return null;
    }

    [RelayCommand(CanExecute = nameof(CanExecuteSetTableFolderCommand))]
    public void SetTableFolder()
    {
        try
        {
            var openFolderDialog = new OpenFolderDialog();

            if (openFolderDialog.ShowDialog() == true)
            {
                string newFolderPath = openFolderDialog.FolderName;
                RegistrySettingsHelper.SetTableFolder(newFolderPath);

                RHMessageBoxHelper.ShowOKMessage($"Table folder has been set to '{newFolderPath}'.", Resources.Success);
            }
        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"Error: {ex.Message}", Resources.Error);
        }
    }

    private bool CanExecuteSetTableFolderCommand()
    {
        return DataTable == null;
    }

    private void LoadTable(DataTable dataTable, DataTable? stringDataTable = null)
    {
        if (dataTable != null)
        {
            DataTable = dataTable;

            DataTable.TableNewRow += DataTableChanged;
            DataTable.RowChanged += DataTableChanged;
            DataTable.RowDeleted += DataTableChanged;
        }

        if (stringDataTable != null)
        {
            DataTableString = stringDataTable;

            DataTableString.TableNewRow += DataTableChanged;
            DataTableString.RowChanged += DataTableChanged;
            DataTableString.RowDeleted += DataTableChanged;
        }

        if (DataTable != null && DataTable.Rows.Count > 0)
        {
            SelectedItem = DataTable.DefaultView[0];
        }

        HasChanges = false;
        OnCanExecuteFileCommandChanged();
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

                if (DataTableString != null && CurrentStringFileName != null)
                {
                    await _fileManager.SaveTempFile(CurrentStringFileName, DataTableString);
                }
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"Error saving backup file: {ex.Message}", "Save File Error");
            }
        }
    }

    [RelayCommand(CanExecute = nameof(CanExecuteFileCommand))]
    public async Task<bool> CloseFile()
    {
        if (DataTable == null) return true;

        if (HasChanges)
        {
            string message = CurrentStringFileName != null
            ? $"Save changes to files '{CurrentFileName}' | '{CurrentStringFileName}' ?"
            : $"Save changes to file '{CurrentFileName}' ?";

            var result = RHMessageBoxHelper.ConfirmMessageYesNoCancel(message);

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
        if (DataTableString != null)
        {
            DataTableString.TableNewRow -= DataTableChanged;
            DataTableString.RowChanged -= DataTableChanged;
            DataTableString.RowDeleted -= DataTableChanged;
        }

        FileManager.ClearTempFile(CurrentFileName);
        FileManager.ClearTempFile(CurrentStringFileName);
        DataTable = null;
        DataTableString = null;
        CurrentFile = null;
        CurrentFileName = null;
        CurrentStringFile = null;
        CurrentStringFileName = null;
        SelectedItem = null;
        SelectedItemString = null;
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
        SaveFileAsXMLCommand.NotifyCanExecuteChanged();
        SaveFileAsXLSXCommand.NotifyCanExecuteChanged();
        AddNewRowCommand.NotifyCanExecuteChanged();
        UndoChangesCommand.NotifyCanExecuteChanged();
        RedoChangesCommand.NotifyCanExecuteChanged();
        CloseFileCommand.NotifyCanExecuteChanged();
        SetTableFolderCommand.NotifyCanExecuteChanged();
    }

    #endregion

    #region Save

    [RelayCommand(CanExecute = nameof(HasChanges))]
    public async Task SaveFile()
    {
        try
        {
            if (DataTable != null && CurrentFile != null)
            {
                await _fileManager.DataTableToRHFileAsync(CurrentFile, DataTable);
                FileManager.ClearTempFile(CurrentFileName!);
            }
            
            if (DataTableString != null && CurrentStringFile != null)
            {
                await _fileManager.DataTableToRHFileAsync(CurrentStringFile, DataTableString);
            }

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
                string? directory = Path.GetDirectoryName(file);
                await _fileManager.DataTableToRHFileAsync(file, DataTable);
                FileManager.ClearTempFile(CurrentFileName);
                
                if (DataTableString != null && CurrentStringFileName != null && directory != null)
                {
                    string stringFilePath = Path.Combine(directory, CurrentStringFileName);
                    await _fileManager.DataTableToRHFileAsync(stringFilePath, DataTableString);
                }

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
                string? directory = Path.GetDirectoryName(file);
                await _fileManager.CompressToMipAsync(DataTable, file, MIPCompressionMode.Compress);

                if (DataTableString != null && CurrentStringFileName != null && directory != null)
                {
                    string stringFileName = CurrentStringFileName + ".mip";
                    string stringFilePath = Path.Combine(directory, stringFileName);
                    await _fileManager.CompressToMipAsync(DataTableString, stringFilePath, MIPCompressionMode.Compress);
                }
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"Error saving file: {ex.Message}", "Save File Error");
            }
        }
    }

    [RelayCommand(CanExecute = nameof(CanExecuteFileCommand))]
    public async Task SaveFileAsXML()
    {
        if (DataTable == null || CurrentFileName == null) return;

        SaveFileDialog saveFileDialog = new()
        {
            Filter = "eXtensible Markup Language file (*.xml)|*.xml|All Files (*.*)|*.*",
            FilterIndex = 1,
            FileName = CurrentFileName
        };

        if (saveFileDialog.ShowDialog() == true)
        {
            try
            {
                string file = saveFileDialog.FileName;
                string? directory = Path.GetDirectoryName(file);
                await FileManager.ExportToXMLAsync(DataTable, file);

                if (DataTableString != null && CurrentStringFileName != null && directory != null)
                {
                    string stringFileName = CurrentStringFileName + ".xml";
                    string stringFilePath = Path.Combine(directory, stringFileName);
                    await FileManager.ExportToXMLAsync(DataTableString, stringFilePath);
                }
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"Error saving file: {ex.Message}", "Save File Error");
            }
        }
    }

    [RelayCommand(CanExecute = nameof(CanExecuteFileCommand))]
    public async Task SaveFileAsXLSX()
    {
        if (DataTable == null || CurrentFileName == null) return;

        SaveFileDialog saveFileDialog = new()
        {
            Filter = "Microsoft Excel Open XML Spreadsheet file (*.xlsx)|*.xlsx|All Files (*.*)|*.*",
            FilterIndex = 1,
            FileName = CurrentFileName
        };

        if (saveFileDialog.ShowDialog() == true)
        {
            try
            {
                string file = saveFileDialog.FileName;
                string? directory = Path.GetDirectoryName(file);
                await FileManager.ExportToXLSXAsync(DataTable, file);

                if (DataTableString != null && CurrentStringFileName != null && directory != null)
                {
                    string stringFileName = CurrentStringFileName + ".xlsx";
                    string stringFilePath = Path.Combine(directory, stringFileName);
                    await FileManager.ExportToXLSXAsync(DataTableString, stringFilePath);
                }
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"Error saving file: {ex.Message}", "Save File Error");
            }
        }
    }

    #endregion

    [RelayCommand]
    public static void CloseWindow(Window window)
    {
        try
        {
            window?.Close();
        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"Error: {ex.Message}", Resources.Error);
        }
    }
    #endregion

    #region Search

    public void OpenSearchDialog(Window owner, string? parameter, DataGridSelectionUnit selectionUnit)
    {
        if (_searchDialog == null || !_searchDialog.IsVisible)
        {
            _searchDialog = new SearchDialog
            {
                Owner = owner
            };
            _searchDialog.FindNext += Search;
            _searchDialog.ReplaceFindNext += Search;
            _searchDialog.Replace += Replace;
            _searchDialog.ReplaceAll += ReplaceAll;
            _searchDialog.CountMatches += CountMatches;
            _searchDialog.Show();
        }
        else
        {
            _searchDialog.Focus();
        }
        _selectionUnit = selectionUnit;
        _searchDialog.SearchTabControl.SelectedIndex = parameter == "Find" ? 0 : 1;
    }

    private void Search(string searchText, bool matchCase)
    {
        if (string.IsNullOrWhiteSpace(searchText) || DataTable == null)
            return;

        StringComparison comparison = matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

        bool found = false;
        bool wrappedAround = false;

        // Clear any existing message at the start of a new search
        _searchDialog?.ShowMessage(string.Empty, Brushes.Transparent);

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
                        _searchDialog?.ShowMessage($"Found the 1st occurrence from the top. The end of the table has been reached.", Brushes.Green);
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
                _searchDialog?.ShowMessage($"End of data reached. Search text '{searchText}' not found.", Brushes.Red);
            }
            else
            {
                _searchDialog?.ShowMessage($"Search text '{searchText}' not found.", Brushes.Red);
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

                _searchDialog?.ShowMessage($"Replaced text in row {rowIndex + 1}, column {colIndex + 1}.", Brushes.Green);
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
                        AffectedRow = DataTable.Rows[rowIndex],
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

            if (SelectedItem != null)
            {
                SelectedItem = DataTable.DefaultView[0];
            }
            OnCanExecuteChangesChanged();
        }

        _searchDialog?.ShowMessage($"Replaced {replaceCount} occurrences.", Brushes.Green);
    }


    public void CountMatches(string searchText, bool matchCase)
    {
        if (string.IsNullOrEmpty(searchText) || DataTable == null)
            return;

        StringComparison comparison = matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

        int count = DataTable.Rows.Cast<DataRow>().Sum(row => row.ItemArray.Count(item => item != null && item.ToString()?.Contains(searchText, comparison) == true));

        _searchDialog?.ShowMessage($"Count: {count} matches in entire table", Brushes.LightBlue);
    }

    #endregion

    #region Row

    [RelayCommand(CanExecute = nameof(CanExecuteFileCommand))]
    public void AddNewRow()
    {
        try
        {
            AddRow(DataTable);
        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"Error: {ex.Message}", Resources.Error);
        }
    }

    [RelayCommand(CanExecute = nameof(CanExecuteSelectedRowCommand))]
    public void DuplicateSelectedRow()
    {
        try
        {
            var newSelectedItem = DuplicateSelectedRow(DataTable, SelectedItem);

            SelectedItem = newSelectedItem;
        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"Error: {ex.Message}", Resources.Error);
        }
    }

    [RelayCommand(CanExecute = nameof(CanExecuteSelectedRowCommand))]
    public void DeleteSelectedRow()
    {
        try
        {
            if (SelectedItem == null || DataTable == null) return;

            var newSelectedItem = DeleteSelectedRow(DataTable, SelectedItem);

            SelectedItem = newSelectedItem;
        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"Error: {ex.Message}", Resources.Error);
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

    #region Helpers
    private void AddRow(DataTable? dataTable)
    {
        if (dataTable == null) return;

        int newID = GetMaxId();
        EditHistory editHistory = CreateEditHistory(EditAction.RowInsert);

        AddNewRow(dataTable, newID, editHistory);
        // Add a new row to DataTableString if it exists
        if (DataTableString != null)
        {
            AddNewRow(DataTableString, newID, editHistory);
        }

        if (dataTable.DefaultView.Count > 0)
        {
            SelectedItem = dataTable.DefaultView[^1];
        }
        else
        {
            SelectedItem = null;
        }

        OnPropertyChanged(nameof(dataTable));
        _undoStack.Push(editHistory);
        _redoStack.Clear();
        OnCanExecuteChangesChanged();
    }

    private DataRowView? DuplicateSelectedRow(DataTable? dataTable, DataRowView? selectedItem)
    {
        if (dataTable == null || selectedItem == null) return null;

        int newID = GetMaxId();
        EditHistory editHistory = CreateEditHistory(EditAction.RowInsert);

        // Duplicate row in DataTable
        var duplicate = DuplicateRow(selectedItem.Row, dataTable);
        UpdateAndAddRow(duplicate, dataTable, newID, editHistory);

        // Add a corresponding row to DataTableString with the same newID
        if (DataTableString != null && DataTableString.Columns.Contains("nID"))
        {
            var selectedItemString = GetRowViewById(DataTableString, selectedItem);
            if (selectedItemString != null)
            {
                var duplicateStringRow = DuplicateRow(selectedItemString.Row, DataTableString);
                UpdateAndAddRow(duplicateStringRow, DataTableString, newID, editHistory);
            }
        }

        if (dataTable.DefaultView.Count > 0)
        {
            SelectedItem = dataTable.DefaultView[^1];
        }
        else
        {
            SelectedItem = null;
        }

        OnPropertyChanged(nameof(dataTable));

        _undoStack.Push(editHistory);
        _redoStack.Clear();
        OnCanExecuteChangesChanged();

        return SelectedItem;
    }

    private DataRowView? DeleteSelectedRow(DataTable? dataTable, DataRowView? selectedItem)
    {
        if (dataTable == null || selectedItem == null) return null;

        EditHistory editHistory = CreateEditHistory(EditAction.RowDelete);

        // Delete row from DataTableString if it exists
        if (DataTableString != null && DataTableString.Columns.Contains("nID"))
        {
            var selectedItemString = GetRowViewById(DataTableString, selectedItem);
            if (selectedItemString != null)
            {
                RemoveRow(DataTableString, selectedItemString.Row, editHistory);
            }
        }

        // Find the index of the selectedItem in the DataView
        int selectedIndex = -1;
        for (int i = 0; i < dataTable.DefaultView.Count; i++)
        {
            if (dataTable.DefaultView[i] == selectedItem)
            {
                selectedIndex = i;
                break;
            }
        }

        // Delete row from the main DataTable
        RemoveRow(dataTable, selectedItem.Row, editHistory);

        // Set new selection based on the next or previous item
        if (dataTable.DefaultView.Count > 0)
        {
            if (selectedIndex < dataTable.DefaultView.Count)
            {
                // Select the next row if the deleted row wasn't the last one
                selectedItem = dataTable.DefaultView[Math.Min(selectedIndex, dataTable.DefaultView.Count - 1)];
            }
            else if (selectedIndex >= dataTable.DefaultView.Count)
            {
                // If the deleted row was the last one, select the previous row
                selectedItem = dataTable.DefaultView[dataTable.DefaultView.Count - 1];
            }
        }
        else
        {
            selectedItem = null;
        }

        OnPropertyChanged(nameof(dataTable));
        _undoStack.Push(editHistory);
        _redoStack.Clear();
        OnCanExecuteChangesChanged();

        return selectedItem;
    }

    private static DataRow AddNewRow(DataTable dataTable, int newID, EditHistory editHistory)
    {
        DataRow newRow = dataTable.NewRow();

        // Initialize the new row with default values
        foreach (DataColumn column in dataTable.Columns)
        {
            newRow[column] = GetDefaultValue(column.DataType);
        }

        dataTable.Rows.Add(newRow);

        if (dataTable.Columns.Contains("nID"))
        {
            newRow["nID"] = newID;
        }

        int rowIndex = dataTable.Rows.IndexOf(newRow);
        editHistory.GroupedEdits.Add(new EditHistory
        {
            Row = rowIndex,
            AffectedRow = newRow,
            Action = EditAction.RowInsert,
            NewValue = newRow.ItemArray
        });

        return newRow;
    }

    private static void RemoveRow(DataTable dataTable, DataRow row, EditHistory editHistory)
    {
        int rowIndex = dataTable.Rows.IndexOf(row);
        object?[] rowValues = row.ItemArray;
        dataTable.Rows.Remove(row);

        editHistory.GroupedEdits.Add(new EditHistory
        {
            Row = rowIndex,
            AffectedRow = row,
            Action = EditAction.RowDelete,
            OldValue = rowValues
        });
    }

    private static DataRow DuplicateRow(DataRow originalRow, DataTable dataTable)
    {
        DataRow duplicate = dataTable.NewRow();
        duplicate.ItemArray = originalRow.ItemArray;
        return duplicate;
    }

    private static void UpdateAndAddRow(DataRow duplicateRow, DataTable dataTable, int newID, EditHistory editHistory)
    {
        dataTable.Rows.Add(duplicateRow);

        if (dataTable.Columns.Contains("nID"))
        {
            duplicateRow["nID"] = newID;
        }

        editHistory.GroupedEdits.Add(new EditHistory
        {
            Row = dataTable.Rows.Count - 1,
            AffectedRow = duplicateRow,
            Action = EditAction.RowInsert,
            NewValue = duplicateRow.ItemArray
        });
    }

    private static DataRowView? GetRowViewById(DataTable dataTable, DataRowView selectedItem)
    {
        int nID = (int)selectedItem["nID"];
        return dataTable.DefaultView.Cast<DataRowView>()
                         .FirstOrDefault(rowView => (int)rowView["nID"] == nID);
    }

    private static EditHistory CreateEditHistory(EditAction action)
    {
        return new EditHistory
        {
            Action = action
        };
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

    public int GetMaxId()
    {
        int newID = 1;

        if (DataTable != null && DataTable.Rows.Count > 0 && DataTable.Columns.Contains("nID"))
        {
            var maxIdTable1 = DataTable.AsEnumerable()
                                    .Max(row => row.Field<int>("nID"));
            newID = maxIdTable1 + 1;
        }

        if (DataTableString != null && DataTableString.Rows.Count > 0 && DataTableString.Columns.Contains("nID"))
        {
            var maxIdTable2 = DataTableString.AsEnumerable()
                                    .Max(row => row.Field<int>("nID"));
            newID = Math.Max(newID, maxIdTable2 + 1);
        }

        return newID;
    }
    #endregion

    #endregion

    #region Edit History

    [RelayCommand(CanExecute = nameof(CanUndo))]
    public void UndoChanges()
    {
        ApplyChanges(isUndo: true);
    }

    [RelayCommand(CanExecute = nameof(CanRedo))]
    public void RedoChanges()
    {
        ApplyChanges(isUndo: false);
    }

    public void ApplyChanges(bool isUndo)
    {
        try
        {
            var sourceStack = isUndo ? _undoStack : _redoStack;
            var targetStack = isUndo ? _redoStack : _undoStack;

            if (sourceStack.Count > 0)
            {
                var edit = sourceStack.Pop();
                targetStack.Push(edit);

                DataRowView? lastAffectedItem = null;

                foreach (var groupedEdit in edit.GroupedEdits)
                {
                    if (groupedEdit.AffectedRow != null)
                    {
                        var table = groupedEdit.AffectedRow.Table;
                        switch (groupedEdit.Action)
                        {
                            case EditAction.CellEdit:
                                if (groupedEdit.Row >= 0 && groupedEdit.Row < table.Rows.Count)
                                {
                                    table.Rows[groupedEdit.Row][groupedEdit.Column] = isUndo ? groupedEdit.OldValue : groupedEdit.NewValue;
                                    lastAffectedItem = table.DefaultView[groupedEdit.Row];
                                }
                                break;
                            case EditAction.RowInsert:
                                if (isUndo)
                                {
                                    if (groupedEdit.Row >= 0 && groupedEdit.Row < table.Rows.Count)
                                    {
                                        table.Rows.RemoveAt(groupedEdit.Row);
                                    }
                                }
                                else
                                {
                                    if (groupedEdit.Row >= 0)
                                    {
                                        DataRow insertRow = table.NewRow();
                                        insertRow.ItemArray = (object?[])groupedEdit.NewValue!;
                                        if (groupedEdit.Row <= table.Rows.Count)
                                        {
                                            table.Rows.InsertAt(insertRow, groupedEdit.Row);
                                        }
                                        else
                                        {
                                            table.Rows.Add(insertRow);
                                        }

                                        RefreshView(table);

                                        lastAffectedItem = table.DefaultView[groupedEdit.Row < table.DefaultView.Count ? groupedEdit.Row : table.DefaultView.Count - 1];
                                    }
                                }
                                break;
                            case EditAction.RowDelete:
                                if (isUndo)
                                {
                                    if (groupedEdit.Row >= 0)
                                    {
                                        DataRow newRow = table.NewRow();
                                        newRow.ItemArray = (object?[])groupedEdit.OldValue!;
                                        table.Rows.InsertAt(newRow, groupedEdit.Row);
                                        if (groupedEdit.Row < table.DefaultView.Count)
                                        {
                                            lastAffectedItem = table.DefaultView[groupedEdit.Row];
                                        }
                                    }
                                }
                                else
                                {
                                    if (groupedEdit.Row >= 0 && groupedEdit.Row < table.Rows.Count)
                                    {
                                        table.Rows.RemoveAt(groupedEdit.Row);
                                    }
                                }
                                break;
                        }
                    }
                }

                OnPropertyChanged(nameof(DataTable));
                OnPropertyChanged(nameof(DataTableString));
                OnCanExecuteChangesChanged();

                if (lastAffectedItem != null)
                {
                    if (lastAffectedItem.Row.Table == SelectedItem?.Row.Table)
                    {
                        SelectedItem = lastAffectedItem;
                    }
                    else if (lastAffectedItem.Row.Table == SelectedItemString?.Row.Table && DataTable != null)
                    {
                        SelectedItem = GetRowViewById(DataTable, lastAffectedItem);
                    }
                }
                else
                {
                    SelectedItem = null;
                }
            }
        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"Error: {ex.Message}", Resources.Error);
        }
    }

    private static void RefreshView(DataTable table)
    {
        if (!string.IsNullOrEmpty(table.DefaultView.RowFilter))
        {
            string currentFilter = table.DefaultView.RowFilter;
            table.DefaultView.RowFilter = string.Empty; // Clear the filter
            table.DefaultView.RowFilter = currentFilter; // Reapply the existing filter
        }
    }


    public void AddGroupedEdits(List<EditHistory> groupedEdits)
    {
        if (groupedEdits.Count > 0)
        {
            var groupedEditHistory = new EditHistory
            {
                Action = EditAction.CellEdit,
                GroupedEdits = new List<EditHistory>(groupedEdits)
            };
            _undoStack.Push(groupedEditHistory);
            _redoStack.Clear();
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

    #region SelectedItem

    private bool _isGroupingEdits;
    private readonly List<EditHistory> _currentGroupedEdits = [];

    public void StartGroupingEdits()
    {
        if (!_isGroupingEdits)
        {
            _isGroupingEdits = true;
            _currentGroupedEdits.Clear();
        }
    }

    public void EndGroupingEdits()
    {
        if (_isGroupingEdits)
        {
            if (_currentGroupedEdits.Count > 0)
            {
                AddGroupedEdits(_currentGroupedEdits);
            }
            _isGroupingEdits = false;
        }
    }

    [RelayCommand]
    private void UpdateItemValue((object? newValue, string column) parameter)
    {
        var (newValue, column) = parameter;

        UpdateItemValue(SelectedItem, newValue, column);
    }

    [RelayCommand]
    private void UpdateItemStringValue((object? newValue, string column) parameter)
    {
        var (newValue, column) = parameter;

        UpdateItemValue(SelectedItemString, newValue, column);
    }

    private void UpdateItemValue(DataRowView? item, object? newValue, string column)
    {
        if (item != null && item.Row.Table.Columns.Contains(column))
        {
            var currentValue = item[column];

            var columnType = item.Row.Table.Columns[column]?.DataType;

            if (newValue == null || string.IsNullOrWhiteSpace(newValue.ToString()))
            {
                if (columnType == typeof(int) || columnType == typeof(Single))
                {
                    newValue = 0;
                }
            }
            else if (columnType == typeof(int) && newValue is not int)
            {
                if (int.TryParse(newValue?.ToString(), out int intValue))
                {
                    if (intValue > int.MaxValue || intValue < int.MinValue)
                    {
                        newValue = currentValue;
                    }
                    else
                    {
                        newValue = intValue;
                    }
                }
                else
                {
                    newValue = currentValue;
                }
            }
            else if (columnType == typeof(float) && newValue is not double)
            {
                newValue = double.TryParse(newValue?.ToString(), out double doubleValue) ? doubleValue : currentValue;
            }

            bool isDifferent = !AreValuesEqual(currentValue, newValue);

            if (isDifferent)
            {
                if (newValue is double doubleValue)
                {
                    newValue = Math.Round(doubleValue, 4);
                }

                item[column] = newValue;

                if (item.Row.Table == SelectedItem?.Row.Table)
                {
                    SelectedItem = null;
                    SelectedItem = item;
                }
                else if (item.Row.Table == SelectedItemString?.Row.Table && DataTable != null)
                {
                    SelectedItem = null;
                    SelectedItem = GetRowViewById(DataTable, item);
                }

                var editHistory = new EditHistory
                {
                    Row = item.Row.Table.Rows.IndexOf(item.Row),
                    Column = item.Row.Table.Columns.IndexOf(column),
                    OldValue = currentValue,
                    NewValue = newValue,
                    AffectedRow = item.Row,
                    Action = EditAction.CellEdit
                };

                if (_isGroupingEdits)
                {
                    _currentGroupedEdits.Add(editHistory);
                }
                else
                {
                    AddGroupedEdits([editHistory]);
                }
            }
        }
    }

    private static bool AreValuesEqual(object? value1, object? value2)
    {
        if (value1 == null || value2 == null)
        {
            return Equals(value1, value2);
        }

        if (value1.GetType() != value2.GetType())
        {
            try
            {
                value2 = Convert.ChangeType(value2, value1.GetType());
            }
            catch
            {
                return false;
            }
        }

        return Equals(value1, value2);
    }

    public void UpdateSelectedItemValue(object? newValue, string column)
    {
        UpdateItemValue(SelectedItem, newValue, column);
    }

    public void UpdateSelectedItemStringValue(object? newValue, string column)
    {
        UpdateItemValue(SelectedItemString, newValue, column);
    }

    #endregion

    private bool CanUndo() => _undoStack.Count > 0;
    private bool CanRedo() => _redoStack.Count > 0;

    public void OnCanExecuteChangesChanged()
    {
        UndoChangesCommand.NotifyCanExecuteChanged();
        RedoChangesCommand.NotifyCanExecuteChanged();
    }
    #endregion

    #region Filter

    public void ApplyFileDataFilter(List<string> filterParts, string[] columns, string? searchText, bool matchCase)
    {
        try
        {
            if (DataTable != null)
            {
                // Text search filter
                if (!string.IsNullOrEmpty(searchText))
                {
                    searchText = matchCase ? searchText : searchText.ToLower();

                    // Escape special characters for SQL LIKE pattern 
                    searchText = searchText.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]");

                    string operatorPattern = matchCase ? "{0} LIKE '{1}'" : "{0} LIKE '%{1}%'";
                    List<string> columnFilters = columns
                        .Select(column => string.Format(operatorPattern, column, searchText))
                        .ToList();

                    string filterText = string.Join(" OR ", columnFilters);
                    filterParts.Add($"({filterText})");
                }

                string filter = string.Join(" AND ", filterParts);

                DataTable.DefaultView.RowFilter = filter;

                SelectedItem = DataTable.DefaultView.Count > 0 ? DataTable.DefaultView[0] : null;
            }
        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"Error: {ex.Message}", Resources.Error);
        }
        
    }

    #endregion

    #region Properties

    [ObservableProperty]
    private Guid? _token;

    [ObservableProperty]
    private string? _currentFile;

    [ObservableProperty]
    private string? _currentFileName;

    [ObservableProperty]
    private string? _currentStringFile;

    [ObservableProperty]
    private string? _currentStringFileName;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveFileCommand))]
    private bool _hasChanges = false;

    [ObservableProperty]
    private int _selectedRow;

    [ObservableProperty]
    private Point _selectedCell;

    [ObservableProperty]
    private DataTable? _dataTable;
    partial void OnDataTableChanged(DataTable? value)
    {
        OnCanExecuteFileCommandChanged();
    }

    [ObservableProperty]
    private DataRowView? _selectedItem;

    partial void OnSelectedItemChanged(DataRowView? value)
    {
        if (value != null && DataTableString != null && DataTableString.Columns.Contains("nID"))
        {
            SelectedItemString = GetRowViewById(DataTableString, value);
        }
        else
        {
            SelectedItemString = null;
        }

        WeakReferenceMessenger.Default.Send(new DataRowViewMessage(value, Token));

        OnCanExecuteSelectedRowCommandChanged();
    }

    [ObservableProperty]
    private DataTable? _dataTableString;

    [ObservableProperty]
    private DataRowView? _selectedItemString;

    #endregion

}
