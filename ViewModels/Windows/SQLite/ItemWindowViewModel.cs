using RHToolkit.Messages;
using RHToolkit.Models;
using RHToolkit.Models.Database;
using RHToolkit.Models.SQLite;
using RHToolkit.Services;
using RHToolkit.ViewModels.Controls;
using static RHToolkit.Models.EnumService;

namespace RHToolkit.ViewModels.Windows;

public partial class ItemWindowViewModel : ObservableObject, IRecipient<ItemDataMessage>, IRecipient<CharacterDataMessage>
{
    private readonly IGMDatabaseService _gmDatabaseService;
    private readonly CachedDataManager _cachedDataManager;

    public ItemWindowViewModel(IGMDatabaseService gmDatabaseService, CachedDataManager cachedDataManager, ItemDataManager itemHelper)
    {
        _gmDatabaseService = gmDatabaseService;
        _cachedDataManager = cachedDataManager;
        _itemDataManager = itemHelper;

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
                    case "StorageItem":
                    case "AccountStorageItem":
                        WeakReferenceMessenger.Default.Send(new ItemDataMessage(itemData, "StorageWindow", MessageType, Token));
                        break;
                    case "CouponItem":
                        WeakReferenceMessenger.Default.Send(new ItemDataMessage(itemData, "CouponWindow", MessageType, Token));
                        break;
                    case "CashShopItem":
                        WeakReferenceMessenger.Default.Send(new ItemDataMessage(itemData, "CashShopEditorWindow", MessageType, Token));
                        break;
                    case "SetItem":
                        WeakReferenceMessenger.Default.Send(new ItemDataMessage(itemData, "SetItemEditorWindow", MessageType, Token));
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

                switch (message.MessageType)
                {
                    case "Mail":
                        SlotIndexMin = 0;
                        SlotIndexMax = 2;
                        IsSlotVisible = Visibility.Visible;
                        IsOptionsVisible = Visibility.Visible;
                        break;
                    case "EquipItem":
                        SlotIndexMin = itemData.SlotIndex;
                        SlotIndexMax = itemData.SlotIndex;
                        SlotFilter(itemData.SlotIndex);
                        IsSlotVisible = Visibility.Visible;
                        IsOptionsVisible = Visibility.Visible;
                        break;
                    case "InventoryItem":
                        switch (itemData.PageIndex)
                        {
                            case 5:
                                SlotIndexMin = 0;
                                SlotIndexMax = 119;
                                break;
                            default:
                                SlotIndexMin = 0;
                                SlotIndexMax = 23;
                                break;
                        }
                        ItemDataManager.InventoryTypeFilter = itemData.PageIndex;
                        IsSlotVisible = Visibility.Visible;
                        IsOptionsVisible = Visibility.Visible;
                        break;
                    case "StorageItem":
                        SlotIndexMin = 0;
                        SlotIndexMax = 179;
                        IsSlotVisible = Visibility.Visible;
                        IsOptionsVisible = Visibility.Visible;
                        break;
                    case "AccountStorageItem":
                        SlotIndexMin = 0;
                        SlotIndexMax = 179;
                        ItemDataManager.AccountStorageFilter = 1;
                        IsSlotVisible = Visibility.Visible;
                        IsOptionsVisible = Visibility.Visible;
                        break;
                    case "CashShopItem":
                    case "CouponItem":
                        IsNewItem = itemData.IsNewItem;
                        IsSlotVisible = Visibility.Hidden;
                        IsOptionsVisible = Visibility.Hidden;
                        break;
                    case "SetItem":
                        SlotIndexMin = 1;
                        SlotIndexMax = 6;
                        IsSlotVisible = Visibility.Visible;
                        IsOptionsVisible = Visibility.Hidden;
                        break;
                    default:
                        SlotIndexMin = itemData.SlotIndex;
                        SlotIndexMax = itemData.SlotIndex;
                        IsSlotVisible = Visibility.Visible;
                        IsOptionsVisible = Visibility.Visible;
                        break;
                }

                if (itemData.ItemId != 0)
                {
                    SelectedItem = ItemDataManager.ItemDataItems?.FirstOrDefault(item => item.ItemId == itemData.ItemId);
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
            "CashShopItem" => $"Add Cash Shop Item",
            "CouponItem" => $"Add Coupon Item",
            "EquipItem" => $"Add Equipment Item ({(EquipCategory)itemData.SlotIndex}) [{CharacterData?.CharacterName}] ",
            "InventoryItem" => $"Add Inventory Item ({(InventoryType)itemData.PageIndex}) [{CharacterData?.CharacterName}] ",
            "StorageItem" => $"Add Storage Item [{CharacterData?.CharacterName}] ",
            "AccountStorageItem" => $"Add Account Storage Item [{CharacterData?.AccountName}] ",
            "Mail" => $"Add Mail Item",
            "SetItem" => $"Add Set Item",
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
        var frameViewModel = ItemDataManager.GetItemData(itemData);

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
            FrameViewModel.IsNewItem = IsNewItem;
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
            var frameViewModel = ItemDataManager.GetItemData(itemData);
            FrameViewModel = frameViewModel;
            ItemName = frameViewModel.ItemName;
        }

    }
    #endregion

    #region Properties

    [ObservableProperty]
    private string _title = "Add Item";

    [ObservableProperty]
    private Visibility _isSlotVisible = Visibility.Hidden;

    [ObservableProperty]
    private Visibility _isOptionsVisible = Visibility.Hidden;

    [ObservableProperty]
    private ItemDataManager _itemDataManager;

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

    [ObservableProperty]
    private bool _isNewItem = false;

    #endregion

    #endregion
}
