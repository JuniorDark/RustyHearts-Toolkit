using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using RHGMTool.Messages;
using RHGMTool.Models;
using RHGMTool.Services;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using static RHGMTool.Models.EnumService;

namespace RHGMTool.ViewModels
{
    public partial class FrameViewModel : ObservableObject, IRecipient<MailItemData>
    {
        private readonly ISqLiteDatabaseService _databaseService;
        private readonly GMDatabaseService _gmDatabaseService;
        private readonly FrameService _frameService;
        private readonly System.Timers.Timer searchTimer;

        public FrameViewModel()
        {
            
            _databaseService = new SqLiteDatabaseService();
            _gmDatabaseService = new GMDatabaseService(_databaseService);
            _frameService = new FrameService();
            searchTimer = new()
            {
                Interval = 500,
                AutoReset = false
            };
            searchTimer.Elapsed += SearchTimerElapsed;

            PopulateItemDataItems();
            PopulateOptionItems();
            PopulateSocketColorItems();
            PopulateItemTypeItemsFilter();
            PopulateCategoryItemsFilter(ItemType.Item);
            PopulateClassItemsFilter();
            PopulateBranchItemsFilter();
            PopulateItemTradeItemsFilter();
            
            _itemDataView = CollectionViewSource.GetDefaultView(ItemDataItems);
            _itemDataView.Filter = FilterItems;
            _optionView = CollectionViewSource.GetDefaultView(OptionItems);
            _optionView.Filter = FilterOption;

            WeakReferenceMessenger.Default.Register<MailItemData>(this);
        }

       
        #region Add Item
        [RelayCommand]
        private void SelectItem(object parameter)
        {
            MailData mailData = new()
            {
                SlotIndex = SlotIndex,
                ItemId = ItemId,
                ItemName = ItemName,
                ItemType = Type,
                ItemBranch = ItemBranch,
                IconName = IconName,
                Durability = Durability,
                MaxDurability = MaxDurability,
                EnchantLevel = EnchantLevel,
                Rank = Rank,
                Weight = Weight,
                Reconstruction = Reconstruction,
                ReconstructionMax = ReconstructionMax,
                Amount = Amount,
                RandomOption01 = RandomOption01,
                RandomOption02 = RandomOption02,
                RandomOption03 = RandomOption03,
                RandomOption01Value = RandomOption01Value,
                RandomOption02Value = RandomOption02Value,
                RandomOption03Value = RandomOption03Value,
                SocketCount = SocketCount,
                Socket01Color = Socket01Color,
                Socket02Color = Socket02Color,
                Socket03Color = Socket03Color,
                SocketOption01 = SocketOption01,
                SocketOption02 = SocketOption02,
                SocketOption03 = SocketOption03,
                SocketOption01Value = SocketOption01Value,
                SocketOption02Value = SocketOption02Value,
                SocketOption03Value = SocketOption03Value,
            };

            // Send the MailData
            WeakReferenceMessenger.Default.Send(new MailItemData(mailData, ViewModelType.MailWindowViewModel));
            //WeakReferenceMessenger.Default.Unregister<MailItemData>(this);

        }
        #endregion

        #region Load Item 

        public void Receive(MailItemData message)
        {
            if (message.Recipient == ViewModelType.ItemViewModel)
            {
                var mailData = message.Value;
                LoadMailItemData(mailData);
            }

        }

        [ObservableProperty]
        private bool _isLoadingMailData = false;

