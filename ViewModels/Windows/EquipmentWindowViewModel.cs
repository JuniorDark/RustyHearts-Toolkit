using RHToolkit.Messages;
using RHToolkit.Models;
using RHToolkit.Models.Database;
using RHToolkit.Models.MessageBox;
using RHToolkit.Properties;
using RHToolkit.Services;
using RHToolkit.ViewModels.Controls;
using static RHToolkit.Models.EnumService;

namespace RHToolkit.ViewModels.Windows;

public partial class EquipmentWindowViewModel : ObservableObject, IRecipient<CharacterInfoMessage>, IRecipient<ItemDataMessage>
{
    private readonly IWindowsService _windowsService;
    private readonly IDatabaseService _databaseService;
    private readonly ItemHelper _itemHelper;

    public EquipmentWindowViewModel(IWindowsService windowsService, IDatabaseService databaseService, ItemHelper itemHelper)
    {
        _windowsService = windowsService;
        _databaseService = databaseService;
        _itemHelper = itemHelper;

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
        NewItemDatabaseList = null;
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
            if (await _databaseService.IsCharacterOnlineAsync(CharacterData.CharacterName!))
            {
                return;
            }

            if (RHMessageBoxHelper.ConfirmMessage($"Save equipment changes?"))
            {
                await _databaseService.SaveEquipItemAsync(NewItemDatabaseList, UpdatedItemDatabaseList, DeletedItemDatabaseList);
                RHMessageBoxHelper.ShowOKMessage("Equipment saved successfully!", "Success");
                await ReadCharacterData(CharacterData.CharacterName!);
            }
           
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
                // Update existing item
                UpdateItem(existingItem, newItemData);

            }
            else
            {
                // Create new item
                CreateItem(newItemData);
            }
        }
    }

    private void UpdateItem(ItemData existingItem, ItemData newItemData)
    {
        if (CharacterData == null) return;

        // Check if the IDs are different
        if (existingItem.ID != newItemData.ID)
        {
            // Create a new item if the IDs are different
            var newItem = ItemHelper.CreateNewItem(CharacterData, newItemData, 0);

            // Add the new item to the list
            NewItemDatabaseList ??= [];
            NewItemDatabaseList.Add(newItem);

            // Remove the existing item from ItemDatabaseList
            ItemDatabaseList?.Remove(existingItem);

            // Move the existing item to DeletedItemDatabaseList
            DeletedItemDatabaseList ??= [];
            DeletedItemDatabaseList.Add(existingItem);

            (var itemData, var frameViewModel) = _itemHelper.GetItemData(newItem);

            SetItemProperties(itemData, frameViewModel);
        }
        else
        {
            // Update existingItem
            existingItem.UpdateTime = DateTime.Now;
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

            UpdatedItemDatabaseList ??= [];

            var newItemIndex = UpdatedItemDatabaseList.FindIndex(item => item.SlotIndex == existingItem.SlotIndex);

            if (newItemIndex != -1)
            {
                UpdatedItemDatabaseList.RemoveAt(newItemIndex);
            }

            UpdatedItemDatabaseList.Add(existingItem);

            (var itemData, var frameViewModel) = _itemHelper.GetItemData(existingItem);

            SetItemProperties(itemData, frameViewModel);
        }
    }

    private void CreateItem(ItemData newItemData)
    {
        if (CharacterData == null) return;

        var newItem = ItemHelper.CreateNewItem(CharacterData, newItemData, 0);

        NewItemDatabaseList ??= [];
        NewItemDatabaseList.Add(newItem);

        (var itemData, var frameViewModel) = _itemHelper.GetItemData(newItem);

        SetItemProperties(itemData, frameViewModel);
    }

    private void LoadEquipmentItems(List<ItemData> equipmentItems)
    {
        if (equipmentItems != null)
        {
            foreach (var equipmentItem in equipmentItems)
            {
                (var itemData, var frameViewModel) = _itemHelper.GetItemData(equipmentItem);

                SetItemProperties(itemData, frameViewModel);
            }
        }
    }

    private void SetItemProperties(ItemData itemData, FrameViewModel frameViewModel)
    {
        // Construct property names dynamically
        string iconNameProperty = $"ItemIcon{itemData.SlotIndex}";
        string iconBranchProperty = $"ItemIconBranch{itemData.SlotIndex}";
        string nameProperty = $"ItemName{itemData.SlotIndex}";
        string frameViewModelProperty = $"FrameViewModel{itemData.SlotIndex}";

        // Set properties using reflection
        GetType().GetProperty(iconNameProperty)?.SetValue(this, itemData.IconName);
        GetType().GetProperty(iconBranchProperty)?.SetValue(this, itemData.Branch);
        GetType().GetProperty(nameProperty)?.SetValue(this, itemData.Name);
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

    [RelayCommand]
    private void RemoveItem(string parameter)
    {
        if (int.TryParse(parameter, out int slotIndex) && ItemDatabaseList != null)
        {
            var removedItemIndex = ItemDatabaseList.FindIndex(item => item.SlotIndex == slotIndex);

            if (removedItemIndex != -1)
            {
                // Get the removed item
                var removedItem = ItemDatabaseList[removedItemIndex];

                // Add the removed item to DeletedItemDatabaseList
                DeletedItemDatabaseList ??= [];
                DeletedItemDatabaseList.Add(removedItem);

                // Remove the item with the specified SlotIndex from ItemDatabaseList
                ItemDatabaseList.RemoveAt(removedItemIndex);
            }
            else if (NewItemDatabaseList != null)
            {
                var newItemIndex = NewItemDatabaseList.FindIndex(item => item.SlotIndex == slotIndex);

                if (newItemIndex != -1)
                {
                    NewItemDatabaseList.RemoveAt(newItemIndex);
                }
            }

            // Update properties to default values for the removed SlotIndex
            ResetItemProperties(slotIndex);
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
    private List<ItemData>? _itemDatabaseList;

    [ObservableProperty]
    private List<ItemData>? _newItemDatabaseList;

    [ObservableProperty]
    private List<ItemData>? _updatedItemDatabaseList;

    [ObservableProperty]
    private List<ItemData>? _deletedItemDatabaseList;

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
