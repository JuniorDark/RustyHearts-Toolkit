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

        FrameViewModels ??= [];
        WeakReferenceMessenger.Default.Register<ItemDataMessage>(this);
        WeakReferenceMessenger.Default.Register<CharacterInfoMessage>(this);
    }

    #region Commands

    #region Save Equipment
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
                var characterInfo = new CharacterInfo(CharacterData.CharacterID, CharacterData.AuthID, CharacterData.CharacterName!, CharacterData.AccountName!, CharacterData.Class, CharacterData.Job);
                
                await _databaseService.SaveInventoryItem(characterInfo, ItemDatabaseList, DeletedItemDatabaseList, "N_EquipItem");
                RHMessageBoxHelper.ShowOKMessage("Equipment saved successfully!", "Success");
                await ReadCharacterData(CharacterData.CharacterName!);

                WeakReferenceMessenger.Default.Send(new CharacterInfoMessage(characterInfo, "CharacterWindow", characterInfo.CharacterID));
            }

        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"Error saving equipment changes: {ex.Message}", "Error");
        }
    }

    #endregion

    #region Remove Item

    [RelayCommand]
    private void RemoveItem(string parameter)
    {
        if (int.TryParse(parameter, out int slotIndex))
        {
            if (ItemDatabaseList != null)
            {
                // Find the existing item in ItemDatabaseList
                var removedItem = ItemDatabaseList.FirstOrDefault(item => item.SlotIndex == slotIndex);
                var removedItemIndex = ItemDatabaseList.FindIndex(item => item.SlotIndex == slotIndex);

                if (removedItem != null)
                {
                    // Remove the item with the specified SlotIndex from ItemDatabaseList
                    ItemDatabaseList?.Remove(removedItem);

                    if (!removedItem.IsNewItem)
                    {
                        DeletedItemDatabaseList ??= [];
                        DeletedItemDatabaseList.Add(removedItem);
                    }

                    RemoveFrameViewModel(removedItemIndex);
                }
                
            }
        }
    }

    private void RemoveFrameViewModel(int itemIndex)
    {
        FrameViewModels?.RemoveAt(itemIndex);
        OnPropertyChanged(nameof(FrameViewModels));
    }
    #endregion

    #endregion

    #region Messenger

    #region Load CharacterData

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
        CharacterData = null;
        ItemDatabaseList = null;
        DeletedItemDatabaseList = null;
        FrameViewModels?.Clear();
        OnPropertyChanged(nameof(FrameViewModels));
    }
    #endregion

    #region Send ItemData

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

    #region Receive ItemData
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
                if (newItemData.ItemId != 0)
                {
                    // Create new item
                    CreateItem(newItemData);
                }

            }
        }
    }

    #endregion

    #endregion

    #region Item Methods

    private void CreateItem(ItemData newItemData)
    {
        if (CharacterData == null) return;

        var newItem = ItemHelper.CreateNewItem(CharacterData, newItemData, 0);

        ItemDatabaseList ??= [];
        ItemDatabaseList.Add(newItem);

        var frameViewModel = _itemHelper.GetItemData(newItem);

        SetFrameViewModel(frameViewModel);
    }

    private void UpdateItem(ItemData existingItem, ItemData newItemData)
    {
        if (CharacterData == null) return;

        ItemDatabaseList ??= [];

        var existingItemIndex = ItemDatabaseList.FindIndex(item => item.SlotIndex == existingItem.SlotIndex);

        // Check if the IDs are different
        if (existingItem.ItemId != newItemData.ItemId)
        {
            if (RHMessageBoxHelper.ConfirmMessage($"The '{(EquipCategory)newItemData.SlotIndex}' slot is already in use. Do you want to overwrite the current item?"))
            {
                // Create a new item if the IDs are different
                var newItem = ItemHelper.CreateNewItem(CharacterData, newItemData, 0);

                // Remove the existing item from ItemDatabaseList
                ItemDatabaseList.Remove(existingItem);
                ItemDatabaseList.Add(newItem);

                if (!existingItem.IsNewItem)
                {
                    DeletedItemDatabaseList ??= [];
                    DeletedItemDatabaseList.Add(existingItem);
                }

                RemoveFrameViewModel(existingItemIndex);

                var frameViewModel = _itemHelper.GetItemData(newItem);

                SetFrameViewModel(frameViewModel);
            }
            else
            {
                return;
            }
            
        }
        else
        {
            ItemDatabaseList.Remove(existingItem);
            RemoveFrameViewModel(existingItemIndex);

            // Update existingItem
            existingItem.IsEditedItem = true;
            existingItem.UpdateTime = DateTime.Now;
            existingItem.Durability = newItemData.Durability;
            existingItem.DurabilityMax = newItemData.DurabilityMax;
            existingItem.EnhanceLevel = newItemData.EnhanceLevel;
            existingItem.AugmentStone = newItemData.AugmentStone;
            existingItem.Rank = newItemData.Rank;
            existingItem.Weight = newItemData.Weight;
            existingItem.Reconstruction = newItemData.Reconstruction;
            existingItem.ReconstructionMax = newItemData.ReconstructionMax;
            existingItem.ItemAmount = newItemData.ItemAmount;
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

            ItemDatabaseList.Add(existingItem);

            var frameViewModel = _itemHelper.GetItemData(existingItem);

            SetFrameViewModel(frameViewModel);
        }
    }

    private void LoadEquipmentItems(List<ItemData> equipmentItems)
    {
        if (equipmentItems != null)
        {
            foreach (var equipmentItem in equipmentItems)
            {
                var frameViewModel = _itemHelper.GetItemData(equipmentItem);
                SetFrameViewModel(frameViewModel);
            }
        }
    }

    private void SetFrameViewModel(FrameViewModel frameViewModel)
    {
        FrameViewModels ??= [];
        FrameViewModels.Add(frameViewModel);
        OnPropertyChanged(nameof(FrameViewModels));
    }

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
    private List<ItemData>? _deletedItemDatabaseList;

    [ObservableProperty]
    private List<FrameViewModel>? _frameViewModels;

    #endregion

    #endregion
}
