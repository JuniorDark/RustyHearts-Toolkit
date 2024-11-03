using Microsoft.Win32;
using RHToolkit.Messages;
using RHToolkit.Models;
using RHToolkit.Models.Database;
using RHToolkit.Models.Editor;
using RHToolkit.Models.MessageBox;
using RHToolkit.Services;
using RHToolkit.Views.Windows;
using System.ComponentModel;
using System.Data;
using System.Windows.Controls;

namespace RHToolkit.ViewModels.Windows
{
    public partial class ItemEditorViewModel : ObservableObject, IRecipient<DataRowViewMessage>
    {
        private readonly Guid _token;
        private readonly IWindowsService _windowsService;
        private readonly IGMDatabaseService _gmDatabaseService;
        private readonly System.Timers.Timer _filterUpdateTimer;
        private readonly System.Timers.Timer _addEffectFilterUpdateTimer;

        public ItemEditorViewModel(IWindowsService windowsService, IGMDatabaseService gmDatabaseService, ItemDataManager itemDataManager)
        {
            _token = Guid.NewGuid();
            _windowsService = windowsService;
            _gmDatabaseService = gmDatabaseService;
            _itemDataManager = itemDataManager;

            DataTableManager = new()
            {
                Token = _token
            };

            _filterUpdateTimer = new()
            {
                Interval = 500,
                AutoReset = false
            };
            _filterUpdateTimer.Elapsed += FilterUpdateTimerElapsed;
            _addEffectFilterUpdateTimer = new()
            {
                Interval = 500,
                AutoReset = false
            };
            _addEffectFilterUpdateTimer.Elapsed += AddEffectFilterUpdateTimerElapsed;
            PopulateListItems();
            WeakReferenceMessenger.Default.Register(this);

            _addEffectView = CollectionViewSource.GetDefaultView(_itemDataManager.AddEffectItems);
            _addEffectView.Filter = FilterAddEffect;
        }

        #region Commands 

        #region File

