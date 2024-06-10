﻿using RHToolkit.Messages;
using RHToolkit.Models;
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
    private readonly FrameViewModel _frameViewModel;
    private readonly System.Timers.Timer _searchTimer;
    public FrameViewModel FrameViewModel => _frameViewModel;

    public ItemWindowViewModel(IGMDatabaseService gmDatabaseService, CachedDataManager cachedDataManager, FrameViewModel frameViewModel)
    {
        _gmDatabaseService = gmDatabaseService;
        _cachedDataManager = cachedDataManager;
        _frameViewModel = frameViewModel;
        _searchTimer = new()
        {
            Interval = 500,
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

    #region Send ItemData
    [RelayCommand(CanExecute = nameof(CanExecuteCommand))]
    private void SelectItem(object parameter)
    {
        ItemData itemData = new()
        {
            SlotIndex = SlotIndex,
            ID = ItemId,
            Name = ItemName,
            Branch = ItemBranch,
            IconName = IconName,
            Durability = MaxDurability,
            DurabilityMax = MaxDurability,
            EnhanceLevel = EnhanceLevel,
            AugmentStone = AugmentValue,
            Rank = Rank,
            Weight = Weight,
            Reconstruction = Reconstruction,
            ReconstructionMax = ReconstructionMax,
            Amount = Amount,
            Option1Code = RandomOption01,
            Option2Code = RandomOption02,
            Option3Code = RandomOption03,
            Option1Value = RandomOption01Value,
            Option2Value = RandomOption02Value,
            Option3Value = RandomOption03Value,
            SocketCount = SocketCount,
            Socket1Color = Socket01Color,
            Socket2Color = Socket02Color,
            Socket3Color = Socket03Color,
            Socket1Code = SocketOption01,
            Socket2Code = SocketOption02,
            Socket3Code = SocketOption03,
            Socket1Value = SocketOption01Value,
            Socket2Value = SocketOption02Value,
            Socket3Value = SocketOption03Value,
        };

        switch (MessageType)
        {
            case "Mail":
                WeakReferenceMessenger.Default.Send(new ItemDataMessage(itemData, "MailWindowViewModel", "Mail", Token));
                break;
            case "EquipItem":
                WeakReferenceMessenger.Default.Send(new ItemDataMessage(itemData, "CharacterWindowViewModel", "EquipItem", Token));
                break;
            case "Coupon":
                WeakReferenceMessenger.Default.Send(new ItemDataMessage(itemData, "CouponViewModel", "Coupon", Token));
                break;
            default:
                break;
        }

    }

    private bool CanExecuteCommand()
    {
        return SelectedItem != null;
    }
    #endregion

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

    #region Receive ItemData
    [ObservableProperty]
    private ItemData? _itemData;

    [ObservableProperty]
    private string? _messageType;

    public void Receive(ItemDataMessage message)
    {
        if (Token == Guid.Empty)
        {
            Token = message.Token;
        }

        if (message.Token != Token) return;

        if (message.Recipient == "ItemWindowViewModel" && message.Token == Token)
        {
            var itemData = message.Value;
            ItemData = null;
            MessageType = message.MessageType;

            Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() =>
            {
                if (message.MessageType == "Mail")
                {
                    Title = $"Add Mail Item";
                    SlotIndexMin = 0;
                    SlotIndexMax = 2;
                }
                else if (message.MessageType == "EquipItem")
                {
                    Title = $"Add Equipment Item ({(EquipCategory)itemData.SlotIndex}) [{CharacterInfo?.CharacterName}] ";

                    SlotIndexMin = itemData.SlotIndex;
                    SlotIndexMax = itemData.SlotIndex;

                    SlotFilter(itemData.SlotIndex);

                }
                else if (message.MessageType == "Coupon")
                {
                    Title = $"Add Coupon Item";

                    SlotIndexMin = itemData.SlotIndex;
                    SlotIndexMax = itemData.SlotIndex;
                }
                else
                {
                    SlotIndexMin = itemData.SlotIndex;
                    SlotIndexMax = itemData.SlotIndex;
                    SlotFilter(itemData.SlotIndex);
                }

                ItemId = itemData.ID;

                LoadItemData(itemData);

            }), DispatcherPriority.Loaded);
        }
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
                    ItemClassFilter = GetRealClass(CharacterInfo.Class);
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

    private static int GetRealClass(int charClass)
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

    private void LoadItemData(ItemData itemData)
    {
        SlotIndex = itemData.SlotIndex;
        Amount = itemData.Amount;
        MaxDurability = itemData.DurabilityMax;
        EnhanceLevel = itemData.EnhanceLevel;
        AugmentValue = itemData.AugmentStone;
        Rank = itemData.Rank;
        Weight = itemData.Weight;
        ReconstructionMax = itemData.ReconstructionMax;
        Reconstruction = itemData.Reconstruction;
        RandomOption01 = itemData.Option1Code;
        RandomOption02 = itemData.Option2Code;
        RandomOption03 = itemData.Option3Code;
        RandomOption01Value = itemData.Option1Value;
        RandomOption02Value = itemData.Option2Value;
        RandomOption03Value = itemData.Option3Value;
        SocketCount = itemData.SocketCount;
        Socket01Color = itemData.Socket1Color;
        Socket02Color = itemData.Socket2Color;
        Socket03Color = itemData.Socket3Color;
        SocketOption01 = itemData.Socket1Code;
        SocketOption02 = itemData.Socket2Code;
        SocketOption03 = itemData.Socket3Code;
        SocketOption01Value = itemData.Socket1Value;
        SocketOption02Value = itemData.Socket2Value;
        SocketOption03Value = itemData.Socket3Value;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedItem))]
    private int _itemId;
    partial void OnItemIdChanged(int value)
    {
        if (value == 0)
        {
            SelectedItem = ItemDataView?.Cast<ItemData>()?.FirstOrDefault(FilterItems);
        }
        else
        {
            SelectedItem = ItemDataItems?.FirstOrDefault(item => item.ID == value);
        }
    }

    [ObservableProperty]
    private ItemData? _selectedItem;
    partial void OnSelectedItemChanged(ItemData? value)
    {
        Dispatcher.CurrentDispatcher.Invoke(new Action(() =>
        {
            UpdateItemData(value);
        }));

        SelectItemCommand.NotifyCanExecuteChanged();
    }

    private void UpdateItemData(ItemData? itemData)
    {
        if (itemData != null)
        {
            ItemId = itemData.ID;
            ItemName = itemData.Name;
            Description = itemData.Description;
            ItemBranch = itemData.Branch;
            IconName = itemData.IconName;
            ItemTrade = itemData.ItemTrade;
            MaxDurability = itemData.Durability;
            Weight = itemData.Weight;
            ReconstructionMax = itemData.ReconstructionMax;
            Reconstruction = itemData.ReconstructionMax;
            OverlapCnt = OverlapCnt == 0 ? 1 : itemData.OverlapCnt;
            Amount = Amount == 0 ? 1 : Amount;
            Rank = (byte)(Rank == 0 ? 1 : Rank);
            Type = itemData.Type;
            Category = itemData.Category;
            SubCategory = itemData.SubCategory;
            JobClass = itemData.JobClass;
            Defense = itemData.Defense;
            MagicDefense = itemData.MagicDefense;
            WeaponID00 = itemData.WeaponID00;
            SellPrice = itemData.SellPrice;
            RequiredLevel = itemData.LevelLimit;
            SetId = itemData.SetId;
            PetFood = itemData.PetFood;
            FixedOption01 = itemData.FixOption1Code;
            FixedOption01Value = itemData.FixOption1Value;
            FixedOption02 = itemData.FixOption2Code;
            FixedOption02Value = itemData.FixOption2Value;
            OptionCountMax = itemData.Type != 1 ? itemData.OptionCountMax : (itemData.Type == 1 && itemData.Category == 29 ? 1 : 0);
            SocketCountMax = itemData.SocketCountMax;
            SocketCount = itemData.SocketCountMax;
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
        _searchTimer.Stop();
        _searchTimer.Start();
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
    private int _slotIndex;

    [ObservableProperty]
    private int _slotIndexMin;

    [ObservableProperty]
    private int _slotIndexMax;

    [ObservableProperty]
    private string? _itemName;
    partial void OnItemNameChanged(string? value)
    {
        _frameViewModel.ItemName = value;
    }

    public string ItemNameColor => FrameService.GetBranchColor(ItemBranch);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ItemNameColor))]
    private int _itemBranch;
    partial void OnItemBranchChanged(int value)
    {
        _frameViewModel.ItemBranch = value;
    }

    [ObservableProperty]
    private string? _iconName;

    [ObservableProperty]
    private string? _description;
    partial void OnDescriptionChanged(string? value)
    {
        _frameViewModel.Description = value;
    }

    [ObservableProperty]
    private int _amount = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EnhanceLevel))]
    [NotifyPropertyChangedFor(nameof(AugmentValue))]
    private int _type;
    partial void OnTypeChanged(int value)
    {
        if (value == 1 || value == 2)
        {
            EnhanceLevel = 0;
            AugmentValue = 0;
        }
        _frameViewModel.Type = value;
    }

    [ObservableProperty]
    private int _weight;
    partial void OnWeightChanged(int value)
    {
        _frameViewModel.Weight = value;
    }

    [ObservableProperty]
    private int _maxDurability;
    partial void OnMaxDurabilityChanged(int value)
    {
        _frameViewModel.MaxDurability = value;
    }

    [ObservableProperty]
    private int _reconstruction;
    partial void OnReconstructionChanged(int value)
    {
        _frameViewModel.Reconstruction = value;
    }

    [ObservableProperty]
    private byte _reconstructionMax;
    partial void OnReconstructionMaxChanged(byte value)
    {
        _frameViewModel.ReconstructionMax = value;
    }

    [ObservableProperty]
    private byte _rank = 1;
    partial void OnRankChanged(byte value)
    {
        _frameViewModel.Rank = value;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Amount))]
    private int _overlapCnt;
    partial void OnOverlapCntChanged(int value)
    {
        if (Amount > value)
            Amount = value;
    }

    [ObservableProperty]
    private int _enhanceLevel;
    partial void OnEnhanceLevelChanged(int value)
    {
        _frameViewModel.EnhanceLevel = value;
    }

    [ObservableProperty]
    private int _category;
    partial void OnCategoryChanged(int value)
    {
        _frameViewModel.Category = value;
    }

    [ObservableProperty]
    private int _subCategory;
    partial void OnSubCategoryChanged(int value)
    {
        _frameViewModel.SubCategory = value;
    }

    [ObservableProperty]
    private int _defense;
    partial void OnDefenseChanged(int value)
    {
        _frameViewModel.Defense = value;
    }

    [ObservableProperty]
    private int _magicDefense;
    partial void OnMagicDefenseChanged(int value)
    {
        _frameViewModel.MagicDefense = value;
    }

    [ObservableProperty]
    private int _weaponID00;
    partial void OnWeaponID00Changed(int value)
    {
        _frameViewModel.WeaponID00 = value;
    }

    [ObservableProperty]
    private int _sellPrice;
    partial void OnSellPriceChanged(int value)
    {
        _frameViewModel.SellPrice = value;
    }

    [ObservableProperty]
    private int _requiredLevel;
    partial void OnRequiredLevelChanged(int value)
    {
        _frameViewModel.RequiredLevel = value;
    }

    [ObservableProperty]
    private int _itemTrade;
    partial void OnItemTradeChanged(int value)
    {
        _frameViewModel.ItemTrade = value;
    }

    [ObservableProperty]
    private int _jobClass;
    partial void OnJobClassChanged(int value)
    {
        _frameViewModel.JobClass = value;
    }

    [ObservableProperty]
    private int _setId;
    partial void OnSetIdChanged(int value)
    {
        _frameViewModel.SetId = value;
    }

    [ObservableProperty]
    private int _petFood;
    partial void OnPetFoodChanged(int value)
    {
        _frameViewModel.PetFood = value;
    }

    [ObservableProperty]
    private int _augmentValue;
    partial void OnAugmentValueChanged(int value)
    {
        _frameViewModel.AugmentValue = value;
    }

    #region Fixed Option

    [ObservableProperty]
    private int _fixedOption01;
    partial void OnFixedOption01Changed(int value)
    {
        _frameViewModel.FixedOption01 = value;
    }

    [ObservableProperty]
    private int _fixedOption01Value;
    partial void OnFixedOption01ValueChanged(int value)
    {
        _frameViewModel.FixedOption01Value = value;
    }

    [ObservableProperty]
    private int _fixedOption02;
    partial void OnFixedOption02Changed(int value)
    {
        _frameViewModel.FixedOption02 = value;
    }

    [ObservableProperty]
    private int _fixedOption02Value;
    partial void OnFixedOption02ValueChanged(int value)
    {
        _frameViewModel.FixedOption02Value = value;
    }

    #endregion

    #region Random Option

    [ObservableProperty]
    private int _optionMinValue;

    [ObservableProperty]
    private int _optionMaxValue;

    private int CalculateOptionValue(int option, int value)
    {
        if (option != 0)
        {
            if (value == 0)
            {
                OptionMinValue = 1;
                OptionMaxValue = 10000;
                return OptionMaxValue;
            }
        }
        else
        {
            OptionMinValue = 0;
            OptionMaxValue = 0;
            return 0;
        }
        return value;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RandomOption01Value))]
    private int _optionCount;
    partial void OnOptionCountChanged(int value)
    {
        _frameViewModel.OptionCount = value;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RandomOption01Value))]
    private int _OptionCountMax;
    partial void OnOptionCountMaxChanged(int value)
    {
        _frameViewModel.OptionCountMax = value;
    }

    [ObservableProperty]
    private int _randomOption01;

    partial void OnRandomOption01Changed(int value)
    {
        RandomOption01Value = CalculateOptionValue(value, RandomOption01Value);
        _frameViewModel.RandomOption01 = value;
    }

    [ObservableProperty]
    private int _randomOption01Value;
    partial void OnRandomOption01ValueChanged(int value)
    {
        _frameViewModel.RandomOption01Value = value;
    }

    [ObservableProperty]
    private int _randomOption02;
    partial void OnRandomOption02Changed(int value)
    {
        RandomOption02Value = CalculateOptionValue(value, RandomOption02Value);
        _frameViewModel.RandomOption02 = value;
    }

    [ObservableProperty]
    private int _randomOption02Value;
    partial void OnRandomOption02ValueChanged(int value)
    {
        _frameViewModel.RandomOption02Value = value;
    }

    [ObservableProperty]
    private int _randomOption03;

    partial void OnRandomOption03Changed(int value)
    {
        RandomOption03Value = CalculateOptionValue(value, RandomOption03Value);
        _frameViewModel.RandomOption03 = value;
    }

    [ObservableProperty]
    private int _randomOption03Value;
    partial void OnRandomOption03ValueChanged(int value)
    {
        _frameViewModel.RandomOption03Value = value;
    }
    #endregion

    #region Socket Option

    [ObservableProperty]
    private int _socketCount;
    partial void OnSocketCountChanged(int value)
    {
        _frameViewModel.SocketCount = value;
    }

    [ObservableProperty]
    private int _socketCountMax;
    partial void OnSocketCountMaxChanged(int value)
    {
        _frameViewModel.SocketCountMax = value;
    }

    [ObservableProperty]
    private int _socket01Color;
    partial void OnSocket01ColorChanged(int value)
    {
        _frameViewModel.Socket01Color = value;
    }

    [ObservableProperty]
    private int _socket02Color;
    partial void OnSocket02ColorChanged(int value)
    {
        _frameViewModel.Socket02Color = value;
    }

    [ObservableProperty]
    private int _socket03Color;
    partial void OnSocket03ColorChanged(int value)
    {
        _frameViewModel.Socket03Color = value;
    }

    [ObservableProperty]
    private int _socketOption01;
    partial void OnSocketOption01Changed(int value)
    {
        SocketOption01Value = CalculateOptionValue(value, SocketOption01Value);
        _frameViewModel.SocketOption01 = value;
    }

    [ObservableProperty]
    private int _socketOption01Value;
    partial void OnSocketOption01ValueChanged(int value)
    {
        _frameViewModel.SocketOption01Value = value;
    }

    [ObservableProperty]
    private int _socketOption02;
    partial void OnSocketOption02Changed(int value)
    {
        SocketOption02Value = CalculateOptionValue(value, SocketOption02Value);
        _frameViewModel.SocketOption02 = value;
    }

    [ObservableProperty]
    private int _socketOption02Value;
    partial void OnSocketOption02ValueChanged(int value)
    {
        _frameViewModel.SocketOption02Value = value;
    }

    [ObservableProperty]
    private int _socketOption03;
    partial void OnSocketOption03Changed(int value)
    {
        SocketOption03Value = CalculateOptionValue(value, SocketOption03Value);
        _frameViewModel.SocketOption03 = value;
    }

    [ObservableProperty]
    private int _socketOption03Value;
    partial void OnSocketOption03ValueChanged(int value)
    {
        _frameViewModel.SocketOption03Value = value;
    }

    #endregion

    #endregion

    #endregion
}
