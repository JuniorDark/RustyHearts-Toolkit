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
        IsSlotVisible = Visibility.Visible;
        IsOptionsVisible = Visibility.Visible;
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
        if (ItemDataManager.FrameViewModel != null)
        {
            var itemData = ItemDataManager.FrameViewModel.GetItemData();

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

                var actions = new Dictionary<string, Action>
                {
                    {
                        "Mail", () =>
                        {
                            SlotIndexMin = 0;
                            SlotIndexMax = 2;
                            IsSlotVisible = Visibility.Visible;
                            IsOptionsVisible = Visibility.Visible;
                        }
                    },
                    {
                        "EquipItem", () =>
                        {
                            SlotIndexMin = itemData.SlotIndex;
                            SlotIndexMax = itemData.SlotIndex;
                            SlotFilter(itemData.SlotIndex);
                            IsSlotVisible = Visibility.Visible;
                            IsOptionsVisible = Visibility.Visible;
                        }
                    },
                    {
                        "InventoryItem", () =>
                        {
                            SlotIndexMin = 0;
                            SlotIndexMax = itemData.PageIndex == 5 ? 119 : 23;
                            ItemDataManager.InventoryTypeFilter = itemData.PageIndex;
                            IsSlotVisible = Visibility.Visible;
                            IsOptionsVisible = Visibility.Visible;
                        }
                    },
                    {
                        "StorageItem", () =>
                        {
                            SlotIndexMin = 0;
                            SlotIndexMax = 179;
                            IsSlotVisible = Visibility.Visible;
                            IsOptionsVisible = Visibility.Visible;
                        }
                    },
                    {
                        "AccountStorageItem", () =>
                        {
                            SlotIndexMin = 0;
                            SlotIndexMax = 179;
                            ItemDataManager.AccountStorageFilter = 1;
                            IsSlotVisible = Visibility.Visible;
                            IsOptionsVisible = Visibility.Visible;
                        }
                    },
                    {
                        "CashShopItemAdd", () =>
                        {
                            SelectionMode = DataGridSelectionMode.Extended;
                            IsSlotVisible = Visibility.Hidden;
                            IsOptionsVisible = Visibility.Hidden;
                            AddItemText = "Add Selected Item(s)";
                        }
                    },
                    {
                        "CashShopItemUpdate", () =>
                        {
                            SelectionMode = DataGridSelectionMode.Single;
                            IsSlotVisible = Visibility.Hidden;
                            IsOptionsVisible = Visibility.Hidden;
                        }
                    },
                    {
                        "CouponItem", () =>
                        {
                            IsNewItem = itemData.IsNewItem;
                            IsSlotVisible = Visibility.Hidden;
                            IsOptionsVisible = Visibility.Hidden;
                        }
                    },
                    {
                        "Package", () =>
                        {
                            SlotIndexMin = 1;
                            SlotIndexMax = 12;
                            IsSlotVisible = Visibility.Visible;
                            IsOptionsVisible = Visibility.Hidden;
                        }
                    },
                    {
                        "RandomRune", () =>
                        {
                            SlotIndexMin = 1;
                            SlotIndexMax = 10;
                            IsSlotVisible = Visibility.Visible;
                            IsOptionsVisible = Visibility.Hidden;
                        }
                    },
                    {
                        "DropGroup", () =>
                        {
                            SlotIndexMin = 0;
                            SlotIndexMax = itemData.OverlapCnt - 1;
                            IsSlotVisible = Visibility.Visible;
                            IsOptionsVisible = Visibility.Hidden;
                        }
                    },
                    {
                        "SetItem", () =>
                        {
                            SlotIndexMin = 0;
                            SlotIndexMax = 5;
                            IsSlotVisible = Visibility.Visible;
                            IsOptionsVisible = Visibility.Hidden;
                        }
                    }
                };

                if (!string.IsNullOrEmpty(message.MessageType) && actions.TryGetValue(message.MessageType, out var action))
                {
                    action();
                }
                else
                {
                    SlotIndexMin = itemData.SlotIndex;
                    SlotIndexMax = itemData.SlotIndex;
                    IsSlotVisible = Visibility.Visible;
                    IsOptionsVisible = Visibility.Visible;
                }

                if (itemData.ItemId != 0)
                {
                    LoadItemData(itemData);
                }
                else
                {
                    if (ItemDataManager.FrameViewModel != null)
                    {
                        ItemDataManager.FrameViewModel.PageIndex = itemData.PageIndex;
                        ItemDataManager.FrameViewModel.SlotIndex = itemData.SlotIndex;
                    }
                }
            }), DispatcherPriority.Background);
        }
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
        var frameViewModel = ItemDataManager.GetItemData(itemData);

        ItemDataManager.FrameViewModel = frameViewModel;

        ItemDataManager.FrameViewModel.SlotIndex = itemData.SlotIndex;
        ItemDataManager.FrameViewModel.ItemId = itemData.ItemId;
        ItemDataManager.FrameViewModel.ItemAmount = itemData.ItemAmount;
        ItemDataManager.FrameViewModel.MaxDurability = itemData.DurabilityMax;
        ItemDataManager.FrameViewModel.EnhanceLevel = itemData.EnhanceLevel;
        ItemDataManager.FrameViewModel.AugmentValue = itemData.AugmentStone;
        ItemDataManager.FrameViewModel.Rank = itemData.Rank;
        ItemDataManager.FrameViewModel.Weight = itemData.Weight;
        ItemDataManager.FrameViewModel.ReconstructionMax = itemData.ReconstructionMax;
        ItemDataManager.FrameViewModel.Reconstruction = itemData.Reconstruction;
        ItemDataManager.FrameViewModel.RandomOption01 = itemData.Option1Code;
        ItemDataManager.FrameViewModel.RandomOption02 = itemData.Option2Code;
        ItemDataManager.FrameViewModel.RandomOption03 = itemData.Option3Code;
        ItemDataManager.FrameViewModel.RandomOption01Value = itemData.Option1Value;
        ItemDataManager.FrameViewModel.RandomOption02Value = itemData.Option2Value;
        ItemDataManager.FrameViewModel.RandomOption03Value = itemData.Option3Value;
        ItemDataManager.FrameViewModel.SocketCount = itemData.SocketCount;
        ItemDataManager.FrameViewModel.Socket01Color = itemData.Socket1Color;
        ItemDataManager.FrameViewModel.Socket02Color = itemData.Socket2Color;
        ItemDataManager.FrameViewModel.Socket03Color = itemData.Socket3Color;
        ItemDataManager.FrameViewModel.SocketOption01 = itemData.Socket1Code;
        ItemDataManager.FrameViewModel.SocketOption02 = itemData.Socket2Code;
        ItemDataManager.FrameViewModel.SocketOption03 = itemData.Socket3Code;
        ItemDataManager.FrameViewModel.SocketOption01Value = itemData.Socket1Value;
        ItemDataManager.FrameViewModel.SocketOption02Value = itemData.Socket2Value;
        ItemDataManager.FrameViewModel.SocketOption03Value = itemData.Socket3Value;

        SelectedItem = ItemDataManager.ItemDataItems?.FirstOrDefault(item => item.ItemId == itemData.ItemId);

    }

    private void UpdateItemData(ItemData itemData)
    {
        if (ItemDataManager.FrameViewModel != null)
        {
            ItemDataManager.FrameViewModel.IsNewItem = IsNewItem;
            ItemDataManager.FrameViewModel.ItemId = itemData.ItemId;
            ItemDataManager.FrameViewModel.ItemName = itemData.ItemName;
            ItemDataManager.FrameViewModel.Description = itemData.Description;
            ItemDataManager.FrameViewModel.ItemBranch = itemData.Branch;
            ItemDataManager.FrameViewModel.IconName = itemData.IconName;
            ItemDataManager.FrameViewModel.ItemTrade = itemData.ItemTrade;
            ItemDataManager.FrameViewModel.MaxDurability = itemData.Durability;
            ItemDataManager.FrameViewModel.Weight = itemData.Weight;
            ItemDataManager.FrameViewModel.ReconstructionMax = itemData.ReconstructionMax;
            ItemDataManager.FrameViewModel.Reconstruction = itemData.ReconstructionMax;
            ItemDataManager.FrameViewModel.OverlapCnt = itemData.OverlapCnt;
            ItemDataManager.FrameViewModel.ItemAmount = itemData.ItemAmount;
            ItemDataManager.FrameViewModel.Rank = itemData.Rank;
            ItemDataManager.FrameViewModel.Type = itemData.Type;
            ItemDataManager.FrameViewModel.Category = itemData.Category;
            ItemDataManager.FrameViewModel.SubCategory = itemData.SubCategory;
            ItemDataManager.FrameViewModel.JobClass = itemData.JobClass;
            ItemDataManager.FrameViewModel.Defense = itemData.Defense;
            ItemDataManager.FrameViewModel.MagicDefense = itemData.MagicDefense;
            ItemDataManager.FrameViewModel.WeaponID00 = itemData.WeaponID00;
            ItemDataManager.FrameViewModel.SellPrice = itemData.SellPrice;
            ItemDataManager.FrameViewModel.RequiredLevel = itemData.LevelLimit;
            ItemDataManager.FrameViewModel.SetId = itemData.SetId;
            ItemDataManager.FrameViewModel.PetFood = itemData.PetFood;
            ItemDataManager.FrameViewModel.FixedOption01 = itemData.FixOption1Code;
            ItemDataManager.FrameViewModel.FixedOption01Value = itemData.FixOption1Value;
            ItemDataManager.FrameViewModel.FixedOption02 = itemData.FixOption2Code;
            ItemDataManager.FrameViewModel.FixedOption02Value = itemData.FixOption2Value;
            ItemDataManager.FrameViewModel.OptionCountMax = itemData.Type != 1 ? itemData.OptionCountMax : (itemData.Type == 1 && itemData.Category == 29 ? 1 : 0);
            ItemDataManager.FrameViewModel.SocketCountMax = itemData.SocketCountMax;
            ItemDataManager.FrameViewModel.SocketCount = itemData.SocketCountMax;
        }
        else
        {
            var frameViewModel = ItemDataManager.GetItemData(itemData);
            ItemDataManager.FrameViewModel = frameViewModel;
        }

    }

    #endregion

    #region Properties

    [ObservableProperty]
    private string _title = "Add Item";

    [ObservableProperty]
    private string _addItemText = "Add Selected Item";

    [ObservableProperty]
    private Visibility _isSlotVisible = Visibility.Hidden;

    [ObservableProperty]
    private Visibility _isOptionsVisible = Visibility.Hidden;

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
