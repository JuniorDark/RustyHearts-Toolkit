using RHToolkit.Messages;
using RHToolkit.Models;
using RHToolkit.Models.Database;
using RHToolkit.Models.MessageBox;
using RHToolkit.Properties;
using RHToolkit.Services;
using RHToolkit.Views.Windows;
using static RHToolkit.Models.EnumService;

namespace RHToolkit.ViewModels.Windows;

public partial class CharacterWindowViewModel : ObservableObject, IRecipient<ItemDataMessage>, IRecipient<CharacterDataMessage>, IRecipient<DatabaseItemMessage>
{
    private readonly WindowsProviderService _windowsProviderService;
    private readonly IDatabaseService _databaseService;
    private readonly IGMDatabaseService _gmDatabaseService;
    private readonly List<ItemData>? _cachedItemDataList = [];

    public CharacterWindowViewModel(WindowsProviderService windowsProviderService, IDatabaseService databaseService, IGMDatabaseService gmDatabaseService)
    {
        _windowsProviderService = windowsProviderService;
        _databaseService = databaseService;
        _gmDatabaseService = gmDatabaseService;

        if (ItemDataManager.Instance.CachedItemDataList == null)
        {
            ItemDataManager.Instance.InitializeCachedLists();
        }

        _cachedItemDataList = ItemDataManager.Instance.CachedItemDataList;

        PopulateClassItems();
        PopulateLobbyItems();

        WeakReferenceMessenger.Default.Register<ItemDataMessage>(this);
        WeakReferenceMessenger.Default.Register<CharacterDataMessage>(this);
        WeakReferenceMessenger.Default.Register<DatabaseItemMessage>(this);

    }

    #region Load Character

    [ObservableProperty]
    private CharacterData? _characterData;

    public void Receive(CharacterDataMessage message)
    {
        var characterData = message.Value;
        CharacterData = message.Value;
        LoadCharacterData(characterData);
    }

    private void LoadCharacterData(CharacterData characterData)
    {
        if (characterData != null)
        {
            CharacterID = characterData.CharacterID;
            CharacterName = characterData.CharacterName;
            WindyCode = characterData.WindyCode;
            Class = characterData.Class;
            Job = characterData.Job;
            Level = characterData.Level;
            CharacterExp = characterData.Experience;
            TotalSkillPoints = characterData.TotalSP;
            SkillPoints = characterData.SP;
            Lobby = characterData.LobbyID;
            CreateTime = characterData.CreateTime;
            LastLoginTime = characterData.LastLogin;
            InventoryGold = characterData.Gold;
            RessurectionScrolls = characterData.Hearts;
            WarehouseGold = characterData.StorageGold;
            WarehouseSlots = characterData.StorageCount;
            GuildExp = characterData.GuildPoint;
            GuildName = characterData.GuildName;
            HasGuild = characterData.HasGuild;
            RestrictCharacter = characterData.BlockYN == "Y";
            IsConnect = characterData.IsConnect == "Y";
            IsTradeEnable = characterData.IsTradeEnable == "Y";
            IsMoveEnable = characterData.IsMoveEnable == "Y";
            IsAdmin = characterData.Permission == 100;
            Title = $"Character Editor ({characterData.CharacterName})";
        }
    }

