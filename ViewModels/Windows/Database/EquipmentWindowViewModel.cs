using RHToolkit.Messages;
using RHToolkit.Models;
using RHToolkit.Models.Database;
using RHToolkit.Models.MessageBox;
using RHToolkit.Properties;
using RHToolkit.Services;
using RHToolkit.ViewModels.Windows.Database.VM;
using static RHToolkit.Models.EnumService;

namespace RHToolkit.ViewModels.Windows;

public partial class EquipmentWindowViewModel : ObservableObject, IRecipient<CharacterDataMessage>, IRecipient<ItemDataMessage>
{
    private readonly IWindowsService _windowsService;
    private readonly IDatabaseService _databaseService;
    private readonly ItemDataManager _itemDataManager;

    public EquipmentWindowViewModel(IWindowsService windowsService, IDatabaseService databaseService, ItemDataManager itemDataManager)
    {
        _windowsService = windowsService;
        _databaseService = databaseService;
        _itemDataManager = itemDataManager;

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
                
                await _databaseService.SaveInventoryItem(CharacterData, EquipmentItemDatabaseList, EquipmentDeletedItemDatabaseList, "N_EquipItem");
                RHMessageBoxHelper.ShowOKMessage("Equipment saved successfully!", "Success");
                await LoadCharacterData(CharacterData.CharacterName!);

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
            RemoveEquipmentItem(slotIndex);
        }
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