        private void LoadMailItemData(MailData mailData)
        {
            if (mailData != null)
            {
                IsLoadingMailData = true;

                SlotIndex = mailData.SlotIndex;
                ItemId = mailData.ItemId;
                Type = mailData.ItemType;
                IconName = mailData.IconName;
                Amount = mailData.Amount;
                Durability = mailData.Durability;
                MaxDurability = mailData.MaxDurability;
                EnchantLevel = mailData.EnchantLevel;
                Rank = mailData.Rank;
                Weight = mailData.Weight;
                Reconstruction = mailData.Reconstruction;
                ReconstructionMax = mailData.ReconstructionMax;
                RandomOption01 = mailData.RandomOption01;
                RandomOption02 = mailData.RandomOption02;
                RandomOption03 = mailData.RandomOption03;
                RandomOption01Value = mailData.RandomOption01Value;
                RandomOption02Value = mailData.RandomOption02Value;
                RandomOption03Value = mailData.RandomOption03Value;
                SocketCount = mailData.SocketCount;
                Socket01Color = mailData.Socket01Color;
                Socket02Color = mailData.Socket02Color;
                Socket03Color = mailData.Socket03Color;
                SocketOption01 = mailData.SocketOption01;
                SocketOption02 = mailData.SocketOption02;
                SocketOption03 = mailData.SocketOption03;
                SocketOption01Value = mailData.SocketOption01Value;
                SocketOption02Value = mailData.SocketOption02Value;
                SocketOption03Value = mailData.SocketOption03Value;

                IsLoadingMailData = false;
            }

        }
        #endregion

        #region Item Data List

        [ObservableProperty]
        private List<ItemData>? _itemDataItems;

