using RHToolkit.Messages;
using RHToolkit.Models;
using RHToolkit.Models.Database;
using RHToolkit.Models.MessageBox;
using RHToolkit.Models.SQLite;
using RHToolkit.Properties;
using RHToolkit.Services;
using RHToolkit.ViewModels.Controls;
using static RHToolkit.Models.EnumService;

namespace RHToolkit.ViewModels.Windows;

public partial class EquipmentWindowViewModel : ObservableObject, IRecipient<CharacterInfoMessage>, IRecipient<ItemDataMessage>
{
    private readonly IWindowsService _windowsService;
    private readonly IDatabaseService _databaseService;
    private readonly IFrameService _frameService;
    private readonly IGMDatabaseService _gmDatabaseService;
    private readonly CachedDataManager _cachedDataManager;

    public EquipmentWindowViewModel(IWindowsService windowsService, IDatabaseService databaseService, CachedDataManager cachedDataManager, IFrameService frameService, IGMDatabaseService gmDatabaseService)
    {
        _windowsService = windowsService;
        _databaseService = databaseService;
        _cachedDataManager = cachedDataManager;
        _frameService = frameService;
        _gmDatabaseService = gmDatabaseService;

        WeakReferenceMessenger.Default.Register<ItemDataMessage>(this);
        WeakReferenceMessenger.Default.Register<CharacterInfoMessage>(this);
    }


    #region Load Character

    public async void Receive(CharacterInfoMessage message)
    {
        if (Token == Guid.Empty)
        {
            Token = message.Token;
        }

        if (message.Recipient == "EquipmentWindow" && message.Token == Token)
        {
            var characterInfo = message.Value;

            await ReadCharacterData(characterInfo.CharacterName!);
        }

        WeakReferenceMessenger.Default.Unregister<CharacterInfoMessage>(this);
    }

