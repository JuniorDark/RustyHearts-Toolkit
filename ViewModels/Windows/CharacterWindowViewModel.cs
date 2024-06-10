using RHToolkit.Messages;
using RHToolkit.Models;
using RHToolkit.Models.Database;
using RHToolkit.Models.MessageBox;
using RHToolkit.Models.SQLite;
using RHToolkit.Properties;
using RHToolkit.Services;
using static RHToolkit.Models.EnumService;

namespace RHToolkit.ViewModels.Windows;

public partial class CharacterWindowViewModel : ObservableObject, IRecipient<CharacterInfoMessage>, IRecipient<ItemDataMessage>
{
    private readonly CharacterManager _characterManager;
    private readonly IDatabaseService _databaseService;
    private readonly IGMDatabaseService _gmDatabaseService;
    private readonly CachedDataManager _cachedDataManager;

    public CharacterWindowViewModel(CharacterManager characterManager, IDatabaseService databaseService, IGMDatabaseService gmDatabaseService, CachedDataManager cachedDataManager)
    {
        _characterManager = characterManager;
        _databaseService = databaseService;
        _gmDatabaseService = gmDatabaseService;
        _cachedDataManager = cachedDataManager;

        PopulateClassItems();
        PopulateLobbyItems();

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

        if (message.Recipient == "CharacterWindow" && message.Token == Token)
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
        Title = $"Character Editor ({characterData.CharacterName})";
        CharacterID = characterData.CharacterID;
        CharacterName = characterData.CharacterName;
        Account = characterData.AccountName;
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

    #region Save Character
    [RelayCommand(CanExecute = nameof(CanExecuteCommand))]
    private async Task SaveCharacter()
    {
        if (CharacterData == null) return;

        if (!SqlCredentialValidator.ValidateCredentials())
        {
            return;
        }

        try
        {
            if (await _databaseService.IsCharacterOnlineAsync(CharacterData.CharacterName!))
            {
                return;
            }

            var newCharacterData = GetCharacterChanges();

            if (!CharacterManager.HasCharacterDataChanges(CharacterData, newCharacterData))
            {
                RHMessageBoxHelper.ShowOKMessage("There are no changes to save.", "Info");
                return;
            }

            string changes = CharacterManager.GenerateCharacterDataMessage(CharacterData, newCharacterData, "changes");

            if (RHMessageBoxHelper.ConfirmMessage($"Save the following changes to the character {CharacterData.CharacterName}?\n\n{changes}"))
            {
                string auditMessage = CharacterManager.GenerateCharacterDataMessage(CharacterData, newCharacterData, "audit");

                await _databaseService.UpdateCharacterDataAsync(newCharacterData, "Character Information Change", auditMessage);

                RHMessageBoxHelper.ShowOKMessage("Character changes saved.", "Success");
                await ReadCharacterData(CharacterData.CharacterName!);
            }
        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"Error saving character changes: {ex.Message}", "Error");
        }
    }