    private async Task ReadCharacterData()
    {
        try
        {
            if (CharacterName != null)
            {
                CharacterData? characterData = await _databaseService.GetCharacterDataAsync(CharacterName);

                if (characterData != null)
                {
                    LoadCharacterData(characterData);
                }
                else
                {
                    RHMessageBox.ShowOKMessage($"The character '{CharacterName}' does not exist.", "Invalid Character");
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            RHMessageBox.ShowOKMessage($"Error reading Character Data: {ex.Message}", "Error");
        }
    }

    #endregion

    #region Save Character
    [RelayCommand]
    private async Task SaveCharacter()
    {
        try
        {
            if (CharacterData != null)
            {

                if (await _databaseService.IsCharacterOnlineAsync(CharacterData.CharacterName!))
                {
                    RHMessageBox.ShowOKMessage("This character is online. You can't edit an online character.", "Info");
                    return;
                }

                var newCharacterData = GetCharacterChanges();

                if (!CharacterManager.HasCharacterDataChanges(CharacterData, newCharacterData))
                {
                    RHMessageBox.ShowOKMessage("There are no changes to save.", "Info");
                    return;
                }

                string changes = CharacterManager.GenerateCharacterDataMessage(CharacterData, newCharacterData, "changes");

                if (RHMessageBox.ConfirmMessage($"Save the following changes to the character {CharacterData.CharacterName}?\n\n{changes}"))
                {
                    string modify = CharacterManager.GenerateCharacterDataMessage(CharacterData, newCharacterData, "audit");

                    await _databaseService.UpdateCharacterDataAsync(CharacterData.CharacterID, newCharacterData);
                    await _databaseService.GMAuditAsync(CharacterData.WindyCode!, CharacterData.CharacterID, CharacterData.CharacterName!, "Character Information Change", modify);

                    RHMessageBox.ShowOKMessage("Character saved successfully!", "Success");
                    await ReadCharacterData();
                }

            }
        }
        catch (Exception ex)
        {
            RHMessageBox.ShowOKMessage($"Error saving character changes: {ex.Message}", "Error");
        }
    }

    private NewCharacterData GetCharacterChanges()
    {
        return new NewCharacterData
        {
            Class = Class,
            Job = Job,
            Level = Level,
            Experience = CharacterExp,
            SP = SkillPoints,
            TotalSP = TotalSkillPoints,
            LobbyID = Lobby,
            Gold = InventoryGold,
            Hearts = RessurectionScrolls,
            StorageGold = WarehouseGold,
            StorageCount = WarehouseSlots,
            GuildPoint = GuildExp,
            Permission = IsAdmin == true ? 100 : 0,
            BlockYN = RestrictCharacter == true ? "Y" : "N",
            IsTradeEnable = IsTradeEnable == true ? "Y" : "N",
            IsMoveEnable = IsMoveEnable == true ? "Y" : "N",
        };
    }

    #endregion

    #region Character Name
    [RelayCommand]
    private async Task SaveCharacterName()
    {
        string? newCharacterName = CharacterName;

        if (CharacterData == null || newCharacterName == null || CharacterData.CharacterName == newCharacterName)
        {
            return;
        }

        if (IsNameNotAllowed(newCharacterName))
        {
            RHMessageBox.ShowOKMessage("This character name is not allowed.", "Error");
            return;
        }

        if (!ValidateCharacterName(newCharacterName))
        {
            RHMessageBox.ShowOKMessage("Invalid character name.", "Error");
            return;
        }

        if (RHMessageBox.ConfirmMessage($"Change this character name to '{newCharacterName}'?"))
        {
            try
            {
                int result = await _databaseService.UpdateCharacterNameAsync(CharacterData.CharacterID, newCharacterName);

                if (result == -1)
                {
                    RHMessageBox.ShowOKMessage("Character name already exists.", "Error");
                    return;
                }

                await _databaseService.GMAuditAsync(CharacterData.WindyCode!, CharacterData.CharacterID, newCharacterName, "Character Name Change", $"Old Name:{CharacterData.CharacterName}, New Name: {newCharacterName}");

                RHMessageBox.ShowOKMessage("Character name updated successfully!", "Success");
                CharacterData.CharacterName = newCharacterName;
            }
            catch (Exception ex)
            {
                RHMessageBox.ShowOKMessage($"Error updating character name: {ex.Message}", "Error");
            }
        }

    }

    private bool IsNameNotAllowed(string characterName)
    {
        return _gmDatabaseService.IsNameInNickFilter(characterName);
    }

    private static bool ValidateCharacterName(string name)
    {
        return !string.IsNullOrEmpty(name) && name.Length >= 3 && name.Length <= 16;
    }
    #endregion

    #region Comboboxes

    [ObservableProperty]
    private int _classSelectedIndex;

    [ObservableProperty]
    private List<NameID>? _classItems;

    private void PopulateClassItems()
    {
        try
        {
            ClassItems = GetEnumItems<CharClass>();

            if (ClassItems.Count > 0)
            {
                ClassSelectedIndex = 0;
            }
        }
        catch (Exception ex)
        {
            RHMessageBox.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
        }
    }

    [ObservableProperty]
    private int _jobSelectedIndex;

    [ObservableProperty]
    private List<NameID>? _jobItems;

    [ObservableProperty]
    private int _lobbySelectedIndex;

    [ObservableProperty]
    private List<NameID>? _lobbyItems;

    private void PopulateLobbyItems()
    {
        try
        {
            LobbyItems = _gmDatabaseService.GetLobbyItems(); ;

            if (LobbyItems.Count > 0)
            {
                LobbySelectedIndex = 0;
            }
        }
        catch (Exception ex)
        {
            RHMessageBox.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
        }
    }

    #endregion

    #region Item Data

    public void Receive(ItemDataMessage message)
    {
        if (message.Recipient == ViewModelType.CharacterWindowViewModel)
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
            }

            SetItemProperties(newItemData);
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

            // Update UI with the new values
            SetItemProperties(existingItem);
        }
    }

