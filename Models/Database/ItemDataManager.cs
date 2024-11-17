using RHToolkit.Models.MessageBox;
using RHToolkit.Models.SQLite;
using RHToolkit.Services;
using RHToolkit.ViewModels.Controls;
using RHToolkit.ViewModels.Windows.Database.VM;
using System.ComponentModel;
using static RHToolkit.Models.EnumService;

namespace RHToolkit.Models.Database;

/// <summary>
/// Manages item data operations and filtering.
/// </summary>
public partial class ItemDataManager : ObservableObject
{
    private readonly IFrameService _frameService;
    private readonly IGMDatabaseService _gmDatabaseService;
    private readonly CachedDataManager _cachedDataManager;
    private readonly System.Timers.Timer _itemFilterUpdateTimer;
    private readonly System.Timers.Timer _optionFilterUpdateTimer;

    /// <summary>
    /// Initializes a new instance of the <see cref="ItemDataManager"/> class.
    /// </summary>
    /// <param name="cachedDataManager">The cached data manager.</param>
    /// <param name="gmDatabaseService">The GM database service.</param>
    /// <param name="frameService">The frame service.</param>
    /// <param name="itemDataViewModel">The item data view model.</param>
    public ItemDataManager(CachedDataManager cachedDataManager, IGMDatabaseService gmDatabaseService, IFrameService frameService, ItemDataViewModel itemDataViewModel)
    {
        _frameService = frameService;
        _gmDatabaseService = gmDatabaseService;
        _cachedDataManager = cachedDataManager;
        _itemDataViewModel = itemDataViewModel;
        _itemFilterUpdateTimer = new()
        {
            Interval = 500,
            AutoReset = false
        };
        _itemFilterUpdateTimer.Elapsed += ItemFilterUpdateTimerElapsed;

        _optionFilterUpdateTimer = new()
        {
            Interval = 500,
            AutoReset = false
        };
        _optionFilterUpdateTimer.Elapsed += OptionFilterUpdateTimerElapsed;

        PopulateItemDataItems();
        PopulateOptionItems();
        PopulateSocketColorItems();
        PopulateItemTypeItemsFilter();
        PopulateCategoryItemsFilter(ItemType.Item);
        PopulateSubCategoryItemsFilter();
        PopulateClassItemsFilter();
        PopulateBranchItemsFilter();
        PopulateItemTradeItemsFilter();
        PopulateInventoryTypeItemsFilter();
        PopulateQuestConditionItems();
        PopulateQuestListItems();
        PopulateAddEffectItems();
        PopulateNpcShopItems();

        _itemDataView = new CollectionViewSource { Source = ItemDataItems }.View;
        _itemDataView.Filter = FilterItems;
        _optionView = CollectionViewSource.GetDefaultView(OptionItems);
        _optionView.Filter = FilterOption;
    }

    /// <summary>
    /// Initializes a collection of inventory items.
    /// </summary>
    /// <param name="itemCount">The number of items to initialize.</param>
    /// <param name="pageIndex">The page index for the items.</param>
    /// <returns>An observable collection of inventory items.</returns>
    public static ObservableCollection<InventoryItem> InitializeCollection(int itemCount, int pageIndex)
    {
        var collection = new ObservableCollection<InventoryItem>();

        for (int i = 0; i < itemCount; i++)
        {
            collection.Add(new InventoryItem
            {
                SlotIndex = i,
                PageIndex = pageIndex
            });
        }

        return collection;
    }

    [ObservableProperty]
    private ItemDataViewModel _itemDataViewModel;

    #region ItemData

    /// <summary>
    /// Checks if an item ID is invalid.
    /// </summary>
    /// <param name="itemID">The item ID to check.</param>
    /// <returns>True if the item ID is invalid, otherwise false.</returns>
    public bool IsInvalidItemID(int itemID)
    {
        return _cachedDataManager.CachedItemDataList == null || !_cachedDataManager.CachedItemDataList.Any(item => item.ItemId == itemID);
    }

