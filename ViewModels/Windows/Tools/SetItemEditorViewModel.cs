using Microsoft.Win32;
using RHToolkit.Messages;
using RHToolkit.Models;
using RHToolkit.Models.Database;
using RHToolkit.Models.Editor;
using RHToolkit.Models.MessageBox;
using RHToolkit.Properties;
using RHToolkit.Services;
using RHToolkit.ViewModels.Controls;
using RHToolkit.Views.Windows;
using System.Data;
using System.Windows.Controls;

namespace RHToolkit.ViewModels.Windows
{
    public partial class SetItemEditorViewModel : ObservableObject, IRecipient<ItemDataMessage>
    {
        private readonly Guid _token;
        private readonly IWindowsService _windowsService;
        private readonly IFrameService _frameService;
        private readonly FileManager _fileManager;
        private readonly System.Timers.Timer _searchTimer;

        public SetItemEditorViewModel(IWindowsService windowsService, IFrameService frameService, ItemDataManager itemDataManager)
        {
            _token = Guid.NewGuid();
            _windowsService = windowsService;
            _frameService = frameService;
            _itemDataManager = itemDataManager;
            DataTableManager = new();
            _fileManager = new();
            _searchTimer = new()
            {
                Interval = 500,
                AutoReset = false
            };
            _searchTimer.Elapsed += SearchTimerElapsed;

            WeakReferenceMessenger.Default.Register(this);
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
                    Filter = "setitem.rh|setitem.rh|All Files (*.*)|*.*",
                    FilterIndex = 1
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    string? fileName = openFileDialog.FileName;
                    if (fileName == null)
                    {
                        RHMessageBoxHelper.ShowOKMessage("No file selected.", Resources.Error);
                        return;
                    }

                    var setItemTable = await _fileManager.FileToDataTableAsync(fileName);

                    // Determine the path of the setitem_string.rh file
                    string? directory = Path.GetDirectoryName(fileName);
                    if (directory == null)
                    {
                        RHMessageBoxHelper.ShowOKMessage("Invalid file directory.", Resources.Error);
                        return;
                    }

                    string setItemStringFilePath = Path.Combine(directory, "setitem_string.rh");

                    // Check if setitem_string.rh exists
                    if (!File.Exists(setItemStringFilePath))
                    {
                        RHMessageBoxHelper.ShowOKMessage("The file 'setitem_string.rh' does not exist in the same directory.", Resources.Error);
                        return;
                    }

                    var setItemStringTable = await _fileManager.FileToDataTableAsync(setItemStringFilePath);

                    if (setItemTable != null && setItemStringTable != null)
                    {
                        if (!setItemTable.Columns.Contains("nSetItemID00"))
                        {
                            RHMessageBoxHelper.ShowOKMessage($"The file '{Path.GetFileName(fileName)}' is not a valid setitem rh file.", Resources.Error);
                            return;
                        }

                        ClearFile();

                        CurrentFile = fileName;
                        CurrentFileName = Path.GetFileName(CurrentFile);

                        DataTableManager.LoadFile(setItemTable);
                        DataTableManager.LoadFileString(setItemStringTable);
                        DataTableManager.CurrentFile = fileName;
                        DataTableManager.CurrentFileName = Path.GetFileName(CurrentFile);

                        if (DataTableManager.DataTable != null && DataTableManager.DataTable.Rows.Count > 0)
                        {
                            SelectedItem = DataTableManager.DataTable.DefaultView[0];
                        }

                        Title = $"Set Item Editor ({CurrentFileName})";
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
                Window? shopEditorWindow = Application.Current.Windows.OfType<CashShopEditorWindow>().FirstOrDefault();
                Window owner = shopEditorWindow ?? Application.Current.MainWindow;
                DataTableManager.OpenSearchDialog(owner, parameter, DataGridSelectionUnit.FullRow);

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
            FrameViewModels = null;
            SelectedItem = null;
            Title = $"Set Item Editor";
            OpenMessage = "Open a file";
            IsVisible = Visibility.Hidden;
            OnCanExecuteFileCommandChanged();
        }

        private bool CanExecuteFileCommand()
        {
            return DataTableManager != null && DataTableManager.DataTable != null;
        }

        private bool CanExecuteSelectedItemCommand()
        {
            return SelectedItem != null;
        }

        private void OnCanExecuteSelectedItemCommandChanged()
        {
            AddItemCommand.NotifyCanExecuteChanged();
        }

        private void OnCanExecuteFileCommandChanged()
        {
            CloseFileCommand.NotifyCanExecuteChanged();
            OpenSearchDialogCommand.NotifyCanExecuteChanged();
            AddItemCommand.NotifyCanExecuteChanged();
            CreateItemCommand.NotifyCanExecuteChanged();
        }

        #endregion

        #region Add Item

        [RelayCommand(CanExecute = nameof(CanExecuteSelectedItemCommand))]
        private void AddItem(string parameter)
        {
            try
            {
                if (int.TryParse(parameter, out int slotIndex))
                {

                    if (SelectedItem != null)
                    {
                        var itemData = new ItemData
                        {
                            SlotIndex = slotIndex,
                            ItemId = (int)SelectedItem[$"nSetItemID0{slotIndex - 1}"]
                        };

                        var token = _token;

                        _windowsService.OpenItemWindow(token, "SetItem", itemData);
                    }
                }

            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"Error: {ex.Message}", "Error");
            }
        }