    private async Task ReadCharacterData(string characterName)
    {
        try
        {
            CharacterData? characterData = await _databaseService.GetCharacterDataAsync(characterName);

            if (characterData != null)
            {
                ClearData();
                CharacterData = characterData;
                LoadCharacterData(characterData);
                List<ItemData> equipItems = await _databaseService.GetItemList(characterData.CharacterID, "N_EquipItem");
                LoadEquipmentItems(equipItems);
                ItemDatabaseList = equipItems;
            }
            else
            {
                RHMessageBoxHelper.ShowOKMessage($"The character '{characterName}' does not exist.", "Invalid Character");
                return;
            }
        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"Error reading Character Data: {ex.Message}", "Error");
        }
    }

    private void LoadCharacterData(CharacterData characterData)
    {
        Title = $"Character Equipment ({characterData.CharacterName})";
        CharacterName = characterData.CharacterName;
        Class = characterData.Class;
        Job = characterData.Job;
        Level = characterData.Level;
    }

    private void ClearData()
    {
        ClearAllEquipItems();
        CharacterData = null;
        ItemDatabaseList = null;
        DeletedItemDatabaseList = null;
    }
    #endregion

    #region Commands

    #region Save
    [RelayCommand]
    private async Task SaveEquipment()
    {
        if (CharacterData == null) return;

        if (!SqlCredentialValidator.ValidateCredentials())
        {
            return;
        }

        try
        {
            
        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"Error saving equipment changes: {ex.Message}", "Error");
        }
    }

    #endregion


    #endregion

    #region Item Data

    #region Receive
    public void Receive(ItemDataMessage message)
    {
        if (message.Recipient == "EquipWindow" && message.Token == Token)
        {
            var newItemData = message.Value;

            ItemDatabaseList ??= [];

            // Find the existing item in ItemDatabaseList
            var existingItem = ItemDatabaseList.FirstOrDefault(item => item.SlotIndex == newItemData.SlotIndex);

            if (existingItem != null)
            {
                // Update existing item with the received data
                UpdateItem(existingItem, newItemData);

            }
            else
            {
                // Create a new item with the received data
                var newItem = CreateNewItem(newItemData);

                // Add the new item to the list
                ItemDatabaseList.Add(newItem);

                SetItemData(newItem);
            }
        }
    }

    private void UpdateItem(ItemData existingItem, ItemData newItemData)
    {
        // Check if the IDs are different
        if (existingItem.ID != newItemData.ID)
        {
            // Move the existing item to DeletedItemDatabaseList
            DeletedItemDatabaseList ??= [];
            DeletedItemDatabaseList.Add(existingItem);

            // Remove the existing item from ItemDatabaseList
            ItemDatabaseList?.Remove(existingItem);

            // Create a new item with the received data
            var newItem = CreateNewItem(newItemData);

            // Add the new item to the list
            ItemDatabaseList?.Add(newItem);

            SetItemData(newItem);
        }
        else
        {
            // Update only the properties sent by SelectItem
            existingItem.Name = newItemData.Name;
            existingItem.IconName = newItemData.IconName;
            existingItem.Durability = newItemData.Durability;
            existingItem.DurabilityMax = newItemData.DurabilityMax;
            existingItem.EnhanceLevel = newItemData.EnhanceLevel;
            existingItem.AugmentStone = newItemData.AugmentStone;
            existingItem.Rank = newItemData.Rank;
            existingItem.Weight = newItemData.Weight;
            existingItem.Reconstruction = newItemData.Reconstruction;
            existingItem.ReconstructionMax = newItemData.ReconstructionMax;
            existingItem.Amount = newItemData.Amount;
            existingItem.Option1Code = newItemData.Option1Code;
            existingItem.Option2Code = newItemData.Option2Code;
            existingItem.Option3Code = newItemData.Option3Code;
            existingItem.Option1Value = newItemData.Option1Value;
            existingItem.Option2Value = newItemData.Option2Value;
            existingItem.Option3Value = newItemData.Option3Value;
            existingItem.SocketCount = newItemData.SocketCount;
            existingItem.Socket1Color = newItemData.Socket1Color;
            existingItem.Socket2Color = newItemData.Socket2Color;
            existingItem.Socket3Color = newItemData.Socket3Color;
            existingItem.Socket1Code = newItemData.Socket1Code;
            existingItem.Socket2Code = newItemData.Socket2Code;
            existingItem.Socket3Code = newItemData.Socket3Code;
            existingItem.Socket1Value = newItemData.Socket1Value;
            existingItem.Socket2Value = newItemData.Socket2Value;
            existingItem.Socket3Value = newItemData.Socket3Value;
            existingItem.UpdateTime = DateTime.Now;

            SetItemData(existingItem);
        }
    }

    private ItemData CreateNewItem(ItemData newItemData)
    {
        var newItem = new ItemData
        {
            CharacterId = CharacterData!.CharacterID,
            AuthId = CharacterData.AuthID,
            ItemUid = Guid.NewGuid(),
            PageIndex = 0,
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
            ID = newItemData.ID,
            Name = newItemData.Name,
            IconName = newItemData.IconName,
            Durability = newItemData.Durability,
            DurabilityMax = newItemData.DurabilityMax,
            EnhanceLevel = newItemData.EnhanceLevel,
            AugmentStone = newItemData.AugmentStone,
            Rank = newItemData.Rank,
            Weight = newItemData.Weight,
            Reconstruction = newItemData.Reconstruction,
            ReconstructionMax = newItemData.ReconstructionMax,
            Amount = newItemData.Amount,
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

    private void LoadEquipmentItems(List<ItemData> equipmentItems)
    {
        if (equipmentItems != null)
        {
            foreach (var equipmentItem in equipmentItems)
            {
                SetItemData(equipmentItem);
            }
        }
    }

    private void SetItemData(ItemData equipmentItem)
    {
        // Find the corresponding ItemData in the _cachedItemDataList
        ItemData cachedItem = _cachedDataManager.CachedItemDataList?.FirstOrDefault(item => item.ID == equipmentItem.ID) ?? new ItemData();

        ItemData itemData = new()
        {
            ID = cachedItem.ID,
            Name = cachedItem.Name ?? "",
            Description = cachedItem.Description ?? "",
            IconName = cachedItem.IconName ?? "",
            Type = cachedItem.Type,
            WeaponID00 = cachedItem.WeaponID00,
            Category = cachedItem.Category,
            SubCategory = cachedItem.SubCategory,
            LevelLimit = cachedItem.LevelLimit,
            ItemTrade = cachedItem.ItemTrade,
            OverlapCnt = cachedItem.OverlapCnt,
            Defense = cachedItem.Defense,
            MagicDefense = cachedItem.MagicDefense,
            Branch = cachedItem.Branch,
            OptionCountMax = cachedItem.OptionCountMax,
            SocketCountMax = cachedItem.SocketCountMax,
            SellPrice = cachedItem.SellPrice,
            PetFood = cachedItem.PetFood,
            JobClass = cachedItem.JobClass,
            SetId = cachedItem.SetId,
            FixOption1Code = cachedItem.FixOption1Code,
            FixOption1Value = cachedItem.FixOption1Value,
            FixOption2Code = cachedItem.FixOption2Code,
            FixOption2Value = cachedItem.FixOption2Value,

            ItemUid = equipmentItem.ItemUid,
            CharacterId = equipmentItem.CharacterId,
            AuthId = equipmentItem.AuthId,
            PageIndex = equipmentItem.PageIndex,
            SlotIndex = equipmentItem.SlotIndex,
            Amount = equipmentItem.Amount,
            Reconstruction = equipmentItem.Reconstruction,
            ReconstructionMax = equipmentItem.ReconstructionMax,
            AugmentStone = equipmentItem.AugmentStone,
            Rank = equipmentItem.Rank,
            AcquireRoute = equipmentItem.AcquireRoute,
            Physical = equipmentItem.Physical,
            Magical = equipmentItem.Magical,
            DurabilityMax = equipmentItem.DurabilityMax,
            Weight = equipmentItem.Weight,
            RemainTime = equipmentItem.RemainTime,
            CreateTime = equipmentItem.CreateTime,
            UpdateTime = equipmentItem.UpdateTime,
            GCode = equipmentItem.GCode,
            Durability = equipmentItem.Durability,
            EnhanceLevel = equipmentItem.EnhanceLevel,
            Option1Code = equipmentItem.Option1Code,
            Option1Value = equipmentItem.Option1Value,
            Option2Code = equipmentItem.Option2Code,
            Option2Value = equipmentItem.Option2Value,
            Option3Code = equipmentItem.Option3Code,
            Option3Value = equipmentItem.Option3Value,
            OptionGroup = equipmentItem.OptionGroup,
            SocketCount = equipmentItem.SocketCount,
            Socket1Code = equipmentItem.Socket1Code,
            Socket1Value = equipmentItem.Socket1Value,
            Socket2Code = equipmentItem.Socket2Code,
            Socket2Value = equipmentItem.Socket2Value,
            Socket3Code = equipmentItem.Socket3Code,
            Socket3Value = equipmentItem.Socket3Value,
            ExpireTime = equipmentItem.ExpireTime,
            LockPassword = equipmentItem.LockPassword,
            LinkId = equipmentItem.LinkId,
            IsSeizure = equipmentItem.IsSeizure,
            Socket1Color = equipmentItem.Socket1Color,
            Socket2Color = equipmentItem.Socket2Color,
            Socket3Color = equipmentItem.Socket3Color,
            DefermentTime = equipmentItem.DefermentTime,
        };

        var frameViewModel = new FrameViewModel(_frameService, _gmDatabaseService)
        {
            ItemData = itemData
        };

        SetItemProperties(itemData);
        SetFrameViewModelProperties(frameViewModel);
    }

    private void SetItemProperties(ItemData itemData)
    {
        // Construct property names dynamically
        string iconNameProperty = $"ItemIcon{itemData.SlotIndex}";
        string iconBranchProperty = $"ItemIconBranch{itemData.SlotIndex}";
        string nameProperty = $"ItemName{itemData.SlotIndex}";

        // Set properties using reflection
        GetType().GetProperty(iconNameProperty)?.SetValue(this, itemData.IconName);
        GetType().GetProperty(iconBranchProperty)?.SetValue(this, itemData.Branch);
        GetType().GetProperty(nameProperty)?.SetValue(this, itemData.Name);
    }

    private void SetFrameViewModelProperties(FrameViewModel frameViewModel)
    {
        // Construct property names dynamically
        string frameViewModelProperty = $"FrameViewModel{frameViewModel.SlotIndex}";

        // Set properties using reflection
        GetType().GetProperty(frameViewModelProperty)?.SetValue(this, frameViewModel);
    }

    #endregion

    #region Send
    [RelayCommand]
    private void AddItem(string parameter)
    {
        if (CharacterData == null) return;

        if (int.TryParse(parameter, out int slotIndex))
        {
            ItemData? itemData = ItemDatabaseList?.FirstOrDefault(i => i.SlotIndex == slotIndex) ?? new ItemData { SlotIndex = slotIndex };
            var characterInfo = new CharacterInfo(CharacterData.CharacterID, CharacterData.AuthID, CharacterData.CharacterName!, CharacterData.AccountName!, CharacterData.Class, CharacterData.Job);

            _windowsService.OpenItemWindow(characterInfo.CharacterID, "EquipItem", itemData, characterInfo);
        }
    }

    #endregion

    #region Remove Item

    [ObservableProperty]
    private List<ItemData>? _deletedItemDatabaseList;

    [RelayCommand]
    private void RemoveItem(string parameter)
    {
        if (int.TryParse(parameter, out int slotIndex))
        {
            if (ItemDatabaseList != null)
            {
                var removedItemIndex = ItemDatabaseList.FindIndex(i => i.SlotIndex == slotIndex);
                if (removedItemIndex != -1)
                {
                    // Get the removed item
                    var removedItem = ItemDatabaseList[removedItemIndex];

                    // Set the SlotIndex to a negative value
                    if (slotIndex == 0)
                    {
                        removedItem.SlotIndex = -1;
                    }
                    else
                    {
                        removedItem.SlotIndex = -slotIndex;
                    }

                    // Remove the ItemData with the specified SlotIndex from ItemDatabaseList
                    ItemDatabaseList.RemoveAt(removedItemIndex);

                    // Add the removed item to DeletedItemDatabaseList
                    DeletedItemDatabaseList ??= [];
                    DeletedItemDatabaseList.Add(removedItem);

                    // Update properties to default values for the removed SlotIndex
                    ResetItemProperties(slotIndex);
                }
            }
        }
    }

    private void ClearAllEquipItems()
    {
        for (int i = 0; i < 20; i++)
        {
            ResetItemProperties(i);
        }
    }

    private void ResetItemProperties(int slotIndex)
    {
        string iconNameProperty = $"ItemIcon{slotIndex}";
        string iconBranchProperty = $"ItemIconBranch{slotIndex}";
        string nameProperty = $"ItemName{slotIndex}";
        string amountProperty = $"ItemAmount{slotIndex}";
        string frameViewModelProperty = $"FrameViewModel{slotIndex}";

        GetType().GetProperty(iconNameProperty)?.SetValue(this, null);
        GetType().GetProperty(iconBranchProperty)?.SetValue(this, 0);
        GetType().GetProperty(nameProperty)?.SetValue(this, Resources.AddItemDesc);
        GetType().GetProperty(amountProperty)?.SetValue(this, 0);
        GetType().GetProperty(frameViewModelProperty)?.SetValue(this, null);
    }

    #endregion

    #endregion

    #region Properties

    [ObservableProperty]
    private Guid? _token = Guid.Empty;

    #region Character
    [ObservableProperty]
    private CharacterData? _characterData;

    [ObservableProperty]
    private bool _isButtonEnabled = true;

    [ObservableProperty]
    private string _title = "Equipment Editor";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CharacterClassText))]
    private string? _className;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CharacterClassText))]
    private string? _jobName = Resources.Basic;

    public string? CharacterNameText => $"Lv.{Level} {CharacterName}";

    public string? CharacterClassText => $"<{ClassName} - {JobName} Focus> ";

    [ObservableProperty]
    private string? _charSilhouetteImage;


    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CharacterNameText))]
    private string? _characterName;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Job))]
    [NotifyPropertyChangedFor(nameof(CharacterClassText))]
    [NotifyPropertyChangedFor(nameof(CharSilhouetteImage))]
    private int _class;
    partial void OnClassChanged(int value)
    {
        ClassName = GetEnumDescription((CharClass)value);
        CharSilhouetteImage = CharacterHelper.GetClassImage(value);
        if (CharacterData != null)
        {
            Job = 0;
            Job = CharacterData.Job;
        }
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(JobName))]
    private int _job;
    partial void OnJobChanged(int value)
    {
        Enum jobEnum = GetJobEnum((CharClass)Class, value);
        JobName = GetEnumDescription(jobEnum);
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CharacterNameText))]
    private int _level;

    #endregion

    #region Equipament 
    [ObservableProperty]
    private FrameViewModel? _frameViewModel0;

    [ObservableProperty]
    private FrameViewModel? _frameViewModel1;

    [ObservableProperty]
    private FrameViewModel? _frameViewModel2;

    [ObservableProperty]
    private FrameViewModel? _frameViewModel3;

    [ObservableProperty]
    private FrameViewModel? _frameViewModel4;

    [ObservableProperty]
    private FrameViewModel? _frameViewModel5;

    [ObservableProperty]
    private FrameViewModel? _frameViewModel6;

    [ObservableProperty]
    private FrameViewModel? _frameViewModel7;

    [ObservableProperty]
    private FrameViewModel? _frameViewModel8;

    [ObservableProperty]
    private FrameViewModel? _frameViewModel9;

    [ObservableProperty]
    private FrameViewModel? _frameViewModel10;

    [ObservableProperty]
    private FrameViewModel? _frameViewModel11;

    [ObservableProperty]
    private FrameViewModel? _frameViewModel12;

    [ObservableProperty]
    private FrameViewModel? _frameViewModel13;

    [ObservableProperty]
    private FrameViewModel? _frameViewModel14;

    [ObservableProperty]
    private FrameViewModel? _frameViewModel15;

    [ObservableProperty]
    private FrameViewModel? _frameViewModel16;

    [ObservableProperty]
    private FrameViewModel? _frameViewModel17;

    [ObservableProperty]
    private FrameViewModel? _frameViewModel18;

    [ObservableProperty]
    private FrameViewModel? _frameViewModel19;

    [ObservableProperty]
    private List<ItemData>? _itemDatabaseList;

    [ObservableProperty]
    private string? _itemName0 = Resources.AddItemDesc;

    [ObservableProperty]
    private string? _itemName1 = Resources.AddItemDesc;

    [ObservableProperty]
    private string? _itemName2 = Resources.AddItemDesc;

    [ObservableProperty]
    private string? _itemName3 = Resources.AddItemDesc;

    [ObservableProperty]
    private string? _itemName4 = Resources.AddItemDesc;

    [ObservableProperty]
    private string? _itemName5 = Resources.AddItemDesc;

    [ObservableProperty]
    private string? _itemName6 = Resources.AddItemDesc;

    [ObservableProperty]
    private string? _itemName7 = Resources.AddItemDesc;

    [ObservableProperty]
    private string? _itemName8 = Resources.AddItemDesc;

    [ObservableProperty]
    private string? _itemName9 = Resources.AddItemDesc;

    [ObservableProperty]
    private string? _itemName10 = Resources.AddItemDesc;

    [ObservableProperty]
    private string? _itemName11 = Resources.AddItemDesc;

    [ObservableProperty]
    private string? _itemName12 = Resources.AddItemDesc;

    [ObservableProperty]
    private string? _itemName13 = Resources.AddItemDesc;

    [ObservableProperty]
    private string? _itemName14 = Resources.AddItemDesc;

    [ObservableProperty]
    private string? _itemName15 = Resources.AddItemDesc;

    [ObservableProperty]
    private string? _itemName16 = Resources.AddItemDesc;

    [ObservableProperty]
    private string? _itemName17 = Resources.AddItemDesc;

    [ObservableProperty]
    private string? _itemName18 = Resources.AddItemDesc;

    [ObservableProperty]
    private string? _itemName19 = Resources.AddItemDesc;

    [ObservableProperty]
    private string? _itemName21 = "Spirit (Not Implemented)";

    [ObservableProperty]
    private string? _itemIcon0;

    [ObservableProperty]
    private string? _itemIcon1;

    [ObservableProperty]
    private string? _itemIcon2;

    [ObservableProperty]
    private string? _itemIcon3;

    [ObservableProperty]
    private string? _itemIcon4;

    [ObservableProperty]
    private string? _itemIcon5;

    [ObservableProperty]
    private string? _itemIcon6;

    [ObservableProperty]
    private string? _itemIcon7;

    [ObservableProperty]
    private string? _itemIcon8;

    [ObservableProperty]
    private string? _itemIcon9;

    [ObservableProperty]
    private string? _itemIcon10;

    [ObservableProperty]
    private string? _itemIcon11;

    [ObservableProperty]
    private string? _itemIcon12;

    [ObservableProperty]
    private string? _itemIcon13;

    [ObservableProperty]
    private string? _itemIcon14;

    [ObservableProperty]
    private string? _itemIcon15;

    [ObservableProperty]
    private string? _itemIcon16;

    [ObservableProperty]
    private string? _itemIcon17;

    [ObservableProperty]
    private string? _itemIcon18;

    [ObservableProperty]
    private string? _itemIcon19;

    [ObservableProperty]
    private int? _itemIconBranch0;

    [ObservableProperty]
    private int? _itemIconBranch1;

    [ObservableProperty]
    private int? _itemIconBranch2;

    [ObservableProperty]
    private int? _itemIconBranch3;

    [ObservableProperty]
    private int? _itemIconBranch4;

    [ObservableProperty]
    private int? _itemIconBranch5;

    [ObservableProperty]
    private int? _itemIconBranch6;

    [ObservableProperty]
    private int? _itemIconBranch7;

    [ObservableProperty]
    private int? _itemIconBranch8;

    [ObservableProperty]
    private int? _itemIconBranch9;

    [ObservableProperty]
    private int? _itemIconBranch10;

    [ObservableProperty]
    private int? _itemIconBranch11;

    [ObservableProperty]
    private int? _itemIconBranch12;

    [ObservableProperty]
    private int? _itemIconBranch13;

    [ObservableProperty]
    private int? _itemIconBranch14;

    [ObservableProperty]
    private int? _itemIconBranch15;

    [ObservableProperty]
    private int? _itemIconBranch16;

    [ObservableProperty]
    private int? _itemIconBranch17;

    [ObservableProperty]
    private int? _itemIconBranch18;

    [ObservableProperty]
    private int? _itemIconBranch19;

    #endregion

    #endregion
}