        [RelayCommand]
        private async Task LoadFile(string? parameter)
        {
            try
            {
                await CloseFile();

                if (int.TryParse(parameter, out int itemType))
                {
                    string? fileName = GetFileName(itemType);
                    string? stringFileName = GetStringFileName(itemType);
                    if (fileName == null) return;
                    ItemType = itemType;
                    bool isLoaded = await DataTableManager.LoadFileFromPath(fileName, stringFileName, "nItemTrade", "Item");

                    if (isLoaded)
                    {
                        IsLoaded();
                    }
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

                string filter = "Item List Files|" +
                                "itemlist.rh;itemlist_costume.rh;itemlist_armor.rh;itemlist_weapon.rh|" +
                                "All Files (*.*)|*.*";

                OpenFileDialog openFileDialog = new()
                {
                    Filter = filter,
                    FilterIndex = 1
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    string fileName = Path.GetFileName(openFileDialog.FileName);
                    int fileType = GetFileTypeFromFileName(fileName);

                    if (fileType == -1)
                    {
                        string message = string.Format(Resources.InvalidTableFileDesc, fileName, "Item");
                        RHMessageBoxHelper.ShowOKMessage(message, Resources.Error);
                        return;
                    }

                    string? stringFileName = GetStringFileName(fileType);
                    ItemType = fileType;

                    bool isLoaded = await DataTableManager.LoadFileAs(openFileDialog.FileName, stringFileName, "nItemTrade", "Item");

                    if (isLoaded)
                    {
                        IsLoaded();
                    }
                }
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
            }
        }

        private static string? GetFileName(int itemType)
        {
            return itemType switch
            {
                1 => "itemlist.rh",
                2 => "itemlist_costume.rh",
                3 => "itemlist_armor.rh",
                4 => "itemlist_weapon.rh",
                _ => throw new ArgumentOutOfRangeException(nameof(itemType)),
            };
        }

        private static string? GetStringFileName(int itemType)
        {
            return itemType switch
            {
                1 => "itemlist_string.rh",
                2 => "itemlist_costume_string.rh",
                3 => "itemlist_armor_string.rh",
                4 => "itemlist_weapon_string.rh",
                _ => throw new ArgumentOutOfRangeException(nameof(itemType)),
            };
        }

        private static int GetFileTypeFromFileName(string fileName)
        {
            return fileName switch
            {
                "itemlist.rh" => 1,
                "itemlist_costume.rh" => 2,
                "itemlist_armor.rh" => 3,
                "itemlist_weapon.rh" => 4,
                _ => -1,
            };
        }

        private void IsLoaded()
        {
            Title = string.Format(Resources.EditorTitleFileName, "Item", DataTableManager.CurrentFileName);
            OpenMessage = "";
            IsVisible = Visibility.Visible;
            OnCanExecuteFileCommandChanged();
        }

        [RelayCommand(CanExecute = nameof(CanExecuteFileCommand))]
        private void OpenSearchDialog(string? parameter)
        {
            try
            {
                Window? window = Application.Current.Windows.OfType<ItemEditorWindow>().FirstOrDefault();
                Window owner = window ?? Application.Current.MainWindow;
                DataTableManager.OpenSearchDialog(owner, parameter, DataGridSelectionUnit.FullRow);

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
            Title = string.Format(Resources.EditorTitle, "Item");
            OpenMessage = Resources.OpenFile;
            SearchText = string.Empty;
            IsVisible = Visibility.Hidden;
            ItemType = 0;
            OnCanExecuteFileCommandChanged();
        }

        private bool CanExecuteFileCommand()
        {
            return DataTableManager.DataTable != null;
        }

        private bool CanExecuteSelectedItemCommand()
        {
            return DataTableManager.SelectedItem != null;
        }

        private void OnCanExecuteFileCommandChanged()
        {
            CloseFileCommand.NotifyCanExecuteChanged();
            OpenSearchDialogCommand.NotifyCanExecuteChanged();
            AddRowCommand.NotifyCanExecuteChanged();
        }

        #endregion

        #region Add Row

        [RelayCommand(CanExecute = nameof(CanExecuteFileCommand))]
        private void AddRow()
        {
            try
            {
                DataTableManager.StartGroupingEdits();
                DataTableManager.AddNewRow();
                if (DataTableManager.SelectedItem != null)
                {
                    DataTableManager.SelectedItem["wszDesc"] = "New Item";
                }

                if (DataTableManager.SelectedItemString != null)    
                {
                    DataTableManager.SelectedItemString["wszDesc"] = "New Item";
                    DataTableManager.SelectedItemString["wszItemDescription"] = "New Item";
                }
                DataTableManager.EndGroupingEdits();
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
            }
        }
        #endregion

        #region DataRowViewMessage
        public void Receive(DataRowViewMessage message)
        {
            if (message.Token == _token)
            {
                var selectedItem = message.Value;

                UpdateSelectedItem(selectedItem);
            }
        }

        private void UpdateSelectedItem(DataRowView? selectedItem)
        {
            _isUpdatingSelectedItem = true;

            if (selectedItem != null)
            {
                ItemDescription = ItemType == 2 ? (string)selectedItem["szItemDescription"] : (string)selectedItem["wszItemDescription"];
                UseableValue = selectedItem.Row.Table.Columns.Contains("fUseableValue")
                        ? (float)selectedItem["fUseableValue"] : 0;

                ItemData itemData = new()
                {
                    ItemName = DataTableManager.SelectedItemString != null ? (string)DataTableManager.SelectedItemString["wszDesc"] : "",
                    Description = DataTableManager.SelectedItemString != null ? (string)DataTableManager.SelectedItemString["wszItemDescription"] : "",
                    Type = ItemType,
                    WeaponID00 = (int)selectedItem["nWeaponID00"],
                    Category = (int)selectedItem["nCategory"],
                    SubCategory = (int)selectedItem["nSubCategory"],
                    LevelLimit = (int)selectedItem["nLevelLimit"],
                    ItemTrade = (int)selectedItem["nItemTrade"],
                    Durability = (int)selectedItem["nDurability"],
                    Weight = (int)selectedItem["nWeight"],
                    Reconstruction = (int)selectedItem["nReconstructionMax"],
                    ReconstructionMax = (byte)(int)selectedItem["nReconstructionMax"],
                    BindingOff = (int)selectedItem["nBindingOff"],
                    Defense = (int)selectedItem["nDefense"],
                    MagicDefense = (int)selectedItem["nMagicDefense"],
                    Branch = (int)selectedItem["nBranch"],
                    SellPrice = (int)selectedItem["nSellPrice"],
                    PetFood = (int)selectedItem["nPetEatGroup"],
                    JobClass = (int)selectedItem["nJobClass"],
                    SetId = (int)selectedItem["nSetId"],
                    TitleList = (int)selectedItem["nTitleList"],
                    Cooltime = (float)selectedItem["fCooltime"],
                    FixOption1Code = (int)selectedItem["nFixOption00"],
                    FixOption1Value = (int)selectedItem["nFixOptionValue00"],
                    FixOption2Code = (int)selectedItem["nFixOption01"],
                    FixOption2Value = (int)selectedItem["nFixOptionValue01"]
                };

                ItemDataManager.ItemDataViewModel.UpdateItemData(itemData);
                IsSelectedItemVisible = Visibility.Visible;
            }
            else
            {
                IsSelectedItemVisible = Visibility.Hidden;
            }

            _isUpdatingSelectedItem = false;
        }

        #endregion

        #endregion

        #region Filter

        private void ApplyFilter()
        {
            List<string> filterParts = [];

            List<string> columns = [];

            columns.Add("CONVERT(nID, 'System.String')");
            columns.Add("CONVERT(nCategory, 'System.String')");
            columns.Add("CONVERT(nSubCategory, 'System.String')");
            columns.Add("CONVERT(nBranch, 'System.String')");
            columns.Add("CONVERT(nInventoryType, 'System.String')");
            columns.Add("CONVERT(nUnionPackageID, 'System.String')");
            columns.Add("CONVERT(nJobClass, 'System.String')");
            columns.Add("CONVERT(nTitleList, 'System.String')");
            columns.Add("CONVERT(nLevelLimit, 'System.String')");
            columns.Add("CONVERT(nItemTrade, 'System.String')");
            columns.Add("CONVERT(nRuneGroup, 'System.String')");
            columns.Add("CONVERT(nSetId, 'System.String')");
            columns.Add("CONVERT(nFixOption00, 'System.String')");
            columns.Add("CONVERT(nFixOption01, 'System.String')");
            columns.Add("szIconName");

            if (ItemCategoryFilter != 0)
            {
                filterParts.Add($"nCategory = {ItemCategoryFilter}");
            }

            if (ItemSubCategoryFilter != 0)
            {
                filterParts.Add($"nSubCategory = {ItemSubCategoryFilter}");
            }

            if (ItemClassFilter != 0)
            {
                filterParts.Add($"nJobClass = {ItemClassFilter}");
            }

            if (ItemBranchFilter != 0)
            {
                filterParts.Add($"nBranch = {ItemBranchFilter}");
            }

            if (ItemTradeFilter != -1)
            {
                filterParts.Add($"nItemTrade = {ItemTradeFilter}");
            }

            if (InventoryTypeFilter != 0)
            {
                filterParts.Add($"nInventoryType = {InventoryTypeFilter}");
            }

            if (columns.Count > 0)
            {
                DataTableManager.ApplyFileDataFilter(filterParts, [.. columns], SearchText, MatchCase);
            }
        }

        private void TriggerFilterUpdate()
        {
            _filterUpdateTimer.Stop();
            _filterUpdateTimer.Start();
        }

        private void FilterUpdateTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                _filterUpdateTimer.Stop();
                ApplyFilter();
            });
        }