    /// <summary>
    /// Creates a new item based on character data and new item data.
    /// </summary>
    /// <param name="characterData">The character data.</param>
    /// <param name="newItemData">The new item data.</param>
    /// <param name="pageIndex">The page index for the new item.</param>
    /// <returns>The created item data.</returns>
    public static ItemData CreateNewItem(CharacterData characterData, ItemData newItemData, int pageIndex)
    {
        var newItem = new ItemData
        {
            IsNewItem = true,
            CharacterId = characterData.CharacterID,
            AuthId = characterData.AuthID,
            ItemUid = Guid.NewGuid(),
            PageIndex = pageIndex, // 0 = Equip, 1 to 6 = Inventory, 11 = QuickSlot, 21 = Storage, 61 = Mail
            CreateTime = DateTime.Now,
            UpdateTime = DateTime.Now,
            ExpireTime = 0,
            RemainTime = 0,
            GCode = 1,
            LockPassword = "",
            LinkId = Guid.Empty,
            IsSeizure = 0,
            DefermentTime = 0,
            SlotIndex = newItemData.SlotIndex,
            ItemId = newItemData.ItemId,
            ItemName = newItemData.ItemName,
            IconName = newItemData.IconName,
            ItemAmount = newItemData.ItemAmount,
            Durability = newItemData.Durability,
            DurabilityMax = newItemData.DurabilityMax,
            EnhanceLevel = newItemData.EnhanceLevel,
            AugmentStone = newItemData.AugmentStone,
            Rank = newItemData.Rank,
            Weight = newItemData.Weight,
            Reconstruction = newItemData.Reconstruction,
            ReconstructionMax = newItemData.ReconstructionMax,
            Option1Code = newItemData.Option1Code,
            Option2Code = newItemData.Option2Code,
            Option3Code = newItemData.Option3Code,
            Option1Value = newItemData.Option1Value,
            Option2Value = newItemData.Option2Value,
            Option3Value = newItemData.Option3Value,
            SocketCount = newItemData.SocketCount,
            Socket1Color = newItemData.Socket1Color,
            Socket2Color = newItemData.Socket2Color,
            Socket3Color = newItemData.Socket3Color,
            Socket1Code = newItemData.Socket1Code,
            Socket2Code = newItemData.Socket2Code,
            Socket3Code = newItemData.Socket3Code,
            Socket1Value = newItemData.Socket1Value,
            Socket2Value = newItemData.Socket2Value,
            Socket3Value = newItemData.Socket3Value,
        };

        return newItem;
    }