            await LoadCharacterData(characterData.CharacterName!);
        }

        WeakReferenceMessenger.Default.Unregister<CharacterDataMessage>(this);
    }

    public async Task LoadCharacterData(string characterName)
    {
        try
        {
            CharacterData? characterData = await _databaseService.GetCharacterDataAsync(characterName);

            if (characterData != null)
            {
                ClearData();
                CharacterData = characterData;
                LoadCharacterData(characterData);
                ObservableCollection<ItemData> equipmentItems = await _databaseService.GetItemList(characterData.CharacterID, "N_EquipItem");
                LoadEquipmentItems(equipmentItems);
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

    private void LoadEquipmentItems(ObservableCollection<ItemData> equipmentItems)
    {
        if (equipmentItems == null) return;

        EquipmentItemDatabaseList = equipmentItems;
        EquipmentItemDataViewModels = InitializeCollection(21, 0);

        foreach (var equipmentItem in equipmentItems)
        {
            SetEquipmentItemDataViewModel(equipmentItem);
        }
    }

    private static ObservableCollection<InventoryItem> InitializeCollection(int itemCount, int pageIndex)
    {
        var collection = new ObservableCollection<InventoryItem>();

        for (int i = 0; i < itemCount; i++)
        {
            collection.Add(new InventoryItem
            {
                SlotIndex = i,
                PageIndex = pageIndex
            });
        }

        return collection;
    }

    private void ClearData()
    {
        CharacterData = null;
        EquipmentItemDatabaseList?.Clear();
        EquipmentDeletedItemDatabaseList?.Clear();
        EquipmentItemDataViewModels?.Clear();
    }
    #endregion

    #region Send ItemData

    [RelayCommand]
    private void AddItem(string parameter)
    {
        if (CharacterData == null) return;

        if (int.TryParse(parameter, out int slotIndex))
        {
            ItemData? itemData = EquipmentItemDatabaseList?.FirstOrDefault(i => i.SlotIndex == slotIndex) ?? new ItemData { SlotIndex = slotIndex };

            _windowsService.OpenItemWindow(CharacterData.CharacterID, "EquipItem", itemData, CharacterData);
        }
    }

    #endregion

    #region Receive ItemData
    public void Receive(ItemDataMessage message)
    {
        if (message.Recipient != "EquipWindow" || message.Token != Token)
            return;

        var newItemData = message.Value;

        EquipmentItemDatabaseList ??= [];

        // Find the existing item in ItemDatabaseList
        var existingItem = EquipmentItemDatabaseList.FirstOrDefault(item => item.SlotIndex == newItemData.SlotIndex);

        if (existingItem != null)
        {
            if (existingItem.ItemId != newItemData.ItemId && !existingItem.IsNewItem)
            {
                RHMessageBoxHelper.ShowOKMessage($"The '{(EquipCategory)newItemData.SlotIndex}' slot is already in use.", "Cant Add Equipment Item");
                return;
            }
            else if (existingItem.IsNewItem)
            {
                RemoveEquipmentItem(existingItem.SlotIndex);
                CreateEquipmentItem(newItemData);
            }
            else
            {
                UpdateEquipmentItem(existingItem, newItemData);
            }
        }
        else if (newItemData.ItemId != 0)
        {
            CreateEquipmentItem(newItemData);
        }

    }

    #endregion

    #endregion

    #region Item Methods

    private void CreateEquipmentItem(ItemData newItemData)
    {
        if (CharacterData == null) return;

        var newItem = ItemDataManager.CreateNewItem(CharacterData, newItemData, 0);

        EquipmentItemDatabaseList ??= [];
        EquipmentItemDatabaseList.Add(newItem);

        OnPropertyChanged(nameof(EquipmentItemDatabaseList));
        SetEquipmentItemDataViewModel(newItem);
    }

    private void UpdateEquipmentItem(ItemData existingItem, ItemData newItem)
    {
        if (EquipmentItemDatabaseList != null)
        {
            var updatedItem = ItemDataManager.UpdateItemData(existingItem, newItem);

            var index = EquipmentItemDatabaseList.IndexOf(existingItem);
            if (index >= 0)
            {
                EquipmentItemDatabaseList[index] = updatedItem;
            }

            OnPropertyChanged(nameof(EquipmentItemDatabaseList));
            SetEquipmentItemDataViewModel(updatedItem);
        }
    }

    private void RemoveEquipmentItem(int slotIndex)
    {
        if (EquipmentItemDatabaseList != null)
        {
            // Find the existing item in ItemDatabaseList
            var removedItem = EquipmentItemDatabaseList.FirstOrDefault(item => item.SlotIndex == slotIndex);

            if (removedItem != null)
            {
                if (!removedItem.IsNewItem)
                {
                    EquipmentDeletedItemDatabaseList ??= [];
                    EquipmentDeletedItemDatabaseList.Add(removedItem);
                }

                EquipmentItemDatabaseList?.Remove(removedItem);
                OnPropertyChanged(nameof(EquipmentItemDatabaseList));
                RemoveEquipmentItemDataViewModel(removedItem);
            }
        }
    }

    private void SetEquipmentItemDataViewModel(ItemData itemData)
    {
        if (EquipmentItemDataViewModels != null)
        {
            var itemDataViewModel = _itemDataManager.GetItemData(itemData);
            EquipmentItemDataViewModels[itemDataViewModel.SlotIndex].ItemDataViewModel = itemDataViewModel;
            OnPropertyChanged(nameof(EquipmentItemDataViewModels));
        }
    }

    private void RemoveEquipmentItemDataViewModel(ItemData removedItem)
    {
        if (EquipmentItemDataViewModels != null)
        {
            EquipmentItemDataViewModels[removedItem.SlotIndex].ItemDataViewModel = null;
            OnPropertyChanged(nameof(EquipmentItemDataViewModels));
        }
    }

    #endregion

    #region Properties

    [ObservableProperty]
    private string _title = "Equipment Editor";

    [ObservableProperty]
    private Guid? _token = Guid.Empty;

    #region Character
    [ObservableProperty]
    private CharacterData? _characterData;

    [ObservableProperty]
    private bool _isButtonEnabled = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CharacterClassText))]
    private string? _className;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CharacterClassText))]
    private string? _jobName = Resources.Basic;

    public string? CharacterNameText => $"Lv.{Level} {CharacterName}";

    public string? CharacterClassText => $"<{ClassName} - {JobName} Focus> ";

    [ObservableProperty]
    private string _charSilhouetteImage = "/Assets/images/char/ui_silhouette_frantz01.png";

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
        CharSilhouetteImage = CharacterDataManager.GetClassImage(value);
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
    private ObservableCollection<ItemData>? _equipmentItemDatabaseList;

    [ObservableProperty]
    private ObservableCollection<ItemData>? _equipmentDeletedItemDatabaseList;

    [ObservableProperty]
    private ObservableCollection<InventoryItem>? _equipmentItemDataViewModels;

    #endregion

    #endregion
}
