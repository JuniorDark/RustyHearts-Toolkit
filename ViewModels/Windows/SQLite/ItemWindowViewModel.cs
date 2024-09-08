using RHToolkit.Messages;
using RHToolkit.Models;
using RHToolkit.Models.Database;
using RHToolkit.Models.SQLite;
using RHToolkit.Services;
using System.Windows.Controls;
using static RHToolkit.Models.EnumService;

namespace RHToolkit.ViewModels.Windows;

public partial class ItemWindowViewModel : ObservableObject, IRecipient<CharacterDataMessage>, IRecipient<ItemDataMessage>
{
    private readonly IGMDatabaseService _gmDatabaseService;
    private readonly CachedDataManager _cachedDataManager;

    public ItemWindowViewModel(IGMDatabaseService gmDatabaseService, CachedDataManager cachedDataManager, ItemDataManager itemDataManager)
    {
        _gmDatabaseService = gmDatabaseService;
        _cachedDataManager = cachedDataManager;
        _itemDataManager = itemDataManager;

        WeakReferenceMessenger.Default.Register<CharacterDataMessage>(this);
        WeakReferenceMessenger.Default.Register<ItemDataMessage>(this);
    }

    #region Messenger

    #region Receive CharacterData
    [ObservableProperty]
    private CharacterData? _characterData;

    [ObservableProperty]
    private Guid? _token = Guid.Empty;

    public void Receive(CharacterDataMessage message)
    {
        if (Token == Guid.Empty)
        {
            Token = message.Token;

            CharacterData = null;
            CharacterData = message.Value;
        }
        WeakReferenceMessenger.Default.Unregister<CharacterDataMessage>(this);
    }

    #endregion