    /// <summary>
    /// Gets the itemdataviewmodel for a specified item.
    /// </summary>
    /// <param name="item">The item data.</param>
    /// <returns>The item data view model.</returns>
    public ItemDataViewModel GetItemData(ItemData item)
    {
        // Find the corresponding ItemData in the _cachedItemDataList
        ItemData? cachedItem = _cachedDataManager.CachedItemDataList?.FirstOrDefault(i => i.ItemId == item.ItemId);

        ItemData itemData = new();

        if (cachedItem != null)
        {
            itemData = new()
            {
                ItemId = cachedItem.ItemId,
                ItemName = cachedItem.ItemName ?? "",
                Description = cachedItem.Description ?? "",
                IconName = cachedItem.IconName ?? "icon_empty_sprite",
                Type = cachedItem.Type,
                WeaponID00 = cachedItem.WeaponID00,
                Category = cachedItem.Category,
                SubCategory = cachedItem.SubCategory,
                LevelLimit = cachedItem.LevelLimit,
                ItemTrade = cachedItem.ItemTrade,
                InventoryType = cachedItem.InventoryType,
                OverlapCnt = cachedItem.OverlapCnt,
                BindingOff = cachedItem.BindingOff,
                Defense = cachedItem.Defense,
                MagicDefense = cachedItem.MagicDefense,
                Branch = cachedItem.Branch,
                OptionCountMax = cachedItem.OptionCountMax,
                SocketCountMax = cachedItem.SocketCountMax,
                SellPrice = cachedItem.SellPrice,
                PetFood = cachedItem.PetFood,
                JobClass = cachedItem.JobClass,
                SetId = cachedItem.SetId,
                TitleList = cachedItem.TitleList,
                Cooltime = cachedItem.Cooltime,
                FixOption1Code = cachedItem.FixOption1Code,
                FixOption1Value = cachedItem.FixOption1Value,
                FixOption2Code = cachedItem.FixOption2Code,
                FixOption2Value = cachedItem.FixOption2Value,

                IsNewItem = item.IsNewItem,
                ItemUid = item.ItemUid,
                ItemAmount = item.ItemAmount,
                CharacterId = item.CharacterId,
                AuthId = item.AuthId,
                PageIndex = item.PageIndex,
                SlotIndex = item.SlotIndex,
                Reconstruction = item.Reconstruction,
                ReconstructionMax = item.ReconstructionMax,
                AugmentStone = item.AugmentStone,
                Rank = item.Rank,
                AcquireRoute = item.AcquireRoute,
                Physical = item.Physical,
                Magical = item.Magical,
                DurabilityMax = item.DurabilityMax,
                Weight = item.Weight,
                RemainTime = item.RemainTime,
                CreateTime = item.CreateTime,
                UpdateTime = item.UpdateTime,
                GCode = item.GCode,
                Durability = item.Durability,
                EnhanceLevel = item.EnhanceLevel,
                Option1Code = item.Option1Code,
                Option1Value = item.Option1Value,
                Option2Code = item.Option2Code,
                Option2Value = item.Option2Value,
                Option3Code = item.Option3Code,
                Option3Value = item.Option3Value,
                OptionGroup = item.OptionGroup,
                SocketCount = item.SocketCount,
                Socket1Code = item.Socket1Code,
                Socket1Value = item.Socket1Value,
                Socket2Code = item.Socket2Code,
                Socket2Value = item.Socket2Value,
                Socket3Code = item.Socket3Code,
                Socket3Value = item.Socket3Value,
                ExpireTime = item.ExpireTime,
                LockPassword = item.LockPassword,
                LinkId = item.LinkId,
                IsSeizure = item.IsSeizure,
                Socket1Color = item.Socket1Color,
                Socket2Color = item.Socket2Color,
                Socket3Color = item.Socket3Color,
                DefermentTime = item.DefermentTime,
            };
        }
        else
        {
            itemData = new()
            {
                SlotIndex = item.SlotIndex,
                ItemId = item.ItemId,
                ItemName = $"Unknown Item ({item.ItemId})",
                IconName = "icon_empty_def",
                Type = 1
            };
        }

        var itemDataViewModel = new ItemDataViewModel(_frameService, _gmDatabaseService)
        {
            ItemData = itemData
        };

        return itemDataViewModel;
    }

    /// <summary>
    /// Updates existing item data with new item data.
    /// </summary>
    /// <param name="existingItem">The existing item data.</param>
    /// <param name="newItem">The new item data.</param>
    /// <returns>The updated item data.</returns>
    public static ItemData UpdateItemData(ItemData existingItem, ItemData newItem)
    {
        existingItem.IsEditedItem = !existingItem.IsNewItem;
        existingItem.UpdateTime = DateTime.Now;
        existingItem.ItemAmount = newItem.ItemAmount;
        existingItem.Durability = newItem.Durability;
        existingItem.DurabilityMax = newItem.DurabilityMax;
        existingItem.EnhanceLevel = newItem.EnhanceLevel;
        existingItem.AugmentStone = newItem.AugmentStone;
        existingItem.Rank = newItem.Rank;
        existingItem.Weight = newItem.Weight;
        existingItem.Reconstruction = newItem.Reconstruction;
        existingItem.ReconstructionMax = newItem.ReconstructionMax;
        existingItem.ItemAmount = newItem.ItemAmount;
        existingItem.Option1Code = newItem.Option1Code;
        existingItem.Option2Code = newItem.Option2Code;
        existingItem.Option3Code = newItem.Option3Code;
        existingItem.Option1Value = newItem.Option1Value;
        existingItem.Option2Value = newItem.Option2Value;
        existingItem.Option3Value = newItem.Option3Value;
        existingItem.SocketCount = newItem.SocketCount;
        existingItem.Socket1Color = newItem.Socket1Color;
        existingItem.Socket2Color = newItem.Socket2Color;
        existingItem.Socket3Color = newItem.Socket3Color;
        existingItem.Socket1Code = newItem.Socket1Code;
        existingItem.Socket2Code = newItem.Socket2Code;
        existingItem.Socket3Code = newItem.Socket3Code;
        existingItem.Socket1Value = newItem.Socket1Value;
        existingItem.Socket2Value = newItem.Socket2Value;
        existingItem.Socket3Value = newItem.Socket3Value;

        return existingItem;
    }