        [ObservableProperty]
        private string? _searchText;
        partial void OnSearchTextChanged(string? value)
        {
            TriggerFilterUpdate();
        }

        [ObservableProperty]
        private bool _matchCase = false;
        partial void OnMatchCaseChanged(bool value)
        {
            ApplyFilter();
        }

        [ObservableProperty]
        private int _itemCategoryFilter;
        partial void OnItemCategoryFilterChanged(int value)
        {
            ApplyFilter();
        }

        [ObservableProperty]
        private int _itemSubCategoryFilter;
        partial void OnItemSubCategoryFilterChanged(int value)
        {
            ApplyFilter();
        }

        [ObservableProperty]
        private int _itemTradeFilter = -1;
        partial void OnItemTradeFilterChanged(int value)
        {
            ApplyFilter();
        }

        [ObservableProperty]
        private int _itemClassFilter;

        partial void OnItemClassFilterChanged(int value)
        {
            ApplyFilter();
        }

        [ObservableProperty]
        private int _itemBranchFilter;

        partial void OnItemBranchFilterChanged(int value)
        {
            ApplyFilter();
        }

        [ObservableProperty]
        private int _inventoryTypeFilter;

        partial void OnInventoryTypeFilterChanged(int value)
        {
            ApplyFilter();
        }

        #region AddEffect Filter
        [ObservableProperty]
        private ICollectionView _addEffectView;

