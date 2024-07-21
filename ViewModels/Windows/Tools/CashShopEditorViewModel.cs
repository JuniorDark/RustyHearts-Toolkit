using Microsoft.Win32;
using RHToolkit.Messages;
using RHToolkit.Models;
using RHToolkit.Models.Database;
using RHToolkit.Models.Editor;
using RHToolkit.Models.MessageBox;
using RHToolkit.Properties;
using RHToolkit.Services;
using RHToolkit.Utilities;
using RHToolkit.ViewModels.Controls;
using RHToolkit.Views.Windows;
using System.Data;
using static RHToolkit.Models.EnumService;
using static RHToolkit.Models.MIP.MIPCoder;

namespace RHToolkit.ViewModels.Windows
{
    public partial class CashShopEditorViewModel : ObservableObject, IRecipient<ItemDataMessage>
    {
        private readonly IWindowsService _windowsService;
        private readonly ISqLiteDatabaseService _sqLiteDatabaseService;
        private readonly ItemHelper _itemHelper;
        private readonly System.Timers.Timer _searchTimer;
        private readonly Guid _token;

        private readonly FileManager _fileManager = new();
        private readonly Stack<EditHistory> _undoStack = new();
        private readonly Stack<EditHistory> _redoStack = new();

        public CashShopEditorViewModel(IWindowsService windowsService, ISqLiteDatabaseService sqLiteDatabaseService, ItemHelper itemHelper)
        {
            _token = Guid.NewGuid();
            _windowsService = windowsService;
            _sqLiteDatabaseService = sqLiteDatabaseService;
            _itemHelper = itemHelper;
            _searchTimer = new()
            {
                Interval = 1000,
                AutoReset = false
            };
            _searchTimer.Elapsed += SearchTimerElapsed;

            PopulateClassItems();
            PopulatePaymentTypeItems();
            PopulateShopCategoryItems();
            PopulateCostumeCategoryItems(0);
            PopulateItemStateItems();

            PopulateClassItemsFilter();
            PopulateShopCategoryItemsFilter();
            PopulateCostumeCategoryItemsFilter(-1);
            PopulateItemStateItemsFilter();

            WeakReferenceMessenger.Default.Register(this);
        }

        #region Commands
        [RelayCommand]
        private async Task CloseWindow(Window window)
        {
            bool shouldContinue = await CloseFile();

            if (!shouldContinue)
            {
                return;
            }

            window?.Close();
        }

        #region File

        [RelayCommand]
        private async Task LoadFile()
        {
            bool shouldContinue = await CloseFile();

            if (!shouldContinue)
            {
                return;
            }

            OpenFileDialog openFileDialog = new()
            {
                Filter = "cashshoplist.rh|cashshoplist.rh|All Files (*.*)|*.*",
                FilterIndex = 1
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    var cashShopTable = await _fileManager.FileToDataTableAsync(openFileDialog.FileName);

                    if (cashShopTable != null)
                    {
                        if (!cashShopTable.Columns.Contains("nCashCost00"))
                        {
                            RHMessageBoxHelper.ShowOKMessage($"The file '{CurrentFileName}' is not a valid cashshoplist rh file.", Resources.Error);
                            return;
                        }

                        ClearFile();

                        CurrentFile = openFileDialog.FileName;
                        CurrentFileName = Path.GetFileName(CurrentFile);
                        FileData = cashShopTable;
                        Title = $"Cash Shop Editor ({CurrentFileName})";
                        FileData.TableNewRow += DataTableChanged;
                        FileData.RowChanged += DataTableChanged;
                        FileData.RowDeleted += DataTableChanged;

                        ApplyFileDataFilter();
                    }

                    HasChanges = false;
                    SaveFileCommand.NotifyCanExecuteChanged();
                }
                catch (Exception ex)
                {
                    RHMessageBoxHelper.ShowOKMessage($"Error loading rh file: {ex.Message}", Resources.Error);
                }
            }
        }

        private int _changesCounter = 0;
        private const int ChangesBeforeSave = 10;

