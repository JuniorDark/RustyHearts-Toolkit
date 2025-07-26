using Microsoft.Win32;
using RHToolkit.Messages;
using RHToolkit.Models.MessageBox;
using RHToolkit.Models.PCK;
using RHToolkit.Models.RH;
using RHToolkit.Models.UISettings;
using RHToolkit.Views.Windows;
using System.Data;
using System.Windows.Controls;
using static RHToolkit.Models.Crypto.ZLibHelper;

namespace RHToolkit.Models.Editor;

/// <summary>
/// Manages DataTables operations including creating, loading, saving, searching, and editing.
/// </summary>
public partial class DataTableManager : ObservableObject
{
    private SearchDialog? _searchDialog;
    private readonly Stack<EditHistory> _undoStack = new();
    private readonly Stack<EditHistory> _redoStack = new();
    private readonly FileManager _fileManager = new();
    private DataGridSelectionUnit? _selectionUnit = null;
    private Point? lastFoundCell = null;

    #region Commands

    #region File

    /// <summary>
    /// Creates a new DataTable with the specified columns and optional string table.
    /// </summary>
    /// <param name="tableName">The name of the table.</param>
    /// <param name="columns">The columns for the table.</param>
    /// <param name="stringTableName">The name of the string table (optional).</param>
    /// <param name="stringColumns">The columns for the string table (optional).</param>
    /// <returns>True if the table was created successfully, otherwise false.</returns>
    public bool CreateTable(string tableName, List<KeyValuePair<string, int>> columns, string? stringTableName = null, List<KeyValuePair<string, int>>? stringColumns = null)
    {
        try
        {
            string? tableFolder = GetTableFolderPath();

            if (tableFolder is not null)
            {
                string newFileName = tableName + "(new).rh";
                string filePath = Path.Combine(tableFolder, newFileName);

                string? stringFilePath = null;
                DataTable? stringTable = null;
                string? newStringFileName = null;

                if (stringTableName is not null && stringColumns is not null)
                {
                    stringTable = DataTableCryptor.CreateDataTable(stringColumns);
                    newStringFileName = stringTableName + "(new).rh";
                    stringFilePath = Path.Combine(tableFolder, newStringFileName);
                }

                var table = DataTableCryptor.CreateDataTable(columns);

                if (table is not null)
                {
                    LoadTable(table, stringTable);
                    CurrentFile = filePath;
                    CurrentFileName = newFileName;

                    if (stringTable is not null)
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
            RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
            return false;
        }
    }

    /// <summary>
    /// Opens a file dialog to load a file with the specified filter.
    /// </summary>
    /// <param name="filter">The filter for the file dialog.</param>
    /// <param name="stringTableName">The name of the string table (optional).</param>
    /// <param name="tableColumnName">The name of the table column (optional).</param>
    /// <param name="fileType">The type of the file (optional).</param>
    /// <returns>True if the file was loaded successfully, otherwise false.</returns>
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

    /// <summary>
    /// Loads a file from the specified path.
    /// </summary>
    /// <param name="fileName">The name of the file to load.</param>
    /// <param name="stringTableName">The name of the string table (optional).</param>
    /// <param name="tableColumnName">The name of the table column (optional).</param>
    /// <param name="fileType">The type of the file (optional).</param>
    /// <returns>True if the file was loaded successfully, otherwise false.</returns>
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
            RHMessageBoxHelper.ShowOKMessage(string.Format(Resources.DataTableManagerMissingFileError, fileName, tableFolder), Resources.Error);
            return false;
        }

        return await LoadFileAs(filePath, stringTableName, tableColumnName, fileType, tableFolder);
    }

    /// <summary>
    /// Loads a file from the specified path and sets it as the current file.
    /// </summary>
    /// <param name="file">The path to the file.</param>
    /// <param name="stringTableName">The name of the string table (optional).</param>
    /// <param name="tableColumnName">The name of the table column (optional).</param>
    /// <param name="fileType">The type of the file (optional).</param>
    /// <param name="baseFolder">The base folder for the file (optional).</param>
    /// <returns>True if the file was loaded successfully, otherwise false.</returns>
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

            if (stringFilePath is not null)
            {
                stringTable = await _fileManager.RHFileToDataTableAsync(stringFilePath);
            }