    /// <summary>
    /// Gets the itemdataviewmodel for a specified item ID, slot index, and item amount.
    /// </summary>
    /// <param name="itemId">The item ID.</param>
    /// <param name="slotIndex">The slot index.</param>
    /// <param name="itemAmount">The item amount.</param>
    /// <returns>The item data view model.</returns>
    public ItemDataViewModel? GetItemDataViewModel(int itemId, int slotIndex, int itemAmount)
    {
        if (itemId != 0)
        {
            ItemData itemData = new()
            {
                ItemId = itemId,
                SlotIndex = slotIndex,
                ItemAmount = itemAmount
            };

            var itemDataViewModel = GetItemData(itemData);
            return itemDataViewModel;
        }
        return null;
    }

    /// <summary>
    /// Gets the real class value for a specified character class.
    /// </summary>
    /// <param name="charClass">The character class value.</param>
    /// <returns>The real class value.</returns>
    public static int GetRealClass(int charClass)
    {
        return charClass switch
        {
            1 or 101 or 102 => 1,
            2 or 201 => 2,
            3 or 301 => 3,
            4 or 401 => 4,
            _ => 0,
        };
    }

    /// <summary>
    /// Gets the shop icon name for a specified icon name.
    /// </summary>
    /// <param name="iconName">The icon name.</param>
    /// <returns>The shop icon name.</returns>
    public static string GetShopIcon(string iconName)
    {
        if (string.IsNullOrEmpty(iconName))
        {
            return "question_icon";
        }

        string? shopIcon = FindShopImage(iconName);

        if (!string.IsNullOrEmpty(shopIcon))
        {
            return shopIcon;
        }
        else
        {
            return iconName;
        }
    }

    /// <summary>
    /// Finds the shop image for a specified icon name.
    /// </summary>
    /// <param name="iconName">The icon name.</param>
    /// <returns>The shop image name, or null if not found.</returns>
    private static string? FindShopImage(string iconName)
    {
        string shopIcon = $"shop_{iconName}";
        string resourcePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources");

        string[] files = Directory.GetFiles(resourcePath, shopIcon.ToLower() + ".png", SearchOption.AllDirectories);

        if (files.Length > 0)
        {
            return shopIcon;
        }
        else
        {
            return null;
        }
    }

    #endregion

    #region Item Data List

    [ObservableProperty]
    private List<ItemData>? _itemDataItems;

    /// <summary>
    /// Populates the item data items from the cached data manager.
    /// </summary>
    private void PopulateItemDataItems()
    {
        try
        {
            ItemDataItems = _cachedDataManager.CachedItemDataList;
        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
        }
    }

    [ObservableProperty]
    private List<NameID>? _optionItems;

    /// <summary>
    /// Populates the option items from the cached data manager.
    /// </summary>
    private void PopulateOptionItems()
    {
        try
        {
            OptionItems = _cachedDataManager.CachedOptionItems;
        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
        }
    }

    [ObservableProperty]
    private ICollectionView _optionView;

