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
    public partial class CashShopEditorViewModel : ObservableObject, IRecipient<ItemDataMessage>, IRecipient<DataRowViewMessage>
    {
        private readonly Guid _token;
        private readonly IWindowsService _windowsService;
        private readonly System.Timers.Timer _filterUpdateTimer;

        public CashShopEditorViewModel(IWindowsService windowsService, ItemDataManager itemDataManager)
        {
            _token = Guid.NewGuid();
            _windowsService = windowsService;
            _itemDataManager = itemDataManager;
            DataTableManager = new()
            {
                Token = _token
            };
            _filterUpdateTimer = new()
            {
                Interval = 300,
                AutoReset = false
            };
            _filterUpdateTimer.Elapsed += FilterUpdateTimerElapsed;

            PopulateClassItems();
            PopulatePaymentTypeItems();
            PopulateShopCategoryItems();
            PopulateCostumeCategoryItems(0);
            PopulateCostumeCategoryItemsFilter(-1);
            PopulateItemStateItems();
            
            WeakReferenceMessenger.Default.Register<ItemDataMessage>(this);
            WeakReferenceMessenger.Default.Register<DataRowViewMessage>(this);
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

                string filter = "cashshoplist.rh|cashshoplist.rh|All Files (*.*)|*.*";
                bool isLoaded = await DataTableManager.LoadFile(filter, null, "nCashCost00", "Cash Shop");

                if (isLoaded)
                {
                    Title = $"Cash Shop Editor ({DataTableManager.CurrentFileName})";
                    OpenMessage = "";
                    ApplyFilter();
                    OnCanExecuteFileCommandChanged();
                    IsVisible = Visibility.Visible;
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
            FrameViewModel = null;
            Title = $"Cash Shop Editor";
            OpenMessage = "Open a file";
            IsVisible = Visibility.Hidden;
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
                if (DataTableManager.SelectedItem != null)
                {
                    var itemData = new ItemData
                    {
                        ItemId = (int)DataTableManager.SelectedItem["nItemID"]
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
            DataTableManager.AddNewRow();

            var frameViewModel = ItemDataManager.GetItemData(itemData);
            FrameViewModel = frameViewModel;

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

        private void UpdateItem(ItemData itemData)
        {
            var frameViewModel = ItemDataManager.GetItemData(itemData);
            FrameViewModel = frameViewModel;

            if (DataTableManager.SelectedItem != null)
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

        #region DataRowView
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
                ItemData itemData = new()
                {
                    ItemId = (int)selectedItem["nItemID"]
                };

                FrameViewModel = ItemDataManager.GetItemData(itemData);
                ItemAmountMax = (int)selectedItem["nCategory"] switch
                {
                    0 => 525600,
                    1 => 0,
                    _ => FrameViewModel.OverlapCnt

                };
                IsSelectedItemVisible = Visibility.Visible;

                ShopID = (int)selectedItem["nID"];
                ItemID = (int)selectedItem["nItemID"];
                Class = (int)selectedItem["nJob"];
                PaymentType = (int)selectedItem["nPaymentType"];
                CashCost = (int)selectedItem["nCashCost00"];
                CashMileage = (int)selectedItem["nCashMileage"];
                ItemName = (string)selectedItem["wszName"];
                IconName = (string)selectedItem["szShopBigIcon"];
                ShopDescription = (string)selectedItem["wszDesc"];
                ShopCategory = (int)selectedItem["nCategory"];
                CostumeCategory = (int)selectedItem["nCostumeCategory"];
                ItemAmount = (int)selectedItem["nValue00"];
                ItemState = (int)selectedItem["nItemState"];
                IsHidden = (int)selectedItem["nHidden"] == 1;
                NoGift = (int)selectedItem["nNoGift"] == 1;
                StartSellingDate = (int)selectedItem["nStartSellingDate"] == 0 ? null : DateTimeFormatter.ConvertIntToDate((int)selectedItem["nStartSellingDate"]);
                EndSellingDate = (int)selectedItem["nEndSellingDate"] == 0 ? null : DateTimeFormatter.ConvertIntToDate((int)selectedItem["nEndSellingDate"]);
                SaleStartSellingDate = (int)selectedItem["nSaleStartSellingDate"] == 0 ? null : DateTimeFormatter.ConvertIntToDate((int)selectedItem["nSaleStartSellingDate"]);
                SaleEndSellingDate = (int)selectedItem["nSaleEndSellingDate"] == 0 ? null : DateTimeFormatter.ConvertIntToDate((int)selectedItem["nSaleEndSellingDate"]);
            }
            else
            {
                IsSelectedItemVisible = Visibility.Hidden;
            }

            _isUpdatingSelectedItem = false;

            OnCanExecuteSelectedItemCommandChanged();
        }
        #endregion

        #region Filter

        private void ApplyFilter()
        {
            if (DataTableManager.DataTable != null)
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

                DataTableManager.FilterText = filter;
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
        private int _classFilter;

        partial void OnClassFilterChanged(int value)
        {
            TriggerFilterUpdate();
        }

        [ObservableProperty]
        private int _itemStateFilter;
        partial void OnItemStateFilterChanged(int value)
        {
            TriggerFilterUpdate();
        }

        [ObservableProperty]
        private int _shopCategoryFilter;
        partial void OnShopCategoryFilterChanged(int value)
        {
            PopulateCostumeCategoryItemsFilter(value);
            TriggerFilterUpdate();
        }

        [ObservableProperty]
        private int _costumeCategoryFilter;
        partial void OnCostumeCategoryFilterChanged(int value)
        {
            TriggerFilterUpdate();
        }
        #endregion

        #region Comboboxes

        [ObservableProperty]
        private List<NameID>? _paymentTypeItems;

        private void PopulatePaymentTypeItems()
        {
            try
            {
                PaymentTypeItems = GetEnumItems<CashShopPaymentType>(false);

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
                var classItems = GetEnumItems<CharClass>(true);
                ClassItems = classItems;

                if (classItems.Count > 0)
                {
                    Class = 0;
                    ClassFilter = 0;
                }
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
            }
        }

        [ObservableProperty]
        private List<NameID>? _shopCategoryItems;
        [ObservableProperty]
        private List<NameID>? _shopCategoryItemsFilter;

        private void PopulateShopCategoryItems()
        {
            try
            {
                ShopCategoryItems = GetEnumItems<CashShopCategory>(false);

                if (ShopCategoryItems.Count > 0)
                {
                    ShopCategory = 0;
                }

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
        private List<NameID>? _itemStateItems;

        private void PopulateItemStateItems()
        {
            try
            {
                ItemStateItems = GetEnumItems<CashShopItemState>(false);

                if (ItemStateItems.Count > 0)
                {
                    ItemState = 0;
                    ItemStateFilter = 0;
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
        private string _title = $"Cash Shop Editor";

        [ObservableProperty]
        private string? _openMessage = "Open a file";

        [ObservableProperty]
        private Visibility _isSelectedItemVisible = Visibility.Hidden;

        [ObservableProperty]
        private Visibility _isVisible = Visibility.Hidden;

        [ObservableProperty]
        private ItemDataManager _itemDataManager;

        [ObservableProperty]
        private DataTableManager _dataTableManager;
        partial void OnDataTableManagerChanged(DataTableManager value)
        {
            OnCanExecuteFileCommandChanged();
        }

        #region SelectedItem

        [ObservableProperty]
        private FrameViewModel? _frameViewModel;

        private bool _isUpdatingSelectedItem = false;

        [ObservableProperty]
        private string? _itemName;
        partial void OnItemNameChanged(string? oldValue, string? newValue)
        {
            if (!_isUpdatingSelectedItem && DataTableManager.SelectedItem != null && oldValue != newValue)
            {
                DataTableManager.SelectedItem["wszName"] = newValue;
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
            if (!_isUpdatingSelectedItem && DataTableManager.SelectedItem != null && oldValue != newValue)
            {
                DataTableManager.SelectedItem["wszDesc"] = newValue;
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
            if (!_isUpdatingSelectedItem && DataTableManager.SelectedItem != null && oldValue != newValue)
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
                DataTableManager.SelectedItem["szShopBigIcon"] = iconName;
            }
        }

        [ObservableProperty]
        private int _itemID;
        partial void OnItemIDChanged(int oldValue, int newValue)
        {
            if (!_isUpdatingSelectedItem && DataTableManager.SelectedItem != null && oldValue != newValue)
            {
                DataTableManager.SelectedItem["nItemID"] = newValue;

                ItemData itemData = new()
                {
                    ItemId = newValue
                };

                FrameViewModel = ItemDataManager.GetItemData(itemData);

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
            if (!_isUpdatingSelectedItem && DataTableManager.SelectedItem != null && oldValue != newValue)
            {
                DataTableManager.SelectedItem["nID"] = newValue;
            }
        }

        [ObservableProperty]
        private int _itemAmount;

        partial void OnItemAmountChanged(int value)
        {
            if (!_isUpdatingSelectedItem && DataTableManager.SelectedItem != null)
            {
                UpdateItemName();
                DataTableManager.SelectedItem["nValue00"] = value;
            }
        }

        [ObservableProperty]
        private int _paymentType;
        partial void OnPaymentTypeChanged(int value)
        {
            if (!_isUpdatingSelectedItem && DataTableManager.SelectedItem != null)
            {
                IsEnabled = value == 0 ? true : false;
                NoGift = value == 0 ? false : true;
                DataTableManager.SelectedItem["nPaymentType"] = value;
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
            if (!_isUpdatingSelectedItem && DataTableManager.SelectedItem != null && oldValue != newValue)
            {
                DataTableManager.SelectedItem["nJob"] = newValue;
                DataTableManager.SelectedItem["szJob"] = newValue;
            }
        }

        [ObservableProperty]
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
        private int _cashCost;
        partial void OnCashCostChanged(int oldValue, int newValue)
        {
            if (!_isUpdatingSelectedItem && DataTableManager.SelectedItem != null)
            {
                if (BonusRate != 0 && PaymentType == 0)
                {
                    CashMileage = newValue * BonusRate / 100;
                }
                else
                {
                    CashMileage = 0;
                }

                DataTableManager.SelectedItem["nCashCost00"] = newValue;
            }
        }

        [ObservableProperty]
        private int _saleCashCost;
        partial void OnSaleCashCostChanged(int oldValue, int newValue)
        {
            if (!_isUpdatingSelectedItem && DataTableManager.SelectedItem != null)
            {
                DataTableManager.SelectedItem["nSaleCashCost00"] = newValue;
            }
        }

        [ObservableProperty]
        private int _cashMileage;
        partial void OnCashMileageChanged(int oldValue, int newValue)
        {
            if (!_isUpdatingSelectedItem && DataTableManager.SelectedItem != null && oldValue != newValue)
            {
                DataTableManager.SelectedItem["nCashMileage"] = newValue;
            }
        }

        [ObservableProperty]
        private int _itemState;
        partial void OnItemStateChanged(int oldValue, int newValue)
        {
            if (!_isUpdatingSelectedItem && DataTableManager.SelectedItem != null && oldValue != newValue)
            {
                DataTableManager.SelectedItem["nItemState"] = newValue;
            }
        }

        [ObservableProperty]
        private bool _isHidden;
        partial void OnIsHiddenChanged(bool oldValue, bool newValue)
        {
            if (!_isUpdatingSelectedItem && DataTableManager.SelectedItem != null && oldValue != newValue)
            {
                DataTableManager.SelectedItem["nHidden"] = newValue == true ? 1 : 0;
            }
        }

        [ObservableProperty]
        private bool _noGift;
        partial void OnNoGiftChanged(bool oldValue, bool newValue)
        {
            if (!_isUpdatingSelectedItem && DataTableManager.SelectedItem != null && oldValue != newValue)
            {
                DataTableManager.SelectedItem["nNoGift"] = newValue == true ? 1 : 0;
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

            if (!_isUpdatingSelectedItem && DataTableManager.SelectedItem != null)
            {
                DataTableManager.SelectedItem["nCategory"] = value;
            }
        }

        [ObservableProperty]
        private int _costumeCategory;
        partial void OnCostumeCategoryChanged(int value)
        {
            if (!_isUpdatingSelectedItem && DataTableManager.SelectedItem != null)
            {
                DataTableManager.SelectedItem["nCostumeCategory"] = value;
            }
        }

        [ObservableProperty]
        private DateTime? _startSellingDate;
        partial void OnStartSellingDateChanged(DateTime? value) => OnDateChanged(value, "StartSellingDate");

        [ObservableProperty]
        private string _startSellingDateValue = "No Selling Date";

        [ObservableProperty]
        private DateTime? _endSellingDate;
        partial void OnEndSellingDateChanged(DateTime? value) => OnDateChanged(value, "EndSellingDate");

        [ObservableProperty]
        private string _endSellingDateValue = "No Selling Date";

        [ObservableProperty]
        private DateTime? _saleStartSellingDate;
        partial void OnSaleStartSellingDateChanged(DateTime? value) => OnDateChanged(value, "SaleStartSellingDate");

        [ObservableProperty]
        private string _saleStartSellingDateValue = "No Sale Date";

        [ObservableProperty]
        private DateTime? _saleEndSellingDate;
        partial void OnSaleEndSellingDateChanged(DateTime? value) => OnDateChanged(value, "SaleEndSellingDate");

        [ObservableProperty]
        private string _saleEndSellingDateValue = "No Sale Date";

        private void OnDateChanged(DateTime? value, string dateType)
        {
            string dateValueProperty = $"{dateType}Value";
            string dataTableKey = $"n{dateType}";

            var formattedDate = value.HasValue ? value.Value.ToString("yyyy/MM/dd") : "No Date Set";

            GetType().GetProperty(dateValueProperty)?.SetValue(this, formattedDate);

            if (!_isUpdatingSelectedItem && DataTableManager.SelectedItem != null)
            {
                int dateIntValue = value == null ? 0 : DateTimeFormatter.ConvertDateToInt((DateTime)value);
                DataTableManager.SelectedItem[dataTableKey] = dateIntValue;
            }
        }


        #endregion

        #endregion
    }
}