        public void Receive(ItemDataMessage message)
        {
            if (message.Recipient == "SetItemEditorWindow" && message.Token == _token)
            {
                var itemData = message.Value;

                if (SelectedItem != null)
                {
                    switch (itemData.SlotIndex)
                    {
                        case 1:
                            SetItemID00 = itemData.ItemId;
                            break;
                        case 2:
                            SetItemID01 = itemData.ItemId;
                            break;
                        case 3:
                            SetItemID02 = itemData.ItemId;
                            break;
                        case 4:
                            SetItemID03 = itemData.ItemId;
                            break;
                        case 5:
                            SetItemID04 = itemData.ItemId;
                            break;
                        case 6:
                            SetItemID05 = itemData.ItemId;
                            break;
                    }
                }
            }
        }

        [RelayCommand(CanExecute = nameof(CanExecuteFileCommand))]
        private void CreateItem()
        {
            if (DataTableManager.DataTable != null)
            {
                DataTableManager.AddNewRow();
                SelectedItem = DataTableManager.SelectedItem;

                if (DataTableManager.DataTable.Rows.Count > 0)
                {
                    var maxId = DataTableManager.DataTable.AsEnumerable()
                                         .Max(row => row.Field<int>("nID"));
                    SetID = maxId + 1;
                }
                else
                {
                    SetID = 1;
                }

                SetName = "New Set";
            }
        }

        private void SetFrameViewModelData(int itemId, int slotIndex)
        {
            if (itemId != 0)
            {
                ItemData itemData = new()
                {
                    ItemId = itemId,
                    SlotIndex = slotIndex,
                };

                var frameViewModel = ItemDataManager.GetItemData(itemData);
                RemoveFrameViewModel(itemData.SlotIndex);
                SetFrameViewModel(frameViewModel);
            }

        }

        private void SetFrameViewModel(FrameViewModel frameViewModel)
        {
            FrameViewModels ??= [];
            FrameViewModels.Add(frameViewModel);
            OnPropertyChanged(nameof(FrameViewModels));
        }
        #endregion

        #region Remove Item

        [RelayCommand]
        private void RemoveItem(string parameter)
        {
            if (int.TryParse(parameter, out int slotIndex))
            {
                switch (slotIndex)
                {
                    case 1:
                        SetItemID00 = 0;
                        break;
                    case 2:
                        SetItemID01 = 0;
                        break;
                    case 3:
                        SetItemID02 = 0;
                        break;
                    case 4:
                        SetItemID03 = 0;
                        break;
                    case 5:
                        SetItemID04 = 0;
                        break;
                    case 6:
                        SetItemID05 = 0;
                        break;
                }

                RemoveFrameViewModel(slotIndex);
            }
        }

        private void RemoveFrameViewModel(int slotIndex)
        {
            if (FrameViewModels != null)
            {
                var removedItemIndex = FrameViewModels.FindIndex(i => i.SlotIndex == slotIndex);
                if (removedItemIndex != -1)
                {
                    FrameViewModels?.RemoveAt(removedItemIndex);
                }
                OnPropertyChanged(nameof(FrameViewModels));
            }
        }
        #endregion

        #endregion

