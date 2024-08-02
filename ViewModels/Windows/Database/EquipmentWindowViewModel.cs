using RHToolkit.Messages;
using RHToolkit.Models;
using RHToolkit.Models.Database;
using RHToolkit.Models.MessageBox;
using RHToolkit.Properties;
using RHToolkit.Services;
using RHToolkit.ViewModels.Controls;
using static RHToolkit.Models.EnumService;

namespace RHToolkit.ViewModels.Windows;

public partial class EquipmentWindowViewModel : ObservableObject, IRecipient<CharacterDataMessage>, IRecipient<ItemDataMessage>
{
    private readonly IWindowsService _windowsService;
    private readonly IDatabaseService _databaseService;
    private readonly ItemDataManager _itemHelper;

    public EquipmentWindowViewModel(IWindowsService windowsService, IDatabaseService databaseService, ItemDataManager itemHelper)
    {
        _windowsService = windowsService;
        _databaseService = databaseService;
        _itemHelper = itemHelper;

        FrameViewModels ??= [];
        WeakReferenceMessenger.Default.Register<ItemDataMessage>(this);
        WeakReferenceMessenger.Default.Register<CharacterDataMessage>(this);
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
                
                await _databaseService.SaveInventoryItem(CharacterData, ItemDatabaseList, DeletedItemDatabaseList, "N_EquipItem");
                RHMessageBoxHelper.ShowOKMessage("Equipment saved successfully!", "Success");
                await ReadCharacterData(CharacterData.CharacterName!);

                WeakReferenceMessenger.Default.Send(new CharacterDataMessage(CharacterData, "CharacterWindow", CharacterData.CharacterID));
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

    public async void Receive(CharacterDataMessage message)
    {
        if (Token == Guid.Empty)
        {
            Token = message.Token;
        }

        if (message.Recipient == "EquipmentWindow" && message.Token == Token)
        {
            var characterData = message.Value;

            await ReadCharacterData(characterData.CharacterName!);
        }

        WeakReferenceMessenger.Default.Unregister<CharacterDataMessage>(this);
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

            _windowsService.OpenItemWindow(CharacterData.CharacterID, "EquipItem", itemData, CharacterData);
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

        var newItem = ItemDataManager.CreateNewItem(CharacterData, newItemData, 0);

        ItemDatabaseList ??= [];
        ItemDatabaseList.Add(newItem);

        var frameViewModel = _itemHelper.GetItemData(newItem);

        SetFrameViewModel(frameViewModel);
    }

    private void UpdateItem(ItemData existingItem, ItemData newItem)
    {
        if (CharacterData == null) return;

        ItemDatabaseList ??= [];

        // Check if the IDs are different
        if (existingItem.ItemId != newItem.ItemId && !existingItem.IsNewItem)
        {
            if (RHMessageBoxHelper.ConfirmMessage($"The '{(EquipCategory)newItem.SlotIndex}' slot is already in use. Do you want to overwrite the current item?"))
            {
                // Create a new item if the IDs are different
                RemoveItem(existingItem.SlotIndex.ToString());
                CreateItem(newItem);
            }
            else
            {
                return;
            }
        }

        if (existingItem.IsNewItem)
        {
            RemoveItem(existingItem.SlotIndex.ToString());
            CreateItem(newItem);
        }
        else
        {
            // Update existingItem
            existingItem.IsEditedItem = !existingItem.IsNewItem;
            existingItem.UpdateTime = DateTime.Now;
            existingItem.Durability = newItem.Durability;
            existingItem.DurabilityMax = newItem.DurabilityMax;
            existingItem.EnhanceLevel = newItem.EnhanceLevel;
            existingItem.AugmentStone = newItem.AugmentStone;
            existingItem.Rank = newItem.Rank;
            existingItem.Weight = newItem.Weight;
            existingItem.Reconstruction = newItem.Reconstruction;
            existingItem.ReconstructionMax = newItem.ReconstructionMax;
            existingItem.ItemAmount = newItem.ItemAmount;
            existingItem.Option1Code = newItem.Option1Code;
            existingItem.Option2Code = newItem.Option2Code;
            existingItem.Option3Code = newItem.Option3Code;
            existingItem.Option1Value = newItem.Option1Value;
            existingItem.Option2Value = newItem.Option2Value;
            existingItem.Option3Value = newItem.Option3Value;
            existingItem.SocketCount = newItem.SocketCount;
            existingItem.Socket1Color = newItem.Socket1Color;
            existingItem.Socket2Color = newItem.Socket2Color;
            existingItem.Socket3Color = newItem.Socket3Color;
            existingItem.Socket1Code = newItem.Socket1Code;
            existingItem.Socket2Code = newItem.Socket2Code;
            existingItem.Socket3Code = newItem.Socket3Code;
            existingItem.Socket1Value = newItem.Socket1Value;
            existingItem.Socket2Value = newItem.Socket2Value;
            existingItem.Socket3Value = newItem.Socket3Value;

            RemoveItem(existingItem.SlotIndex.ToString());
            var frameViewModel = _itemHelper.GetItemData(existingItem);
            ItemDatabaseList.Add(existingItem);
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