            if (table is not null)
            {
                LoadTable(table, stringTable);
                SetCurrentFile(file, fileName, stringFilePath);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"{Resources.DataTableManagerLoadFileError}: {ex.Message}", Resources.Error);
            return false;
        }
    }

    /// <summary>
    /// Loads a file from a PCK archive.
    /// </summary>
    /// <param name="fileNameInPck"></param>
    /// <param name="stringTableName"></param>
    public async Task<bool> LoadFileFromPCK(string fileNameInPck, string? stringTableName = null)
    {
        try
        {
            var gameDirectory = GetClientFolderPath();

            if (string.IsNullOrEmpty(gameDirectory))
            {
                return false;
            }

            var fileData = await PCKReader.LoadRHFromPCKAsync(gameDirectory, fileNameInPck);
            var table = _fileManager.RHFileDataToDataTableAsync(fileData);

            if (!IsValidTable(table, null, fileNameInPck, null))
                return false;

            DataTable? stringTable = null;
            if (stringTableName is not null)
            {
                var fileStringData = await PCKReader.LoadRHFromPCKAsync(gameDirectory, stringTableName);
                stringTable = _fileManager.RHFileDataToDataTableAsync(fileStringData);
            }

            LoadTable(table, stringTable);
            CurrentFileName = fileNameInPck;
            CurrentStringFileName = stringTableName;
            IsPCKMode = true;

            return true;
        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"{Resources.DataTableManagerLoadFileError}: {ex.Message}\n{ex.StackTrace}", Resources.Error);
            return false;
        }
    }

    /// <summary>
    /// Gets the folder path for the table from the registry settings.
    /// </summary>
    /// <returns>The folder path for the table folder.</returns>
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
                return string.Empty;
            }
        }

        return tableFolder;
    }

    /// <summary>
    /// Gets the folder path for the client from the registry settings.
    /// </summary>
    /// <returns>The folder path for the client.</returns>
    private static string GetClientFolderPath()
    {
        string clientFolder = RegistrySettingsHelper.GetClientFolder();

        if (string.IsNullOrEmpty(clientFolder) || !Directory.Exists(clientFolder))
        {
            var openFolderDialog = new OpenFolderDialog();

            if (openFolderDialog.ShowDialog() == true)
            {
                clientFolder = openFolderDialog.FolderName;
                RegistrySettingsHelper.SetClientFolder(clientFolder);
            }
            else
            {
                return string.Empty;
            }
        }

        return clientFolder;
    }

    /// <summary>
    /// Validates if the table contains the specified column.
    /// </summary>
    /// <param name="table">The DataTable to validate.</param>
    /// <param name="tableColumnName">The name of the column to check.</param>
    /// <param name="fileName">The name of the file.</param>
    /// <param name="fileType">The type of the file.</param>
    /// <returns>True if the table is valid, otherwise false.</returns>
    private static bool IsValidTable(DataTable? table, string? tableColumnName, string fileName, string? fileType)
    {
        if (table is not null && tableColumnName is not null && !table.Columns.Contains(tableColumnName))
        {
            string message = string.Format(Resources.InvalidTableFileDesc, fileName, fileType);
            RHMessageBoxHelper.ShowOKMessage(message, Resources.Error);
            return false;
        }
        return true;
    }

    /// <summary>
    /// Gets the file path for the string table.
    /// </summary>
    /// <param name="file">The path to the file.</param>
    /// <param name="stringTableName">The name of the string table.</param>
    /// <param name="baseFolder">The base folder for the file.</param>
    /// <returns>The file path for the string table.</returns>
    private static string? GetStringFilePath(string file, string? stringTableName, string? baseFolder)
    {
        if (stringTableName is null)
        {
            return null;
        }

        string? directory = baseFolder ?? Path.GetDirectoryName(file);

        if (directory is not null)
        {
            string? stringFilePath = FindFileInSubdirectories(directory, stringTableName);

            if (!File.Exists(stringFilePath))
            {
                RHMessageBoxHelper.ShowOKMessage(string.Format(Resources.DataTableManagerMissingStringFileError, stringTableName, Path.GetFileName(file)), Resources.Error);
                return null;
            }

            return stringFilePath;
        }

        return null;
    }

    /// <summary>
    /// Sets the current file and updates the file commands.
    /// </summary>
    /// <param name="file">The path to the file.</param>
    /// <param name="fileName">The name of the file.</param>
    /// <param name="stringFilePath">The path to the string file.</param>
    private void SetCurrentFile(string file, string fileName, string? stringFilePath)
    {
        CurrentFile = file;
        CurrentFileName = fileName;

        if (stringFilePath is not null)
        {
            CurrentStringFile = stringFilePath;
            CurrentStringFileName = Path.GetFileName(stringFilePath);
        }

        OnCanExecuteFileCommandChanged();
    }

    /// <summary>
    /// Finds a file in the specified directory and its subdirectories.
    /// </summary>
    /// <param name="directory">The directory to search.</param>
    /// <param name="fileName">The name of the file to find.</param>
    /// <returns>The path to the file if found, otherwise null.</returns>
    private static string? FindFileInSubdirectories(string directory, string fileName)
    {
        foreach (var file in Directory.EnumerateFiles(directory, fileName, SearchOption.AllDirectories))
        {
            return file;
        }

        return null;
    }

    /// <summary>
    /// Sets the folder path for the table using a folder dialog.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanExecuteSetFolderCommand))]
    public void SetTableFolder()
    {
        try
        {
            var openFolderDialog = new OpenFolderDialog();

            if (openFolderDialog.ShowDialog() == true)
            {
                string newFolderPath = openFolderDialog.FolderName;
                RegistrySettingsHelper.SetTableFolder(newFolderPath);

                RHMessageBoxHelper.ShowOKMessage(string.Format(Resources.DataTableManagerFolderSetMessage, newFolderPath), Resources.Success);
            }
        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
        }
    }

    /// <summary>
    /// Sets the folder path for the client using a folder dialog.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanExecuteSetFolderCommand))]
    public void SetClientFolder()
    {
        try
        {
            var openFolderDialog = new OpenFolderDialog();

            if (openFolderDialog.ShowDialog() == true)
            {
                string newFolderPath = openFolderDialog.FolderName;
                RegistrySettingsHelper.SetClientFolder(newFolderPath);

                RHMessageBoxHelper.ShowOKMessage(string.Format(Resources.DataTableManagerFolderSetMessage, newFolderPath), Resources.Success);
            }
        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
        }
    }

    /// <summary>
    /// Checks if the SetFolder command can be executed.
    /// </summary>
    /// <returns>True if the command can be executed, otherwise false.</returns>
    private bool CanExecuteSetFolderCommand()
    {
        return DataTable is null;
    }

    /// <summary>
    /// Loads the specified DataTable and optional string DataTable.
    /// </summary>
    /// <param name="dataTable">The DataTable to load.</param>
    /// <param name="stringDataTable">The string DataTable to load (optional).</param>
    private void LoadTable(DataTable dataTable, DataTable? stringDataTable = null)
    {
        if (dataTable is not null)
        {
            DataTable = dataTable;

            DataTable.TableNewRow += DataTableChanged;
            DataTable.RowChanged += DataTableChanged;
            DataTable.RowDeleted += DataTableChanged;
        }

        if (stringDataTable is not null)
        {
            DataTableString = stringDataTable;

            DataTableString.TableNewRow += DataTableChanged;
            DataTableString.RowChanged += DataTableChanged;
            DataTableString.RowDeleted += DataTableChanged;
        }

        if (DataTable is not null && DataTable.Rows.Count > 0)
        {
            SelectedItem = DataTable.DefaultView[0];
        }

        HasChanges = false;
        OnCanExecuteFileCommandChanged();
    }

    private int _changesCounter = 0;
    private const int ChangesBeforeSave = 10;

    /// <summary>
    /// Handles changes to the DataTable and saves temporary files after a certain number of changes.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
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

                if (DataTableString is not null && CurrentStringFileName is not null)
                {
                    await _fileManager.SaveTempFile(CurrentStringFileName, DataTableString);
                }
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"{Resources.DataTableManagerSaveFileErrorMessage}: {ex.Message}", Resources.Error);
            }
        }
    }

    /// <summary>
    /// Closes the current file, prompting the user to save changes if necessary.
    /// </summary>
    /// <returns>True if the file was closed successfully, otherwise false.</returns>
    [RelayCommand(CanExecute = nameof(CanExecuteFileCommand))]
    public async Task<bool> CloseFile()
    {
        if (DataTable is null) return true;

        if (HasChanges)
        {
            string message = CurrentStringFileName is not null
            ? $"{Resources.DataTableManagerSaveFileMessage} '{CurrentFileName}' | '{CurrentStringFileName}' ?"
            : $"{Resources.DataTableManagerSaveFileMessage} '{CurrentFileName}' ?";

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

    /// <summary>
    /// Clears the current file and resets the DataTable and related properties.
    /// </summary>
    public void ClearFile()
    {
        if (DataTable is not null)
        {
            DataTable.TableNewRow -= DataTableChanged;
            DataTable.RowChanged -= DataTableChanged;
            DataTable.RowDeleted -= DataTableChanged;
        }
        if (DataTableString is not null)
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
        IsPCKMode = false;
        _undoStack.Clear();
        _redoStack.Clear();
        OnCanExecuteFileCommandChanged();
    }

    /// <summary>
    /// Determines if file commands can be executed.
    /// </summary>
    /// <returns>True if file commands can be executed, otherwise false.</returns>
    private bool CanExecuteFileCommand()
    {
        return DataTable is not null;
    }

    /// <summary>
    /// Updates the execution state of file commands.
    /// </summary>
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
        SetClientFolderCommand.NotifyCanExecuteChanged();
    }

    #endregion

    #region Save

    /// <summary>
    /// Saves the current DataTable to the current file.
    /// </summary>
    [RelayCommand(CanExecute = nameof(HasChanges))]
    public async Task SaveFile()
    {
        try
        {
            if (IsPCKMode)
            {
                await SaveFileToPCK();
            }
            else
            {
                if (DataTable is not null && CurrentFile is not null)
                {
                    await _fileManager.DataTableToRHFileAsync(CurrentFile, DataTable);
                    FileManager.ClearTempFile(CurrentFileName!);
                }

                if (DataTableString is not null && CurrentStringFile is not null)
                {
                    await _fileManager.DataTableToRHFileAsync(CurrentStringFile, DataTableString);
                }
            }
            HasChanges = false;
        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"{Resources.DataTableManagerSaveFileErrorMessage}: {ex.Message}", Resources.Error);
        }
    }

    public async Task SaveFileToPCK()
    {
        try
        {
            var gameDirectory = RegistrySettingsHelper.GetClientFolder();

            if (string.IsNullOrEmpty(gameDirectory))
            {
                return;
            }

            if (DataTable is not null && CurrentFileName is not null)
            {
                byte[] fileData = await _fileManager.DataTableDataToRHFileAsync(DataTable);
                await PCKWriter.SaveRHToPCKAsync(gameDirectory, CurrentFileName, fileData);
            }

            if (DataTableString is not null && CurrentStringFileName is not null)
            {
                byte[] fileData = await _fileManager.DataTableDataToRHFileAsync(DataTableString);
                await PCKWriter.SaveRHToPCKAsync(gameDirectory, CurrentStringFileName, fileData);
            }

            HasChanges = false;
        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"{Resources.DataTableManagerSaveFileErrorMessage}: {ex.Message}\n {ex.StackTrace}", Resources.Error);
        }
    }

    [RelayCommand(CanExecute = nameof(CanExecuteFileCommand))]
    public async Task SaveFileAs()
    {
        if (DataTable is null || CurrentFileName is null) return;

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

                if (DataTableString is not null && CurrentStringFileName is not null && directory is not null)
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
                RHMessageBoxHelper.ShowOKMessage($"{Resources.DataTableManagerSaveFileErrorMessage}: {ex.Message}", Resources.Error);
            }
        }
    }

    /// <summary>
    /// Saves the current DataTable as a MIP file.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanExecuteFileCommand))]
    public async Task SaveFileAsMIP()
    {
        if (DataTable is null || CurrentFileName is null) return;

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
                await _fileManager.CompressToMipAsync(DataTable, file, ZLibOperationMode.Compress);

                if (DataTableString is not null && CurrentStringFileName is not null && directory is not null)
                {
                    string stringFileName = CurrentStringFileName + ".mip";
                    string stringFilePath = Path.Combine(directory, stringFileName);
                    await _fileManager.CompressToMipAsync(DataTableString, stringFilePath, ZLibOperationMode.Compress);
                }
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"{Resources.DataTableManagerSaveFileErrorMessage}: {ex.Message}", Resources.Error);
            }
        }
    }

    /// <summary>
    /// Saves the current DataTable as an XML file.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanExecuteFileCommand))]
    public async Task SaveFileAsXML()
    {
        if (DataTable is null || CurrentFileName is null) return;

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

                if (DataTableString is not null && CurrentStringFileName is not null && directory is not null)
                {
                    string stringFileName = CurrentStringFileName + ".xml";
                    string stringFilePath = Path.Combine(directory, stringFileName);
                    await FileManager.ExportToXMLAsync(DataTableString, stringFilePath);
                }
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"{Resources.DataTableManagerSaveFileErrorMessage}: {ex.Message}", Resources.Error);
            }
        }
    }

    /// <summary>
    /// Saves the current DataTable as an XLSX file.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanExecuteFileCommand))]
    public async Task SaveFileAsXLSX()
    {
        if (DataTable is null || CurrentFileName is null) return;

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

                if (DataTableString is not null && CurrentStringFileName is not null && directory is not null)
                {
                    string stringFileName = CurrentStringFileName + ".xlsx";
                    string stringFilePath = Path.Combine(directory, stringFileName);
                    await FileManager.ExportToXLSXAsync(DataTableString, stringFilePath);
                }
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"{Resources.DataTableManagerSaveFileErrorMessage}: {ex.Message}", Resources.Error);
            }
        }
    }

    #endregion

    #region Close

    /// <summary>
    /// Closes the specified window.
    /// </summary>
    /// <param name="window">The window to close.</param>
    [RelayCommand]
    public static void CloseWindow(Window window)
    {
        try
        {
            window?.Close();
        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
        }
    }
    #endregion

    #endregion

    #region Search

    /// <summary>
    /// Opens the search dialog.
    /// </summary>
    /// <param name="owner">The owner window.</param>
    /// <param name="parameter">The search parameter.</param>
    /// <param name="selectionUnit">The selection unit for the DataGrid.</param>
    public void OpenSearchDialog(Window owner, string? parameter, DataGridSelectionUnit selectionUnit)
    {
        if (_searchDialog is null || !_searchDialog.IsVisible)
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

    /// <summary>
    /// Searches for the specified text in the DataTable.
    /// </summary>
    /// <param name="searchText">The text to search for.</param>
    /// <param name="matchCase">Whether to match case.</param>
    private void Search(string searchText, bool matchCase)
    {
        if (string.IsNullOrWhiteSpace(searchText) || DataTable is null)
            return;

        StringComparison comparison = matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

        bool found = false;
        bool wrappedAround = false;

        // Clear any existing message at the start of a new search
        _searchDialog?.ShowMessage(string.Empty, Brushes.Transparent);

        int startRowIndex = 0;
        int startColIndex = 0;

        if (lastFoundCell is not null)
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
                        _searchDialog?.ShowMessage(Resources.DataTableManagerSearchMessage, Brushes.Green);
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
                _searchDialog?.ShowMessage($"{Resources.DataTableManagerSearchEndMessage} {string.Format(Resources.DataTableManagerSearchNotFoundMessage, searchText)}", Brushes.Red);
            }
            else
            {
                _searchDialog?.ShowMessage(string.Format(Resources.DataTableManagerSearchNotFoundMessage, searchText), Brushes.Red);
            }
            lastFoundCell = null;
        }
    }

    /// <summary>
    /// Replaces the specified text in the DataTable.
    /// </summary>
    /// <param name="searchText">The text to search for.</param>
    /// <param name="replaceText">The text to replace with.</param>
    /// <param name="matchCase">Whether to match case.</param>
    private void Replace(string searchText, string replaceText, bool matchCase)
    {
        if (string.IsNullOrEmpty(searchText) || DataTable is null)
            return;

        StringComparison comparison = matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

        if (lastFoundCell is not null)
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

                _searchDialog?.ShowMessage(string.Format(Resources.DataTableManagerSearchReplaceMessage, rowIndex + 1, colIndex + 1), Brushes.Green);
                lastFoundCell = null;
                return;
            }
        }

        Search(searchText, matchCase);
    }

    /// <summary>
    /// Replaces all occurrences of the specified text in the DataTable.
    /// </summary>
    /// <param name="searchText">The text to search for.</param>
    /// <param name="replaceText">The text to replace with.</param>
    /// <param name="matchCase">Whether to match case.</param>
    public void ReplaceAll(string searchText, string replaceText, bool matchCase)
    {
        if (string.IsNullOrEmpty(searchText) || DataTable is null)
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

            if (SelectedItem is not null)
            {
                SelectedItem = DataTable.DefaultView[0];
            }
            OnCanExecuteChangesChanged();
        }

        _searchDialog?.ShowMessage(string.Format(Resources.DataTableManagerReplaceCountMessage, replaceCount), Brushes.Green);
    }

    /// <summary>
    /// Counts the number of matches for the specified text in the DataTable.
    /// </summary>
    /// <param name="searchText">The text to search for.</param>
    /// <param name="matchCase">Whether to match case.</param>
    public void CountMatches(string searchText, bool matchCase)
    {
        if (string.IsNullOrEmpty(searchText) || DataTable is null)
            return;

        StringComparison comparison = matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

        int count = DataTable.Rows.Cast<DataRow>().Sum(row => row.ItemArray.Count(item => item is not null && item.ToString()?.Contains(searchText, comparison) == true));

        _searchDialog?.ShowMessage(string.Format(Resources.DataTableManagerCountMessage, count), Brushes.LightBlue);
    }

    #endregion

    #region Row

    /// <summary>
    /// Adds a new row to the DataTable.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanExecuteFileCommand))]
    public void AddNewRow()
    {
        try
        {
            AddRow(DataTable);
        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
        }
    }

    /// <summary>
    /// Duplicates the selected row in the DataTable.
    /// </summary>
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
            RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
        }
    }

    /// <summary>
    /// Deletes the selected row from the DataTable.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanExecuteSelectedRowCommand))]
    public void DeleteSelectedRow()
    {
        try
        {
            if (SelectedItem is null || DataTable is null) return;

            var newSelectedItem = DeleteSelectedRow(DataTable, SelectedItem);

            SelectedItem = newSelectedItem;
        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
        }
    }

    /// <summary>
    /// Determines if the selected row command can be executed.
    /// </summary>
    /// <returns>True if the selected row command can be executed, otherwise false.</returns>
    private bool CanExecuteSelectedRowCommand()
    {
        return SelectedItem is not null;
    }

    /// <summary>
    /// Updates the execution state of selected row commands.
    /// </summary>
    private void OnCanExecuteSelectedRowCommandChanged()
    {
        DuplicateSelectedRowCommand.NotifyCanExecuteChanged();
        DeleteSelectedRowCommand.NotifyCanExecuteChanged();
    }

    #region Helpers

    /// <summary>
    /// Adds a new row to the specified DataTable.
    /// </summary>
    /// <param name="dataTable">The DataTable to add a new row to.</param>
    private void AddRow(DataTable? dataTable)
    {
        if (dataTable is null) return;

        int newID = GetMaxId();
        EditHistory editHistory = CreateEditHistory(EditAction.RowInsert);

        AddNewRow(dataTable, newID, editHistory);
        // Add a new row to DataTableString if it exists
        if (DataTableString is not null)
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

    /// <summary>
    /// Duplicates the selected row in the specified DataTable.
    /// </summary>
    /// <param name="dataTable">The DataTable to duplicate the row in.</param>
    /// <param name="selectedItem">The selected row to duplicate.</param>
    /// <returns>The duplicated DataRowView, or null if the operation fails.</returns>
    private DataRowView? DuplicateSelectedRow(DataTable? dataTable, DataRowView? selectedItem)
    {
        if (dataTable is null || selectedItem is null) return null;

        // Get original row's index and nID
        var originalRow = selectedItem.Row;
        int originalIndex = dataTable.Rows.IndexOf(originalRow);
        int originalID = originalRow.Table.Columns.Contains("nID") ? Convert.ToInt32(originalRow["nID"]) : -1;

        // Try to assign newID = originalID + 1 if not already used
        int newID = originalID + 1;
        var usedIDs = dataTable.AsEnumerable().Select(row => row.Field<int>("nID")).ToHashSet();
        while (usedIDs.Contains(newID)) newID++;

        EditHistory editHistory = CreateEditHistory(EditAction.RowInsert);

        // Duplicate the row
        var duplicate = DuplicateRow(originalRow, dataTable);
        duplicate["nID"] = newID;

        // Insert directly below the original row
        int insertIndex = originalIndex + 1;
        dataTable.Rows.InsertAt(duplicate, insertIndex);

        // Track the edit
        editHistory.GroupedEdits.Add(new EditHistory
        {
            Row = insertIndex,
            AffectedRow = duplicate,
            Action = EditAction.RowInsert,
            NewValue = duplicate.ItemArray
        });

        // Handle DataTableString if needed
        if (DataTableString is not null && DataTableString.Columns.Contains("nID"))
        {
            var selectedItemString = GetRowViewById(DataTableString, selectedItem);
            if (selectedItemString is not null)
            {
                var duplicateStringRow = DuplicateRow(selectedItemString.Row, DataTableString);
                duplicateStringRow["nID"] = newID;
                DataTableString.Rows.InsertAt(duplicateStringRow, DataTableString.Rows.IndexOf(selectedItemString.Row) + 1);

                editHistory.GroupedEdits.Add(new EditHistory
                {
                    Row = DataTableString.Rows.IndexOf(duplicateStringRow),
                    AffectedRow = duplicateStringRow,
                    Action = EditAction.RowInsert,
                    NewValue = duplicateStringRow.ItemArray
                });
            }
        }

        // Set new selection
        SelectedItem = dataTable.DefaultView.Cast<DataRowView>()
            .FirstOrDefault(row => Convert.ToInt32(row["nID"]) == newID);

        OnPropertyChanged(nameof(dataTable));
        _undoStack.Push(editHistory);
        _redoStack.Clear();
        OnCanExecuteChangesChanged();

        return SelectedItem;
    }


    /// <summary>
    /// Deletes the selected row from the specified DataTable.
    /// </summary>
    /// <param name="dataTable">The DataTable to delete the row from.</param>
    /// <param name="selectedItem">The selected row to delete.</param>
    /// <returns>The new selected DataRowView, or null if the operation fails.</returns>
    private DataRowView? DeleteSelectedRow(DataTable? dataTable, DataRowView? selectedItem)
    {
        if (dataTable is null || selectedItem is null) return null;

        EditHistory editHistory = CreateEditHistory(EditAction.RowDelete);

        // Delete row from DataTableString if it exists
        if (DataTableString is not null && DataTableString.Columns.Contains("nID"))
        {
            var selectedItemString = GetRowViewById(DataTableString, selectedItem);
            if (selectedItemString is not null)
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

    /// <summary>
    /// Adds a new row to the specified DataTable with the given ID and edit history.
    /// </summary>
    /// <param name="dataTable">The DataTable to add the new row to.</param>
    /// <param name="newID">The ID for the new row.</param>
    /// <param name="editHistory">The edit history to record the addition.</param>
    /// <returns>The newly added DataRow.</returns>
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

    /// <summary>
    /// Removes the specified row from the DataTable and records the edit history.
    /// </summary>
    /// <param name="dataTable">The DataTable to remove the row from.</param>
    /// <param name="row">The DataRow to remove.</param>
    /// <param name="editHistory">The edit history to record the removal.</param>
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

    /// <summary>
    /// Duplicates the specified row in the DataTable.
    /// </summary>
    /// <param name="originalRow">The original DataRow to duplicate.</param>
    /// <param name="dataTable">The DataTable to add the duplicated row to.</param>
    /// <returns>The duplicated DataRow.</returns>
    private static DataRow DuplicateRow(DataRow originalRow, DataTable dataTable)
    {
        DataRow duplicate = dataTable.NewRow();
        duplicate.ItemArray = originalRow.ItemArray;
        return duplicate;
    }

    /// <summary>
    /// Updates and adds the duplicated row to the DataTable with the given ID and edit history.
    /// </summary>
    /// <param name="duplicateRow">The duplicated DataRow to add.</param>
    /// <param name="dataTable">The DataTable to add the duplicated row to.</param>
    /// <param name="newID">The ID for the duplicated row.</param>
    /// <param name="editHistory">The edit history to record the addition.</param>
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

    /// <summary>
    /// Gets the DataRowView by ID from the specified DataTable.
    /// </summary>
    /// <param name="dataTable">The DataTable to search.</param>
    /// <param name="selectedItem">The selected DataRowView to match the ID.</param>
    /// <returns>The matching DataRowView, or null if not found.</returns>
    private static DataRowView? GetRowViewById(DataTable dataTable, DataRowView selectedItem)
    {
        int nID = (int)selectedItem["nID"];
        return dataTable.DefaultView.Cast<DataRowView>()
                         .FirstOrDefault(rowView => (int)rowView["nID"] == nID);
    }

    /// <summary>
    /// Creates an EditHistory object with the specified action.
    /// </summary>
    /// <param name="action">The EditAction to record.</param>
    /// <returns>The created EditHistory object.</returns>
    private static EditHistory CreateEditHistory(EditAction action)
    {
        return new EditHistory
        {
            Action = action
        };
    }

    /// <summary>
    /// Gets the default value for the specified type.
    /// </summary>
    /// <param name="type">The type to get the default value for.</param>
    /// <returns>The default value for the type.</returns>
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

    /// <summary>
    /// Gets the maximum ID value from the DataTable and DataTableString.
    /// </summary>
    /// <returns>The maximum ID value.</returns>
    public int GetMaxId()
    {
        int newID = 1;

        if (DataTable is not null && DataTable.Rows.Count > 0 && DataTable.Columns.Contains("nID"))
        {
            var maxIdTable1 = DataTable.AsEnumerable()
                                    .Max(row => row.Field<int>("nID"));
            newID = maxIdTable1 + 1;
        }

        if (DataTableString is not null && DataTableString.Rows.Count > 0 && DataTableString.Columns.Contains("nID"))
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

    /// <summary>
    /// Undoes the last change made to the DataTable.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanUndo))]
    public void UndoChanges()
    {
        ApplyChanges(isUndo: true);
    }

    /// <summary>
    /// Redoes the last undone change to the DataTable.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanRedo))]
    public void RedoChanges()
    {
        ApplyChanges(isUndo: false);
    }

    /// <summary>
    /// Applies changes to the DataTable, either undoing or redoing them.
    /// </summary>
    /// <param name="isUndo">Whether to undo (true) or redo (false) the changes.</param>
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
                    if (groupedEdit.AffectedRow is not null)
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

                if (lastAffectedItem is not null)
                {
                    if (lastAffectedItem.Row.Table == SelectedItem?.Row.Table)
                    {
                        SelectedItem = lastAffectedItem;
                    }
                    else if (lastAffectedItem.Row.Table == SelectedItemString?.Row.Table && DataTable is not null)
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
            RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
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

    /// <summary>
    /// Adds a list of grouped edits to the undo stack.
    /// </summary>
    /// <param name="groupedEdits">The list of grouped edits to add.</param>
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

    /// <summary>
    /// Records an edit to the DataTable.
    /// </summary>
    /// <param name="row">The row index of the edit.</param>
    /// <param name="column">The column index of the edit.</param>
    /// <param name="oldValue">The old value before the edit.</param>
    /// <param name="newValue">The new value after the edit.</param>
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

    /// <summary>
    /// Starts grouping edits together.
    /// </summary>
    public void StartGroupingEdits()
    {
        if (!_isGroupingEdits)
        {
            _isGroupingEdits = true;
            _currentGroupedEdits.Clear();
        }
    }

    /// <summary>
    /// Ends grouping edits together.
    /// </summary>
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

    /// <summary>
    /// Updates the value of the specified column in the selected item.
    /// </summary>
    /// <param name="parameter">A tuple containing the new value and the column name.</param>
    [RelayCommand]
    private void UpdateItemValue((object? newValue, string column) parameter)
    {
        var (newValue, column) = parameter;

        UpdateItemValue(SelectedItem, newValue, column);
    }

    /// <summary>
    /// Updates the value of the specified column in the selected string item.
    /// </summary>
    /// <param name="parameter">A tuple containing the new value and the column name.</param>
    [RelayCommand]
    private void UpdateItemStringValue((object? newValue, string column) parameter)
    {
        var (newValue, column) = parameter;

        UpdateItemValue(SelectedItemString, newValue, column);
    }

    /// <summary>
    /// Updates the value of the specified column in the given DataRowView.
    /// </summary>
    /// <param name="item">The DataRowView to update.</param>
    /// <param name="newValue">The new value to set.</param>
    /// <param name="column">The column to update.</param>
    private void UpdateItemValue(DataRowView? item, object? newValue, string column)
    {
        if (item is not null && item.Row.Table.Columns.Contains(column))
        {
            var currentValue = item[column];

            var columnType = item.Row.Table.Columns[column]?.DataType;

            if (newValue is null || string.IsNullOrWhiteSpace(newValue.ToString()))
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
                else if (item.Row.Table == SelectedItemString?.Row.Table && DataTable is not null)
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

    /// <summary>
    /// Determines if two values are equal.
    /// </summary>
    /// <param name="value1">The first value to compare.</param>
    /// <param name="value2">The second value to compare.</param>
    /// <returns>True if the values are equal, otherwise false.</returns>
    private static bool AreValuesEqual(object? value1, object? value2)
    {
        if (value1 is null || value2 is null)
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

    /// <summary>
    /// Updates the value of the specified column in the selected item.
    /// </summary>
    /// <param name="newValue">The new value to set.</param>
    /// <param name="column">The column to update.</param>
    public void UpdateSelectedItemValue(object? newValue, string column)
    {
        UpdateItemValue(SelectedItem, newValue, column);
    }

    /// <summary>
    /// Updates the value of the specified column in the selected string item.
    /// </summary>
    /// <param name="newValue">The new value to set.</param>
    /// <param name="column">The column to update.</param>
    public void UpdateSelectedItemStringValue(object? newValue, string column)
    {
        UpdateItemValue(SelectedItemString, newValue, column);
    }

    #endregion

    private bool CanUndo() => _undoStack.Count > 0;
    private bool CanRedo() => _redoStack.Count > 0;

    /// <summary>
    /// Updates the execution state of undo and redo commands.
    /// </summary>
    public void OnCanExecuteChangesChanged()
    {
        UndoChangesCommand.NotifyCanExecuteChanged();
        RedoChangesCommand.NotifyCanExecuteChanged();
    }
    #endregion

    #region Filter

    /// <summary>
    /// Applies a filter to the DataTable based on the specified filter parts and search text.
    /// </summary>
    /// <param name="filterParts">The list of filter parts to apply.</param>
    /// <param name="columns">The columns to search in.</param>
    /// <param name="searchText">The text to search for.</param>
    /// <param name="matchCase">Whether to match case.</param>
    public void ApplyFileDataFilter(List<string> filterParts, string[] columns, string? searchText, bool matchCase)
    {
        try
        {
            if (DataTable is not null)
            {
                // Text search filter
                if (!string.IsNullOrEmpty(searchText))
                {
                    searchText = matchCase ? searchText : searchText.ToLower();

                    // Escape special characters for SQL LIKE pattern 
                    searchText = searchText.Replace("[", "[[]")
                                       .Replace("%", "[%]")
                                       .Replace("_", "[_]")
                                       .Replace("'", "''");

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
            RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
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
    private bool _isPCKMode = false;

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
        if (value is not null && DataTableString is not null && DataTableString.Columns.Contains("nID"))
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

    [ObservableProperty] private string? _selectedClientFolder = RegistrySettingsHelper.GetClientFolder();
    #endregion

}