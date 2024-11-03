using RHToolkit.Messages;
using RHToolkit.Models;
using RHToolkit.Models.Database;
using RHToolkit.Models.MessageBox;
using RHToolkit.Services;
using RHToolkit.ViewModels.Windows.Database.VM;

namespace RHToolkit.ViewModels.Windows;

public partial class InventoryWindowViewModel : ObservableObject, IRecipient<CharacterDataMessage>, IRecipient<ItemDataMessage>
{
    private readonly IWindowsService _windowsService;
    private readonly IDatabaseService _databaseService;
    private readonly ItemDataManager _itemDataManager;

    public InventoryWindowViewModel(IWindowsService windowsService, IDatabaseService databaseService, ItemDataManager itemDataManager)
    {
        _windowsService = windowsService;
        _databaseService = databaseService;
        _itemDataManager = itemDataManager;

        CurrentPage = 1;

        WeakReferenceMessenger.Default.Register<ItemDataMessage>(this);
        WeakReferenceMessenger.Default.Register<CharacterDataMessage>(this);
    }

    #region Commands

    #region Save Inventory
    [RelayCommand]
    private async Task SaveInventory()
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

            if (RHMessageBoxHelper.ConfirmMessage(Resources.SaveChangesMessage))
            {
                await _databaseService.SaveInventoryItem(CharacterData, ItemDatabaseList, DeletedItemDatabaseList, "N_InventoryItem");
                RHMessageBoxHelper.ShowOKMessage(Resources.SaveSuccessMessage, Resources.Success);
                await ReadCharacterData(CharacterData.CharacterName!);
            }

        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
        }
    }

    #endregion

    #region Remove Item

    [RelayCommand]
    private void RemoveItem(string parameter)
    {
        var parameters = parameter.Split(',');
        if (parameters.Length != 2) return;

        if (int.TryParse(parameters[0], out int slotIndex) && int.TryParse(parameters[1], out int pageIndex))
        {
            RemoveInventoryItem(slotIndex, pageIndex);
        }
    }

    #endregion

    #region Page
    [RelayCommand(CanExecute = nameof(CanExecuteNextPageCommand))]
    private void NextPage()
    {
        if (CurrentPage < 5)
        {
            CurrentPage++;
        }
    }

    [RelayCommand(CanExecute = nameof(CanExecutePreviousPageCommand))]
    private void PreviousPage()
    {
        if (CurrentPage > 1)
        {
            CurrentPage--;
        }
    }

    private bool CanExecutePreviousPageCommand()
    {
        return CurrentPage > 1;
    }

    private bool CanExecuteNextPageCommand()
    {
        return CurrentPage < 5;
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

        if (message.Recipient == "InventoryWindow" && message.Token == Token)
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
            var characterData = await _databaseService.GetCharacterDataAsync(characterName);

            if (characterData != null)
            {
                ClearInventoryData();
                CharacterData = characterData;
                Title = string.Format(Resources.EditorTitleFileName,Resources.CharacterInventory, characterData.CharacterName);
                ObservableCollection<ItemData> inventoryItems = await _databaseService.GetItemList(characterData.CharacterID, "N_InventoryItem");
                LoadInventoryItems(inventoryItems);
                ItemDatabaseList = inventoryItems;
                var accountData = await _databaseService.GetAccountDataAsync(characterData.AccountName!);

                if (accountData != null)
                {
                    CashValue = accountData.Zen;
                    BonusCashValue = accountData.CashMileage;
                }
            }
            else
            {
                RHMessageBoxHelper.ShowOKMessage(string.Format(Resources.InexistentCharacterMessage, characterName), Resources.InvalidCharacter);
                return;
            }
        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
        }
    }

    private void LoadInventoryItems(ObservableCollection<ItemData> inventoryItems)
    {
        if (inventoryItems == null) return;

        EquipmentItemDataViewModels = ItemDataManager.InitializeCollection(24, 1);
        ConsumeItemDataViewModels = ItemDataManager.InitializeCollection(24, 2);
        OtherItemDataViewModels = ItemDataManager.InitializeCollection(24, 3);
        QuestItemDataViewModels = ItemDataManager.InitializeCollection(24, 4);
        CostumeItemDataViewModels = ItemDataManager.InitializeCollection(120, 5);
        HiddenItemDataViewModels = ItemDataManager.InitializeCollection(24, 6);

        foreach (var inventoryItem in inventoryItems)
        {
            SetInventoryItemDataViewModel(inventoryItem);
        }

    }

    private ObservableCollection<InventoryItem>? GetItemDataViewModelList(int pageIndex)
    {
        return pageIndex switch
        {
            1 => EquipmentItemDataViewModels,
            2 => ConsumeItemDataViewModels,
            3 => OtherItemDataViewModels,
            4 => QuestItemDataViewModels,
            5 => CostumeItemDataViewModels,
            6 => HiddenItemDataViewModels,
            _ => null
        };
    }

    private void ClearInventoryData()
    {
        CharacterData = null;
        ItemDatabaseList?.Clear();
        DeletedItemDatabaseList?.Clear();
        EquipmentItemDataViewModels?.Clear();
        ConsumeItemDataViewModels?.Clear();
        OtherItemDataViewModels?.Clear();
        QuestItemDataViewModels?.Clear();
        CostumeItemDataViewModels?.Clear();
        HiddenItemDataViewModels?.Clear();
    }
    #endregion

    #region Send ItemData

    [RelayCommand]
    private void AddItem(string parameter)
    {
        if (CharacterData == null) return;

        var parameters = parameter.Split(',');
        if (parameters.Length != 2) return;

        if (int.TryParse(parameters[0], out int slotIndex) && int.TryParse(parameters[1], out int pageIndex))
        {
            ItemData? itemData = ItemDatabaseList?.FirstOrDefault(i => i.SlotIndex == slotIndex && i.PageIndex == pageIndex) ?? new ItemData { SlotIndex = slotIndex, PageIndex = pageIndex };
            _windowsService.OpenItemWindow(CharacterData.CharacterID, "InventoryItem", itemData, CharacterData);
        }
    }

    #endregion

    #region Receive ItemData
    public void Receive(ItemDataMessage message)
    {
        if (message.Recipient != "InventoryWindow" || message.Token != Token)
            return;

        var newItemData = message.Value;

        ItemDatabaseList ??= [];

        var existingItem = ItemDatabaseList.FirstOrDefault(item => item.SlotIndex == newItemData.SlotIndex && item.PageIndex == newItemData.PageIndex);

        if (existingItem != null)
        {
            if (existingItem.ItemId != newItemData.ItemId && !existingItem.IsNewItem)
            {
                RHMessageBoxHelper.ShowOKMessage(string.Format(Resources.EquipmentEditorSlotInUseMessage, newItemData.SlotIndex, Resources.Error));
                return;
            }
            else if (existingItem.IsNewItem)
            {
                RemoveInventoryItem(existingItem.SlotIndex, existingItem.PageIndex);
                CreateInventoryItem(newItemData);
            }
            else
            {
                UpdateInventoryItem(existingItem, newItemData);
            }
        }
        else if (newItemData.ItemId != 0)
        {
            CreateInventoryItem(newItemData);
        }
    }

    #endregion

    #endregion

    #region Item Methods

    private void CreateInventoryItem(ItemData newItemData)
    {
        if (CharacterData == null) return;

        var newItem = ItemDataManager.CreateNewItem(CharacterData, newItemData, newItemData.PageIndex);

        ItemDatabaseList ??= [];
        ItemDatabaseList.Add(newItem);

        OnPropertyChanged(nameof(ItemDatabaseList));
        SetInventoryItemDataViewModel(newItem);
    }

    private void UpdateInventoryItem(ItemData existingItem, ItemData newItem)
    {
        if (ItemDatabaseList != null)
        {
            var updatedItem = ItemDataManager.UpdateItemData(existingItem, newItem);

            var index = ItemDatabaseList.IndexOf(existingItem);
            if (index >= 0)
            {
                ItemDatabaseList[index] = updatedItem;
            }

            OnPropertyChanged(nameof(ItemDatabaseList));
            SetInventoryItemDataViewModel(updatedItem);
        }
    }

    private void RemoveInventoryItem(int slotIndex, int pageIndex)
    {
        if (ItemDatabaseList != null)
        {
            // Find the existing item in ItemDatabaseList
            var removedItem = ItemDatabaseList.FirstOrDefault(item => item.SlotIndex == slotIndex && item.PageIndex == pageIndex);

            if (removedItem != null)
            {
                if (!removedItem.IsNewItem)
                {
                    DeletedItemDatabaseList ??= [];
                    DeletedItemDatabaseList.Add(removedItem);
                }

                ItemDatabaseList?.Remove(removedItem);
                OnPropertyChanged(nameof(ItemDatabaseList));
                RemoveInventoryItemDataViewModel(removedItem);
            }
        }
    }

    private void SetInventoryItemDataViewModel(ItemData itemData)
    {
        var inventoryItemDataViewModels = GetItemDataViewModelList(itemData.PageIndex);

        if (inventoryItemDataViewModels != null)
        {
            var itemDataViewModel = _itemDataManager.GetItemData(itemData);
            inventoryItemDataViewModels[itemDataViewModel.SlotIndex].ItemDataViewModel = itemDataViewModel;
            OnPropertyChanged(nameof(inventoryItemDataViewModels));
        }
    }

    private void RemoveInventoryItemDataViewModel(ItemData removedItem)
    {
        var inventoryItemDataViewModels = GetItemDataViewModelList(removedItem.PageIndex);

        if (inventoryItemDataViewModels != null)
        {
            inventoryItemDataViewModels[removedItem.SlotIndex].ItemDataViewModel = null;
            OnPropertyChanged(nameof(inventoryItemDataViewModels));
        }
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
    private string _title = Resources.CharacterInventory;

    [ObservableProperty]
    private long _cashValue;

    [ObservableProperty]
    private int _bonusCashValue;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PageText))]
    private int _currentPage;
    partial void OnCurrentPageChanged(int value)
    {
        NextPageCommand.NotifyCanExecuteChanged();
        PreviousPageCommand.NotifyCanExecuteChanged();
    }

    public string PageText => $"{CurrentPage}/5";

    #endregion

    #region Inventory

    [ObservableProperty]
    private ObservableCollection<ItemData>? _itemDatabaseList;

    [ObservableProperty]
    private ObservableCollection<ItemData>? _deletedItemDatabaseList;

    [ObservableProperty]
    private ObservableCollection<InventoryItem>? _equipmentItemDataViewModels;

    [ObservableProperty]
    private ObservableCollection<InventoryItem>? _consumeItemDataViewModels;

    [ObservableProperty]
    private ObservableCollection<InventoryItem>? _otherItemDataViewModels;

    [ObservableProperty]
    private ObservableCollection<InventoryItem>? _questItemDataViewModels;

    [ObservableProperty]
    private ObservableCollection<InventoryItem>? _costumeItemDataViewModels;

    [ObservableProperty]
    private ObservableCollection<InventoryItem>? _hiddenItemDataViewModels;

    #endregion

    #endregion
}