    #region Send ItemData
    [RelayCommand(CanExecute = nameof(CanExecuteCommand))]
    private void AddItem()
    {
        if (ItemDataManager.ItemDataViewModel != null)
        {
            var itemData = ItemDataManager.ItemDataViewModel.GetItemData();

            if (itemData != null && !string.IsNullOrEmpty(MessageType))
            {
                var windowMapping = new Dictionary<string, string>
                {
                    { "Mail", "MailWindow" },
                    { "EquipItem", "EquipWindow" },
                    { "InventoryItem", "InventoryWindow" },
                    { "StorageItem", "StorageWindow" },
                    { "AccountStorageItem", "StorageWindow" },
                    { "CouponItem", "CouponWindow" },
                    { "SetItem", "SetItemEditorWindow" },
                    { "Package", "PackageEditorWindow" },
                    { "RandomRune", "RandomRuneEditorWindow" },
                    { "NpcShopItems", "NpcShopEditorWindowItems" },
                    { "NpcShopFilterItems", "NpcShopEditorWindowItems" },
                    { "TradeShopItems", "NpcShopEditorWindowItems" },
                    { "ItemMixItems", "NpcShopEditorWindowItems" },
                    { "NpcShopItem", "NpcShopEditorWindowItem" },
                    { "TradeShopItem", "NpcShopEditorWindowItem" },
                    { "ItemMixItem", "NpcShopEditorWindowItem" },
                    { "DropGroup", "DropGroupEditorWindow" },
                    { "CashShopItemUpdate", "CashShopEditorWindow" }
                };

                if (windowMapping.TryGetValue(MessageType, out var windowName))
                {
                    WeakReferenceMessenger.Default.Send(new ItemDataMessage(itemData, windowName, MessageType, Token));
                }
                else if (MessageType == "CashShopItemAdd" && ItemDataList != null)
                {
                    WeakReferenceMessenger.Default.Send(new ItemDataListMessage(ItemDataList, "CashShopEditorWindow", MessageType, Token));
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

    private bool _isProcessing = false;

    public void Receive(ItemDataMessage message)
    {
        if (_isProcessing) return;

        try
        {
            _isProcessing = true;

            if (Token == Guid.Empty)
            {
                Token = message.Token;
            }

            if (message.Recipient == "ItemWindow" && message.Token == Token)
            {
                var itemData = message.Value;
                MessageType = message.MessageType;

                Dispatcher.CurrentDispatcher.BeginInvoke(() =>
                {
                    Title = GetTitle(MessageType, itemData);

                    var settings = GetVisibilitySettings(message.MessageType, itemData);

                    ApplyVisibilitySettings(settings);

                    if (itemData.ItemId != 0)
                    {
                        LoadItemData(itemData);
                    }
                    else
                    {
                        UpdateItemDataViewModel(itemData);
                    }
                }, DispatcherPriority.ContextIdle);
            }
        }
        finally
        {
            _isProcessing = false;
        }
    }

    private VisibilitySettings GetVisibilitySettings(string? messageType, ItemData itemData)
    {
        var defaultSettings = new VisibilitySettings
        {
            IsSlotVisible = Visibility.Collapsed,
            IsOptionsVisible = Visibility.Collapsed,
            IsItemAmountVisible = Visibility.Collapsed,
            SelectionMode = DataGridSelectionMode.Single,
            SlotIndexMin = 0,
            SlotIndexMax = 0
        };

        var visibilitySettings = new Dictionary<string, Action<VisibilitySettings>>
        {
            ["Mail"] = settings =>
            {
                settings.SlotIndexMax = 2;
                settings.IsSlotVisible = Visibility.Visible;
                settings.IsItemAmountVisible = Visibility.Visible;
                settings.IsOptionsVisible = Visibility.Visible;
            },
            ["EquipItem"] = settings =>
            {
                settings.SlotIndexMin = itemData.SlotIndex;
                settings.SlotIndexMax = itemData.SlotIndex;
                SlotFilter(itemData.SlotIndex);
                settings.IsOptionsVisible = Visibility.Visible;
            },
            ["InventoryItem"] = settings =>
            {
                settings.SlotIndexMax = itemData.PageIndex == 5 ? 119 : 23;
                ItemDataManager.InventoryTypeFilter = itemData.PageIndex;
                settings.IsSlotVisible = Visibility.Visible;
                settings.IsItemAmountVisible = Visibility.Visible;
                settings.IsOptionsVisible = Visibility.Visible;
            },
            ["StorageItem"] = settings =>
            {
                settings.SlotIndexMax = 179;
                settings.IsSlotVisible = Visibility.Visible;
                settings.IsItemAmountVisible = Visibility.Visible;
                settings.IsOptionsVisible = Visibility.Visible;
            },
            ["AccountStorageItem"] = settings =>
            {
                settings.SlotIndexMax = 179;
                ItemDataManager.AccountStorageFilter = 1;
                settings.IsSlotVisible = Visibility.Visible;
                settings.IsItemAmountVisible = Visibility.Visible;
                settings.IsOptionsVisible = Visibility.Visible;
            },
            ["CashShopItemAdd"] = settings =>
            {
                settings.SelectionMode = DataGridSelectionMode.Extended;
                settings.IsItemAmountVisible = Visibility.Visible;
                AddItemText = "Add Selected Item(s)";
            },
            ["CashShopItemUpdate"] = settings =>
            {
                settings.IsItemAmountVisible = Visibility.Visible;
            },
            ["CouponItem"] = settings =>
            {
                IsNewItem = itemData.IsNewItem;
            },
            ["Package"] = settings =>
            {
                settings.SlotIndexMax = 11;
                settings.IsSlotVisible = Visibility.Visible;
            },
            ["RandomRune"] = settings =>
            {
                settings.SlotIndexMax = 11;
                settings.IsSlotVisible = Visibility.Visible;
                settings.IsItemAmountVisible = Visibility.Visible;
            },
            ["TradeShopItems"] = settings =>
            {
                settings.SlotIndexMax = 4;
                settings.IsItemAmountVisible = Visibility.Visible;
                settings.IsSlotVisible = Visibility.Visible;
                settings.IsItemAmountVisible = Visibility.Visible;
            },
            ["ItemMixItems"] = settings =>
            {
                settings.SlotIndexMax = 4;
                settings.IsSlotVisible = Visibility.Visible;
            },
            ["NpcShopItems"] = settings =>
            {
                settings.SlotIndexMax = 19;
                settings.IsItemAmountVisible = Visibility.Collapsed;
                settings.IsSlotVisible = Visibility.Visible;
            },
            ["NpcShopFilterItems"] = settings =>
            {
                settings.SlotIndexMax = 2;
                settings.IsSlotVisible = Visibility.Visible;
            },
            ["NpcShopItem"] = settings => { },
            ["TradeShopItem"] = settings => { },
            ["ItemMixItem"] = settings => { },
            ["DropGroup"] = settings =>
            {
                settings.SlotIndexMax = itemData.OverlapCnt - 1;
                settings.IsSlotVisible = Visibility.Visible;
            },
            ["SetItem"] = settings =>
            {
                settings.SlotIndexMax = 5;
                settings.IsSlotVisible = Visibility.Visible;
            }
        };

        if (!string.IsNullOrEmpty(messageType) && visibilitySettings.TryGetValue(messageType, out var action))
        {
            action(defaultSettings);
        }

        return defaultSettings;
    }

    private void ApplyVisibilitySettings(VisibilitySettings settings)
    {
        SlotIndexMin = settings.SlotIndexMin;
        SlotIndexMax = settings.SlotIndexMax;
        IsSlotVisible = settings.IsSlotVisible;
        IsOptionsVisible = settings.IsOptionsVisible;
        IsItemAmountVisible = settings.IsItemAmountVisible;
        SelectionMode = settings.SelectionMode;
    }

    private void UpdateItemDataViewModel(ItemData itemData)
    {
        if (ItemDataManager.ItemDataViewModel != null)
        {
            ItemDataManager.ItemDataViewModel.PageIndex = itemData.PageIndex;
            ItemDataManager.ItemDataViewModel.SlotIndex = itemData.SlotIndex;
        }
    }

    private class VisibilitySettings
    {
        public Visibility IsSlotVisible { get; set; }
        public Visibility IsOptionsVisible { get; set; }
        public Visibility IsItemAmountVisible { get; set; }
        public DataGridSelectionMode SelectionMode { get; set; }
        public int SlotIndexMin { get; set; }
        public int SlotIndexMax { get; set; }
    }

    private string GetTitle(string? messageType, ItemData itemData)
    {
        return (messageType ?? string.Empty) switch
        {
            "CashShopItemAdd" => "Add Cash Shop Item",
            "CashShopItemUpdate" => "Update Cash Shop Item",
            "CouponItem" => "Add Coupon Item",
            "EquipItem" => $"Add Equipment Item ({(EquipCategory)itemData.SlotIndex}) [{CharacterData?.CharacterName}]",
            "InventoryItem" => $"Add Inventory Item ({(InventoryType)itemData.PageIndex}) [{CharacterData?.CharacterName}]",
            "StorageItem" => $"Add Storage Item [{CharacterData?.CharacterName}]",
            "AccountStorageItem" => $"Add Account Storage Item [{CharacterData?.AccountName}]",
            "Mail" => "Add Mail Item",
            "Package" => "Add Package Item",
            "RandomRune" => "Add Random Rune Item",
            "NpcShopItem" or "NpcShopItems" or "NpcShopFilterItems" => "Add Npc Shop Item",
            "TradeShopItem" or "TradeShopItems" => "Add Trade Shop Item",
            "ItemMixItem" or "ItemMixItems" => "Add Item Craft Item",
            "DropGroup" => "Add Drop Group Item",
            "SetItem" => "Add Set Item",
            _ => "Add Item",
        };
    }

    #endregion

    #endregion

    #region Load ItemData

    private void SlotFilter(int slotIndex)
    {
        if (CharacterData != null)
        {
            switch (slotIndex)
            {
                //Equipment
                case 0: // Weapon
                    ItemDataManager.ItemTypeFilter = 4;
                    ItemDataManager.ItemSubCategoryFilter = 1;
                    ItemDataManager.ItemClassFilter = ItemDataManager.GetRealClass(CharacterData.Class);
                    ItemDataManager.ItemTypeEnabled = false;
                    ItemDataManager.ItemSubCategoryEnabled = false;
                    ItemDataManager.ItemClassEnabled = false;
                    break;
                case 1: //Chest
                    ItemDataManager.ItemTypeFilter = 3;
                    ItemDataManager.ItemSubCategoryFilter = 2;
                    ItemDataManager.ItemClassFilter = 0;
                    ItemDataManager.ItemTypeEnabled = false;
                    ItemDataManager.ItemSubCategoryEnabled = false;
                    ItemDataManager.ItemClassEnabled = true;
                    break;
                case 2: //Head
                    ItemDataManager.ItemTypeFilter = 3;
                    ItemDataManager.ItemSubCategoryFilter = 3;
                    ItemDataManager.ItemClassFilter = 0;
                    ItemDataManager.ItemTypeEnabled = false;
                    ItemDataManager.ItemSubCategoryEnabled = false;
                    ItemDataManager.ItemClassEnabled = true;
                    break;
                case 3: //Legs
                    ItemDataManager.ItemTypeFilter = 3;
                    ItemDataManager.ItemSubCategoryFilter = 4;
                    ItemDataManager.ItemClassFilter = 0;
                    ItemDataManager.ItemTypeEnabled = false;
                    ItemDataManager.ItemSubCategoryEnabled = false;
                    ItemDataManager.ItemClassEnabled = true;
                    break;
                case 4: //Feet
                    ItemDataManager.ItemTypeFilter = 3;
                    ItemDataManager.ItemSubCategoryFilter = 5;
                    ItemDataManager.ItemClassFilter = 0;
                    ItemDataManager.ItemTypeEnabled = false;
                    ItemDataManager.ItemSubCategoryEnabled = false;
                    ItemDataManager.ItemClassEnabled = true;
                    break;
                case 5: //Waist
                    ItemDataManager.ItemTypeFilter = 3;
                    ItemDataManager.ItemSubCategoryFilter = 6;
                    ItemDataManager.ItemClassFilter = 0;
                    ItemDataManager.ItemTypeEnabled = false;
                    ItemDataManager.ItemSubCategoryEnabled = false;
                    ItemDataManager.ItemClassEnabled = true;
                    break;
                case 6: //Necklace
                    ItemDataManager.ItemTypeFilter = 3;
                    ItemDataManager.ItemSubCategoryFilter = 7;
                    ItemDataManager.ItemClassFilter = 0;
                    ItemDataManager.ItemTypeEnabled = false;
                    ItemDataManager.ItemSubCategoryEnabled = false;
                    ItemDataManager.ItemClassEnabled = true;
                    break;
                case 7: //Earrings
                    ItemDataManager.ItemTypeFilter = 3;
                    ItemDataManager.ItemSubCategoryFilter = 8;
                    ItemDataManager.ItemClassFilter = 0;
                    ItemDataManager.ItemTypeEnabled = false;
                    ItemDataManager.ItemSubCategoryEnabled = false;
                    ItemDataManager.ItemClassEnabled = true;
                    break;
                case 8: //Ring
                    ItemDataManager.ItemTypeFilter = 3;
                    ItemDataManager.ItemSubCategoryFilter = 9;
                    ItemDataManager.ItemClassFilter = 0;
                    ItemDataManager.ItemTypeEnabled = false;
                    ItemDataManager.ItemSubCategoryEnabled = false;
                    ItemDataManager.ItemClassEnabled = true;
                    break;
                case 9: //Hands
                    ItemDataManager.ItemTypeFilter = 3;
                    ItemDataManager.ItemCategoryFilter = 0;
                    ItemDataManager.ItemSubCategoryFilter = 10;
                    ItemDataManager.ItemClassFilter = 0;
                    ItemDataManager.ItemTypeEnabled = false;
                    ItemDataManager.ItemSubCategoryEnabled = false;
                    ItemDataManager.ItemClassEnabled = true;
                    break;
                //Costume
                case 10: //Hair
                    ItemDataManager.ItemTypeFilter = 2;
                    ItemDataManager.ItemSubCategoryFilter = 11;
                    ItemDataManager.ItemClassFilter = CharacterData.Class;
                    ItemDataManager.ItemTypeEnabled = false;
                    ItemDataManager.ItemSubCategoryEnabled = false;
                    ItemDataManager.ItemClassEnabled = false;
                    break;
                case 11: //Face
                    ItemDataManager.ItemTypeFilter = 2;
                    ItemDataManager.ItemSubCategoryFilter = 12;
                    ItemDataManager.ItemClassFilter = 0;
                    ItemDataManager.ItemTypeEnabled = false;
                    ItemDataManager.ItemSubCategoryEnabled = false;
                    ItemDataManager.ItemClassEnabled = true;
                    break;
                case 12: //Neck
                    ItemDataManager.ItemTypeFilter = 2;
                    ItemDataManager.ItemSubCategoryFilter = 13;
                    ItemDataManager.ItemClassFilter = CharacterData.Class;
                    ItemDataManager.ItemTypeEnabled = false;
                    ItemDataManager.ItemSubCategoryEnabled = false;
                    ItemDataManager.ItemClassEnabled = false;
                    break;
                case 13: //Outerwear
                    ItemDataManager.ItemTypeFilter = 2;
                    ItemDataManager.ItemSubCategoryFilter = 14;
                    ItemDataManager.ItemClassFilter = CharacterData.Class;
                    ItemDataManager.ItemTypeEnabled = false;
                    ItemDataManager.ItemSubCategoryEnabled = false;
                    ItemDataManager.ItemClassEnabled = false;
                    break;
                case 14: //Top
                    ItemDataManager.ItemTypeFilter = 2;
                    ItemDataManager.ItemSubCategoryFilter = 15;
                    ItemDataManager.ItemClassFilter = CharacterData.Class;
                    ItemDataManager.ItemTypeEnabled = false;
                    ItemDataManager.ItemSubCategoryEnabled = false;
                    ItemDataManager.ItemClassEnabled = false;
                    break;
                case 15: //Bottom
                    ItemDataManager.ItemTypeFilter = 2;
                    ItemDataManager.ItemSubCategoryFilter = 16;
                    ItemDataManager.ItemClassFilter = CharacterData.Class;
                    ItemDataManager.ItemTypeEnabled = false;
                    ItemDataManager.ItemSubCategoryEnabled = false;
                    ItemDataManager.ItemClassEnabled = false;
                    break;
                case 16: //Gloves
                    ItemDataManager.ItemTypeFilter = 2;
                    ItemDataManager.ItemSubCategoryFilter = 17;
                    ItemDataManager.ItemClassFilter = CharacterData.Class;
                    ItemDataManager.ItemTypeEnabled = false;
                    ItemDataManager.ItemSubCategoryEnabled = false;
                    ItemDataManager.ItemClassEnabled = false;
                    break;
                case 17: //Shoes
                    ItemDataManager.ItemTypeFilter = 2;
                    ItemDataManager.ItemSubCategoryFilter = 18;
                    ItemDataManager.ItemClassFilter = CharacterData.Class;
                    ItemDataManager.ItemTypeEnabled = false;
                    ItemDataManager.ItemSubCategoryEnabled = false;
                    ItemDataManager.ItemClassEnabled = false;
                    break;
                case 18: //Accessory 1
                    ItemDataManager.ItemTypeFilter = 2;
                    ItemDataManager.ItemSubCategoryFilter = 19;
                    ItemDataManager.ItemClassFilter = CharacterData.Class;
                    ItemDataManager.ItemTypeEnabled = false;
                    ItemDataManager.ItemSubCategoryEnabled = false;
                    ItemDataManager.ItemClassEnabled = false;
                    break;
                case 19: //Accessory 2
                    ItemDataManager.ItemTypeFilter = 2;
                    ItemDataManager.ItemSubCategoryFilter = 20;
                    ItemDataManager.ItemClassFilter = 0;
                    ItemDataManager.ItemTypeEnabled = false;
                    ItemDataManager.ItemSubCategoryEnabled = false;
                    ItemDataManager.ItemClassEnabled = true;
                    break;
            }
        }
    }

    private void LoadItemData(ItemData itemData)
    {
        var itemDataViewModel = ItemDataManager.GetItemData(itemData);
        ItemDataManager.ItemDataViewModel = itemDataViewModel;
        SelectedItem = ItemDataManager.ItemDataItems?.FirstOrDefault(item => item.ItemId == itemData.ItemId);
    }

    private void UpdateItemData(ItemData itemData)
    {
        if (ItemDataManager.ItemDataViewModel != null)
        {
            ItemDataManager.ItemDataViewModel.IsNewItem = IsNewItem;
            ItemDataManager.ItemDataViewModel.ItemId = itemData.ItemId;
            ItemDataManager.ItemDataViewModel.ItemName = itemData.ItemName;
            ItemDataManager.ItemDataViewModel.Description = itemData.Description;
            ItemDataManager.ItemDataViewModel.ItemBranch = itemData.Branch;
            ItemDataManager.ItemDataViewModel.IconName = itemData.IconName;
            ItemDataManager.ItemDataViewModel.ItemTrade = itemData.ItemTrade;
            ItemDataManager.ItemDataViewModel.MaxDurability = itemData.Durability;
            ItemDataManager.ItemDataViewModel.Weight = itemData.Weight;
            ItemDataManager.ItemDataViewModel.ReconstructionMax = itemData.ReconstructionMax;
            ItemDataManager.ItemDataViewModel.Reconstruction = itemData.ReconstructionMax;
            ItemDataManager.ItemDataViewModel.OverlapCnt = itemData.OverlapCnt;
            ItemDataManager.ItemDataViewModel.ItemAmount = ItemDataManager.ItemDataViewModel.ItemAmount <= itemData.OverlapCnt ? ItemDataManager.ItemDataViewModel.ItemAmount : 1;
            ItemDataManager.ItemDataViewModel.Rank = itemData.Rank;
            ItemDataManager.ItemDataViewModel.Type = itemData.Type;
            ItemDataManager.ItemDataViewModel.Category = itemData.Category;
            ItemDataManager.ItemDataViewModel.SubCategory = itemData.SubCategory;
            ItemDataManager.ItemDataViewModel.JobClass = itemData.JobClass;
            ItemDataManager.ItemDataViewModel.Defense = itemData.Defense;
            ItemDataManager.ItemDataViewModel.MagicDefense = itemData.MagicDefense;
            ItemDataManager.ItemDataViewModel.WeaponID00 = itemData.WeaponID00;
            ItemDataManager.ItemDataViewModel.SellPrice = itemData.SellPrice;
            ItemDataManager.ItemDataViewModel.RequiredLevel = itemData.LevelLimit;
            ItemDataManager.ItemDataViewModel.SetId = itemData.SetId;
            ItemDataManager.ItemDataViewModel.TitleList = itemData.TitleList;
            ItemDataManager.ItemDataViewModel.CooldownValue = itemData.Cooltime;
            ItemDataManager.ItemDataViewModel.PetFood = itemData.PetFood;
            ItemDataManager.ItemDataViewModel.FixedOption01 = itemData.FixOption1Code;
            ItemDataManager.ItemDataViewModel.FixedOption01Value = itemData.FixOption1Value;
            ItemDataManager.ItemDataViewModel.FixedOption02 = itemData.FixOption2Code;
            ItemDataManager.ItemDataViewModel.FixedOption02Value = itemData.FixOption2Value;
            ItemDataManager.ItemDataViewModel.OptionCountMax = itemData.Type != 1 ? itemData.OptionCountMax : (itemData.Type == 1 && itemData.Category == 29 ? 1 : 0);
            ItemDataManager.ItemDataViewModel.SocketCountMax = itemData.SocketCountMax;
            ItemDataManager.ItemDataViewModel.SocketCount = itemData.SocketCountMax;
        }
        else
        {
            var itemDataViewModel = ItemDataManager.GetItemData(itemData);
            ItemDataManager.ItemDataViewModel = itemDataViewModel;
        }

    }

    #endregion

    #region Properties

    [ObservableProperty]
    private string _title = "Add Item";

    [ObservableProperty]
    private string _addItemText = "Add Selected Item";

    [ObservableProperty]
    private Visibility _isSlotVisible = Visibility.Collapsed;

    [ObservableProperty]
    private Visibility _isItemAmountVisible = Visibility.Collapsed;

    [ObservableProperty]
    private Visibility _isOptionsVisible = Visibility.Collapsed;

    [ObservableProperty]
    private DataGridSelectionMode _selectionMode = DataGridSelectionMode.Single;

    [ObservableProperty]
    private ItemDataManager _itemDataManager;

    #region ItemData

    [ObservableProperty]
    private ItemData? _selectedItem;
    partial void OnSelectedItemChanged(ItemData? value)
    {
        if (value != null)
        {
            if (_isProcessing) return;
            UpdateItemData(value);
        }

        AddItemCommand.NotifyCanExecuteChanged();
    }

    [ObservableProperty]
    private List<ItemData>? _itemDataList;

    [ObservableProperty]
    private int _slotIndexMin;

    [ObservableProperty]
    private int _slotIndexMax;

    [ObservableProperty]
    private bool _isNewItem = false;

    #endregion

    #endregion
}
