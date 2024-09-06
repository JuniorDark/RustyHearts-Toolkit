using RHToolkit.Messages;
using RHToolkit.Models;
using RHToolkit.Models.Database;
using RHToolkit.Models.MessageBox;
using RHToolkit.Services;
using RHToolkit.ViewModels.Windows.Database.VM;
using System.Data;

namespace RHToolkit.ViewModels.Windows;

public partial class StorageWindowViewModel : ObservableObject, IRecipient<CharacterDataMessage>, IRecipient<ItemDataMessage>
{
    private readonly IWindowsService _windowsService;
    private readonly IDatabaseService _databaseService;
    private readonly ItemDataManager _itemDataManager;

    private enum StorageType
    {
        PersonalStorage,
        AccountStorage
    }

    public StorageWindowViewModel(IWindowsService windowsService, IDatabaseService databaseService, ItemDataManager itemDataManager)
    {
        _windowsService = windowsService;
        _databaseService = databaseService;
        _itemDataManager = itemDataManager;

        CurrentStoragePage = 1;
        CurrentAccountStoragePage = 1;

        WeakReferenceMessenger.Default.Register<ItemDataMessage>(this);
        WeakReferenceMessenger.Default.Register<CharacterDataMessage>(this);
    }

    #region Commands

    #region Storage

    #region Save Storage
    [RelayCommand]
    private async Task SaveStorage()
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