    private NewCharacterData GetCharacterChanges()
    {
        return new NewCharacterData
        {
            AccountName = Account,
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

    private bool CanExecuteCommand()
    {
        return CharacterData != null;
    }

    private void OnCanExecuteCommandChanged()
    {
        SaveCharacterCommand.NotifyCanExecuteChanged();
        SaveCharacterNameCommand.NotifyCanExecuteChanged();
        SaveCharacterClassCommand.NotifyCanExecuteChanged();
        SaveCharacterJobCommand.NotifyCanExecuteChanged();
        OpenTitleWindowCommand.NotifyCanExecuteChanged();
        OpenSanctionWindowCommand.NotifyCanExecuteChanged();
        OpenFortuneWindowCommand.NotifyCanExecuteChanged();
    }
    #endregion

    #region Character Name
    [RelayCommand(CanExecute = nameof(CanExecuteSaveCharacterNameCommand))]
    private async Task SaveCharacterName()
    {
        string? newCharacterName = CharacterName;

        if (CharacterData == null || newCharacterName == null || CharacterData.CharacterName == newCharacterName)
        {
            return;
        }

        if (!SqlCredentialValidator.ValidateCredentials())
        {
            return;
        }

        if (IsNameNotAllowed(newCharacterName))
        {
            RHMessageBoxHelper.ShowOKMessage("This character name is on the nick filter and its not allowed.", "Error");
            return;
        }

        if (!ValidateCharacterName(newCharacterName))
        {
            RHMessageBoxHelper.ShowOKMessage("Invalid character name.", "Error");
            return;
        }

        if (RHMessageBoxHelper.ConfirmMessage($"Change this character name to '{newCharacterName}'?"))
        {
            try
            {
                if (await _databaseService.IsCharacterOnlineAsync(CharacterData.CharacterName!))
                {
                    return;
                }

                int result = await _databaseService.UpdateCharacterNameAsync(CharacterData.CharacterID, newCharacterName);

                if (result == -1)
                {
                    RHMessageBoxHelper.ShowOKMessage($"The character name '{newCharacterName}' already exists.", "Error");
                    return;
                }

                await _databaseService.GMAuditAsync(CharacterData.AccountName!, CharacterData.CharacterID, newCharacterName, "Character Name Change", $"Old Name:{CharacterData.CharacterName}, New Name: {newCharacterName}");

                RHMessageBoxHelper.ShowOKMessage("Character name updated successfully!", "Success");
                CharacterData.CharacterName = newCharacterName;
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"Error updating character name: {ex.Message}", "Error");
            }
        }

    }

    private bool IsNameNotAllowed(string characterName)
    {
        return _gmDatabaseService.IsNameInNickFilter(characterName);
    }

    private static bool ValidateCharacterName(string characterName)
    {
        return !string.IsNullOrWhiteSpace(characterName) && characterName.Length >= 3 && characterName.Length <= 16;
    }

    private bool CanExecuteSaveCharacterNameCommand()
    {
        return CharacterData != null && !string.IsNullOrWhiteSpace(CharacterName) && CharacterData.CharacterName != CharacterName;
    }
    #endregion

    #region Save Character Class / Job
    [RelayCommand(CanExecute = nameof(CanExecuteCharacterClassCommand))]
    private async Task SaveCharacterClass()
    {
        if (CharacterData == null) return;

        if (!SqlCredentialValidator.ValidateCredentials())
        {
            return;
        }

        try
        {
            if (await _databaseService.IsCharacterOnlineAsync(CharacterData.CharacterName!))
            {
                return;
            }

            if (RHMessageBoxHelper.ConfirmMessage($"EXPERIMENTAL\n\nThis will reset all character skills and unequip the character weapon/costumes and send via mail.\n\nAre you sure you want to change character '{CharacterData.CharacterName}' class to '{GetEnumDescription((CharClass)Class)}'?"))
            {
                await _databaseService.UpdateCharacterClassAsync(CharacterData.CharacterID, CharacterData.AccountName!, CharacterData.CharacterName!, CharacterData.Class, Class);

                RHMessageBoxHelper.ShowOKMessage("Character class changed successfully!", "Success");
                await ReadCharacterData(CharacterData.CharacterName!);
            }
        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"Error changing character class: {ex.Message}", "Error");
        }
    }

    private bool CanExecuteCharacterClassCommand()
    {
        return CharacterData != null && CharacterData.Class != Class;
    }