        private void FormatSetEffect()
        {
            string setEffect01 = _frameService.GetOptionName(SetOption00, SetOptionValue00);
            string setEffect02 = _frameService.GetOptionName(SetOption01, SetOptionValue01);
            string setEffect03 = _frameService.GetOptionName(SetOption02, SetOptionValue02);
            string setEffect04 = _frameService.GetOptionName(SetOption03, SetOptionValue03);
            string setEffect05 = _frameService.GetOptionName(SetOption04, SetOptionValue04);

            string setEffect = $"{Resources.SetEffect}\n";
            if (SetOption00 != 0)
                setEffect += $"{Resources.Set2}: {setEffect01}\n";
            if (SetOption01 != 0)
                setEffect += $"{Resources.Set3}: {setEffect02}\n";
            if (SetOption02 != 0)
                setEffect += $"{Resources.Set4}: {setEffect03}\n";
            if (SetOption03 != 0)
                setEffect += $"{Resources.Set5}: {setEffect04}\n";
            if (SetOption04 != 0)
                setEffect += $"{Resources.Set6}: {setEffect05}\n";

            SetEffectText = setEffect;
        }

        #region Filter

        private void ApplyFileDataFilter()
        {
            if (DataTableManager.DataTable != null)
            {
                List<string> filterParts = [];

                // Text search filter
                if (!string.IsNullOrEmpty(SearchText))
                {
                    string searchText = SearchText.ToLower();
                    filterParts.Add($"(CONVERT(nID, 'System.String') LIKE '%{searchText}%' OR wszName LIKE '%{searchText}%' OR CONVERT(nSetItemID00, 'System.String') LIKE '%{searchText}%' OR CONVERT(nSetItemID01, 'System.String') LIKE '%{searchText}%' OR CONVERT(nSetItemID02, 'System.String') LIKE '%{searchText}%' OR CONVERT(nSetItemID03, 'System.String') LIKE '%{searchText}%' OR CONVERT(nSetItemID04, 'System.String') LIKE '%{searchText}%' OR CONVERT(nSetItemID05, 'System.String') LIKE '%{searchText}%' OR CONVERT(nSetOption00, 'System.String') LIKE '%{searchText}%' OR CONVERT(nSetOption01, 'System.String') LIKE '%{searchText}%' OR CONVERT(nSetOption02, 'System.String') LIKE '%{searchText}%' OR CONVERT(nSetOption03, 'System.String') LIKE '%{searchText}%' OR CONVERT(nSetOption04, 'System.String') LIKE '%{searchText}%')");
                }

                string filter = string.Join(" AND ", filterParts);

                DataTableManager.DataTable.DefaultView.RowFilter = filter;
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
            if (DataTableManager.DataTable != null)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    _searchTimer.Stop();
                    ApplyFileDataFilter();
                });
            }

        }

        #endregion

        #region Properties
        [ObservableProperty]
        private string _title = $"Set Item Editor";

        [ObservableProperty]
        private string? _openMessage = "Open a file";

        [ObservableProperty]
        private string? _setEffectText;

        [ObservableProperty]
        private Visibility _isSelectedItemVisible = Visibility.Hidden;

        [ObservableProperty]
        private Visibility _isVisible = Visibility.Hidden;

        [ObservableProperty]
        private DataTableManager _dataTableManager;
        partial void OnDataTableManagerChanged(DataTableManager value)
        {
            OnCanExecuteFileCommandChanged();
        }

        [ObservableProperty]
        private ItemDataManager _itemDataManager;

        [ObservableProperty]
        private string? _currentFile;

        [ObservableProperty]
        private string? _currentFileName;

        [ObservableProperty]
        private DataTable? _setItemStringTable;

        [ObservableProperty]
        private int _optionMinValue;

        [ObservableProperty]
        private int _optionMaxValue = 10000;

        #region SelectedItem

        [ObservableProperty]
        private List<FrameViewModel>? _frameViewModels;

        private bool _isUpdatingSelectedItem = false;

        [ObservableProperty]
        private DataRowView? _selectedItem;
        partial void OnSelectedItemChanged(DataRowView? value)
        {
            _isUpdatingSelectedItem = true;

            if (DataTableManager != null)
            {
                DataTableManager.SelectedItem = value;
            }

            if (value != null)
            {
                IsSelectedItemVisible = Visibility.Visible;
                FrameViewModels = null;
                SetID = (int)value["nID"];
                SetName = (string)value["wszName"];
                SetItemID00 = (int)value["nSetItemID00"];
                SetItemID01 = (int)value["nSetItemID01"];
                SetItemID02 = (int)value["nSetItemID02"];
                SetItemID03 = (int)value["nSetItemID03"];
                SetItemID04 = (int)value["nSetItemID04"];
                SetItemID05 = (int)value["nSetItemID05"];


                SetOption00 = (int)value["nSetOption00"];
                SetOption01 = (int)value["nSetOption01"];
                SetOption02 = (int)value["nSetOption02"];
                SetOption03 = (int)value["nSetOption03"];
                SetOption04 = (int)value["nSetOption04"];

                SetOptionValue00 = (int)value["nSetOptionvlue00"];
                SetOptionValue01 = (int)value["nSetOptionvlue01"];
                SetOptionValue02 = (int)value["nSetOptionvlue02"];
                SetOptionValue03 = (int)value["nSetOptionvlue03"];
                SetOptionValue04 = (int)value["nSetOptionvlue04"];

            }
            else
            {
                IsSelectedItemVisible = Visibility.Hidden;
            }

            _isUpdatingSelectedItem = false;

            OnCanExecuteSelectedItemCommandChanged();
        }

        [ObservableProperty]
        private int _setID;
        partial void OnSetIDChanged(int oldValue, int newValue)
        {
            if (!_isUpdatingSelectedItem && SelectedItem != null && oldValue != newValue)
            {
                SelectedItem["nID"] = newValue;
            }
        }

        [ObservableProperty]
        private string? _setName;
        partial void OnSetNameChanged(string? oldValue, string? newValue)
        {
            if (!_isUpdatingSelectedItem && SelectedItem != null && oldValue != newValue)
            {
                SelectedItem["wszName"] = newValue;
            }
        }

        [ObservableProperty]
        private int _setItemID00;
        partial void OnSetItemID00Changed(int oldValue, int newValue)
        {
            if (!_isUpdatingSelectedItem && SelectedItem != null && oldValue != newValue)
            {
                SelectedItem["nSetItemID00"] = newValue;
            }

            SetFrameViewModelData(newValue, 1);
            UpdateSetOptions();
        }

        [ObservableProperty]
        private int _setItemID01;
        partial void OnSetItemID01Changed(int oldValue, int newValue)
        {
            if (!_isUpdatingSelectedItem && SelectedItem != null && oldValue != newValue)
            {
                SelectedItem["nSetItemID01"] = newValue;
            }

            SetFrameViewModelData(newValue, 2);
            UpdateSetOptions();
        }

        [ObservableProperty]
        private int _setItemID02;
        partial void OnSetItemID02Changed(int oldValue, int newValue)
        {
            if (!_isUpdatingSelectedItem && SelectedItem != null && oldValue != newValue)
            {
                SelectedItem["nSetItemID02"] = newValue;
            }

            SetFrameViewModelData(newValue, 3);
            UpdateSetOptions();
        }

        [ObservableProperty]
        private int _setItemID03;
        partial void OnSetItemID03Changed(int oldValue, int newValue)
        {
            if (!_isUpdatingSelectedItem && SelectedItem != null && oldValue != newValue)
            {
                SelectedItem["nSetItemID03"] = newValue;
            }

            SetFrameViewModelData(newValue, 4);
            UpdateSetOptions();
        }

        [ObservableProperty]
        private int _setItemID04;
        partial void OnSetItemID04Changed(int oldValue, int newValue)
        {
            if (!_isUpdatingSelectedItem && SelectedItem != null && oldValue != newValue)
            {
                SelectedItem["nSetItemID04"] = newValue;
            }

            SetFrameViewModelData(newValue, 5);
            UpdateSetOptions();
        }

        [ObservableProperty]
        private int _setItemID05;
        partial void OnSetItemID05Changed(int oldValue, int newValue)
        {
            if (!_isUpdatingSelectedItem && SelectedItem != null && oldValue != newValue)
            {
                SelectedItem["nSetItemID05"] = newValue;
            }

            SetFrameViewModelData(newValue, 6);
            UpdateSetOptions();
        }

        private void UpdateSetOptions()
        {
            int nonZeroCount = 0;

            if (SetItemID00 != 0) nonZeroCount++;
            if (SetItemID01 != 0) nonZeroCount++;
            if (SetItemID02 != 0) nonZeroCount++;
            if (SetItemID03 != 0) nonZeroCount++;
            if (SetItemID04 != 0) nonZeroCount++;
            if (SetItemID05 != 0) nonZeroCount++;

            IsSetOption00Enabled = nonZeroCount > 1;
            IsSetOption01Enabled = nonZeroCount > 2;
            IsSetOption02Enabled = nonZeroCount > 3;
            IsSetOption03Enabled = nonZeroCount > 4;
            IsSetOption04Enabled = nonZeroCount > 5;
        }


        [ObservableProperty]
        private int _setOption00;
        partial void OnSetOption00Changed(int oldValue, int newValue)
        {
            if (!_isUpdatingSelectedItem && SelectedItem != null && oldValue != newValue)
            {
                SelectedItem["nSetOption00"] = newValue;
            }
            if (newValue == 0)
            {
                SetOptionValue00 = 0;
            }
            FormatSetEffect();
        }

        [ObservableProperty]
        private int _setOption01;
        partial void OnSetOption01Changed(int oldValue, int newValue)
        {
            if (!_isUpdatingSelectedItem && SelectedItem != null && oldValue != newValue)
            {
                SelectedItem["nSetOption01"] = newValue;
            }
            if (newValue == 0)
            {
                SetOptionValue01 = 0;
            }
            FormatSetEffect();

        }

        [ObservableProperty]
        private int _setOption02;
        partial void OnSetOption02Changed(int oldValue, int newValue)
        {
            if (!_isUpdatingSelectedItem && SelectedItem != null && oldValue != newValue)
            {
                SelectedItem["nSetOption02"] = newValue;
            }
            if (newValue == 0)
            {
                SetOptionValue02 = 0;
            }
            FormatSetEffect();

        }

        [ObservableProperty]
        private int _setOption03;
        partial void OnSetOption03Changed(int oldValue, int newValue)
        {
            if (!_isUpdatingSelectedItem && SelectedItem != null && oldValue != newValue)
            {
                SelectedItem["nSetOption03"] = newValue;
            }
            if (newValue == 0)
            {
                SetOptionValue03 = 0;
            }
            FormatSetEffect();
        }

        [ObservableProperty]
        private int _setOption04;
        partial void OnSetOption04Changed(int oldValue, int newValue)
        {
            if (!_isUpdatingSelectedItem && SelectedItem != null && oldValue != newValue)
            {
                SelectedItem["nSetOption04"] = newValue;
            }
            if (newValue == 0)
            {
                SetOptionValue04 = 0;
            }
            FormatSetEffect();
        }

        [ObservableProperty]
        private int _setOptionValue00;
        partial void OnSetOptionValue00Changed(int oldValue, int newValue)
        {
            if (!_isUpdatingSelectedItem && SelectedItem != null && oldValue != newValue)
            {
                SelectedItem["nSetOptionvlue00"] = newValue;
            }
            FormatSetEffect();
        }

        [ObservableProperty]
        private int _setOptionValue01;
        partial void OnSetOptionValue01Changed(int oldValue, int newValue)
        {
            if (!_isUpdatingSelectedItem && SelectedItem != null && oldValue != newValue)
            {
                SelectedItem["nSetOptionvlue01"] = newValue;
            }
            FormatSetEffect();
        }

        [ObservableProperty]
        private int _setOptionValue02;
        partial void OnSetOptionValue02Changed(int oldValue, int newValue)
        {
            if (!_isUpdatingSelectedItem && SelectedItem != null && oldValue != newValue)
            {
                SelectedItem["nSetOptionvlue02"] = newValue;
            }
            FormatSetEffect();
        }

        [ObservableProperty]
        private int _setOptionValue03;
        partial void OnSetOptionValue03Changed(int oldValue, int newValue)
        {
            if (!_isUpdatingSelectedItem && SelectedItem != null && oldValue != newValue)
            {
                SelectedItem["nSetOptionvlue03"] = newValue;
            }
            FormatSetEffect();
        }

        [ObservableProperty]
        private int _setOptionValue04;
        partial void OnSetOptionValue04Changed(int oldValue, int newValue)
        {
            if (!_isUpdatingSelectedItem && SelectedItem != null && oldValue != newValue)
            {
                SelectedItem["nSetOptionvlue04"] = newValue;
            }
            FormatSetEffect();
        }

        [ObservableProperty]
        private bool _isEnabled = true;

        [ObservableProperty]
        private bool _isSetOption00Enabled = false;

        [ObservableProperty]
        private bool _isSetOption01Enabled = false;

        [ObservableProperty]
        private bool _isSetOption02Enabled = false;

        [ObservableProperty]
        private bool _isSetOption03Enabled = false;

        [ObservableProperty]
        private bool _isSetOption04Enabled = false;

        #endregion

        #endregion
    }
}