            if (RHMessageBoxHelper.ConfirmMessage($"Save storage changes?"))
            {
                await _databaseService.SaveInventoryItem(CharacterData, StorageItemDatabaseList, DeletedStorageItemDatabaseList, "N_InventoryItem");
                RHMessageBoxHelper.ShowOKMessage("Storage saved successfully!", "Success");
                await ReadStorageData();
            }

        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"Error saving storage changes: {ex.Message}", "Error");
        }
    }

    private async Task ReadStorageData()
    {
        if (CharacterData != null)
        {
            ClearStorageData();
            var storageItems = await _databaseService.GetItemList(CharacterData.CharacterID, "tbl_Personal_Storage");
            LoadStorageItems(storageItems, StorageType.PersonalStorage);
            StorageItemDatabaseList = storageItems;
        }
    }

    private void ClearStorageData()
    {
        StorageItemDatabaseList?.Clear();
        DeletedStorageItemDatabaseList?.Clear();
        StorageItemDataViewModels?.Clear();
    }

    #endregion

    #region Remove Storage Item

    [RelayCommand]
    private void RemoveStorageItem(string parameter)
    {
        if (int.TryParse(parameter, out int slotIndex))
        {
            RemoveItem(slotIndex, StorageType.PersonalStorage);
        }
    }

    #endregion

    #region Storage Page
    [RelayCommand(CanExecute = nameof(CanExecuteNextStoragePageCommand))]
    private void NextStoragePage()
    {
        if (CurrentStoragePage < 5)
        {
            CurrentStoragePage++;
        }
    }

    [RelayCommand(CanExecute = nameof(CanExecutePreviousStoragePageCommand))]
    private void PreviousStoragePage()
    {
        if (CurrentStoragePage > 1)
        {
            CurrentStoragePage--;
        }
    }

    private bool CanExecutePreviousStoragePageCommand()
    {
        return CurrentStoragePage > 1;
    }

    private bool CanExecuteNextStoragePageCommand()
    {
        return CurrentStoragePage < 5;
    }
    #endregion

    #endregion

    #region Account Storage

    #region Save Account Storage
    [RelayCommand]
    private async Task SaveAccountStorage()
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

            if (RHMessageBoxHelper.ConfirmMessage($"Save account storage changes?"))
            {
                await _databaseService.SaveInventoryItem(CharacterData, AccountStorageItemDatabaseList, DeletedAccountStorageItemDatabaseList, "tbl_Account_Storage");
                RHMessageBoxHelper.ShowOKMessage("Account Storage saved successfully!", "Success");
                await ReadAccountStorageData();
            }

        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"Error saving storage changes: {ex.Message}", "Error");
        }
    }

    private async Task ReadAccountStorageData()
    {
        if (CharacterData != null)
        {
            ClearAccountStorageData();
            var accountStorageItems = await _databaseService.GetItemList(CharacterData.AuthID, "tbl_Account_Storage");
            LoadAccountStorageItems(accountStorageItems, StorageType.AccountStorage);
            AccountStorageItemDatabaseList = accountStorageItems;
        }
    }

    private void ClearAccountStorageData()
    {
        AccountStorageItemDatabaseList?.Clear();
        DeletedAccountStorageItemDatabaseList?.Clear();
        AccountStorageItemDataViewModels?.Clear();
    }

    #endregion

    #region Remove Account Storage Item

    [RelayCommand]
    private void RemoveAccountStorageItem(string parameter)
    {
        if (int.TryParse(parameter, out int slotIndex))
        {
            RemoveItem(slotIndex, StorageType.AccountStorage);
        }
    }

    #endregion

    #region Account Storage Page
    [RelayCommand(CanExecute = nameof(CanExecuteNextAccountStoragePageCommand))]
    private void NextAccountStoragePage()
    {
        if (CurrentAccountStoragePage < 5)
        {
            CurrentAccountStoragePage++;
        }
    }

    [RelayCommand(CanExecute = nameof(CanExecutePreviousAccountStoragePageCommand))]
    private void PreviousAccountStoragePage()
    {
        if (CurrentAccountStoragePage > 1)
        {
            CurrentAccountStoragePage--;
        }
    }

    private bool CanExecutePreviousAccountStoragePageCommand()
    {
        return CurrentAccountStoragePage > 1;
    }

    private bool CanExecuteNextAccountStoragePageCommand()
    {
        return CurrentAccountStoragePage < 5;
    }
    #endregion

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

        if (message.Recipient == "StorageWindow" && message.Token == Token)
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
                ClearData();
                CharacterData = characterData;
                Title = $"Character Storage ({characterData.CharacterName})";
                UniAccountInfo = await _databaseService.GetUniAccountInfoAsync(characterData.AuthID);

                await ReadStorageData();
                await ReadAccountStorageData();
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

    private void LoadStorageItems(ObservableCollection<ItemData> storageItems, StorageType storageType)
    {
        if (storageItems == null) return;

        StorageItemDataViewModels = ItemDataManager.InitializeCollection(180, 21);

        foreach (var storageItem in storageItems)
        {
            SetStorageItemDataViewModel(storageItem, storageType);
        }
    }

    private void LoadAccountStorageItems(ObservableCollection<ItemData> storageItems, StorageType storageType)
    {
        if (storageItems == null) return;

        AccountStorageItemDataViewModels = ItemDataManager.InitializeCollection(180, 3);

        foreach (var storageItem in storageItems)
        {
            SetStorageItemDataViewModel(storageItem, storageType);
        }
    }

    private bool IsSlotLocked(int slotIndex, StorageType storageType)
    {
        if (storageType == StorageType.PersonalStorage)
        {
            return slotIndex > CharacterData!.StorageCount - 1;
        }
        else if (storageType == StorageType.AccountStorage)
        {
            return slotIndex > AccountStorageCount - 1;
        }
        return true;
    }

    private ObservableCollection<ItemData>? GetStorageItemList(StorageType storageType)
    {
        return storageType switch
        {
            StorageType.PersonalStorage => StorageItemDatabaseList,
            StorageType.AccountStorage => AccountStorageItemDatabaseList,
            _ => null
        };
    }

    private void ClearData()
    {
        CharacterData = null;
        StorageItemDatabaseList?.Clear();
        DeletedStorageItemDatabaseList?.Clear();
        StorageItemDataViewModels?.Clear();
        AccountStorageItemDatabaseList?.Clear();
        DeletedAccountStorageItemDatabaseList?.Clear();
        AccountStorageItemDataViewModels?.Clear();
    }
    #endregion

    #region Send ItemData

    [RelayCommand]
    private void AddStorageItem(string parameter)
    {
        if (CharacterData == null) return;

        try
        {
            if (int.TryParse(parameter, out int slotIndex))
            {
                ItemData? itemData = StorageItemDatabaseList?.FirstOrDefault(i => i.SlotIndex == slotIndex) ?? new ItemData { SlotIndex = slotIndex };
                _windowsService.OpenItemWindow(CharacterData.CharacterID, "StorageItem", itemData, CharacterData);
            }
        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"Error adding storage item: {ex.Message}", "Error");
        }
    }

    [RelayCommand]
    private void AddAccountStorageItem(string parameter)
    {
        if (CharacterData == null) return;

        try
        {
            if (int.TryParse(parameter, out int slotIndex))
            {
                ItemData? itemData = AccountStorageItemDatabaseList?.FirstOrDefault(i => i.SlotIndex == slotIndex) ?? new ItemData { SlotIndex = slotIndex };
                _windowsService.OpenItemWindow(CharacterData.CharacterID, "AccountStorageItem", itemData, CharacterData);
            }
        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"Error adding account storage item: {ex.Message}", "Error");
        }
    }

    #endregion

    #region Receive ItemData
    public void Receive(ItemDataMessage message)
    {
        if (message.Recipient != "StorageWindow" || message.Token != Token)
            return;

        var newItemData = message.Value;
        var storageType = message.MessageType == "StorageItem" ? StorageType.PersonalStorage : StorageType.AccountStorage;

        if (IsSlotLocked(newItemData.SlotIndex, storageType))
        {
            RHMessageBoxHelper.ShowOKMessage($"The slot '{newItemData.SlotIndex}' is locked.", "Cant Add Storage Item");
            return;
        }

        var storageList = GetStorageItemList(storageType);
        if (storageList == null)
            return;

        var existingItem = storageList.FirstOrDefault(item => item.SlotIndex == newItemData.SlotIndex);

        if (existingItem != null)
        {
            if (existingItem.ItemId != newItemData.ItemId && !existingItem.IsNewItem)
            {
                RHMessageBoxHelper.ShowOKMessage($"The slot '{newItemData.SlotIndex}' is already in use.", "Cant Add Storage Item");
                return;
            }
            else if (existingItem.IsNewItem)
            {
                RemoveItem(existingItem.SlotIndex, storageType);
                CreateStorageItem(newItemData, storageType);
            }
            else
            {
                UpdateStorageItem(existingItem, newItemData, storageType);
            }
            
        }
        else if (newItemData.ItemId != 0)
        {
            CreateStorageItem(newItemData, storageType);
        }
    }

    #endregion

    #endregion

    #region Storage Item Methods

    private void CreateStorageItem(ItemData newItemData, StorageType storageType)
    {
        if (CharacterData == null) return;

        var storagePage = storageType == StorageType.PersonalStorage ? 21 : 3;

        var newItem = ItemDataManager.CreateNewItem(CharacterData, newItemData, storagePage);

        if (storageType == StorageType.PersonalStorage)
        {
            StorageItemDatabaseList ??= [];
            StorageItemDatabaseList.Add(newItem);
        }
        else if (storageType == StorageType.AccountStorage)
        {
            AccountStorageItemDatabaseList ??= [];
            AccountStorageItemDatabaseList.Add(newItem);
        }

        OnPropertyChanged(GetPropertyName(storageType));

        SetStorageItemDataViewModel(newItem, storageType);
    }

    private void UpdateStorageItem(ItemData existingItem, ItemData newItem, StorageType storageType)
    {
        var updatedItem = ItemDataManager.UpdateItemData(existingItem, newItem);

        if (StorageItemDatabaseList != null && storageType == StorageType.PersonalStorage)
        {
            var index = StorageItemDatabaseList.IndexOf(existingItem);
            if (index >= 0)
            {
                StorageItemDatabaseList[index] = updatedItem;
            }
        }
        else if (AccountStorageItemDatabaseList != null && storageType == StorageType.AccountStorage)
        {
            var index = AccountStorageItemDatabaseList.IndexOf(existingItem);
            if (index >= 0)
            {
                AccountStorageItemDatabaseList[index] = updatedItem;
            }
        }

        OnPropertyChanged(GetPropertyName(storageType));
        SetStorageItemDataViewModel(updatedItem, storageType);
    }

    private void SetStorageItemDataViewModel(ItemData itemData, StorageType storageType)
    {
        var storageItemDataViewModels = GetStorageItemDataViewModels(storageType);

        if (storageItemDataViewModels != null)
        {
            var itemDataViewModel = _itemDataManager.GetItemData(itemData);
            storageItemDataViewModels[itemDataViewModel.SlotIndex].ItemDataViewModel = itemDataViewModel;
            storageItemDataViewModels[itemDataViewModel.SlotIndex].SlotIndex = itemDataViewModel.SlotIndex;
            OnPropertyChanged(GetItemDataViewModelsPropertyName(storageType));
        }
    }

    private void RemoveStorageItemDataViewModel(ItemData removedItem, StorageType storageType)
    {
        var storageItemDataViewModels = GetStorageItemDataViewModels(storageType);

        if (storageItemDataViewModels != null)
        {
            storageItemDataViewModels[removedItem.SlotIndex].ItemDataViewModel = null;
            OnPropertyChanged(GetItemDataViewModelsPropertyName(storageType));
        }
    }

    private void RemoveItem(int slotIndex, StorageType storageType)
    {
        if (StorageItemDatabaseList != null && storageType == StorageType.PersonalStorage)
        {
            // Find the existing item in ItemDatabaseList
            var removedItem = StorageItemDatabaseList.FirstOrDefault(item => item.SlotIndex == slotIndex);

            if (removedItem != null)
            {
                if (!removedItem.IsNewItem)
                {
                    DeletedStorageItemDatabaseList ??= [];
                    DeletedStorageItemDatabaseList.Add(removedItem);
                }

                StorageItemDatabaseList.Remove(removedItem);
                RemoveStorageItemDataViewModel(removedItem, storageType);
            }
        }
        else if (AccountStorageItemDatabaseList != null && storageType == StorageType.AccountStorage)
        {
            // Find the existing item in ItemDatabaseList
            var removedItem = AccountStorageItemDatabaseList.FirstOrDefault(item => item.SlotIndex == slotIndex);

            if (removedItem != null)
            {
                if (!removedItem.IsNewItem)
                {
                    DeletedAccountStorageItemDatabaseList ??= [];
                    DeletedAccountStorageItemDatabaseList.Add(removedItem);
                }

                AccountStorageItemDatabaseList.Remove(removedItem);
                RemoveStorageItemDataViewModel(removedItem, storageType);
            }
        }
    }

    private string GetPropertyName(StorageType storageType) =>
        storageType == StorageType.PersonalStorage ? nameof(StorageItemDatabaseList) : nameof(AccountStorageItemDatabaseList);

    private string GetItemDataViewModelsPropertyName(StorageType storageType) =>
        storageType == StorageType.PersonalStorage ? nameof(StorageItemDataViewModels) : nameof(AccountStorageItemDataViewModels);

    private ObservableCollection<InventoryItem>? GetStorageItemDataViewModels(StorageType storageType) =>
        storageType == StorageType.PersonalStorage ? StorageItemDataViewModels : AccountStorageItemDataViewModels;

    #endregion

    #region Properties 

    [ObservableProperty]
    private Guid? _token = Guid.Empty;

    #region Character

    [ObservableProperty]
    private CharacterData? _characterData;

    [ObservableProperty]
    private DataRow? _uniAccountInfo;
    partial void OnUniAccountInfoChanged(DataRow? value)
    {
        if (UniAccountInfo != null)
        {
            AccountStorageCount = (int)UniAccountInfo["AccountStorage_Count"];
            AccountStorageGold = (int)UniAccountInfo["AccountStorage_Gold"];
        }
    }

    [ObservableProperty]
    private int _accountStorageCount;

    [ObservableProperty]
    private int _accountStorageGold;

    [ObservableProperty]
    private bool _isButtonEnabled = true;

    [ObservableProperty]
    private string _title = "Warehouse";

    #endregion

    #region Storage

    [ObservableProperty]
    private ObservableCollection<ItemData>? _storageItemDatabaseList;

    [ObservableProperty]
    private ObservableCollection<ItemData>? _deletedStorageItemDatabaseList;

    [ObservableProperty]
    private ObservableCollection<InventoryItem>? _storageItemDataViewModels;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StoragePageText))]
    private int _currentStoragePage;
    partial void OnCurrentStoragePageChanged(int value)
    {
        NextStoragePageCommand.NotifyCanExecuteChanged();
        PreviousStoragePageCommand.NotifyCanExecuteChanged();
    }

    public string StoragePageText => $"{CurrentStoragePage}/5";

    #endregion

    #region Account Storage

    [ObservableProperty]
    private ObservableCollection<ItemData>? _accountStorageItemDatabaseList;

    [ObservableProperty]
    private ObservableCollection<ItemData>? _deletedAccountStorageItemDatabaseList;

    [ObservableProperty]
    private ObservableCollection<InventoryItem>? _accountStorageItemDataViewModels;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AccountStoragePageText))]
    private int _currentAccountStoragePage;
    partial void OnCurrentAccountStoragePageChanged(int value)
    {
        NextAccountStoragePageCommand.NotifyCanExecuteChanged();
        PreviousAccountStoragePageCommand.NotifyCanExecuteChanged();
    }

    public string AccountStoragePageText => $"{CurrentAccountStoragePage}/5";

    #endregion

    #endregion
}
