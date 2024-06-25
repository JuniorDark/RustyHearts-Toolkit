using RHToolkit.Messages;
using RHToolkit.Models;
using RHToolkit.Models.Database;
using RHToolkit.Models.MessageBox;
using RHToolkit.Models.SQLite;
using RHToolkit.Properties;
using RHToolkit.Services;
using RHToolkit.ViewModels.Controls;
using System.ComponentModel;
using static RHToolkit.Models.EnumService;

namespace RHToolkit.ViewModels.Windows;

public partial class ItemWindowViewModel : ObservableObject, IRecipient<ItemDataMessage>, IRecipient<CharacterInfoMessage>
{
    private readonly IGMDatabaseService _gmDatabaseService;
    private readonly CachedDataManager _cachedDataManager;
    private readonly ItemHelper _itemHelper;
    private readonly System.Timers.Timer _searchTimer;

    public ItemWindowViewModel(IGMDatabaseService gmDatabaseService, CachedDataManager cachedDataManager, ItemHelper itemHelper)
    {
        _gmDatabaseService = gmDatabaseService;
        _cachedDataManager = cachedDataManager;
        _itemHelper = itemHelper;
        _searchTimer = new()
        {
            Interval = 1000,
            AutoReset = false
        };
        _searchTimer.Elapsed += SearchTimerElapsed;

        PopulateItemDataItems();
        PopulateOptionItems();
        PopulateSocketColorItems();
        PopulateItemTypeItemsFilter();
        PopulateCategoryItemsFilter(ItemType.Item);
        PopulateClassItemsFilter();
        PopulateBranchItemsFilter();
        PopulateItemTradeItemsFilter();

        _itemDataView = new CollectionViewSource { Source = ItemDataItems }.View;
        _itemDataView.Filter = FilterItems;
        _optionView = CollectionViewSource.GetDefaultView(OptionItems);
        _optionView.Filter = FilterOption;
        ItemTradeFilter = 2;

        WeakReferenceMessenger.Default.Register<CharacterInfoMessage>(this);
        WeakReferenceMessenger.Default.Register<ItemDataMessage>(this);
    }

    #region Messenger

    #region Receive CharacterInfo
    [ObservableProperty]
    private CharacterInfo? _characterInfo;

    [ObservableProperty]
    private Guid? _token = Guid.Empty;

    public void Receive(CharacterInfoMessage message)
    {
        if (Token == Guid.Empty)
        {
            Token = message.Token;

            CharacterInfo = null;
            CharacterInfo = message.Value;
        }

        WeakReferenceMessenger.Default.Unregister<CharacterInfoMessage>(this);
    }

    #endregion

    #region Send ItemData
    [RelayCommand(CanExecute = nameof(CanExecuteCommand))]
    private void SelectItem(object parameter)
    {
        if (FrameViewModel != null)
        {
            var itemData = FrameViewModel.GetItemData();

            if (itemData != null)
            {
                switch (MessageType)
                {
                    case "Mail":
                        WeakReferenceMessenger.Default.Send(new ItemDataMessage(itemData, "MailWindow", MessageType, Token));
                        break;
                    case "EquipItem":
                        WeakReferenceMessenger.Default.Send(new ItemDataMessage(itemData, "EquipWindow", MessageType, Token));
                        break;
                    case "InventoryItem":
                        WeakReferenceMessenger.Default.Send(new ItemDataMessage(itemData, "InventoryWindow", MessageType, Token));
                        break;
                    case "Coupon":
                        WeakReferenceMessenger.Default.Send(new ItemDataMessage(itemData, "CouponWindow", MessageType, Token));
                        break;
                    default:
                        break;
                }
            }
            
        }
    }

    private bool CanExecuteCommand()
    {
        return SelectedItem != null;
    }
    #endregion

    #region Receive ItemData
    [ObservableProperty]
    private string? _messageType;