    [RelayCommand(CanExecute = nameof(CanExecuteCharacterJobCommand))]
    private async Task SaveCharacterJob()
    {
        if (CharacterData == null) return;

        if (!SqlCredentialValidator.ValidateCredentials())
        {
            return;
        }

        try
        {
            if (await _databaseService.IsCharacterOnlineAsync(CharacterData.CharacterName!))
            {
                return;
            }

            if (RHMessageBoxHelper.ConfirmMessage($"This will reset all character skills.\nAre you sure you want to change character '{CharacterData.CharacterName}' focus?"))
            {
                await _databaseService.UpdateCharacterClassAsync(CharacterData.CharacterID, CharacterData.AccountName!, CharacterData.CharacterName!, CharacterData.Class, Class);

                RHMessageBoxHelper.ShowOKMessage("Character focus changed successfully!", "Success");
                await ReadCharacterData(CharacterData.CharacterName!);
            }
        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"Error changing character focus: {ex.Message}", "Error");
        }
    }

    private bool CanExecuteCharacterJobCommand()
    {
        return CharacterData != null && CharacterData.Job != Job;
    }
    #endregion

    #region Windows Buttons
    private void OpenWindow(Action<CharacterInfo> openWindowAction, string errorMessage)
    {
        if (CharacterData == null) return;

        try
        {
            var characterInfo = new CharacterInfo(CharacterData.CharacterID, CharacterData.AuthID, CharacterData.CharacterName!, CharacterData.AccountName!, CharacterData.Class, CharacterData.Job);
            openWindowAction(characterInfo);
        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"Error reading Character {errorMessage}: {ex.Message}", "Error");
        }
    }

    #region Title

    [RelayCommand(CanExecute = nameof(CanExecuteCommand))]
    private void OpenTitleWindow()
    {
        OpenWindow(_characterManager.OpenTitleWindow, "Title");
    }

    #endregion

    #region Sanction

    [RelayCommand(CanExecute = nameof(CanExecuteCommand))]
    private void OpenSanctionWindow()
    {
        OpenWindow(_characterManager.OpenSanctionWindow, "Sanction");
    }

    #endregion

    #region Fortune

    [RelayCommand(CanExecute = nameof(CanExecuteCommand))]
    private void OpenFortuneWindow()
    {
        OpenWindow(_characterManager.OpenFortuneWindow, "Fortune");
    }

    #endregion

    #endregion

    #endregion

    #region Comboboxes
    private void PopulateClassItems()
    {
        try
        {
            ClassItems = GetEnumItems<CharClass>();

            if (ClassItems.Count > 0)
            {
                Class = 1;
            }
        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
        }
    }

    private void PopulateLobbyItems()
    {
        try
        {
            LobbyItems = _gmDatabaseService.GetLobbyItems();

            if (LobbyItems.Count > 0)
            {
                Lobby = 1;
            }
        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
        }
    }

    #endregion

    #region Item Data

    #region Receive
    public void Receive(ItemDataMessage message)
    {
        if (message.Recipient == "CharacterWindowViewModel" && message.Token == Token)
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
                ItemData? cachedItem = _cachedDataManager.CachedItemDataList?.FirstOrDefault(item => item.ID == code);

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
            
            _characterManager.OpenItemWindow(characterInfo, itemData);
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
            RemoveItem(i.ToString());
        }
    }

    private void ResetItemProperties(int slotIndex)
    {
        string iconNameProperty = $"ItemIcon{slotIndex}";
        string iconBranchProperty = $"ItemIconBranch{slotIndex}";
        string nameProperty = $"ItemName{slotIndex}";
        string amountProperty = $"ItemAmount{slotIndex}";

        GetType().GetProperty(iconNameProperty)?.SetValue(this, null);
        GetType().GetProperty(iconBranchProperty)?.SetValue(this, 0);
        GetType().GetProperty(nameProperty)?.SetValue(this, Resources.AddItemDesc);
        GetType().GetProperty(amountProperty)?.SetValue(this, 0);
    }

    #endregion

    #endregion

    #region Properties

    [ObservableProperty]
    private Guid? _token = Guid.Empty;

    #region Character
    [ObservableProperty]
    private CharacterData? _characterData;
    partial void OnCharacterDataChanged(CharacterData? value)
    {
        OnCanExecuteCommandChanged();
    }

    [ObservableProperty]
    private List<NameID>? _classItems;

    [ObservableProperty]
    private List<NameID>? _jobItems;

    [ObservableProperty]
    private List<NameID>? _lobbyItems;

    [ObservableProperty]
    private bool _isButtonEnabled = true;

    [ObservableProperty]
    private string _title = "Character Editor";

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
    private string? _account;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CharacterIDText))]
    private Guid _characterID;

    public string? CharacterIDText => $"{CharacterID.ToString().ToUpper()}";

    [ObservableProperty]
    private DateTime _createTime;

    [ObservableProperty]
    private DateTime _lastLoginTime;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CharacterNameText))]
    private string? _characterName;
    partial void OnCharacterNameChanged(string? value)
    {
        SaveCharacterNameCommand.NotifyCanExecuteChanged();
    }

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
    [NotifyPropertyChangedFor(nameof(Job))]
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
            Job = 0;
            Job = CharacterData.Job;
        }
        SaveCharacterClassCommand.NotifyCanExecuteChanged();
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(JobName))]
    private int _job;
    partial void OnJobChanged(int value)
    {
        Enum jobEnum = GetJobEnum((CharClass)Class, value);
        JobName = GetEnumDescription(jobEnum);
        SaveCharacterJobCommand.NotifyCanExecuteChanged();
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
    #endregion

    #region Equipament 
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
