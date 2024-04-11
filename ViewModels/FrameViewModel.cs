using RHGMTool.Models;
using RHGMTool.Services;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Data;
using static RHGMTool.Models.EnumService;

namespace RHGMTool.ViewModels
{
    public class FrameViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private readonly ISqLiteDatabaseService _databaseService;
        private readonly GMDatabaseService _gmDatabaseService;
        private readonly FrameService _frameService;
        private readonly ItemDataManager _itemDataManager;
        private readonly System.Timers.Timer searchTimer;

        public FrameViewModel()
        {
            _databaseService = new SqLiteDatabaseService();
            _gmDatabaseService = new GMDatabaseService(_databaseService);
            _frameService = new FrameService();
            _itemDataManager = new ItemDataManager();
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

        }

        #region Item Data List

        private List<ItemData>? _itemDataItems;
        public List<ItemData>? ItemDataItems
        {
            get
            {
                return _itemDataItems;
            }
            set
            {
                if (_itemDataItems != value)
                {
                    _itemDataItems = value;
                    OnPropertyChanged(nameof(ItemDataItems));
                }
            }
        }

        private void PopulateItemDataItems()
        {
            try
            {
                if (_itemDataManager.CachedItemDataList == null)
                {
                    _itemDataManager.InitializeItemDataList();
                }

                ItemDataItems = _itemDataManager.CachedItemDataList;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private List<NameID>? _optionItems;
        public List<NameID>? OptionItems
        {
            get { return _optionItems; }
            set
            {
                if (_optionItems != value)
                {
                    _optionItems = value;
                    OnPropertyChanged(nameof(OptionItems));
                }
            }
        }

        private void PopulateOptionItems()
        {
            try
            {
                OptionItems = new List<NameID>(_gmDatabaseService.GetOptionItems());
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region CollectionView

        private ICollectionView _itemDataView;
        public ICollectionView ItemDataView
        {
            get { return _itemDataView; }
            set
            {
                _itemDataView = value;
                OnPropertyChanged(nameof(ItemDataView));
            }
        }

        private bool FilterItems(object obj)
        {
            if (obj is ItemData item)
            {
                //combobox filter
                if (_itemTypeFilter != 0 && item.Type != _itemTypeFilter)
                    return false;

                if (_itemCategoryFilter != 0 && item.Category != _itemCategoryFilter)
                    return false;

                if (_itemSubCategoryFilter != 0 && item.SubCategory != _itemSubCategoryFilter)
                    return false;

                if (_itemClassFilter != 0 && item.JobClass != _itemClassFilter)
                    return false;

                if (_itemBranchFilter != 0 && item.Branch != _itemBranchFilter)
                    return false;

                if (_itemTradeFilter != 2 && item.ItemTrade != _itemTradeFilter)
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
        
        private string? _searchText;
        public string? SearchText
        {
            get { return _searchText; }
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    OnPropertyChanged(nameof(SearchText));
                    searchTimer.Stop();
                    searchTimer.Start();
                }
            }
        }

        private void SearchTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(_itemDataView.Refresh);
        }

        #endregion

        #region Comboboxes Filter

        private List<NameID>? _categoryFilterItems;
        public List<NameID>? CategoryItemsFilter
        {
            get { return _categoryFilterItems; }
            set
            {
                if (_categoryFilterItems != value)
                {
                    _categoryFilterItems = value;
                    OnPropertyChanged(nameof(CategoryItemsFilter));
                }
            }
        }

        private List<NameID>? _subCategoryItemsFilter;
        public List<NameID>? SubCategoryItemsFilter
        {
            get { return _subCategoryItemsFilter; }
            set
            {
                if (_subCategoryItemsFilter != value)
                {
                    _subCategoryItemsFilter = value;
                    OnPropertyChanged(nameof(SubCategoryItemsFilter));
                }
            }
        }

        private void PopulateCategoryItemsFilter(ItemType itemType)
        {
            try
            {
                CategoryItemsFilter = new List<NameID>(_gmDatabaseService.GetCategoryItems(itemType, false));
                SubCategoryItemsFilter = new List<NameID>(_gmDatabaseService.GetCategoryItems(itemType, true));

                if (CategoryItemsFilter.Count > 0)
                {
                    ItemCategoryFilterSelectedIndex = 0;
                    OnPropertyChanged(nameof(ItemCategoryFilterSelectedIndex));
                }
                if (SubCategoryItemsFilter.Count > 0)
                {
                    ItemSubCategoryFilterSelectedIndex = 0;
                    OnPropertyChanged(nameof(ItemSubCategoryFilterSelectedIndex));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private int _itemTypeFilter;

        public int ItemTypeFilter
        {
            get { return _itemTypeFilter; }
            set
            {
                if (_itemTypeFilter != value)
                {
                    _itemTypeFilter = value;
                    OnPropertyChanged(nameof(ItemTypeFilter));
                    PopulateCategoryItemsFilter((ItemType)value);
                    _itemDataView.Refresh();
                }
            }
        }

        private int _itemTypeFilterSelectedIndex;

        public int ItemTypeFilterSelectedIndex
        {
            get { return _itemTypeFilterSelectedIndex; }
            set
            {
                if (_itemTypeFilterSelectedIndex != value)
                {
                    _itemTypeFilterSelectedIndex = value;
                    OnPropertyChanged(nameof(ItemTypeFilterSelectedIndex));

                }
            }
        }

        private int _itemCategoryFilter;

        public int ItemCategoryFilter
        {
            get { return _itemCategoryFilter; }
            set
            {
                if (_itemCategoryFilter != value)
                {
                    _itemCategoryFilter = value;
                    OnPropertyChanged(nameof(ItemCategoryFilter));
                    _itemDataView.Refresh();
                }
            }
        }

        private int _itemCategoryFilterSelectedIndex;

        public int ItemCategoryFilterSelectedIndex
        {
            get { return _itemCategoryFilterSelectedIndex; }
            set
            {
                if (_itemCategoryFilterSelectedIndex != value)
                {
                    _itemCategoryFilterSelectedIndex = value;
                    OnPropertyChanged(nameof(ItemCategoryFilterSelectedIndex));

                }
            }
        }

        private int _itemSubCategoryFilter;

        public int ItemSubCategoryFilter
        {
            get { return _itemSubCategoryFilter; }
            set
            {
                if (_itemSubCategoryFilter != value)
                {
                    _itemSubCategoryFilter = value;
                    OnPropertyChanged(nameof(ItemSubCategoryFilter));
                    _itemDataView.Refresh();
                }
            }
        }

        private int _itemSubCategoryFilterSelectedIndex;

        public int ItemSubCategoryFilterSelectedIndex
        {
            get { return _itemSubCategoryFilterSelectedIndex; }
            set
            {
                if (_itemSubCategoryFilterSelectedIndex != value)
                {
                    _itemSubCategoryFilterSelectedIndex = value;
                    OnPropertyChanged(nameof(ItemSubCategoryFilterSelectedIndex));

                }
            }
        }

        private int _itemClassFilter;

        public int ItemClassFilter
        {
            get { return _itemClassFilter; }
            set
            {
                if (_itemClassFilter != value)
                {
                    _itemClassFilter = value;
                    OnPropertyChanged(nameof(ItemClassFilter));
                    _itemDataView.Refresh();
                }
            }
        }

        private int _itemClassFilterSelectedIndex;

        public int ItemClassFilterSelectedIndex
        {
            get { return _itemClassFilterSelectedIndex; }
            set
            {
                if (_itemClassFilterSelectedIndex != value)
                {
                    _itemClassFilterSelectedIndex = value;
                    OnPropertyChanged(nameof(ItemClassFilterSelectedIndex));

                }
            }
        }

        private int _itemBranchFilter;

        public int ItemBranchFilter
        {
            get { return _itemBranchFilter; }
            set
            {
                if (_itemBranchFilter != value)
                {
                    _itemBranchFilter = value;
                    OnPropertyChanged(nameof(ItemBranchFilter));
                    _itemDataView.Refresh();
                }
            }
        }

        private int _itemBranchFilterSelectedIndex;

        public int ItemBranchFilterSelectedIndex
        {
            get { return _itemBranchFilterSelectedIndex; }
            set
            {
                if (_itemBranchFilterSelectedIndex != value)
                {
                    _itemBranchFilterSelectedIndex = value;
                    OnPropertyChanged(nameof(ItemBranchFilterSelectedIndex));

                }
            }
        }

        private List<NameID>? _itemTypeItemsFilter;
        public List<NameID>? ItemTypeItemsFilter
        {
            get { return _itemTypeItemsFilter; }
            set
            {
                if (_itemTypeItemsFilter != value)
                {
                    _itemTypeItemsFilter = value;
                    OnPropertyChanged(nameof(ItemTypeItemsFilter));
                }
            }
        }

        private void PopulateItemTypeItemsFilter()
        {
            try
            {
                ItemTypeItemsFilter = GetEnumItems<ItemType>();

                if (ItemTypeItemsFilter.Count > 0)
                {
                    ItemTypeFilterSelectedIndex = 0;
                    OnPropertyChanged(nameof(ItemTypeFilterSelectedIndex));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private List<NameID>? _classItemsFilter;
        public List<NameID>? ClassItemsFilter
        {
            get { return _classItemsFilter; }
            set
            {
                if (_classItemsFilter != value)
                {
                    _classItemsFilter = value;
                    OnPropertyChanged(nameof(ClassItemsFilter));
                }
            }
        }

        private void PopulateClassItemsFilter()
        {
            try
            {
                ClassItemsFilter = GetEnumItems<CharClass>();

                if (ClassItemsFilter.Count > 0)
                {
                    ItemClassFilterSelectedIndex = 0;
                    OnPropertyChanged(nameof(ItemClassFilterSelectedIndex));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private List<NameID>? _branchItemsFilter;
        public List<NameID>? BranchItemsFilter
        {
            get { return _branchItemsFilter; }
            set
            {
                if (_branchItemsFilter != value)
                {
                    _branchItemsFilter = value;
                    OnPropertyChanged(nameof(BranchItemsFilter));
                }
            }
        }

        private void PopulateBranchItemsFilter()
        {
            try
            {
                BranchItemsFilter = GetEnumItems<Branch>();

                if (BranchItemsFilter.Count > 0)
                {
                    ItemBranchFilterSelectedIndex = 0;
                    OnPropertyChanged(nameof(ItemBranchFilterSelectedIndex));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private int _itemTradeFilter;

        public int ItemTradeFilter
        {
            get { return _itemTradeFilter; }
            set
            {
                if (_itemTradeFilter != value)
                {
                    _itemTradeFilter = value;
                    OnPropertyChanged(nameof(ItemTradeFilter));
                    _itemDataView.Refresh();
                }
            }
        }

        private int _itemTradeFilterSelectedIndex;

        public int ItemTradeFilterSelectedIndex
        {
            get { return _itemTradeFilterSelectedIndex; }
            set
            {
                if (_itemTradeFilterSelectedIndex != value)
                {
                    _itemTradeFilterSelectedIndex = value;
                    OnPropertyChanged(nameof(ItemTradeFilterSelectedIndex));

                }
            }
        }

        private List<NameID>? _itemTradeFilterItems;
        public List<NameID>? ItemTradeItemsFilter
        {
            get { return _itemTradeFilterItems; }
            set
            {
                if (_itemTradeFilterItems != value)
                {
                    _itemTradeFilterItems = value;
                    OnPropertyChanged(nameof(ItemTradeItemsFilter));
                }
            }
        }

        private void PopulateItemTradeItemsFilter()
        {
            try
            {
                ItemTradeItemsFilter =
                [
                    new NameID { ID = 2, Name = "All" },
                    new NameID { ID = 1, Name = "Tradeable" },
                    new NameID { ID = 0, Name = "Untradeable" }
                ];

                ItemTradeFilterSelectedIndex = 0;
                OnPropertyChanged(nameof(ItemTradeFilterSelectedIndex));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private List<NameID>? _socketColorItems;
        public List<NameID>? SocketColorItems
        {
            get { return _socketColorItems; }
            set
            {
                if (_socketColorItems != value)
                {
                    _socketColorItems = value;
                    OnPropertyChanged(nameof(SocketColorItems));
                }
            }
        }

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

        private ItemData? _item;
        public ItemData? Item
        {
            get { return _item; }
            set
            {
                _item = value;
                OnPropertyChanged();
                UpdateItemData();
            }
        }

        // Update UI elements based on Item data
        private void UpdateItemData()
        {
            if (Item != null)
            {
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
                OverlapCnt = Item.OverlapCnt;
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


                // Raise PropertyChanged for all properties
                OnPropertyChanged(nameof(ItemName));
                OnPropertyChanged(nameof(ItemBranch));
                OnPropertyChanged(nameof(IconName));
                OnPropertyChanged(nameof(Description));
                OnPropertyChanged(nameof(ItemTrade));
                OnPropertyChanged(nameof(Durability));
                OnPropertyChanged(nameof(MaxDurability));
                OnPropertyChanged(nameof(Weight));
                OnPropertyChanged(nameof(Reconstruction));
                OnPropertyChanged(nameof(ReconstructionMax));
                OnPropertyChanged(nameof(OverlapCnt));
                OnPropertyChanged(nameof(Type));
                OnPropertyChanged(nameof(Category));
                OnPropertyChanged(nameof(SubCategory));
                OnPropertyChanged(nameof(JobClass));
                OnPropertyChanged(nameof(Defense));
                OnPropertyChanged(nameof(MagicDefense));
                OnPropertyChanged(nameof(WeaponID00));
                OnPropertyChanged(nameof(SellPrice));
                OnPropertyChanged(nameof(RequiredLevel));
                OnPropertyChanged(nameof(SetId));
                OnPropertyChanged(nameof(PetFood));
                OnPropertyChanged(nameof(FixedOption01));
                OnPropertyChanged(nameof(FixedOption01Value));
                OnPropertyChanged(nameof(FixedOption02));
                OnPropertyChanged(nameof(FixedOption02Value));
                OnPropertyChanged(nameof(OptionCountMax));
                OnPropertyChanged(nameof(SocketCountMax));
                OnPropertyChanged(nameof(SocketCount));

            }

        }

        private string? _itemName;

        public string? ItemName
        {
            get { return _itemName; }
            set
            {
                if (_itemName != value)
                {
                    _itemName = value;
                    OnPropertyChanged(nameof(ItemName));
                }
            }
        }

        public string? ItemNameColor
        {
            get { return _frameService.GetBranchColor(ItemBranch); }
        }

        private int _itemBranch;

        public int ItemBranch
        {
            get { return _itemBranch; }
            set
            {
                if (_itemBranch != value)
                {
                    _itemBranch = value;
                    OnPropertyChanged(nameof(ItemBranch));
                    OnPropertyChanged(nameof(ItemNameColor));
                }
            }
        }

        private string? _iconName;
        public string? IconName
        {
            get { return _iconName; }
            private set
            {
                if (_iconName != value)
                {
                    _iconName = value;
                    OnPropertyChanged(nameof(IconName));
                }
            }
        }

        private string? _description;

        public string? Description
        {
            get { return _description; }
            set
            {
                if (_description != value)
                {
                    _description = value;
                    OnPropertyChanged(nameof(Description));
                }
            }
        }

        private int _type;

        public int Type
        {
            get { return _type; }
            set
            {
                if (_type != value)
                {
                    _type = value;
                    OnPropertyChanged(nameof(Type));

                    if (_type == 1 || _type == 2)
                    {
                        EnchantLevel = 0;
                        OnPropertyChanged(nameof(EnchantLevel));
                    }
                }
            }
        }

        private int _weight;

        public int Weight
        {
            get { return _weight; }
            set
            {
                if (_weight != value)
                {
                    _weight = value;
                    OnPropertyChanged(nameof(Weight));
                    OnPropertyChanged(nameof(WeightText));
                }
            }
        }

        public string? WeightText
        {
            get { return _frameService.FormatWeight(Weight); }
        }

        private int _durability;

        public int Durability
        {
            get { return _durability; }
            set
            {
                if (_durability != value)
                {
                    _durability = value;
                    OnPropertyChanged(nameof(Durability));
                    OnPropertyChanged(nameof(DurabilityText));
                }
            }
        }

        
        private int _maxDurability;

        public int MaxDurability
        {
            get { return _maxDurability; }
            set
            {
                if (_maxDurability != value)
                {
                    _maxDurability = value;
                    OnPropertyChanged(nameof(MaxDurability));
                    OnPropertyChanged(nameof(DurabilityText));

                    if (Durability > MaxDurability)
                        Durability = MaxDurability;

                    if (Durability < MaxDurability)
                        Durability = MaxDurability;
                }
            }
        }

        public string? DurabilityText
        {
            get { return _frameService.FormatDurability(Durability, MaxDurability); }
        }

        private int _reconstruction;

        public int Reconstruction
        {
            get { return _reconstruction; }
            set
            {
                if (_reconstruction != value)
                {
                    _reconstruction = value;
                    OnPropertyChanged(nameof(Reconstruction));
                    OnPropertyChanged(nameof(ReconstructionText));
                }

            }
        }

        private int _reconstructionMax;

        public int ReconstructionMax
        {
            get { return _reconstructionMax; }
            set
            {
                if (_reconstructionMax != value)
                {
                    _reconstructionMax = value;
                    OnPropertyChanged(nameof(ReconstructionMax));
                    OnPropertyChanged(nameof(ReconstructionText));
                }
            }
        }

        public string? ReconstructionText
        {
            get { return _frameService.FormatReconstruction(ReconstructionMax, ItemTrade); }
        }

        private int _rank = 1;
        public int Rank
        {
            get { return _rank; }
            set
            {
                if (_rank != value)
                {
                    _rank = value;
                    OnPropertyChanged(nameof(Rank));
                    OnPropertyChanged(nameof(RankText));
                }
            }
        }

        public string? RankText
        {
            get { return _frameService.GetRankText(Rank); }
        }


        private int _overlapCnt;

        public int OverlapCnt
        {
            get { return _overlapCnt; }
            set
            {
                if (_overlapCnt != value)
                {
                    _overlapCnt = value;
                    OnPropertyChanged(nameof(OverlapCnt));

                }
            }
        }

        private int _enchantLevel;

        public int EnchantLevel
        {
            get { return _enchantLevel; }
            set
            {
                if (_enchantLevel != value)
                {
                    _enchantLevel = value;
                    OnPropertyChanged(nameof(EnchantLevel));

                }
            }
        }


        private int _category;
        public int Category
        {
            get { return _category; }
            private set
            {
                if (_category != value)
                {
                    _category = value;
                    OnPropertyChanged(nameof(Category));
                    OnPropertyChanged(nameof(CategoryText));
                    OnPropertyChanged(nameof(RandomOption));
                }
            }
        }

        public string? CategoryText
        {
            get { return _gmDatabaseService.GetCategoryName(Category); }
        }

        private int _subCategory;
        public int SubCategory
        {
            get { return _subCategory; }
            private set
            {
                if (_subCategory != value)
                {
                    _subCategory = value;
                    OnPropertyChanged(nameof(SubCategory));
                    OnPropertyChanged(nameof(SubCategoryText));
                }
            }
        }

        public string? SubCategoryText
        {
            get { return _gmDatabaseService.GetSubCategoryName(SubCategory); }
        }

        private int _defense;
        public int Defense
        {
            get { return _defense; }
            private set
            {
                if (_defense != value)
                {
                    _defense = value;
                    OnPropertyChanged(nameof(Defense));
                    OnPropertyChanged(nameof(MainStatText));
                }
            }
        }


        private int _magicDefense;
        public int MagicDefense
        {
            get { return _magicDefense; }
            private set
            {
                if (_magicDefense != value)
                {
                    _magicDefense = value;
                    OnPropertyChanged(nameof(MagicDefense));
                    OnPropertyChanged(nameof(MainStatText));
                }
            }
        }

        private int _weaponID00;
        public int WeaponID00
        {
            get { return _weaponID00; }
            private set
            {
                if (_weaponID00 != value)
                {
                    _weaponID00 = value;
                    OnPropertyChanged(nameof(WeaponID00));
                    OnPropertyChanged(nameof(MainStatText));
                }
            }
        }

        public string MainStatText
        {
            get { return _frameService.FormatMainStat(Type, Defense, MagicDefense, JobClass, WeaponID00); }
        }

        private int _sellPrice;
        public int SellPrice
        {
            get { return _sellPrice; }
            private set
            {
                if (_sellPrice != value)
                {
                    _sellPrice = value;
                    OnPropertyChanged(nameof(SellPrice));
                    OnPropertyChanged(nameof(SellValueText));
                }
            }
        }

        public string SellValueText
        {
            get { return _frameService.FormatSellValue(SellPrice); }
        }

        private int _requiredLevel;
        public int RequiredLevel
        {
            get { return _requiredLevel; }
            private set
            {
                if (_requiredLevel != value)
                {
                    _requiredLevel = value;
                    OnPropertyChanged(nameof(RequiredLevel));
                    OnPropertyChanged(nameof(RequiredLevelText));
                }
            }
        }

        public string RequiredLevelText
        {
            get { return _frameService.FormatRequiredLevel(RequiredLevel); }
        }

        private int _itemTrade;
        public int ItemTrade
        {
            get { return _itemTrade; }
            private set
            {
                if (_itemTrade != value)
                {
                    _itemTrade = value;
                    OnPropertyChanged(nameof(ItemTrade));
                    OnPropertyChanged(nameof(ItemTradeText));
                }
            }
        }

        public string? ItemTradeText
        {
            get { return _frameService.FormatItemTrade(ItemTrade); }
        }

        private int _jobClass;
        public int JobClass
        {
            get { return _jobClass; }
            private set
            {
                if (_jobClass != value)
                {
                    _jobClass = value;
                    OnPropertyChanged(nameof(JobClass));
                    OnPropertyChanged(nameof(JobClassText));
                }
            }
        }

        public string? JobClassText
        {
            get { return JobClass != 0 ? GetEnumDescription((CharClass)JobClass) : ""; }
        }

        private int _setId;
        public int SetId
        {
            get { return _setId; }
            private set
            {
                if (_setId != value)
                {
                    _setId = value;
                    OnPropertyChanged(nameof(SetId));
                    OnPropertyChanged(nameof(SetNameText));
                }
            }
        }

        public string? SetNameText
        {
            get { return _gmDatabaseService.GetSetName(SetId); }
        }

        private int _petFood;
        public int PetFood
        {
            get { return _petFood; }
            private set
            {
                if (_petFood != value)
                {
                    _petFood = value;
                    OnPropertyChanged(nameof(PetFood));
                    OnPropertyChanged(nameof(PetFoodText));
                    OnPropertyChanged(nameof(PetFoodColor));
                }
            }
        }

        public string? PetFoodText
        {
            get { return _frameService.FormatPetFood(PetFood); }
        }

        public string? PetFoodColor
        {
            get { return _frameService.FormatPetFoodColor(PetFood); }
        }

        #region Fixed Option

        public static string FixedOption
        {
            get { return "[Fixed Buff]"; }
        }

        private int _fixedOption01;
        public int FixedOption01
        {
            get { return _fixedOption01; }
            set
            {
                if (_fixedOption01 != value)
                {
                    _fixedOption01 = value;
                    OnPropertyChanged(nameof(FixedOption01));
                    OnPropertyChanged(nameof(FixedOption01Text));
                    OnPropertyChanged(nameof(FixedOption01Color));
                }
            }
        }

        private int _fixedOption01Value;
        public int FixedOption01Value
        {
            get { return _fixedOption01Value; }
            set
            {
                if (_fixedOption01Value != value)
                {
                    _fixedOption01Value = value;
                    OnPropertyChanged(nameof(FixedOption01Value));
                    OnPropertyChanged(nameof(FixedOption01Text));
                }
            }
        }

        public string? FixedOption01Text
        {
            get { return _frameService.GetOptionName(FixedOption01, FixedOption01Value); }
        }

        public string? FixedOption01Color
        {
            get { return _frameService.GetColorFromOption(FixedOption01); }
        }


        private int _fixedOption02;
        public int FixedOption02
        {
            get { return _fixedOption02; }
            set
            {
                if (_fixedOption02 != value)
                {
                    _fixedOption02 = value;
                    OnPropertyChanged(nameof(FixedOption02));
                    OnPropertyChanged(nameof(FixedOption02Text));
                    OnPropertyChanged(nameof(FixedOption02Color));
                }
            }
        }

        private int _fixedOption02Value;
        public int FixedOption02Value
        {
            get { return _fixedOption02Value; }
            set
            {
                if (_fixedOption02Value != value)
                {
                    _fixedOption02Value = value;
                    OnPropertyChanged(nameof(FixedOption02Value));
                    OnPropertyChanged(nameof(FixedOption02Text));
                }
            }
        }

        public string? FixedOption02Text
        {
            get { return _frameService.GetOptionName(FixedOption02, FixedOption02Value); }
        }

        public string? FixedOption02Color
        {
            get { return _frameService.GetColorFromOption(FixedOption02); }
        }

        #endregion

        #endregion

        #region Random Option

        private int _optionCount;
        public int OptionCount
        {
            get { return _optionCount; }
            set
            {
                if (_optionCount != value)
                {
                    _optionCount = value;
                    OnPropertyChanged(nameof(OptionCount));
                    UpdateRandomOption();
                }
            }
        }

        private int _OptionCountMax;
        public int OptionCountMax
        {
            get { return _OptionCountMax; }
            set
            {

                if (_OptionCountMax != value)
                {
                    _OptionCountMax = value;
                    OnPropertyChanged(nameof(OptionCountMax));
                    UpdateRandomOption();
                }

            }
        }

        public string? RandomOption
        {
            get { return Category == 29 ? $"[Buff]" : "[Random Buff]"; }
        }

        private int _randomOption01;
        public int RandomOption01
        {
            get { return _randomOption01; }
            set
            {
                if (_randomOption01 != value)
                {
                    _randomOption01 = value;
                    OnPropertyChanged(nameof(RandomOption01));
                    OnPropertyChanged(nameof(RandomOption01Text));
                    OnPropertyChanged(nameof(RandomOption01Color));
                    UpdateRandomOption();
                }
            }
        }

        private int _randomOption01Value;
        public int RandomOption01Value
        {
            get { return _randomOption01Value; }
            set
            {
                if (_randomOption01Value != value)
                {
                    _randomOption01Value = value;
                    OnPropertyChanged(nameof(RandomOption01Value));
                    OnPropertyChanged(nameof(RandomOption01Text));
                }
            }
        }

        public string? RandomOption01Text
        {
            get { return  _frameService.GetOptionName(RandomOption01, RandomOption01Value); }
        }

        public string? RandomOption01Color
        {
            get { return _frameService.GetColorFromOption(RandomOption01); }
        }

        private int _randomOption01MinValue;
        public int RandomOption01MinValue
        {
            get { return _randomOption01MinValue; }
            set
            {
                if (_randomOption01MinValue != value)
                {
                    _randomOption01MinValue = value;
                    OnPropertyChanged(nameof(RandomOption01MinValue));
                }
            }
        }

        private int _randomOption01MaxValue;
        public int RandomOption01MaxValue
        {
            get { return _randomOption01MaxValue; }
            set
            {
                if (_randomOption01MaxValue != value)
                {
                    _randomOption01MaxValue = value;
                    OnPropertyChanged(nameof(RandomOption01MaxValue));

                    if (RandomOption01Value > value)
                        RandomOption01Value = value;
                }
            }
        }

        private int _randomOption02;
        public int RandomOption02
        {
            get { return _randomOption02; }
            set
            {
                if (_randomOption02 != value)
                {
                    _randomOption02 = value;
                    OnPropertyChanged(nameof(RandomOption02));
                    OnPropertyChanged(nameof(RandomOption02Text));
                    OnPropertyChanged(nameof(RandomOption02Color));
                    UpdateRandomOption();
                }
            }
        }

        private int _randomOption02Value;
        public int RandomOption02Value
        {
            get { return _randomOption02Value; }
            set
            {
                if (_randomOption02Value != value)
                {
                    _randomOption02Value = value;
                    OnPropertyChanged(nameof(RandomOption02Value));
                    OnPropertyChanged(nameof(RandomOption02Text));
                }
            }
        }

        public string? RandomOption02Text
        {
            get { return _frameService.GetOptionName(RandomOption02, RandomOption02Value); }
        }

        public string? RandomOption02Color
        {
            get { return _frameService.GetColorFromOption(RandomOption02); }
        }

        private int _randomOption02MinValue;
        public int RandomOption02MinValue
        {
            get { return _randomOption02MinValue; }
            set
            {
                if (_randomOption02MinValue != value)
                {
                    _randomOption02MinValue = value;
                    OnPropertyChanged(nameof(RandomOption02MinValue));
                }
            }
        }

        private int _randomOption02MaxValue;
        public int RandomOption02MaxValue
        {
            get { return _randomOption02MaxValue; }
            set
            {
                if (_randomOption02MaxValue != value)
                {
                    _randomOption02MaxValue = value;
                    OnPropertyChanged(nameof(RandomOption02MaxValue));

                    if (RandomOption02Value > value)
                        RandomOption02Value = value;
                }
            }
        }

        private int _randomOption03;
        public int RandomOption03
        {
            get { return _randomOption03; }
            set
            {
                if (_randomOption03 != value)
                {
                    _randomOption03 = value;
                    OnPropertyChanged(nameof(RandomOption03));
                    OnPropertyChanged(nameof(RandomOption03Text));
                    OnPropertyChanged(nameof(RandomOption03Color));
                    UpdateRandomOption();
                }
            }
        }

        private int _randomOption03Value;
        public int RandomOption03Value
        {
            get { return _randomOption03Value; }
            set
            {
                if (_randomOption03Value != value)
                {
                    _randomOption03Value = value;
                    OnPropertyChanged(nameof(RandomOption03Value));
                    OnPropertyChanged(nameof(RandomOption03Text));
                }
            }
        }

        public string? RandomOption03Text
        {
            get { return _frameService.GetOptionName(RandomOption03, RandomOption03Value); }
        }

        public string? RandomOption03Color
        {
            get { return _frameService.GetColorFromOption(RandomOption03); }
        }

        private int _randomOption03MinValue;
        public int RandomOption03MinValue
        {
            get { return _randomOption03MinValue; }
            set
            {
                if (_randomOption03MinValue != value)
                {
                    _randomOption03MinValue = value;
                    OnPropertyChanged(nameof(RandomOption03MinValue));
                }
            }
        }

        private int _randomOption03MaxValue;
        public int RandomOption03MaxValue
        {
            get { return _randomOption03MaxValue; }
            set
            {
                if (_randomOption03MaxValue != value)
                {
                    _randomOption03MaxValue = value;
                    OnPropertyChanged(nameof(RandomOption03MaxValue));

                    if (RandomOption03Value > value)
                        RandomOption03Value = value;
                }
            }
        }


        private void UpdateRandomOption()
        {
            (RandomOption01MinValue, RandomOption01MaxValue) = _gmDatabaseService.GetOptionValue(RandomOption01);
            (RandomOption02MinValue, RandomOption02MaxValue) = _gmDatabaseService.GetOptionValue(RandomOption02);
            (RandomOption03MinValue, RandomOption03MaxValue) = _gmDatabaseService.GetOptionValue(RandomOption03);

            RandomOption01Value = CalculateOptionValue(RandomOption01, RandomOption01Value, RandomOption01MaxValue);
            RandomOption02Value = CalculateOptionValue(RandomOption02, RandomOption02Value, RandomOption02MaxValue);
            RandomOption03Value = CalculateOptionValue(RandomOption03, RandomOption03Value, RandomOption03MaxValue);
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

        private int _socketCount;
        public int SocketCount
        {
            get { return _socketCount; }
            set
            {
                if (_socketCount != value)
                {
                    _socketCount = value;
                    OnPropertyChanged(nameof(SocketCount));
                    OnPropertyChanged(nameof(SocketOption));
                    UpdateSocketOption();
                }
            }
        }


        private int _socketCountMax;
        public int SocketCountMax
        {
            get { return _socketCountMax; }
            set
            {

                if (_socketCountMax != value)
                {
                    _socketCountMax = value;
                    OnPropertyChanged(nameof(SocketCountMax));
                    UpdateSocketOption();
                }

            }
        }

        public string? SocketOption
        {
            get { return $"Socket: {SocketCount}"; }
        }

        private int _socket01Color;
        public int Socket01Color
        {
            get { return _socket01Color; }
            set
            {
                _socket01Color = value;
                OnPropertyChanged(nameof(Socket01Color));
                OnPropertyChanged(nameof(SocketOption01Text));
                OnPropertyChanged(nameof(SocketOption01Color));
                UpdateSocketOption();
            }
        }

        private int _socket02Color;
        public int Socket02Color
        {
            get { return _socket02Color; }
            set
            {
                _socket02Color = value;
                OnPropertyChanged(nameof(Socket02Color));
                OnPropertyChanged(nameof(SocketOption02Text));
                OnPropertyChanged(nameof(SocketOption02Color));
                UpdateSocketOption();
            }
        }

        private int _socket03Color;
        public int Socket03Color
        {
            get { return _socket03Color; }
            set
            {
                _socket03Color = value;
                OnPropertyChanged(nameof(Socket03Color));
                OnPropertyChanged(nameof(SocketOption03Text));
                OnPropertyChanged(nameof(SocketOption03Color));
                UpdateSocketOption();
            }
        }

        private int _socketOption01;
        public int SocketOption01
        {
            get { return _socketOption01; }
            set
            {
                if (_socketOption01 != value)
                {
                    _socketOption01 = value;
                    OnPropertyChanged(nameof(SocketOption01));
                    OnPropertyChanged(nameof(SocketOption01Text));
                    OnPropertyChanged(nameof(SocketOption01Color));
                    UpdateSocketOption();
                }
            }
        }

        private int _socketOption01Value;
        public int SocketOption01Value
        {
            get { return _socketOption01Value; }
            set
            {
                if (_socketOption01Value != value)
                {
                    _socketOption01Value = value;
                    OnPropertyChanged(nameof(SocketOption01Value));
                    OnPropertyChanged(nameof(SocketOption01Text));
                }
            }
        }

        public string? SocketOption01Text
        {
            get { return SocketOption01 != 0 ? _frameService.GetOptionName(SocketOption01, SocketOption01Value) : _frameService.GetSocketText(Socket01Color); }
        }

        public string? SocketOption01Color
        {
            get { return SocketOption01 != 0 ? _frameService.GetColorFromOption(SocketOption01) : _frameService.GetSocketColor(Socket01Color); }
        }

        private int _socketOption01MinValue;
        public int SocketOption01MinValue
        {
            get { return _socketOption01MinValue; }
            set
            {
                if (_socketOption01MinValue != value)
                {
                    _socketOption01MinValue = value;
                    OnPropertyChanged(nameof(SocketOption01MinValue));
                }
            }
        }

        private int _socketOption01MaxValue;
        public int SocketOption01MaxValue
        {
            get { return _socketOption01MaxValue; }
            set
            {
                if (_socketOption01MaxValue != value)
                {
                    _socketOption01MaxValue = value;
                    OnPropertyChanged(nameof(SocketOption01MaxValue));

                    if (SocketOption01Value > value)
                        SocketOption01Value = value;
                }
            }
        }

        private int _socketOption02;
        public int SocketOption02
        {
            get { return _socketOption02; }
            set
            {
                if (_socketOption02 != value)
                {
                    _socketOption02 = value;
                    OnPropertyChanged(nameof(SocketOption02));
                    OnPropertyChanged(nameof(SocketOption02Text));
                    OnPropertyChanged(nameof(SocketOption02Color));
                    UpdateSocketOption();
                }
            }
        }

        private int _socketOption02Value;
        public int SocketOption02Value
        {
            get { return _socketOption02Value; }
            set
            {
                if (_socketOption02Value != value)
                {
                    _socketOption02Value = value;
                    OnPropertyChanged(nameof(SocketOption02Value));
                    OnPropertyChanged(nameof(SocketOption02Text));
                }
            }
        }

        public string? SocketOption02Text
        {
            get { return SocketOption02 != 0 ? _frameService.GetOptionName(SocketOption02, SocketOption02Value) : _frameService.GetSocketText(Socket02Color); }
        }

        public string? SocketOption02Color
        {
            get { return SocketOption02 != 0 ? _frameService.GetColorFromOption(SocketOption02) : _frameService.GetSocketColor(Socket02Color); }
        }

        private int _socketOption02MinValue;
        public int SocketOption02MinValue
        {
            get { return _socketOption02MinValue; }
            set
            {
                if (_socketOption02MinValue != value)
                {
                    _socketOption02MinValue = value;
                    OnPropertyChanged(nameof(SocketOption02MinValue));
                }
            }
        }

        private int _socketOption02MaxValue;
        public int SocketOption02MaxValue
        {
            get { return _socketOption02MaxValue; }
            set
            {
                if (_socketOption02MaxValue != value)
                {
                    _socketOption02MaxValue = value;
                    OnPropertyChanged(nameof(SocketOption02MaxValue));

                    if (SocketOption02Value > value)
                        SocketOption02Value = value;
                }
            }
        }


        private int _socketOption03;
        public int SocketOption03
        {
            get { return _socketOption03; }
            set
            {
                if (_socketOption03 != value)
                {
                    _socketOption03 = value;
                    OnPropertyChanged(nameof(SocketOption03));
                    OnPropertyChanged(nameof(SocketOption03Text));
                    OnPropertyChanged(nameof(SocketOption03Color));
                    UpdateSocketOption();
                }
            }
        }

        private int _socketOption03Value;
        public int SocketOption03Value
        {
            get { return _socketOption03Value; }
            set
            {
                if (_socketOption03Value != value)
                {
                    _socketOption03Value = value;
                    OnPropertyChanged(nameof(SocketOption03Value));
                    OnPropertyChanged(nameof(SocketOption03Text));
                }
            }
        }

        public string? SocketOption03Text
        {
            get { return SocketOption03 != 0 ? _frameService.GetOptionName(SocketOption03, SocketOption03Value) : _frameService.GetSocketText(Socket03Color); }
        }

        public string? SocketOption03Color
        {
            get { return SocketOption03 != 0 ? _frameService.GetColorFromOption(SocketOption03) : _frameService.GetSocketColor(Socket03Color); }
        }

        private int _socketOption03MinValue;
        public int SocketOption03MinValue
        {
            get { return _socketOption03MinValue; }
            set
            {
                if (_socketOption03MinValue != value)
                {
                    _socketOption03MinValue = value;
                    OnPropertyChanged(nameof(SocketOption03MinValue));
                }
            }
        }

        private int _socketOption03MaxValue;
        public int SocketOption03MaxValue
        {
            get { return _socketOption03MaxValue; }
            set
            {
                if (_socketOption03MaxValue != value)
                {
                    _socketOption03MaxValue = value;
                    OnPropertyChanged(nameof(SocketOption03MaxValue));

                    if (SocketOption03Value > value)
                        SocketOption03Value = value;
                }
            }
        }

        private void UpdateSocketOption()
        {
            (SocketOption01MinValue, SocketOption01MaxValue) = _gmDatabaseService.GetOptionValue(SocketOption01);
            (SocketOption02MinValue, SocketOption02MaxValue) = _gmDatabaseService.GetOptionValue(SocketOption02);
            (SocketOption03MinValue, SocketOption03MaxValue) = _gmDatabaseService.GetOptionValue(SocketOption03);

            SocketOption01Value = CalculateOptionValue(SocketOption01, SocketOption01Value, SocketOption01MaxValue);
            SocketOption02Value = CalculateOptionValue(SocketOption02, SocketOption02Value, SocketOption02MaxValue);
            SocketOption03Value = CalculateOptionValue(SocketOption03, SocketOption03Value, SocketOption03MaxValue);
        }

        #endregion
    }
}