    private ItemData CreateNewItem(ItemData newItemData)
    {
        // Create a new item with the received data
        var newItem = new ItemData
        {
            // Assign received data
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
            // Generate values for additional properties
            CharacterId = CharacterData!.CharacterID,
            AuthId = CharacterData.AuthID,
            ItemUid = Guid.NewGuid(),
            CreateTime = DateTime.Now,
            UpdateTime = DateTime.Now,

        };

        return newItem;
    }

    [ObservableProperty]
    private List<ItemData>? _itemDatabaseList;

    public void Receive(DatabaseItemMessage message)
    {
        switch (message.ItemStorageType)
        {
            case ItemStorageType.Inventory:
                // Handle inventory items
                List<ItemData> inventoryItems = message.Value;

                break;

            case ItemStorageType.Equipment:
                // Handle equipment items
                var equipmentItems = message.Value;
                LoadEquipmentItems(equipmentItems);
                ItemDatabaseList = equipmentItems;
                break;

            case ItemStorageType.Storage:
                // Handle equipment items
                List<ItemData> storageItems = message.Value;
                // Do something with the storage items...
                break;

            default:
                break;
        }
    }

    private void LoadEquipmentItems(List<ItemData> equipmentItems)
    {
        if (equipmentItems != null)
        {
            for (int i = 0; i < equipmentItems.Count; i++)
            {
                ItemData equipmentItem = equipmentItems[i];

                // Access the property from the DatabaseItem object
                int index = equipmentItem.SlotIndex;
                int code = equipmentItem.ID;

                // Find the corresponding ItemData object in the _cachedItemDataList
                ItemData? cachedItem = _cachedItemDataList?.FirstOrDefault(item => item.ID == code);

                ItemData itemData = new()
                {
                    SlotIndex = index,
                    ID = code,
                    Name = cachedItem?.Name ?? "",
                    IconName = cachedItem?.IconName ?? "",
                    Branch = cachedItem?.Branch ?? 0,

                };

                SetItemProperties(itemData);
            }

        }
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

    private ItemWindow? _itemWindowInstance;

    [RelayCommand]
    private void AddItem(string parameter)
    {
        if (int.TryParse(parameter, out int slotIndex))
        {
            ItemData? itemData = ItemDatabaseList?.FirstOrDefault(i => i.SlotIndex == slotIndex);

            itemData ??= new ItemData
            {
                SlotIndex = slotIndex
            };

            if (_itemWindowInstance == null)
            {
                _windowsProviderService.Show<ItemWindow>();
                _itemWindowInstance = Application.Current.Windows.OfType<ItemWindow>().FirstOrDefault();

                if (_itemWindowInstance != null)
                {
                    _itemWindowInstance.Closed += (sender, args) => _itemWindowInstance = null;

                    _itemWindowInstance.ContentRendered += (sender, args) =>
                    {
                        WeakReferenceMessenger.Default.Send(new ItemDataMessage(itemData, ViewModelType.ItemWindowViewModel, "EquipItem"));

                        if (CharacterData != null)
                        {
                            WeakReferenceMessenger.Default.Send(new CharacterDataMessage(CharacterData));
                        }
                    };
                }
            }
            else
            {
                WeakReferenceMessenger.Default.Send(new ItemDataMessage(itemData, ViewModelType.ItemWindowViewModel, "EquipItem"));
                if (CharacterData != null)
                {
                    WeakReferenceMessenger.Default.Send(new CharacterDataMessage(CharacterData));
                }
                Task.Delay(500);
                _itemWindowInstance.Focus();
            }
        }
    }

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


    private void ResetItemProperties(int slotIndex)
    {
        // Construct property names dynamically
        string iconNameProperty = $"ItemIcon{slotIndex}";
        string iconBranchProperty = $"ItemIconBranch{slotIndex}";
        string nameProperty = $"ItemName{slotIndex}";
        string amountProperty = $"ItemAmount{slotIndex}";

        // Set properties using reflection
        GetType().GetProperty(iconNameProperty)?.SetValue(this, null);
        GetType().GetProperty(iconBranchProperty)?.SetValue(this, 0);
        GetType().GetProperty(nameProperty)?.SetValue(this, Resources.AddItemDesc);
        GetType().GetProperty(amountProperty)?.SetValue(this, 0);
    }

    #endregion


    #endregion

    #region Character Data Properties

    [ObservableProperty]
    private bool _isButtonEnabled = true;

    [ObservableProperty]
    private string? _title;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CharacterClassText))]
    private string? _className;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CharacterClassText))]
    private string? _jobName;

    public string? CharacterNameText => $"Lv.{Level} {CharacterName}";

    public string? CharacterClassText => $"<{ClassName} - {JobName} Focus> ";

    [ObservableProperty]
    private string? _charSilhouetteImage;

    [ObservableProperty]
    private string? _windyCode;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CharacterIDText))]
    private Guid _characterID;

    public string? CharacterIDText => $"{CharacterID.ToString().ToUpper()} ";

    [ObservableProperty]
    private DateTime _createTime;

    [ObservableProperty]
    private DateTime _lastLoginTime;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CharacterNameText))]
    private string? _characterName;

    [ObservableProperty]
    private bool _hasGuild;

    [ObservableProperty]
    private bool _isConnect;

    [ObservableProperty]
    private bool _restrictCharacter;

    [ObservableProperty]
    private bool _isMoveEnable;

    [ObservableProperty]
    private bool _isTradeEnable;

    [ObservableProperty]
    private bool _isAdmin;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CharacterClassText))]
    [NotifyPropertyChangedFor(nameof(CharSilhouetteImage))]
    private int _class;
    partial void OnClassChanged(int value)
    {
        ClassName = GetEnumDescription((CharClass)value);
        CharSilhouetteImage = CharacterManager.GetClassImage(value);
        JobItems?.Clear();
        JobItems = CharacterManager.GetJobItems((CharClass)value);
        if (CharacterData != null)
        {
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
    private int _lobby;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CharacterNameText))]
    private int _level;
    partial void OnLevelChanged(int value)
    {
        long experience = _gmDatabaseService.GetExperienceFromLevel(value);
        CharacterExp = experience;
    }

    [ObservableProperty]
    private long _characterExp;

    [ObservableProperty]
    private int _skillPoints;

    [ObservableProperty]
    private int _totalSkillPoints;
    partial void OnTotalSkillPointsChanged(int value)
    {
        if (SkillPoints > value)
        {
            SkillPoints = value;
        }
    }

    [ObservableProperty]
    private int _inventoryGold;

    [ObservableProperty]
    private int _ressurectionScrolls;

    [ObservableProperty]
    private int _warehouseSlots;

    [ObservableProperty]
    private int _warehouseGold;

    [ObservableProperty]
    private int _guildExp;

    [ObservableProperty]
    private string? _guildName;

    #region Equipament 

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