        private async void DataTableChanged(object sender, EventArgs e)
        {
            HasChanges = true;
            Title = $"Cash Shop Editor ({CurrentFileName})*";
            SaveFileCommand.NotifyCanExecuteChanged();

            _changesCounter++;
            if (_changesCounter >= ChangesBeforeSave)
            {
                try
                {
                    _changesCounter = 0;
                    //await _fileManager.SaveTempFile(CurrentFileName!, FileData!);
                }
                catch (Exception ex)
                {
                    RHMessageBoxHelper.ShowOKMessage($"Error saving backup file: {ex.Message}", "Save File Error");
                }
            }
        }

        [RelayCommand(CanExecute = nameof(CanExecuteSaveCommand))]
        private async Task SaveFile()
        {
            if (FileData == null) return;
            if (CurrentFile == null) return;
            try
            {
                await _fileManager.DataTableToFileAsync(CurrentFile, FileData);
                FileManager.ClearTempFile(CurrentFileName!);

                HasChanges = false;
                Title = $"Cash Shop Editor ({CurrentFileName})";
                SaveFileCommand.NotifyCanExecuteChanged();
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"Error saving file: {ex.Message}", "Save File Error");
            }
        }

        [RelayCommand(CanExecute = nameof(CanExecuteFileCommand))]
        private async Task SaveFileAs()
        {
            if (FileData == null || CurrentFileName == null) return;

            SaveFileDialog saveFileDialog = new()
            {
                Filter = "cashshoplist.rh|cashshoplist.rh|All Files (*.*)|*.*",
                FilterIndex = 1,
                FileName = CurrentFileName
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    string file = saveFileDialog.FileName;
                    await _fileManager.DataTableToFileAsync(file, FileData);
                    FileManager.ClearTempFile(CurrentFileName);
                    HasChanges = false;
                    CurrentFile = file;
                    CurrentFileName = Path.GetFileName(file);
                    Title = $"Cash Shop Editor ({CurrentFileName})";
                    SaveFileCommand.NotifyCanExecuteChanged();
                }
                catch (Exception ex)
                {
                    RHMessageBoxHelper.ShowOKMessage($"Error saving file: {ex.Message}", "Save File Error");
                }
            }
        }

        [RelayCommand(CanExecute = nameof(CanExecuteFileCommand))]
        public async Task<bool> CloseFile()
        {
            if (FileData == null) return true;

            if (HasChanges)
            {
                var result = RHMessageBoxHelper.ConfirmMessageYesNoCancel($"Save file '{CurrentFileName}' ?");
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

        private void ClearFile()
        {
            if (FileData != null)
            {
                FileData.TableNewRow -= DataTableChanged;
                FileData.RowChanged -= DataTableChanged;
                FileData.RowDeleted -= DataTableChanged;
            }

            FileManager.ClearTempFile(CurrentFileName);
            FileData = null;
            CurrentFile = null;
            CurrentFileName = null;
            FrameViewModel = null;
            SelectedItem = null;
            Title = $"Cash Shop Editor";
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
            SaveFileAsMIPCommand.NotifyCanExecuteChanged();
            CloseFileCommand.NotifyCanExecuteChanged();
            OpenSearchDialogCommand.NotifyCanExecuteChanged();
            AddNewRowCommand.NotifyCanExecuteChanged();
            AddItemCommand.NotifyCanExecuteChanged();
        }

        private SearchDialog? searchDialog;

        [RelayCommand(CanExecute = nameof(CanExecuteFileCommand))]
        private void OpenSearchDialog(string? parameter)
        {
            if (searchDialog == null || !searchDialog.IsVisible)
            {
                searchDialog = new SearchDialog();
                Window? shopEditorWindow = Application.Current.Windows.OfType<CashShopEditorWindow>().FirstOrDefault();
                if (shopEditorWindow != null)
                {
                    searchDialog.Owner = shopEditorWindow;
                }
                else
                {
                    searchDialog.Owner = Application.Current.MainWindow;
                }
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

            searchDialog.SearchTabControl.SelectedIndex = parameter == "Find" ? 0 : 1;
        }

        [ObservableProperty]
        private int _selectedRow;

        private int? lastFoundRow = null;

        private void Search(string searchText, bool matchCase)
        {
            if (string.IsNullOrWhiteSpace(searchText) || FileData == null)
                return;

            StringComparison comparison = matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

            bool found = false;
            bool wrappedAround = false;

            // Clear any existing message at the start of a new search
            searchDialog?.ShowMessage(string.Empty, Brushes.Transparent);

            // Determine the starting row index
            int startRowIndex = lastFoundRow.HasValue ? (lastFoundRow.Value + 1) % FileData.Rows.Count : 0;

            // Iterate through rows starting from the startRowIndex
            for (int rowIndex = startRowIndex; rowIndex < FileData.Rows.Count + startRowIndex; rowIndex++)
            {
                int currentRowIndex = rowIndex % FileData.Rows.Count; // Wrap around using modulus operator

                // Check if we've wrapped around
                if (rowIndex >= FileData.Rows.Count && !wrappedAround)
                {
                    wrappedAround = true;
                }

                var rowValues = FileData.Rows[currentRowIndex].ItemArray;
                if (rowValues.Any(cell => cell?.ToString()?.Contains(searchText, comparison) == true))
                {
                    // Found the value
                    found = true;

                    SelectedRow = currentRowIndex;
                    lastFoundRow = SelectedRow; // Update last found row

                    // Show message if wrapped around
                    if (wrappedAround)
                    {
                        searchDialog?.ShowMessage($"Found the 1st occurrence from the top. The end of the table has been reached.", Brushes.Green);
                    }

                    break;
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
                lastFoundRow = null;
            }
        }

        private void CountMatches(string searchText, bool matchCase)
        {
            if (string.IsNullOrEmpty(searchText) || FileData == null)
                return;

            StringComparison comparison = matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

            int count = 0;
            foreach (DataRow row in FileData.Rows)
            {
                foreach (var item in row.ItemArray)
                {
                    if (item != null && item.ToString()?.Contains(searchText, comparison) == true)
                    {
                        count++;
                    }
                }
            }

            searchDialog?.ShowMessage($"Count: {count} matches in entire table", Brushes.LightBlue);
        }

        private Point? lastFoundCell = null;
        private void Replace(string searchText, string replaceText, bool matchCase)
        {
            if (string.IsNullOrEmpty(searchText) || FileData == null)
                return;

            StringComparison comparison = matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

            if (lastFoundCell != null)
            {
                int rowIndex = (int)lastFoundCell.Value.X;
                int colIndex = (int)lastFoundCell.Value.Y;

                object cellValue = FileData.Rows[rowIndex][colIndex];
                string oldValue = cellValue?.ToString() ?? string.Empty;
                string newValue = oldValue.Replace(searchText, replaceText, comparison);

                if (!string.IsNullOrEmpty(oldValue) && oldValue.Contains(searchText, comparison))
                {
                    FileData.Rows[rowIndex][colIndex] = newValue;
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

        private void ReplaceAll(string searchText, string replaceText, bool matchCase)
        {
            if (string.IsNullOrEmpty(searchText) || FileData == null)
                return;

            StringComparison comparison = matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

            int replaceCount = 0;
            List<EditHistory> groupedEdits = [];

            for (int rowIndex = 0; rowIndex < FileData.Rows.Count; rowIndex++)
            {
                for (int colIndex = 0; colIndex < FileData.Columns.Count; colIndex++)
                {
                    string? oldValue = FileData.Rows[rowIndex][colIndex].ToString();
                    if (!string.IsNullOrEmpty(oldValue) && oldValue.Contains(searchText, comparison))
                    {
                        string newValue = oldValue.Replace(searchText, replaceText, comparison);
                        FileData.Rows[rowIndex][colIndex] = newValue;
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
                return 0;
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

        #endregion

        #region Edit History

        [RelayCommand(CanExecute = nameof(CanUndo))]
        private void UndoChanges()
        {
            if (_undoStack.Count > 0)
            {
                var edit = _undoStack.Pop();
                _redoStack.Push(edit);

                if (edit.GroupedEdits != null)
                {
                    foreach (var groupedEdit in edit.GroupedEdits)
                    {
                        FileData!.Rows[groupedEdit.Row][groupedEdit.Column] = groupedEdit.OldValue;
                    }
                }
                else
                {
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

                if (edit.GroupedEdits != null)
                {
                    foreach (var groupedEdit in edit.GroupedEdits)
                    {
                        FileData!.Rows[groupedEdit.Row][groupedEdit.Column] = groupedEdit.NewValue;
                    }
                }
                else
                {
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

        #region MIP
        [RelayCommand(CanExecute = nameof(CanExecuteFileCommand))]
        private async Task SaveFileAsMIP()
        {
            if (FileData == null || CurrentFileName == null) return;

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
                    await _fileManager.CompressToMipAsync(FileData, file, MIPCompressionMode.Compress);
                    SaveFileCommand.NotifyCanExecuteChanged();
                }
                catch (Exception ex)
                {
                    RHMessageBoxHelper.ShowOKMessage($"Error saving MIP file: {ex.Message}", "Save File Error");
                }
            }
        }
        #endregion

        #endregion

        #region Add Item

        [RelayCommand(CanExecute = nameof(CanExecuteFileCommand))]
        private void AddItem()
        {
            try
            {
                var itemData = new ItemData
                {
                    IsNewItem = true
                };

                var token = _token;

                _windowsService.OpenItemWindow(token, "CashShopItem", itemData);

            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"Error adding item: {ex.Message}", "Error");
            }
        }

        [RelayCommand]
        private void UpdateSelectedItem()
        {
            try
            {
                if (SelectedItem != null)
                {
                    var itemData = new ItemData
                    {
                        ItemId = (int)SelectedItem["nItemID"]
                    };

                    var token = _token;

                    _windowsService.OpenItemWindow(token, "CashShopItem", itemData);
                }
                
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"Error updating item: {ex.Message}", "Error");
            }
        }

        public void Receive(ItemDataMessage message)
        {
            if (message.Recipient == "CashShopEditorWindow" && message.Token == _token)
            {
                var itemData = message.Value;

                if (itemData.IsNewItem && itemData.ItemId != 0)
                {
                    // Create new item
                    CreateItem(itemData);
                }
                else
                {
                    // Update existing item
                    UpdateItem(itemData);
                }
            }
        }

        private void CreateItem(ItemData itemData)
        {
            AddNewRow();
            var frameViewModel = _itemHelper.GetItemData(itemData);
            FrameViewModel = frameViewModel;

            if (SelectedItem != null)
            {
                if (FileData != null && FileData.Rows.Count > 0)
                {
                    var maxId = FileData.AsEnumerable()
                                         .Max(row => row.Field<int>("nID"));
                    ShopID = maxId + 1;
                }
                else
                {
                    ShopID = 0; 
                }
                ItemID = frameViewModel.ItemId;
                ItemName = frameViewModel.ItemName;
                IconName = $"shop_{frameViewModel.IconName}";
                ItemAmount = frameViewModel.ItemAmount;
                ShopDescription = PaymentType == 0 ? $"{BonusRate}% of the price goes to Bonus": "Purchased with Bonus";
                ShopCategory = GetShopCategory(frameViewModel.SubCategory);
                CostumeCategory = GetCostumeCategory(frameViewModel.SubCategory);
                Class = frameViewModel.JobClass;

            }

        }

        private void UpdateItem(ItemData itemData)
        {
            var frameViewModel = _itemHelper.GetItemData(itemData);
            FrameViewModel = frameViewModel;

            if (SelectedItem != null)
            {
                ItemID = frameViewModel.ItemId;
                ItemName = frameViewModel.ItemName;
                IconName = $"shop_{frameViewModel.IconName}";
                ItemAmount = frameViewModel.ItemAmount;
                ShopCategory = GetShopCategory(frameViewModel.SubCategory);
                CostumeCategory = GetCostumeCategory(frameViewModel.SubCategory);
                Class = frameViewModel.JobClass;
            }
        }

        private static int GetShopCategory(int category)
        {
            return category switch
            {
                10 or 11 or 12 or 12 or 13 or 14 or 15 or 16 or 17 or 18 or 19 or 20 => 1,
                22 => 3,
                46 => 0,
                53 => 2,
                _ => 2
            };
        }

        private static int GetCostumeCategory(int category)
        {
            return category switch
            {
                11 => 0,
                12 => 8,
                13 => 2,
                14 => 3,
                15 => 4,
                16 => 5,
                17 => 6,
                18 => 7,
                19 or 20 => 1,
                38 or 53 => 0,
                54 => 2,
                _ => 0
            };
        }

        #endregion

        #region Filter

        private void ApplyFileDataFilter()
        {
            if (FileData != null)
            {
                List<string> filterParts = [];

                // Category filters
                if (ClassFilter != 0)
                {
                    filterParts.Add($"nJob = {ClassFilter}");
                }

                if (ShopCategoryFilter != -1)
                {
                    filterParts.Add($"nCategory = {ShopCategoryFilter}");
                }

                if (CostumeCategoryFilter != 0)
                {
                    filterParts.Add($"nCostumeCategory = {CostumeCategoryFilter}");
                }

                if (ItemStateFilter != 0)
                {
                    filterParts.Add($"nItemState = {ItemStateFilter}");
                }

                // Text search filter
                if (!string.IsNullOrEmpty(SearchText))
                {
                    string searchText = SearchText.ToLower();
                    filterParts.Add($"(CONVERT(nItemID, 'System.String') LIKE '%{searchText}%' OR wszName LIKE '%{searchText}%')");
                }

                // Join all parts with ' AND '
                string filter = string.Join(" AND ", filterParts);

                FileData.DefaultView.RowFilter = filter;
            }
        }


        [ObservableProperty]
        private string? _searchText;
        partial void OnSearchTextChanged(string? value)
        {
            _searchTimer.Stop();
            _searchTimer.Start();
        }

        private void SearchTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            if (FileData != null)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    _searchTimer.Stop();
                    ApplyFileDataFilter();
                });
            }

        }

        #region Comboboxes Filter

        [ObservableProperty]
        private List<NameID>? _classItemsFilter;

        private void PopulateClassItemsFilter()
        {
            try
            {
                ClassItemsFilter = GetEnumItems<CharClass>(true);

                if (ClassItemsFilter.Count > 0)
                {
                    ClassFilter = 0;
                }
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
            }
        }

        [ObservableProperty]
        private List<NameID>? _shopCategoryItemsFilter;

        private void PopulateShopCategoryItemsFilter()
        {
            try
            {
                ShopCategoryItemsFilter = GetEnumItems<CashShopCategoryFilter>(false);

                if (ShopCategoryItemsFilter.Count > 0)
                {
                    ShopCategoryFilter = -1;
                }
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
            }
        }

        [ObservableProperty]
        private List<NameID>? _costumeCategoryItemsFilter;

        private void PopulateCostumeCategoryItemsFilter(int cashShopCategory)
        {
            try
            {
                CostumeCategoryItemsFilter = cashShopCategory switch
                {
                    -1 => GetEnumItems<CashShopAllCategory>(false),
                    0 => GetEnumItems<CashShopPackageCategory>(false),
                    1 => GetEnumItems<CashShopCostumeCategory>(true),
                    2 => GetEnumItems<CashShopItemCategory>(true),
                    3 => GetEnumItems<CashShopPetCategory>(true),
                    4 => GetEnumItems<CashShopBonusCategory>(true),
                    _ => throw new ArgumentOutOfRangeException(nameof(cashShopCategory), $"Invalid category value '{cashShopCategory}'"),
                };

                if (CostumeCategoryItemsFilter.Count > 0)
                {
                    CostumeCategoryFilter = 0;
                    OnPropertyChanged(nameof(CostumeCategoryFilter));
                }
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
            }
        }

        [ObservableProperty]
        private List<NameID>? _itemStateItemsFilter;

        private void PopulateItemStateItemsFilter()
        {
            try
            {
                ItemStateItemsFilter = GetEnumItems<CashShopItemState>(false);

                if (ItemStateItemsFilter.Count > 0)
                {
                    ItemStateFilter = 0;
                }
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
            }
        }

        [ObservableProperty]
        private int _classFilter;

        partial void OnClassFilterChanged(int value)
        {
            if (FileData != null)
            {
                ApplyFileDataFilter();
            }
        }

        [ObservableProperty]
        private int _itemStateFilter;
        partial void OnItemStateFilterChanged(int value)
        {
            if (FileData != null)
            {
                ApplyFileDataFilter();
            }

        }

        [ObservableProperty]
        private int _shopCategoryFilter;
        partial void OnShopCategoryFilterChanged(int value)
        {
            if (FileData != null)
            {
                PopulateCostumeCategoryItemsFilter(value);
                ApplyFileDataFilter();
            }
        }

        [ObservableProperty]
        private int _costumeCategoryFilter;
        partial void OnCostumeCategoryFilterChanged(int value)
        {
            if (FileData != null)
            {
                ApplyFileDataFilter();
            }

        }
        #endregion

        #endregion

        #region Properties
        [ObservableProperty]
        private string _title = $"Cash Shop Editor";

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
            if (value != null)
            {
                ItemData itemData = new()
                {
                    ItemId = (int)value["nItemID"]
                };

                FrameViewModel = _itemHelper.GetItemData(itemData);
                OnCanExecuteSelectedRowCommandChanged();

                IsSelectedItemVisible = Visibility.Visible;

                ShopID = (int)value["nID"];
                ItemID = (int)value["nItemID"];
                Class = (int)value["nJob"];
                PaymentType = (int)value["nPaymentType"];
                CashCost = (int)value["nCashCost00"];
                CashMileage = (int)value["nCashMileage"];
                ItemName = (string)value["wszName"];
                IconName = (string)value["szShopBigIcon"];
                ShopDescription = (string)value["wszDesc"];
                ShopCategory = (int)value["nCategory"];
                CostumeCategory = (int)value["nCostumeCategory"];
                ItemAmount = (int)value["nValue00"];
                ItemState = (int)value["nItemState"];
                IsHidden = (int)value["nHidden"] == 1 ? true : false;
                NoGift = (int)value["nNoGift"] == 1 ? true : false;
                StartSellingDate = (int)value["nStartSellingDate"] == 0 ? (DateTime?)null : DateTimeFormatter.ConvertIntToDate((int)value["nStartSellingDate"]);
                EndSellingDate = (int)value["nEndSellingDate"] == 0 ? (DateTime?)null : DateTimeFormatter.ConvertIntToDate((int)value["nEndSellingDate"]);
                SaleStartSellingDate = (int)value["nSaleStartSellingDate"] == 0 ? (DateTime?)null : DateTimeFormatter.ConvertIntToDate((int)value["nSaleStartSellingDate"]);
                SaleEndSellingDate = (int)value["nSaleEndSellingDate"] == 0 ? (DateTime?)null : DateTimeFormatter.ConvertIntToDate((int)value["nSaleEndSellingDate"]);
            }
            else
            {
                IsSelectedItemVisible = Visibility.Hidden;
            }
        }

        [ObservableProperty]
        private string? _currentFile;

        [ObservableProperty]
        private string? _currentFileName;

        [ObservableProperty]
        private bool _hasChanges = false;

        #region SelectedItem

        [ObservableProperty]
        private FrameViewModel? _frameViewModel;

        [ObservableProperty]
        private string? _itemName;
        partial void OnItemNameChanged(string? oldValue, string? newValue)
        {
            if (SelectedItem != null && oldValue != newValue)
            {
                SelectedItem["wszName"] = newValue;
            }
        }

        [ObservableProperty]
        private string? _shopDescription;
        partial void OnShopDescriptionChanged(string? oldValue, string? newValue)
        {
            if (SelectedItem != null && oldValue != newValue)
            {
                SelectedItem["wszDesc"] = newValue;
            }
        }

        [ObservableProperty]
        private string? _iconName;
        partial void OnIconNameChanged(string? oldValue, string? newValue)
        {
            if (SelectedItem != null && oldValue != newValue)
            {
                SelectedItem["szShopBigIcon"] = newValue;
            }
        }

        [ObservableProperty]
        private int _itemID;
        partial void OnItemIDChanged(int oldValue, int newValue)
        {
            if (SelectedItem != null && oldValue != newValue)
            {
                SelectedItem["nItemID"] = newValue;

                ItemData itemData = new()
                {
                    ItemId = newValue
                };

                FrameViewModel = _itemHelper.GetItemData(itemData);
                OnCanExecuteSelectedRowCommandChanged();

                if (FrameViewModel.ItemName == "Unknown Item")
                {
                    ItemName = $"{FrameViewModel.ItemName}";
                    IconName = $"{FrameViewModel.IconName}";
                }
                else
                {
                    ItemName = FrameViewModel.ItemName;
                    IconName = $"shop_{FrameViewModel.IconName}";
                }


            }
        }

        [ObservableProperty]
        private int _shopID;
        partial void OnShopIDChanged(int oldValue, int newValue)
        {
            if (SelectedItem != null && oldValue != newValue)
            {
                SelectedItem["nID"] = newValue;
            }
        }

        [ObservableProperty]
        private int _itemAmount;
        partial void OnItemAmountChanged(int value)
        {
            if (SelectedItem != null)
            {
                ItemName = ShopCategory != 0 && value > 1 ? $"{ItemName} ({value})" : ItemName;
                SelectedItem["nValue00"] = value;
            }
        }

        [ObservableProperty]
        private int _paymentType;
        partial void OnPaymentTypeChanged(int value)
        {
            if (SelectedItem != null)
            {
                IsEnabled = value == 0 ? true : false;
                SelectedItem["nPaymentType"] = value;
                if (BonusRate != 0)
                {
                    CashMileage = value == 1 ? 0 : CashCost * BonusRate / 100;
                }
                
                ShopDescription = value == 0 ? $"{BonusRate}% of the price goes to Bonus" : "Purchased with Bonus";
            }
        }

        [ObservableProperty]
        private int _class;
        partial void OnClassChanged(int oldValue, int newValue)
        {
            if (SelectedItem != null && oldValue != newValue)
            {
                SelectedItem["nJob"] = newValue;
                SelectedItem["szJob"] = newValue;
            }
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CashMileage))]
        private int _bonusRate = 10;
        partial void OnBonusRateChanged(int oldValue, int newValue)
        {
            if (newValue != 0 && PaymentType == 0)
            {
                CashMileage = CashCost * newValue / 100;
                ShopDescription = $"{BonusRate}% of the price goes to Bonus";
            }
            else
            {
                CashMileage = 0;
                ShopDescription = "Purchased with Bonus";
            }
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CashMileage))]
        private int _cashCost;
        partial void OnCashCostChanged(int oldValue, int newValue)
        {
            if (SelectedItem != null)
            {
                if (BonusRate != 0 && PaymentType == 0)
                {
                    CashMileage = newValue * BonusRate / 100;
                }
                else
                {
                    CashMileage = 0;
                }

                SelectedItem["nCashCost00"] = newValue;
            }
        }

        [ObservableProperty]
        private int _saleCashCost;
        partial void OnSaleCashCostChanged(int oldValue, int newValue)
        {
            if (SelectedItem != null)
            {
                SelectedItem["nSaleCashCost00"] = newValue;
            }
        }

        [ObservableProperty]
        private int _cashMileage;
        partial void OnCashMileageChanged(int oldValue, int newValue)
        {
            if (SelectedItem != null && oldValue != newValue)
            {
                SelectedItem["nCashMileage"] = newValue;
            }
        }

        [ObservableProperty]
        private int _itemState;
        partial void OnItemStateChanged(int oldValue, int newValue)
        {
            if (SelectedItem != null && oldValue != newValue)
            {
                SelectedItem["nItemState"] = newValue;
            }
        }

        [ObservableProperty]
        private bool _isHidden;
        partial void OnIsHiddenChanged(bool oldValue, bool newValue)
        {
            if (SelectedItem != null && oldValue != newValue)
            {
                SelectedItem["nHidden"] = newValue == true ? 1 : 0;
            }
        }

        [ObservableProperty]
        private bool _noGift;
        partial void OnNoGiftChanged(bool oldValue, bool newValue)
        {
            if (SelectedItem != null && oldValue != newValue)
            {
                SelectedItem["nNoGift"] = newValue == true ? 1 : 0;
            }
        }

        [ObservableProperty]
        private bool _isEnabled = true;

        [ObservableProperty]
        private int _itemAmountMax;

        [ObservableProperty]
        private int _shopCategory;
        partial void OnShopCategoryChanged(int value)
        {
            if (SelectedItem != null)
            {
                if (FrameViewModel != null)
                {
                    ItemAmountMax = value switch
                    {
                        0 => 525600,
                        1 => 0,
                        _ => FrameViewModel.OverlapCnt

                    };
                }

                PopulateCostumeCategoryItems(value);
                SelectedItem["nCategory"] = value;
            }
        }

        [ObservableProperty]
        private int _costumeCategory;
        partial void OnCostumeCategoryChanged(int value)
        {
            if (SelectedItem != null)
            {
                SelectedItem["nCostumeCategory"] = value;
            }
        }

        [ObservableProperty]
        private string? _startSellingDateValue;

        [ObservableProperty]
        private DateTime? _startSellingDate;

        partial void OnStartSellingDateChanged(DateTime? value)
        {
            if (SelectedItem != null)
            {
                SelectedItem["nStartSellingDate"] = value == null ? 0 : DateTimeFormatter.ConvertDateToInt((DateTime)value);
                StartSellingDateValue = value.HasValue ? value.Value.ToString("yyyy/MM/dd") : "No Selling Date";
            }
        }

        [ObservableProperty]
        private string? _endSellingDateValue;

        [ObservableProperty]
        private DateTime? _endSellingDate;
        partial void OnEndSellingDateChanged(DateTime? value)
        {
            if (SelectedItem != null)
            {
                SelectedItem["nEndSellingDate"] = value == null ? 0 : DateTimeFormatter.ConvertDateToInt((DateTime)value);
                EndSellingDateValue = value.HasValue ? value.Value.ToString("yyyy/MM/dd") : "No Selling Date";
            }
        }

        [ObservableProperty]
        private string _saleStartSellingDateValue = "No Sale Date";

        [ObservableProperty]
        private DateTime? _saleStartSellingDate;
        partial void OnSaleStartSellingDateChanged(DateTime? value)
        {
            if (SelectedItem != null)
            {
                SelectedItem["nSaleStartSellingDate"] = value == null ? 0 : DateTimeFormatter.ConvertDateToInt((DateTime)value);
                SaleStartSellingDateValue = value.HasValue ? value.Value.ToString("yyyy/MM/dd") : "No Sale Date";
            }
        }

        [ObservableProperty]
        private string _saleEndSellingDateValue = "No Sale Date";

        [ObservableProperty]
        private DateTime? _saleEndSellingDate;
        partial void OnSaleEndSellingDateChanged(DateTime? value)
        {
            if (SelectedItem != null)
            {
                SelectedItem["nSaleEndSellingDate"] = value == null ? 0 : DateTimeFormatter.ConvertDateToInt((DateTime)value);
                SaleEndSellingDateValue = value.HasValue ? value.Value.ToString("yyyy/MM/dd") : "No Sale Date";
            }
        }

        #endregion


        #region Comboboxes

        [ObservableProperty]
        private List<NameID>? _paymentTypeItems;

        private void PopulatePaymentTypeItems()
        {
            try
            {
                PaymentTypeItems =
                [
                    new NameID { ID = 0, Name = "Cash" },
                    new NameID { ID = 1, Name = "Bonus" }
                ];

            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
            }
        }

        [ObservableProperty]
        private List<NameID>? _classItems;

        private void PopulateClassItems()
        {
            try
            {
                ClassItems = GetEnumItems<CharClass>(true);

                if (ClassItems.Count > 0)
                {
                    Class = 0;
                }
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
            }
        }

        [ObservableProperty]
        private List<NameID>? _shopCategoryItems;

        private void PopulateShopCategoryItems()
        {
            try
            {
                ShopCategoryItems = GetEnumItems<CashShopCategory>(false);

                if (ShopCategoryItems.Count > 0)
                {
                    ShopCategory = 0;
                }
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
            }
        }

        [ObservableProperty]
        private List<NameID>? _costumeCategoryItems;

        private void PopulateCostumeCategoryItems(int cashShopCategory)
        {
            try
            {
                CostumeCategoryItems = cashShopCategory switch
                {
                    0 => GetEnumItems<CashShopPackageCategory>(false),
                    1 => GetEnumItems<CashShopCostumeCategory>(false),
                    2 => GetEnumItems<CashShopItemCategory>(false),
                    3 => GetEnumItems<CashShopPetCategory>(false),
                    4 => GetEnumItems<CashShopBonusCategory>(false),
                    _ => throw new ArgumentOutOfRangeException(nameof(cashShopCategory), $"Invalid category value '{cashShopCategory}'"),
                };

                if (CostumeCategoryItems.Count > 0)
                {
                    CostumeCategory = CostumeCategoryItems.First().ID;
                    OnPropertyChanged(nameof(CostumeCategory));
                }
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
            }
        }

        [ObservableProperty]
        private List<NameID>? _itemStateItems;

        private void PopulateItemStateItems()
        {
            try
            {
                ItemStateItems = GetEnumItems<CashShopItemState>(false);

                if (ItemStateItems.Count > 0)
                {
                    ItemState = 0;
                }
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
            }
        }
        #endregion

        [ObservableProperty]
        private Visibility _isSelectedItemVisible = Visibility.Hidden;

        #endregion
    }
}