    public void Receive(ItemDataMessage message)
    {
        if (Token == Guid.Empty)
        {
            Token = message.Token;
        }

        if (message.Recipient == "ItemWindow" && message.Token == Token)
        {
            var itemData = message.Value;
            MessageType = message.MessageType;

            Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() =>
            {
                Title = GetTitle(MessageType, itemData);

                if (message.MessageType == "Mail")
                {
                    SlotIndexMin = 0;
                    SlotIndexMax = 2;
                }
                else if (message.MessageType == "EquipItem")
                {
                    SlotIndexMin = itemData.SlotIndex;
                    SlotIndexMax = itemData.SlotIndex;
                    SlotFilter(itemData.SlotIndex);
                }
                else if (message.MessageType == "InventoryItem")
                {
                    SlotIndexMin = itemData.SlotIndex;
                    SlotIndexMax = itemData.SlotIndex;
                    InventoryTypeFilter = itemData.PageIndex;
                }
                else
                {
                    SlotIndexMin = itemData.SlotIndex;
                    SlotIndexMax = itemData.SlotIndex;
                }

                if (itemData.ItemId != 0)
                {
                    SelectedItem = ItemDataItems?.FirstOrDefault(item => item.ItemId == itemData.ItemId);
                    LoadItemData(itemData);
                }
                else
                {
                    if (FrameViewModel != null)
                    {
                        FrameViewModel.PageIndex = itemData.PageIndex;
                        FrameViewModel.SlotIndex = itemData.SlotIndex;
                    }
                    
                }
            }), DispatcherPriority.ContextIdle);
        }
    }

    private string GetTitle(string? messageType, ItemData itemData)
    {
        return messageType switch
        {
            "Coupon" => $"Add Coupon Item",
            "EquipItem" => $"Add Equipment Item ({(EquipCategory)itemData.SlotIndex}) [{CharacterInfo?.CharacterName}] ",
            "InventoryItem" => $"Add Inventory Item ({(InventoryType)itemData.PageIndex}) [{CharacterInfo?.CharacterName}] ",
            "Mail" => $"Add Mail Item",
            _ => "Add Item",
        };
    }

    #endregion

    #endregion

    #region Load ItemData

    private void SlotFilter(int slotIndex)
    {
        if (CharacterInfo != null)
        {
            switch (slotIndex)
            {
                //Equipment
                case 0: // Weapon
                    ItemTypeFilter = 4;
                    ItemSubCategoryFilter = 1;
                    ItemClassFilter = ItemHelper.GetRealClass(CharacterInfo.Class);
                    ItemTypeEnabled = false;
                    ItemSubCategoryEnabled = false;
                    ItemClassEnabled = false;
                    break;
                case 1: //Chest
                    ItemTypeFilter = 3;
                    ItemSubCategoryFilter = 2;
                    ItemClassFilter = 0;
                    ItemTypeEnabled = false;
                    ItemSubCategoryEnabled = false;
                    ItemClassEnabled = true;
                    break;
                case 2: //Head
                    ItemTypeFilter = 3;
                    ItemSubCategoryFilter = 3;
                    ItemClassFilter = 0;
                    ItemTypeEnabled = false;
                    ItemSubCategoryEnabled = false;
                    ItemClassEnabled = true;
                    break;
                case 3: //Legs
                    ItemTypeFilter = 3;
                    ItemSubCategoryFilter = 4;
                    ItemClassFilter = 0;
                    ItemTypeEnabled = false;
                    ItemSubCategoryEnabled = false;
                    ItemClassEnabled = true;
                    break;
                case 4: //Feet
                    ItemTypeFilter = 3;
                    ItemSubCategoryFilter = 5;
                    ItemClassFilter = 0;
                    ItemTypeEnabled = false;
                    ItemSubCategoryEnabled = false;
                    ItemClassEnabled = true;
                    break;
                case 5: //Waist
                    ItemTypeFilter = 3;
                    ItemSubCategoryFilter = 6;
                    ItemClassFilter = 0;
                    ItemTypeEnabled = false;
                    ItemSubCategoryEnabled = false;
                    ItemClassEnabled = true;
                    break;
                case 6: //Necklace
                    ItemTypeFilter = 3;
                    ItemSubCategoryFilter = 7;
                    ItemClassFilter = 0;
                    ItemTypeEnabled = false;
                    ItemSubCategoryEnabled = false;
                    ItemClassEnabled = true;
                    break;
                case 7: //Earrings
                    ItemTypeFilter = 3;
                    ItemSubCategoryFilter = 8;
                    ItemClassFilter = 0;
                    ItemTypeEnabled = false;
                    ItemSubCategoryEnabled = false;
                    ItemClassEnabled = true;
                    break;
                case 8: //Ring
                    ItemTypeFilter = 3;
                    ItemSubCategoryFilter = 9;
                    ItemClassFilter = 0;
                    ItemTypeEnabled = false;
                    ItemSubCategoryEnabled = false;
                    ItemClassEnabled = true;
                    break;
                case 9: //Hands
                    ItemTypeFilter = 3;
                    ItemCategoryFilter = 0;
                    ItemSubCategoryFilter = 10;
                    ItemClassFilter = 0;
                    ItemTypeEnabled = false;
                    ItemSubCategoryEnabled = false;
                    ItemClassEnabled = true;
                    break;
                //Costume
                case 10: //Hair
                    ItemTypeFilter = 2;
                    ItemSubCategoryFilter = 11;
                    ItemClassFilter = CharacterInfo.Class;
                    ItemTypeEnabled = false;
                    ItemSubCategoryEnabled = false;
                    ItemClassEnabled = false;
                    break;
                case 11: //Face
                    ItemTypeFilter = 2;
                    ItemSubCategoryFilter = 12;
                    ItemClassFilter = 0;
                    ItemTypeEnabled = false;
                    ItemSubCategoryEnabled = false;
                    ItemClassEnabled = true;
                    break;
                case 12: //Neck
                    ItemTypeFilter = 2;
                    ItemSubCategoryFilter = 13;
                    ItemClassFilter = CharacterInfo.Class;
                    ItemTypeEnabled = false;
                    ItemSubCategoryEnabled = false;
                    ItemClassEnabled = false;
                    break;
                case 13: //Outerwear
                    ItemTypeFilter = 2;
                    ItemSubCategoryFilter = 14;
                    ItemClassFilter = CharacterInfo.Class;
                    ItemTypeEnabled = false;
                    ItemSubCategoryEnabled = false;
                    ItemClassEnabled = false;
                    break;
                case 14: //Top
                    ItemTypeFilter = 2;
                    ItemSubCategoryFilter = 15;
                    ItemClassFilter = CharacterInfo.Class;
                    ItemTypeEnabled = false;
                    ItemSubCategoryEnabled = false;
                    ItemClassEnabled = false;
                    break;
                case 15: //Bottom
                    ItemTypeFilter = 2;
                    ItemSubCategoryFilter = 16;
                    ItemClassFilter = CharacterInfo.Class;
                    ItemTypeEnabled = false;
                    ItemSubCategoryEnabled = false;
                    ItemClassEnabled = false;
                    break;
                case 16: //Gloves
                    ItemTypeFilter = 2;
                    ItemSubCategoryFilter = 17;
                    ItemClassFilter = CharacterInfo.Class;
                    ItemTypeEnabled = false;
                    ItemSubCategoryEnabled = false;
                    ItemClassEnabled = false;
                    break;
                case 17: //Shoes
                    ItemTypeFilter = 2;
                    ItemSubCategoryFilter = 18;
                    ItemClassFilter = CharacterInfo.Class;
                    ItemTypeEnabled = false;
                    ItemSubCategoryEnabled = false;
                    ItemClassEnabled = false;
                    break;
                case 18: //Accessory 1
                    ItemTypeFilter = 2;
                    ItemSubCategoryFilter = 19;
                    ItemClassFilter = CharacterInfo.Class;
                    ItemTypeEnabled = false;
                    ItemSubCategoryEnabled = false;
                    ItemClassEnabled = false;
                    break;
                case 19: //Accessory 2
                    ItemTypeFilter = 2;
                    ItemSubCategoryFilter = 20;
                    ItemClassFilter = 0;
                    ItemTypeEnabled = false;
                    ItemSubCategoryEnabled = false;
                    ItemClassEnabled = true;
                    break;
            }
        }
    }

    private void LoadItemData(ItemData itemData)
    {
        var frameViewModel = _itemHelper.GetItemData(itemData);

        FrameViewModel = frameViewModel;

        FrameViewModel.SlotIndex = itemData.SlotIndex;
        FrameViewModel.ItemId = itemData.ItemId;
        FrameViewModel.ItemAmount = itemData.ItemAmount;
        FrameViewModel.MaxDurability = itemData.DurabilityMax;
        FrameViewModel.EnhanceLevel = itemData.EnhanceLevel;
        FrameViewModel.AugmentValue = itemData.AugmentStone;
        FrameViewModel.Rank = itemData.Rank;
        FrameViewModel.Weight = itemData.Weight;
        FrameViewModel.ReconstructionMax = itemData.ReconstructionMax;
        FrameViewModel.Reconstruction = itemData.Reconstruction;
        FrameViewModel.RandomOption01 = itemData.Option1Code;
        FrameViewModel.RandomOption02 = itemData.Option2Code;
        FrameViewModel.RandomOption03 = itemData.Option3Code;
        FrameViewModel.RandomOption01Value = itemData.Option1Value;
        FrameViewModel.RandomOption02Value = itemData.Option2Value;
        FrameViewModel.RandomOption03Value = itemData.Option3Value;
        FrameViewModel.SocketCount = itemData.SocketCount;
        FrameViewModel.Socket01Color = itemData.Socket1Color;
        FrameViewModel.Socket02Color = itemData.Socket2Color;
        FrameViewModel.Socket03Color = itemData.Socket3Color;
        FrameViewModel.SocketOption01 = itemData.Socket1Code;
        FrameViewModel.SocketOption02 = itemData.Socket2Code;
        FrameViewModel.SocketOption03 = itemData.Socket3Code;
        FrameViewModel.SocketOption01Value = itemData.Socket1Value;
        FrameViewModel.SocketOption02Value = itemData.Socket2Value;
        FrameViewModel.SocketOption03Value = itemData.Socket3Value;

    }

    private void UpdateItemData(ItemData itemData)
    {
        if (FrameViewModel != null)
        {
            FrameViewModel.ItemId = itemData.ItemId;
            FrameViewModel.ItemName = itemData.ItemName;
            FrameViewModel.Description = itemData.Description;
            FrameViewModel.ItemBranch = itemData.Branch;
            FrameViewModel.IconName = itemData.IconName;
            FrameViewModel.ItemTrade = itemData.ItemTrade;
            FrameViewModel.MaxDurability = itemData.Durability;
            FrameViewModel.Weight = itemData.Weight;
            FrameViewModel.ReconstructionMax = itemData.ReconstructionMax;
            FrameViewModel.Reconstruction = itemData.ReconstructionMax;
            FrameViewModel.OverlapCnt = itemData.OverlapCnt;
            FrameViewModel.ItemAmount = itemData.ItemAmount;
            FrameViewModel.Rank = itemData.Rank;
            FrameViewModel.Type = itemData.Type;
            FrameViewModel.Category = itemData.Category;
            FrameViewModel.SubCategory = itemData.SubCategory;
            FrameViewModel.JobClass = itemData.JobClass;
            FrameViewModel.Defense = itemData.Defense;
            FrameViewModel.MagicDefense = itemData.MagicDefense;
            FrameViewModel.WeaponID00 = itemData.WeaponID00;
            FrameViewModel.SellPrice = itemData.SellPrice;
            FrameViewModel.RequiredLevel = itemData.LevelLimit;
            FrameViewModel.SetId = itemData.SetId;
            FrameViewModel.PetFood = itemData.PetFood;
            FrameViewModel.FixedOption01 = itemData.FixOption1Code;
            FrameViewModel.FixedOption01Value = itemData.FixOption1Value;
            FrameViewModel.FixedOption02 = itemData.FixOption2Code;
            FrameViewModel.FixedOption02Value = itemData.FixOption2Value;
            FrameViewModel.OptionCountMax = itemData.Type != 1 ? itemData.OptionCountMax : (itemData.Type == 1 && itemData.Category == 29 ? 1 : 0);
            FrameViewModel.SocketCountMax = itemData.SocketCountMax;
            FrameViewModel.SocketCount = itemData.SocketCountMax;
        }
        else
        {
            var frameViewModel = _itemHelper.GetItemData(itemData);
            FrameViewModel = frameViewModel;
            ItemName = frameViewModel.ItemName;
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
            ItemDataItems = _cachedDataManager.CachedItemDataList;
        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
        }
    }

    [ObservableProperty]
    private List<NameID>? _optionItems;

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

            if (FrameViewModel != null)
            {
                selectedOptions.Add(FrameViewModel.RandomOption01);
                selectedOptions.Add(FrameViewModel.RandomOption02);
                selectedOptions.Add(FrameViewModel.RandomOption03);
                selectedOptions.Add(FrameViewModel.SocketOption01);
                selectedOptions.Add(FrameViewModel.SocketOption02);
                selectedOptions.Add(FrameViewModel.SocketOption03);

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
        _searchTimer.Stop();
        _searchTimer.Start();
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

            if (InventoryTypeFilter != 0 && item.InventoryType != InventoryTypeFilter)
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
        _searchTimer.Stop();
        _searchTimer.Start();
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
    private int _itemTypeFilter;
    partial void OnItemTypeFilterChanged(int value)
    {
        PopulateCategoryItemsFilter((ItemType)value);
        ItemDataView.Refresh();
    }

    [ObservableProperty]
    private bool _itemTypeEnabled = true;

    [ObservableProperty]
    private int _itemCategoryFilter;
    partial void OnItemCategoryFilterChanged(int value)
    {
        ItemDataView.Refresh();
    }

    [ObservableProperty]
    private bool _itemCategoryEnabled = true;

    [ObservableProperty]
    private int _itemSubCategoryFilter;
    partial void OnItemSubCategoryFilterChanged(int value)
    {
        ItemDataView.Refresh();
    }

    [ObservableProperty]
    private bool _itemSubCategoryEnabled = true;

    [ObservableProperty]
    private int _itemTradeFilter;
    partial void OnItemTradeFilterChanged(int value)
    {
        ItemDataView.Refresh();
    }

    [ObservableProperty]
    private int _itemClassFilter;

    partial void OnItemClassFilterChanged(int value)
    {
        ItemDataView.Refresh();
    }

    [ObservableProperty]
    private bool _itemClassEnabled = true;

    [ObservableProperty]
    private int _itemBranchFilter;

    partial void OnItemBranchFilterChanged(int value)
    {
        ItemDataView.Refresh();
    }

    [ObservableProperty]
    private List<NameID>? _itemTypeItemsFilter;

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
    private List<NameID>? _classItemsFilter;

    private void PopulateClassItemsFilter()
    {
        try
        {
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
        ItemDataView.Refresh();
    }

    [ObservableProperty]
    private List<NameID>? _itemTradeFilterItems;

    private void PopulateItemTradeItemsFilter()
    {
        try
        {
            ItemTradeFilterItems =
            [
                new NameID { ID = 2, Name = Resources.All },
                new NameID { ID = 1, Name = Resources.Tradeable },
                new NameID { ID = 0, Name = Resources.Untradeable }
            ];

        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
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
            RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
        }
    }

    #endregion

    #region Properties

    [ObservableProperty]
    private string _title = "Add Item";

    #region ItemData

    [ObservableProperty]
    private ItemData? _selectedItem;
    partial void OnSelectedItemChanged(ItemData? value)
    {
        if (value != null)
        {
            UpdateItemData(value);
        }

        SelectItemCommand.NotifyCanExecuteChanged();
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ItemName))]
    private FrameViewModel? _frameViewModel;
    partial void OnFrameViewModelChanged(FrameViewModel? value)
    {
        ItemName = value != null ? value.ItemName : "Select a Item";
    }

    [ObservableProperty]
    private string? _itemName;

    [ObservableProperty]
    private int _slotIndexMin;

    [ObservableProperty]
    private int _slotIndexMax;
    
    #endregion

    #endregion
}
