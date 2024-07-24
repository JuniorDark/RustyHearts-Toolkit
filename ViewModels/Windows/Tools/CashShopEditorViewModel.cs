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
using System.Windows.Controls;
using static RHToolkit.Models.EnumService;

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

        public CashShopEditorViewModel(IWindowsService windowsService, ISqLiteDatabaseService sqLiteDatabaseService, ItemHelper itemHelper)
        {
            _token = Guid.NewGuid();
            _windowsService = windowsService;
            _sqLiteDatabaseService = sqLiteDatabaseService;
            _itemHelper = itemHelper;
            DataTableManager = new DataTableManager();
            _searchTimer = new()
            {
                Interval = 500,
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

        [RelayCommand]
        private void ResetSellingDate()
        {
            StartSellingDate = null;
            EndSellingDate = null;
        }

        [RelayCommand]
        private void ResetSaleDate()
        {
            SaleStartSellingDate = null;
            SaleEndSellingDate = null;
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
                    Filter = "cashshoplist.rh|cashshoplist.rh|All Files (*.*)|*.*",
                    FilterIndex = 1
                };

                if (openFileDialog.ShowDialog() == true)
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
                        if (DataTableManager != null)
                        {
                            DataTableManager.LoadFile(cashShopTable);
                            DataTableManager.CurrentFile = openFileDialog.FileName;
                            DataTableManager.CurrentFileName = Path.GetFileName(CurrentFile);
                        }

                        Title = $"Cash Shop Editor ({CurrentFileName})";

                        ApplyFileDataFilter();
                        OnCanExecuteFileCommandChanged();
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
                if (DataTableManager != null)
                {
                    Window? shopEditorWindow = Application.Current.Windows.OfType<CashShopEditorWindow>().FirstOrDefault();
                    Window owner = shopEditorWindow ?? Application.Current.MainWindow;
                    DataTableManager.OpenSearchDialog(owner, parameter, DataGridSelectionUnit.FullRow);
                }

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
            FrameViewModel = null;
            SelectedItem = null;
            Title = $"Cash Shop Editor";
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
            UpdateSelectedItemCommand.NotifyCanExecuteChanged();
        }

        private void OnCanExecuteFileCommandChanged()
        {
            CloseFileCommand.NotifyCanExecuteChanged();
            OpenSearchDialogCommand.NotifyCanExecuteChanged();
            AddItemCommand.NotifyCanExecuteChanged();
        }

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
                RHMessageBoxHelper.ShowOKMessage($"Error: {ex.Message}", "Error");
            }
        }

        [RelayCommand(CanExecute = nameof(CanExecuteSelectedItemCommand))]
        private void UpdateSelectedItem()
        {
            try
            {
                if (!_isUpdatingSelectedItem && SelectedItem != null)
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
                RHMessageBoxHelper.ShowOKMessage($"Error: {ex.Message}", "Error");
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
            if (DataTableManager != null)
            {
                DataTableManager.AddNewRow();
                SelectedItem = DataTableManager.SelectedItem;

                var frameViewModel = _itemHelper.GetItemData(itemData);
                FrameViewModel = frameViewModel;

                if (DataTableManager.DataTable != null && DataTableManager.DataTable.Rows.Count > 0)
                {
                    var maxId = DataTableManager.DataTable.AsEnumerable()
                                         .Max(row => row.Field<int>("nID"));
                    ShopID = maxId + 1;
                }
                else
                {
                    ShopID = 1;
                }
                ItemID = frameViewModel.ItemId;
                ItemAmount = frameViewModel.ItemAmount;
                ItemName = frameViewModel.ItemName;
                IconName = $"{frameViewModel.IconName}";
                ShopDescription = PaymentType == 0 ? $"{BonusRate}% of the price goes to Bonus" : "Purchased with Bonus";
                ShopCategory = GetShopCategory(frameViewModel.SubCategory);
                CostumeCategory = GetCostumeCategory(frameViewModel.SubCategory);
                Class = frameViewModel.JobClass;
                StartSellingDate = DateTime.Today;
                EndSellingDate = DateTime.Today.AddYears(10);
                SaleStartSellingDate = null;
                SaleEndSellingDate = null;
            }
        }

        private void UpdateItem(ItemData itemData)
        {
            var frameViewModel = _itemHelper.GetItemData(itemData);
            FrameViewModel = frameViewModel;

            if (DataTableManager != null && SelectedItem != null)
            {
                ItemID = frameViewModel.ItemId;
                ItemName = frameViewModel.ItemName;
                IconName = frameViewModel.IconName;
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

        #endregion

        #region Filter

        private void ApplyFileDataFilter()
        {
            if (DataTableManager != null && DataTableManager.DataTable != null)
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
            if (DataTableManager != null && DataTableManager.DataTable != null)
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
            if (DataTableManager != null && DataTableManager.DataTable != null)
            {
                ApplyFileDataFilter();
            }
        }

        [ObservableProperty]
        private int _itemStateFilter;
        partial void OnItemStateFilterChanged(int value)
        {
            if (DataTableManager != null && DataTableManager.DataTable != null)
            {
                ApplyFileDataFilter();
            }

        }

        [ObservableProperty]
        private int _shopCategoryFilter;
        partial void OnShopCategoryFilterChanged(int value)
        {
            if (DataTableManager != null && DataTableManager.DataTable != null)
            {
                PopulateCostumeCategoryItemsFilter(value);
                ApplyFileDataFilter();
            }
        }

        [ObservableProperty]
        private int _costumeCategoryFilter;
        partial void OnCostumeCategoryFilterChanged(int value)
        {
            if (DataTableManager != null && DataTableManager.DataTable != null)
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
        private Visibility _isSelectedItemVisible = Visibility.Hidden;

        [ObservableProperty]
        private DataTableManager? _dataTableManager;
        partial void OnDataTableManagerChanged(DataTableManager? value)
        {
            OnCanExecuteFileCommandChanged();
        }

        [ObservableProperty]
        private string? _currentFile;

        [ObservableProperty]
        private string? _currentFileName;

        #region SelectedItem

        [ObservableProperty]
        private FrameViewModel? _frameViewModel;

        private bool _isUpdatingSelectedItem = false;

        [ObservableProperty]
        private DataRowView? _selectedItem;
        partial void OnSelectedItemChanged(DataRowView? value)
        {
            _isUpdatingSelectedItem = true;

            if (value != null && DataTableManager != null)
            {
                DataTableManager.SelectedItem = value;

                ItemData itemData = new()
                {
                    ItemId = (int)value["nItemID"]
                };

                FrameViewModel = _itemHelper.GetItemData(itemData);

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

            _isUpdatingSelectedItem = false;

            OnCanExecuteSelectedItemCommandChanged();
        }

        [ObservableProperty]
        private string? _itemName;
        partial void OnItemNameChanged(string? oldValue, string? newValue)
        {
            if (!_isUpdatingSelectedItem && SelectedItem != null && oldValue != newValue)
            {
                SelectedItem["wszName"] = newValue;
            }
        }

        private void UpdateItemName()
        {
            var baseItemName = ItemName;
            var index = ItemName?.LastIndexOf(" (") ?? -1;
            if (index != -1)
            {
                baseItemName = ItemName?[..index];
            }
            ItemName = ItemAmount > 1 ? $"{baseItemName} ({ItemAmount})" : baseItemName;
        }

        [ObservableProperty]
        private string? _shopDescription;
        partial void OnShopDescriptionChanged(string? oldValue, string? newValue)
        {
            if (!_isUpdatingSelectedItem && SelectedItem != null && oldValue != newValue)
            {
                SelectedItem["wszDesc"] = newValue;
            }
        }

        [ObservableProperty]
        private bool _useShopIcon = true;

        partial void OnUseShopIconChanged(bool value)
        {
            if (value)
            {
                if (!IconName?.StartsWith("shop_") == true)
                {
                    IconName = $"shop_{IconName}";
                }
            }
            else
            {
                if (IconName?.StartsWith("shop_") == true)
                {
                    IconName = IconName.Substring(5);
                }
            }
        }

        [ObservableProperty]
        private string? _iconName;

        partial void OnIconNameChanged(string? oldValue, string? newValue)
        {
            if (!_isUpdatingSelectedItem && SelectedItem != null && oldValue != newValue)
            {
                var iconName = newValue;
                if (UseShopIcon == true && !newValue?.StartsWith("shop_") == true)
                {
                    iconName = $"shop_{newValue}";
                }
                else if (UseShopIcon == false && newValue?.StartsWith("shop_") == true)
                {
                    iconName = newValue.Substring(5);
                }
                IconName = iconName;
                SelectedItem["szShopBigIcon"] = iconName;
            }
        }

        [ObservableProperty]
        private int _itemID;
        partial void OnItemIDChanged(int oldValue, int newValue)
        {
            if (!_isUpdatingSelectedItem && SelectedItem != null && oldValue != newValue)
            {
                SelectedItem["nItemID"] = newValue;

                ItemData itemData = new()
                {
                    ItemId = newValue
                };

                FrameViewModel = _itemHelper.GetItemData(itemData);

                ItemName = FrameViewModel.ItemName;
                IconName = FrameViewModel.IconName;
                ShopCategory = GetShopCategory(FrameViewModel.SubCategory);
                CostumeCategory = GetCostumeCategory(FrameViewModel.SubCategory);
                Class = FrameViewModel.JobClass;
                ItemAmountMax = FrameViewModel.OverlapCnt;

            }
        }

        [ObservableProperty]
        private int _shopID;
        partial void OnShopIDChanged(int oldValue, int newValue)
        {
            if (!_isUpdatingSelectedItem && SelectedItem != null && oldValue != newValue)
            {
                SelectedItem["nID"] = newValue;
            }
        }

        [ObservableProperty]
        private int _itemAmount;

        partial void OnItemAmountChanged(int value)
        {
            if (!_isUpdatingSelectedItem && SelectedItem != null)
            {
                UpdateItemName();
                SelectedItem["nValue00"] = value;
            }
        }

        [ObservableProperty]
        private int _paymentType;
        partial void OnPaymentTypeChanged(int value)
        {
            if (!_isUpdatingSelectedItem && SelectedItem != null)
            {
                IsEnabled = value == 0 ? true : false;
                NoGift = value == 0 ? false : true;
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
            if (!_isUpdatingSelectedItem && SelectedItem != null && oldValue != newValue)
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
            if (!_isUpdatingSelectedItem && SelectedItem != null)
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
            if (!_isUpdatingSelectedItem && SelectedItem != null)
            {
                SelectedItem["nSaleCashCost00"] = newValue;
            }
        }

        [ObservableProperty]
        private int _cashMileage;
        partial void OnCashMileageChanged(int oldValue, int newValue)
        {
            if (!_isUpdatingSelectedItem && SelectedItem != null && oldValue != newValue)
            {
                SelectedItem["nCashMileage"] = newValue;
            }
        }

        [ObservableProperty]
        private int _itemState;
        partial void OnItemStateChanged(int oldValue, int newValue)
        {
            if (!_isUpdatingSelectedItem && SelectedItem != null && oldValue != newValue)
            {
                SelectedItem["nItemState"] = newValue;
            }
        }

        [ObservableProperty]
        private bool _isHidden;
        partial void OnIsHiddenChanged(bool oldValue, bool newValue)
        {
            if (!_isUpdatingSelectedItem && SelectedItem != null && oldValue != newValue)
            {
                SelectedItem["nHidden"] = newValue == true ? 1 : 0;
            }
        }

        [ObservableProperty]
        private bool _noGift;
        partial void OnNoGiftChanged(bool oldValue, bool newValue)
        {
            if (!_isUpdatingSelectedItem && SelectedItem != null && oldValue != newValue)
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
            PopulateCostumeCategoryItems(value);

            if (FrameViewModel != null)
            {
                ItemAmountMax = value switch
                {
                    0 => 525600,
                    1 => 0,
                    _ => FrameViewModel.OverlapCnt

                };
            }

            if (!_isUpdatingSelectedItem && SelectedItem != null)
            {
                SelectedItem["nCategory"] = value;
            }
        }

        [ObservableProperty]
        private int _costumeCategory;
        partial void OnCostumeCategoryChanged(int value)
        {
            if (!_isUpdatingSelectedItem && SelectedItem != null)
            {
                SelectedItem["nCostumeCategory"] = value;
            }
        }

        [ObservableProperty]
        private string _startSellingDateValue = "No Selling Date";

        [ObservableProperty]
        private DateTime? _startSellingDate;

        partial void OnStartSellingDateChanged(DateTime? value)
        {
            StartSellingDateValue = value.HasValue ? value.Value.ToString("yyyy/MM/dd") : "No Selling Date";
            if (!_isUpdatingSelectedItem && SelectedItem != null)
            {
                SelectedItem["nStartSellingDate"] = value == null ? 0 : DateTimeFormatter.ConvertDateToInt((DateTime)value);
            }
        }

        [ObservableProperty]
        private string _endSellingDateValue = "No Selling Date";

        [ObservableProperty]
        private DateTime? _endSellingDate;
        partial void OnEndSellingDateChanged(DateTime? value)
        {
            EndSellingDateValue = value.HasValue ? value.Value.ToString("yyyy/MM/dd") : "No Selling Date";

            if (!_isUpdatingSelectedItem && SelectedItem != null)
            {
                SelectedItem["nEndSellingDate"] = value == null ? 0 : DateTimeFormatter.ConvertDateToInt((DateTime)value);
            }
        }

        [ObservableProperty]
        private string _saleStartSellingDateValue = "No Sale Date";

        [ObservableProperty]
        private DateTime? _saleStartSellingDate;
        partial void OnSaleStartSellingDateChanged(DateTime? value)
        {
            SaleStartSellingDateValue = value.HasValue ? value.Value.ToString("yyyy/MM/dd") : "No Sale Date";

            if (!_isUpdatingSelectedItem && SelectedItem != null)
            {
                SelectedItem["nSaleStartSellingDate"] = value == null ? 0 : DateTimeFormatter.ConvertDateToInt((DateTime)value);
            }
        }

        [ObservableProperty]
        private string _saleEndSellingDateValue = "No Sale Date";

        [ObservableProperty]
        private DateTime? _saleEndSellingDate;
        partial void OnSaleEndSellingDateChanged(DateTime? value)
        {
            SaleEndSellingDateValue = value.HasValue ? value.Value.ToString("yyyy/MM/dd") : "No Sale Date";

            if (!_isUpdatingSelectedItem && SelectedItem != null)
            {
                SelectedItem["nSaleEndSellingDate"] = value == null ? 0 : DateTimeFormatter.ConvertDateToInt((DateTime)value);
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

        #endregion
    }
}