        private readonly List<int> selectedAddEffect = [];
        private bool FilterAddEffect(object obj)
        {
            if (obj is NameID addEffect)
            {
                if (addEffect.ID == 0)
                    return true;

                if (DataTableManager.SelectedItem != null)
                {
                    var selectedEffect01 = (int)DataTableManager.SelectedItem["nAddEffectID"];
                    var selectedEffect02 = (int)DataTableManager.SelectedItem["nAddEffectID01"];

                    selectedAddEffect.Add(selectedEffect01);
                    selectedAddEffect.Add(selectedEffect02);

                    if (selectedAddEffect.Contains(addEffect.ID))
                        return true;
                }

                // text search filter
                if (!string.IsNullOrEmpty(AddEffectSearch))
                {
                    string searchText = AddEffectSearch.ToLower();

                    // Check if either option ID or option Name contains the search text
                    if (!string.IsNullOrEmpty(addEffect.ID.ToString()) && addEffect.ID.ToString().Contains(searchText, StringComparison.CurrentCultureIgnoreCase))
                        return true;

                    if (!string.IsNullOrEmpty(addEffect.Name) && addEffect.Name.Contains(searchText, StringComparison.CurrentCultureIgnoreCase))
                        return true;

                    return false;
                }

                return true;
            }
            return false;
        }

        [ObservableProperty]
        private string? _addEffectSearch;
        partial void OnAddEffectSearchChanged(string? value)
        {
            _addEffectFilterUpdateTimer.Stop();
            _addEffectFilterUpdateTimer.Start();
        }

        private void AddEffectFilterUpdateTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(AddEffectView.Refresh);
        }
        #endregion
        #endregion

        #region Comboboxes

        [ObservableProperty]
        private List<NameID>? _auctionCategoryItems;

        [ObservableProperty]
        private List<NameID>? _fielMeshItems;

        [ObservableProperty]
        private List<NameID>? _unionPackageItems;

        [ObservableProperty]
        private List<NameID>? _costumePackItems;

        [ObservableProperty]
        private List<NameID>? _titleListItems;

        [ObservableProperty]
        private List<NameID>? _setItemItems;

        [ObservableProperty]
        private List<NameID>? _petEatItems;

        [ObservableProperty]
        private List<NameID>? _riddleGroupItems;

        [ObservableProperty]
        private List<NameID>? _runeBranchItems;

        private void PopulateListItems()
        {
            try
            {
                AuctionCategoryItems = _gmDatabaseService.GetAuctionCategoryItems();
                FielMeshItems = _gmDatabaseService.GetFielMeshItems();
                UnionPackageItems = _gmDatabaseService.GetUnionPackageItems();
                CostumePackItems = _gmDatabaseService.GetCostumePackItems();
                TitleListItems = _gmDatabaseService.GetTitleListItems();
                SetItemItems = _gmDatabaseService.GetSetItemItems();
                PetEatItems = _gmDatabaseService.GetPetEatItems();
                RiddleGroupItems = _gmDatabaseService.GetRiddleGroupItems();
                RuneBranchItems =
                [
                    new NameID { ID = 0, Name = "None" },
                    new NameID { ID = 1, Name = "★" },
                    new NameID { ID = 2, Name = "★★" },
                    new NameID { ID = 3, Name = "★★★" },
                    new NameID { ID = 4, Name = "★★★★" },
                    new NameID { ID = 5, Name = "★★★★★" },
                    new NameID { ID = 6, Name = "★★★★★★" },
                    new NameID { ID = 7, Name = "★★★★★★★" }
                ];
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
            }
            
        }
        #endregion

        #region Properties
        [ObservableProperty]
        private string _title = string.Format(Resources.EditorTitle, "Item");

        [ObservableProperty]
        private string? _openMessage = Resources.OpenFile;

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

        #region SelectedItem

        [ObservableProperty]
        private string? _itemDescription;
        partial void OnItemDescriptionChanged(string? value)
        {
            UpdateSelectedItemValue(value, ItemType == 2 ? "szItemDescription" : "wszItemDescription");
        }

        [ObservableProperty]
        private double _useableValue;
        partial void OnUseableValueChanged(double value)
        {
            if (DataTableManager.SelectedItem != null && DataTableManager.SelectedItem.Row.Table.Columns.Contains("fUseableValue"))
            {
                UpdateSelectedItemValue(value, "fUseableValue");
            }
        }

        [ObservableProperty]
        private int _itemType;

        #endregion

        #endregion

        #region Properties Helper

        private bool _isUpdatingSelectedItem = false;

        private void UpdateSelectedItemValue(object? newValue, string column)
        {
            if (_isUpdatingSelectedItem)
                return;

            DataTableManager.UpdateSelectedItemValue(newValue, column);
        }

        #endregion
    }
}