        private void PopulateItemDataItems()
        {
            try
            {
                if (ItemDataManager.Instance.CachedItemDataList == null)
                {
                    ItemDataManager.Instance.InitializeCachedLists();
                }

                ItemDataItems = ItemDataManager.Instance.CachedItemDataList;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [ObservableProperty]
        private List<NameID>? _optionItems;

        private void PopulateOptionItems()
        {
            try
            {
                if (ItemDataManager.Instance.CachedOptionItems == null)
                {
                    ItemDataManager.Instance.InitializeCachedLists();
                }

                OptionItems = ItemDataManager.Instance.CachedOptionItems;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [ObservableProperty]
        private ICollectionView _optionView;

        private readonly List<int> selectedOptions = [];
        private bool FilterOption(object obj)
        {
            if (obj is NameID option)
            {
                // Always include "None" option
                if (option.ID == 0)
                    return true;

                selectedOptions.Add(RandomOption01);
                selectedOptions.Add(RandomOption02);
                selectedOptions.Add(RandomOption03);
                selectedOptions.Add(SocketOption01);
                selectedOptions.Add(SocketOption02);
                selectedOptions.Add(SocketOption03);

                if (selectedOptions.Contains(option.ID))
                    return true;

                // text search filter
                if (!string.IsNullOrEmpty(OptionSearch))
                {
                    string searchText = OptionSearch.ToLower();

                    // Check if either option ID or option Name contains the search text
                    if (!string.IsNullOrEmpty(option.ID.ToString()) && option.ID.ToString().Contains(searchText, StringComparison.CurrentCultureIgnoreCase))
                        return true;

                    if (!string.IsNullOrEmpty(option.Name) && option.Name.Contains(searchText, StringComparison.CurrentCultureIgnoreCase))
                        return true;

                    return false;
                }

                return true;
            }
            return false;
        }

        [ObservableProperty]
        private string? _optionSearch;
        partial void OnOptionSearchChanged(string? value)
        {
            searchTimer.Stop();
            searchTimer.Start();
        }

        #endregion

        #region CollectionView

        [ObservableProperty]
        private ICollectionView _itemDataView;

        private bool FilterItems(object obj)
        {
            if (obj is ItemData item)
            {
                //combobox filter
                if (ItemTypeFilter != 0 && item.Type != ItemTypeFilter)
                    return false;

                if (ItemCategoryFilter != 0 && item.Category != ItemCategoryFilter)
                    return false;

                if (ItemSubCategoryFilter != 0 && item.SubCategory != ItemSubCategoryFilter)
                    return false;

                if (ItemClassFilter != 0 && item.JobClass != ItemClassFilter)
                    return false;

                if (ItemBranchFilter != 0 && item.Branch != ItemBranchFilter)
                    return false;

                if (ItemTradeFilter != 2 && item.ItemTrade != ItemTradeFilter)
                    return false;

                // text search filter
                if (!string.IsNullOrEmpty(SearchText))
                {
                    string searchText = SearchText.ToLower();

                    // Check if either item ID or item Name contains the search text
                    if (!string.IsNullOrEmpty(item.ID.ToString()) && item.ID.ToString().Contains(searchText, StringComparison.CurrentCultureIgnoreCase))
                        return true;

                    if (!string.IsNullOrEmpty(item.Name) && item.Name.Contains(searchText, StringComparison.CurrentCultureIgnoreCase))
                        return true;

                    return false;
                }

                return true;
            }
            return false;
        }

        [ObservableProperty]
        private string? _searchText;
        partial void OnSearchTextChanged(string? value)
        {
            searchTimer.Stop();
            searchTimer.Start();
        }

        private void SearchTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(ItemDataView.Refresh);
            Application.Current.Dispatcher.Invoke(OptionView.Refresh);
        }

        #endregion

        #region Comboboxes Filter

        [ObservableProperty]
        private List<NameID>? _categoryFilterItems;

        [ObservableProperty]
        private List<NameID>? _subCategoryItemsFilter;

        private void PopulateCategoryItemsFilter(ItemType itemType)
        {
            try
            {
                CategoryFilterItems = new List<NameID>(_gmDatabaseService.GetCategoryItems(itemType, false));
                SubCategoryItemsFilter = new List<NameID>(_gmDatabaseService.GetCategoryItems(itemType, true));

                if (CategoryFilterItems.Count > 0)
                {
                    ItemCategoryFilterSelectedIndex = 0;
                }
                if (SubCategoryItemsFilter.Count > 0)
                {
                    ItemSubCategoryFilterSelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [ObservableProperty]
        private int _itemTypeFilter;
        partial void OnItemTypeFilterChanged(int value)
        {
            PopulateCategoryItemsFilter((ItemType)value);
            ItemDataView.Refresh();
        }

        [ObservableProperty]
        private int _itemTypeFilterSelectedIndex;

        [ObservableProperty]
        private int _itemCategoryFilter;
        partial void OnItemCategoryFilterChanged(int value)
        {
            ItemDataView.Refresh();
        }

        [ObservableProperty]
        private int _itemCategoryFilterSelectedIndex;

        [ObservableProperty]
        private int _itemSubCategoryFilter;
        partial void OnItemSubCategoryFilterChanged(int value)
        {
            ItemDataView.Refresh();
        }

        [ObservableProperty]
        private int _itemSubCategoryFilterSelectedIndex;

        [ObservableProperty]
        private int _itemClassFilter;

        partial void OnItemClassFilterChanged(int value)
        {
            ItemDataView.Refresh();
        }

        [ObservableProperty]
        private int _itemClassFilterSelectedIndex;

        [ObservableProperty]
        private int _itemBranchFilter;

        partial void OnItemBranchFilterChanged(int value)
        {
            ItemDataView.Refresh();
        }
        [ObservableProperty]
        private int _itemBranchFilterSelectedIndex;

        [ObservableProperty]
        private List<NameID>? _itemTypeItemsFilter;

        private void PopulateItemTypeItemsFilter()
        {
            try
            {
                ItemTypeItemsFilter = GetEnumItems<ItemType>();

                if (ItemTypeItemsFilter.Count > 0)
                {
                    ItemTypeFilterSelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [ObservableProperty]
        private List<NameID>? _classItemsFilter;

        private void PopulateClassItemsFilter()
        {
            try
            {
                ClassItemsFilter = GetEnumItems<CharClass>();

                if (ClassItemsFilter.Count > 0)
                {
                    ItemClassFilterSelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [ObservableProperty]
        private List<NameID>? _branchItemsFilter;

        private void PopulateBranchItemsFilter()
        {
            try
            {
                BranchItemsFilter = GetEnumItems<Branch>();

                if (BranchItemsFilter.Count > 0)
                {
                    ItemBranchFilterSelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [ObservableProperty]
        private int _itemTradeFilter;
        partial void OnItemTradeFilterChanged(int value)
        {
            ItemDataView.Refresh();
        }

        [ObservableProperty]
        private int _itemTradeFilterSelectedIndex;

        [ObservableProperty]
        private List<NameID>? _itemTradeFilterItems;

        private void PopulateItemTradeItemsFilter()
        {
            try
            {
                ItemTradeFilterItems =
                [
                    new NameID { ID = 2, Name = "All" },
                    new NameID { ID = 1, Name = "Tradeable" },
                    new NameID { ID = 0, Name = "Untradeable" }
                ];

                ItemTradeFilterSelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [ObservableProperty]
        private List<NameID>? _socketColorItems;

        private void PopulateSocketColorItems()
        {
            try
            {
                SocketColorItems = GetSocketColorItems();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region ItemData

        [ObservableProperty]
        private ItemData? _item;
        partial void OnItemChanged(ItemData? value)
        {
            UpdateItemData();
        }

        // Update UI elements based on Item data
        private void UpdateItemData()
        {
            if (Item != null)
            {
                ItemId = Item.ID;
                ItemName = Item.Name;
                ItemBranch = Item.Branch;
                IconName = Item.IconName;
                Description = Item.Description;
                ItemTrade = Item.ItemTrade;
                Durability = Item.Durability;
                MaxDurability = Item.Durability;
                Weight = Item.Weight;
                Reconstruction = Item.ReconstructionMax;
                ReconstructionMax = Item.ReconstructionMax;
                OverlapCnt = OverlapCnt == 0 ? 1: Item.OverlapCnt;
                Type = Item.Type;
                Category = Item.Category;
                SubCategory = Item.SubCategory;
                JobClass = Item.JobClass;
                Defense = Item.Defense;
                MagicDefense = Item.MagicDefense;
                WeaponID00 = Item.WeaponID00;
                SellPrice = Item.SellPrice;
                RequiredLevel = Item.LevelLimit;
                SetId = Item.SetId;
                PetFood = Item.PetFood;
                FixedOption01 = Item.FixOption00;
                FixedOption01Value = Item.FixOptionValue00;
                FixedOption02 = Item.FixOption01;
                FixedOption02Value = Item.FixOptionValue01;
                OptionCountMax = Item.Type != 1 ? Item.OptionCountMax : (Item.Type == 1 && Item.Category == 29 ? 1 : 0);
                SocketCountMax = Item.SocketCountMax;
                SocketCount = Item.SocketCountMax;

            }

        }

        [ObservableProperty]
        private int _slotIndex;

        [ObservableProperty]
        private int _itemId;

        [ObservableProperty]
        private string? _itemName;

        public string ItemNameColor => _frameService.GetBranchColor(ItemBranch);

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ItemNameColor))]
        private int _itemBranch;

        [ObservableProperty]
        private string? _iconName;

        [ObservableProperty]
        private string? _description;

        [ObservableProperty]
        private int _amount = 1;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(EnchantLevel))]
        private int _type;
        partial void OnTypeChanged(int value)
        {
            if (value == 1 || value == 2)
            {
                EnchantLevel = 0;
            }
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(WeightText))]
        private int _weight;

        public string WeightText => _frameService.FormatWeight(Weight);

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(DurabilityText))]
        private int _durability;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(DurabilityText))]
        private int _maxDurability;

        partial void OnMaxDurabilityChanged(int value)
        {
            if (Durability > value)
                Durability = value;

            if (Durability < value)
                Durability = value;
        }

        public string DurabilityText => _frameService.FormatDurability(Durability, MaxDurability);

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ReconstructionText))]
        private int _reconstruction;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ReconstructionText))]
        private int _reconstructionMax;

        public string ReconstructionText => _frameService.FormatReconstruction(Reconstruction, ReconstructionMax, ItemTrade);

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(RankText))]
        private int _rank = 1;

        public string RankText => _frameService.GetRankText(Rank);

        [ObservableProperty]
        private int _overlapCnt;

        [ObservableProperty]
        private int _enchantLevel;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CategoryText))]
        [NotifyPropertyChangedFor(nameof(RandomOption))]
        private int _category;

        public string CategoryText => _gmDatabaseService.GetCategoryName(Category);

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SubCategoryText))]
        private int _subCategory;

        public string? SubCategoryText => _gmDatabaseService.GetSubCategoryName(SubCategory);

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(MainStatText))]
        private int _defense;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(MainStatText))]
        private int _magicDefense;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(MainStatText))]
        private int _weaponID00;

        public string MainStatText => _frameService.FormatMainStat(Type, Defense, MagicDefense, JobClass, WeaponID00);

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SellValueText))]
        private int _sellPrice;

        public string SellValueText => _frameService.FormatSellValue(SellPrice);

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(RequiredLevelText))]
        private int _requiredLevel;

        public string RequiredLevelText => _frameService.FormatRequiredLevel(RequiredLevel);

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ItemTradeText))]
        private int _itemTrade;

        public string ItemTradeText => _frameService.FormatItemTrade(ItemTrade);

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(JobClassText))]
        private int _jobClass;

        public string JobClassText => JobClass != 0 ? GetEnumDescription((CharClass)JobClass) : "";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SetNameText))]
        private int _setId;

        public string SetNameText => _gmDatabaseService.GetSetName(SetId);

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(PetFoodText))]
        [NotifyPropertyChangedFor(nameof(PetFoodColor))]
        private int _petFood;

        public string PetFoodText => _frameService.FormatPetFood(PetFood);

        public string PetFoodColor => _frameService.FormatPetFoodColor(PetFood);

        #region Fixed Option

        public string FixedOption => "[Fixed Buff]";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(FixedOption01Text))]
        [NotifyPropertyChangedFor(nameof(FixedOption01Color))]
        private int _fixedOption01;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(FixedOption01Text))]
        private int _fixedOption01Value;

        public string FixedOption01Text => _frameService.GetOptionName(FixedOption01, FixedOption01Value);

        public string FixedOption01Color => _frameService.GetColorFromOption(FixedOption01);


        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(FixedOption02Text))]
        [NotifyPropertyChangedFor(nameof(FixedOption02Color))]
        private int _fixedOption02;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(FixedOption02Text))]
        private int _fixedOption02Value;

        public string FixedOption02Text => _frameService.GetOptionName(FixedOption02, FixedOption02Value);

        public string FixedOption02Color => _frameService.GetColorFromOption(FixedOption02);

        #endregion

        #endregion

        #region Random Option

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(RandomOption01MinValue))]
        [NotifyPropertyChangedFor(nameof(RandomOption01MaxValue))]
        [NotifyPropertyChangedFor(nameof(RandomOption01Value))]
        private int _optionCount;


        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(RandomOption01MinValue))]
        [NotifyPropertyChangedFor(nameof(RandomOption01MaxValue))]
        [NotifyPropertyChangedFor(nameof(RandomOption01Value))]
        private int _OptionCountMax;

        public string RandomOption => Category == 29 ? $"[Buff]" : "[Random Buff]";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(RandomOption01Text))]
        [NotifyPropertyChangedFor(nameof(RandomOption01Color))]
        [NotifyPropertyChangedFor(nameof(RandomOption01MinValue))]
        [NotifyPropertyChangedFor(nameof(RandomOption01MaxValue))]
        private int _randomOption01;

        partial void OnRandomOption01Changed(int value)
        {
            (RandomOption01MinValue, RandomOption01MaxValue) = _gmDatabaseService.GetOptionValue(value);
            RandomOption01Value = CalculateOptionValue(value, RandomOption01Value, RandomOption01MaxValue);
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(RandomOption01Text))]
        private int _randomOption01Value;

        public string RandomOption01Text => _frameService.GetOptionName(RandomOption01, RandomOption01Value);

        public string? RandomOption01Color => _frameService.GetColorFromOption(RandomOption01);

        [ObservableProperty]
        private int _randomOption01MinValue;

        [ObservableProperty]
        private int _randomOption01MaxValue;
        partial void OnRandomOption01MaxValueChanged(int value)
        {
            if (RandomOption01Value > value)
                RandomOption01Value = value;
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(RandomOption02Text))]
        [NotifyPropertyChangedFor(nameof(RandomOption02Color))]
        [NotifyPropertyChangedFor(nameof(RandomOption02MinValue))]
        [NotifyPropertyChangedFor(nameof(RandomOption02MaxValue))]
        private int _randomOption02;

        partial void OnRandomOption02Changed(int value)
        {
            (RandomOption02MinValue, RandomOption02MaxValue) = _gmDatabaseService.GetOptionValue(value);
            RandomOption02Value = CalculateOptionValue(value, RandomOption02Value, RandomOption02MaxValue);
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(RandomOption02Text))]
        private int _randomOption02Value;

        public string RandomOption02Text => _frameService.GetOptionName(RandomOption02, RandomOption02Value);

        public string? RandomOption02Color => _frameService.GetColorFromOption(RandomOption02);

        [ObservableProperty]
        private int _randomOption02MinValue;

        [ObservableProperty]
        private int _randomOption02MaxValue;
        partial void OnRandomOption02MaxValueChanged(int value)
        {
            if (RandomOption02Value > value)
                RandomOption02Value = value;
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(RandomOption03Text))]
        [NotifyPropertyChangedFor(nameof(RandomOption03Color))]
        [NotifyPropertyChangedFor(nameof(RandomOption03MinValue))]
        [NotifyPropertyChangedFor(nameof(RandomOption03MaxValue))]
        private int _randomOption03;

        partial void OnRandomOption03Changed(int value)
        {
            (RandomOption03MinValue, RandomOption03MaxValue) = _gmDatabaseService.GetOptionValue(value);
            RandomOption03Value = CalculateOptionValue(value, RandomOption03Value, RandomOption03MaxValue);
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(RandomOption03Text))]
        private int _randomOption03Value;

        public string RandomOption03Text => _frameService.GetOptionName(RandomOption03, RandomOption03Value);

        public string? RandomOption03Color => _frameService.GetColorFromOption(RandomOption03);

        [ObservableProperty]
        private int _randomOption03MinValue;

        [ObservableProperty]
        private int _randomOption03MaxValue;
        partial void OnRandomOption03MaxValueChanged(int value)
        {
            if (RandomOption03Value > value)
                RandomOption03Value = value;
        }

        private static int CalculateOptionValue(int option, int value, int maxValue)
        {
            if (option != 0)
            {
                if (value == 0)
                {
                    return maxValue;
                }
            }
            else
            {
                return 0;
            }
            return value;
        }
        #endregion

        #region Socket Option

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SocketOption))]
        private int _socketCount;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SocketOption))]
        private int _socketCountMax;

        public string SocketOption => $"Socket: {SocketCount}/{SocketCountMax}";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SocketOption01Text))]
        [NotifyPropertyChangedFor(nameof(SocketOption01Color))]
        private int _socket01Color;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SocketOption02Text))]
        [NotifyPropertyChangedFor(nameof(SocketOption02Color))]
        private int _socket02Color;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SocketOption03Text))]
        [NotifyPropertyChangedFor(nameof(SocketOption03Color))]
        private int _socket03Color;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SocketOption01Text))]
        [NotifyPropertyChangedFor(nameof(SocketOption01Color))]
        [NotifyPropertyChangedFor(nameof(SocketOption01MinValue))]
        [NotifyPropertyChangedFor(nameof(SocketOption01MaxValue))]
        private int _socketOption01;

        partial void OnSocketOption01Changed(int value)
        {
            (SocketOption01MinValue, SocketOption01MaxValue) = _gmDatabaseService.GetOptionValue(value);
            SocketOption01Value = CalculateOptionValue(value, SocketOption01Value, SocketOption01MaxValue);
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SocketOption01Text))]
        private int _socketOption01Value;

        public string SocketOption01Text => SocketOption01 != 0 ? _frameService.GetOptionName(SocketOption01, SocketOption01Value) : _frameService.GetSocketText(Socket01Color);

        public string SocketOption01Color => SocketOption01 != 0 ? _frameService.GetColorFromOption(SocketOption01) : _frameService.GetSocketColor(Socket01Color);

        [ObservableProperty]
        private int _socketOption01MinValue;

        [ObservableProperty]
        private int _socketOption01MaxValue;
        partial void OnSocketOption01MaxValueChanged(int value)
        {
            if (SocketOption01Value > value)
                SocketOption01Value = value;
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SocketOption02Text))]
        [NotifyPropertyChangedFor(nameof(SocketOption02Color))]
        [NotifyPropertyChangedFor(nameof(SocketOption02MinValue))]
        [NotifyPropertyChangedFor(nameof(SocketOption02MaxValue))]
        private int _socketOption02;

        partial void OnSocketOption02Changed(int value)
        {
            (SocketOption02MinValue, SocketOption02MaxValue) = _gmDatabaseService.GetOptionValue(value);
            SocketOption02Value = CalculateOptionValue(value, SocketOption02Value, SocketOption02MaxValue);
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SocketOption02Text))]
        private int _socketOption02Value;

        public string SocketOption02Text => SocketOption02 != 0 ? _frameService.GetOptionName(SocketOption02, SocketOption02Value) : _frameService.GetSocketText(Socket02Color);

        public string? SocketOption02Color => SocketOption02 != 0 ? _frameService.GetColorFromOption(SocketOption02) : _frameService.GetSocketColor(Socket02Color);

        [ObservableProperty]
        private int _socketOption02MinValue;

        [ObservableProperty]
        private int _socketOption02MaxValue;
        partial void OnSocketOption02MaxValueChanged(int value)
        {
            if (SocketOption02Value > value)
                SocketOption02Value = value;
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SocketOption03Text))]
        [NotifyPropertyChangedFor(nameof(SocketOption03Color))]
        [NotifyPropertyChangedFor(nameof(SocketOption03MinValue))]
        [NotifyPropertyChangedFor(nameof(SocketOption03MaxValue))]
        private int _socketOption03;

        partial void OnSocketOption03Changed(int value)
        {
            (SocketOption03MinValue, SocketOption03MaxValue) = _gmDatabaseService.GetOptionValue(value);
            SocketOption03Value = CalculateOptionValue(value, SocketOption03Value, SocketOption03MaxValue);
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SocketOption03Text))]
        private int _socketOption03Value;

        public string SocketOption03Text => SocketOption03 != 0 ? _frameService.GetOptionName(SocketOption03, SocketOption03Value) : _frameService.GetSocketText(Socket03Color);

        public string? SocketOption03Color => SocketOption03 != 0 ? _frameService.GetColorFromOption(SocketOption03) : _frameService.GetSocketColor(Socket03Color);

        [ObservableProperty]
        private int _socketOption03MinValue;

        [ObservableProperty]
        private int _socketOption03MaxValue;
        partial void OnSocketOption03MaxValueChanged(int value)
        {
            if (SocketOption03Value > value)
                SocketOption03Value = value;
        }

        #endregion
    }
}