    private readonly List<int> selectedOptions = [];
    private bool FilterOption(object obj)
    {
        if (obj is NameID option)
        {
            if (option.ID == 0)
                return true;

            if (ItemDataViewModel != null)
            {
                selectedOptions.Add(ItemDataViewModel.RandomOption01);
                selectedOptions.Add(ItemDataViewModel.RandomOption02);
                selectedOptions.Add(ItemDataViewModel.RandomOption03);
                selectedOptions.Add(ItemDataViewModel.SocketOption01);
                selectedOptions.Add(ItemDataViewModel.SocketOption02);
                selectedOptions.Add(ItemDataViewModel.SocketOption03);

                if (selectedOptions.Contains(option.ID))
                    return true;
            }

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
        _optionFilterUpdateTimer.Stop();
        _optionFilterUpdateTimer.Start();
    }

    #endregion

    #region CollectionView Filter

    [ObservableProperty]
    private ICollectionView _itemDataView;

    private bool FilterItems(object obj)
    {
        if (obj is ItemData item)
        {
            //combobox filter 
            if (ItemTypesFilter.Count != 0 && !ItemTypesFilter.Contains(item.Type))
                return false;

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

            if (ItemTradeFilter != -1 && item.ItemTrade != ItemTradeFilter)
                return false;

            if (InventoryTypeFilter != 0 && item.InventoryType != InventoryTypeFilter)
                return false;

            if (AccountStorageFilter != 0 && item.AccountStorage != AccountStorageFilter)
                return false;

            // text search filter
            if (!string.IsNullOrEmpty(SearchText))
            {
                string searchText = SearchText.ToLower();

                // Check if either item ID or item Name contains the search text
                if (!string.IsNullOrEmpty(item.ItemId.ToString()) && item.ItemId.ToString().Contains(searchText, StringComparison.CurrentCultureIgnoreCase))
                    return true;

                if (!string.IsNullOrEmpty(item.ItemName) && item.ItemName.Contains(searchText, StringComparison.CurrentCultureIgnoreCase))
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
        _itemFilterUpdateTimer.Stop();
        _itemFilterUpdateTimer.Start();
    }

    private void ItemFilterUpdateTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        RefreshView();
    }

    private void OptionFilterUpdateTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        Application.Current.Dispatcher.Invoke(OptionView.Refresh);
    }

    private void RefreshView()
    {
        Application.Current.Dispatcher.Invoke(ItemDataView.Refresh);
    }

    #endregion

    #region Comboboxes Filter

    [ObservableProperty]
    private List<NameID>? _categoryFilterItems;

    [ObservableProperty]
    private List<NameID>? _subCategoryItemsFilter;

    /// <summary>
    /// Populates the category items filter based on the specified item type.
    /// </summary>
    /// <param name="itemType">The item type.</param>
    private void PopulateCategoryItemsFilter(ItemType itemType)
    {
        try
        {
            CategoryFilterItems = _gmDatabaseService.GetCategoryItems(itemType, false);
            SubCategoryItemsFilter = _gmDatabaseService.GetCategoryItems(itemType, true);

            if (CategoryFilterItems.Count > 0)
            {
                ItemCategoryFilter = CategoryFilterItems.First().ID;
                OnPropertyChanged(nameof(ItemCategoryFilter));
            }
            if (SubCategoryItemsFilter.Count > 0)
            {
                ItemSubCategoryFilter = SubCategoryItemsFilter.First().ID;
                OnPropertyChanged(nameof(ItemSubCategoryFilter));
            }
        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
        }
    }

    [ObservableProperty]
    private List<NameID>? _subCategory02ItemsFilter;

    /// <summary>
    /// Populates the sub-category items filter.
    /// </summary>
    private void PopulateSubCategoryItemsFilter()
    {
        try
        {
            SubCategory02ItemsFilter = _gmDatabaseService.GetSubCategoryItems();
        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
        }
    }

    [ObservableProperty]
    private int _itemTypeFilter;
    partial void OnItemTypeFilterChanged(int value)
    {
        PopulateCategoryItemsFilter((ItemType)value);
        RefreshView();
    }

    [ObservableProperty]
    private bool _itemTypeEnabled = true;

    [ObservableProperty]
    private List<int> _itemTypesFilter = [];

    partial void OnItemTypesFilterChanged(List<int> value)
    {
        RefreshView();
    }

    [ObservableProperty]
    private int _itemCategoryFilter;
    partial void OnItemCategoryFilterChanged(int value)
    {
        RefreshView();
    }

    [ObservableProperty]
    private bool _itemCategoryEnabled = true;

    [ObservableProperty]
    private int _itemSubCategoryFilter;
    partial void OnItemSubCategoryFilterChanged(int value)
    {
        RefreshView();
    }

    [ObservableProperty]
    private bool _itemSubCategoryEnabled = true;

    [ObservableProperty]
    private int _itemTradeFilter = -1;
    partial void OnItemTradeFilterChanged(int value)
    {
        RefreshView();
    }

    [ObservableProperty]
    private int _itemClassFilter;

    partial void OnItemClassFilterChanged(int value)
    {
        RefreshView();
    }

    [ObservableProperty]
    private bool _itemClassEnabled = true;

    [ObservableProperty]
    private int _itemBranchFilter;

    partial void OnItemBranchFilterChanged(int value)
    {
        RefreshView();
    }

    [ObservableProperty]
    private List<NameID>? _itemTypeItemsFilter;

    /// <summary>
    /// Populates the item type items filter.
    /// </summary>
    private void PopulateItemTypeItemsFilter()
    {
        try
        {
            ItemTypeItemsFilter = GetEnumItems<ItemType>();

            if (ItemTypeItemsFilter.Count > 0)
            {
                ItemTypeFilter = 0;
            }
        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
        }
    }

    [ObservableProperty]
    private List<NameID>? _classItems;

    [ObservableProperty]
    private List<NameID>? _classItemsFilter;

    /// <summary>
    /// Populates the class items filter.
    /// </summary>
    private void PopulateClassItemsFilter()
    {
        try
        {
            ClassItems = GetEnumItems<CharClass>(false);
            ClassItemsFilter = GetEnumItems<CharClass>(true);

            if (ClassItemsFilter.Count > 0)
            {
                ItemClassFilter = 0;
            }
        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
        }
    }

    [ObservableProperty]
    private List<NameID>? _branchItemsFilter;

    /// <summary>
    /// Populates the branch items filter.
    /// </summary>
    private void PopulateBranchItemsFilter()
    {
        try
        {
            BranchItemsFilter = GetEnumItems<Branch>(true);

            if (BranchItemsFilter.Count > 0)
            {
                ItemBranchFilter = 0;
            }
        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
        }
    }

    [ObservableProperty]
    private int _inventoryTypeFilter;
    partial void OnInventoryTypeFilterChanged(int value)
    {
        RefreshView();
    }

    [ObservableProperty]
    private List<NameID>? _inventoryTypeItems;

    /// <summary>
    /// Populates the inventory type items filter.
    /// </summary>
    private void PopulateInventoryTypeItemsFilter()
    {
        try
        {
            InventoryTypeItems =
            [
                new NameID { ID = 0, Name = Resources.None },
                new NameID { ID = 1, Name = Resources.Equipment },
                new NameID { ID = 2, Name = Resources.Consume },
                new NameID { ID = 3, Name = Resources.Other },
                new NameID { ID = 4, Name = Resources.Quest },
                new NameID { ID = 5, Name = Resources.Costume },
                new NameID { ID = 6, Name = Resources.TriggerItem }
            ];
        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
        }
    }

    [ObservableProperty]
    private int _accountStorageFilter;
    partial void OnAccountStorageFilterChanged(int value)
    {
        RefreshView();
    }

    [ObservableProperty]
    private List<NameID>? _itemTradeFilterItems;

    /// <summary>
    /// Populates the item trade items filter.
    /// </summary>
    private void PopulateItemTradeItemsFilter()
    {
        try
        {
            ItemTradeFilterItems =
            [
                new NameID { ID = -1, Name = Resources.All },
                new NameID { ID = 0, Name = Resources.Untradeable },
                new NameID { ID = 1, Name = Resources.Tradeable }
            ];
        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
        }
    }

    [ObservableProperty]
    private List<NameID>? _socketColorItems;

    /// <summary>
    /// Populates the socket color items.
    /// </summary>
    private void PopulateSocketColorItems()
    {
        try
        {
            SocketColorItems = GetSocketColorItems();
        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
        }
    }

    [ObservableProperty]
    private List<NameID>? _questConditionItems;

    /// <summary>
    /// Populates the quest condition items.
    /// </summary>
    private void PopulateQuestConditionItems()
    {
        try
        {
            QuestConditionItems =
            [
                new NameID { ID = 0, Name = Resources.None },
                new NameID { ID = 1, Name = "In Progress" },
                new NameID { ID = 2, Name = "Paused" },
                new NameID { ID = 100, Name = "Finished" },
                new NameID { ID = 255, Name = "Completed" }
            ];
        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
        }
    }

    [ObservableProperty]
    private List<NameID>? _questListItems;

    /// <summary>
    /// Populates the quest list items.
    /// </summary>
    private void PopulateQuestListItems()
    {
        try
        {
            QuestListItems = _gmDatabaseService.GetQuestListItems();
        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
        }
    }

    [ObservableProperty]
    private List<NameID>? _addEffectItems;

    /// <summary>
    /// Populates the add effect items.
    /// </summary>
    private void PopulateAddEffectItems()
    {
        try
        {
            AddEffectItems = _gmDatabaseService.GetAddEffectItems();
        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
        }
    }

    [ObservableProperty]
    private List<NameID>? _npcShopItems;

    /// <summary>
    /// Populates the NPC shop items.
    /// </summary>
    private void PopulateNpcShopItems()
    {
        try
        {
            NpcShopItems = _gmDatabaseService.GetNpcShopItems();
        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
        }
    }
    #endregion
}