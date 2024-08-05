using RHToolkit.Messages;
using RHToolkit.Models;
using RHToolkit.Models.Database;
using RHToolkit.Models.MessageBox;
using RHToolkit.Services;
using RHToolkit.ViewModels.Controls;
using System.Data;

namespace RHToolkit.ViewModels.Windows;

public partial class StorageWindowViewModel : ObservableObject, IRecipient<CharacterDataMessage>, IRecipient<ItemDataMessage>
{
    private readonly IWindowsService _windowsService;
    private readonly IDatabaseService _databaseService;
    private readonly ItemDataManager _itemDataManager;

    public StorageWindowViewModel(IWindowsService windowsService, IDatabaseService databaseService, ItemDataManager itemHelper)
    {
        _windowsService = windowsService;
        _databaseService = databaseService;
        _itemDataManager = itemHelper;

        CurrentStoragePage = 1;
        CurrentAccountStoragePage = 1;

        WeakReferenceMessenger.Default.Register<ItemDataMessage>(this);
        WeakReferenceMessenger.Default.Register<CharacterDataMessage>(this);
    }

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
                var storageItems = await _databaseService.GetItemList(characterData.CharacterID, "tbl_Personal_Storage");
                var accountStorageItems = await _databaseService.GetItemList(characterData.AuthID, "tbl_Account_Storage");
                LoadStorageItems(storageItems);
                StorageItemDatabaseList = storageItems;

                LoadAccountStorageItems(accountStorageItems);
                AccountStorageItemDatabaseList = accountStorageItems;
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

    private void ClearData()
    {
        CharacterData = null;
        StorageItemDatabaseList = null;
        DeletedStorageItemDatabaseList = null;
        StorageFrameViewModels?.Clear();
        OnPropertyChanged(nameof(StorageFrameViewModels));
        AccountStorageItemDatabaseList = null;
        DeletedAccountStorageItemDatabaseList = null;
        AccountStorageFrameViewModels?.Clear();
        OnPropertyChanged(nameof(AccountStorageFrameViewModels));
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
        if (message.MessageType == "StorageItem" && message.Token == Token)
        {
            var newItemData = message.Value;

            StorageItemDatabaseList ??= [];

            // Find the existing item in ItemDatabaseList
            var existingItem = StorageItemDatabaseList.FirstOrDefault(item => item.SlotIndex == newItemData.SlotIndex);

            if (existingItem != null)
            {
                // Update existing item
                UpdateStorageItem(existingItem, newItemData);

            }
            else
            {
                if (newItemData.ItemId != 0)
                {
                    // Create new item
                    CreateStorageItem(newItemData);
                }

            }
        }
        else if (message.MessageType == "AccountStorageItem" && message.Token == Token)
        {
            var newItemData = message.Value;

            AccountStorageItemDatabaseList ??= [];

            // Find the existing item in ItemDatabaseList
            var existingItem = AccountStorageItemDatabaseList.FirstOrDefault(item => item.SlotIndex == newItemData.SlotIndex);

            if (existingItem != null)
            {
                // Update existing item
                UpdateAccountStorageItem(existingItem, newItemData);

            }
            else
            {
                if (newItemData.ItemId != 0)
                {
                    // Create new item
                    CreateAccountStorageItem(newItemData);
                }

            }
        }
    }

    #endregion

    #endregion

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
                await ReadCharacterData(CharacterData.CharacterName!);
            }

        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"Error saving storage changes: {ex.Message}", "Error");
        }
    }

    #endregion

    #region Remove Storage Item

    [RelayCommand]
    private void RemoveStorageItem(string parameter)
    {
        if (int.TryParse(parameter, out int slotIndex))
        {
            if (StorageItemDatabaseList != null)
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
                    RemoveStorageFrameViewModel(removedItem);
                }

            }
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
                await ReadCharacterData(CharacterData.CharacterName!);
            }

        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"Error saving storage changes: {ex.Message}", "Error");
        }
    }

    #endregion

    #region Remove Account Storage Item

    [RelayCommand]
    private void RemoveAccountStorageItem(string parameter)
    {
        if (int.TryParse(parameter, out int slotIndex))
        {
            if (AccountStorageItemDatabaseList != null)
            {
                // Find the existing item in ItemDatabaseList
                var removedItem = AccountStorageItemDatabaseList.FirstOrDefault(item => item.SlotIndex == slotIndex);
                var removedItemIndex = AccountStorageItemDatabaseList.FindIndex(item => item.SlotIndex == slotIndex);
                
                if (removedItem != null)
                {
                    if (!removedItem.IsNewItem)
                    {
                        DeletedAccountStorageItemDatabaseList ??= [];
                        DeletedAccountStorageItemDatabaseList.Add(removedItem);
                    }

                    AccountStorageItemDatabaseList.RemoveAt(removedItemIndex);
                    RemoveAccountStorageFrameViewModel(removedItem);
                }

            }
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

    #region Item Methods

    #region Storage

    private void LoadStorageItems(List<ItemData> storageItems)
    {
        if (storageItems != null)
        {
            foreach (var storageItem in storageItems)
            {
                var frameViewModel = _itemDataManager.GetItemData(storageItem);
                SetStorageFrameViewModel(frameViewModel);
            }
        }
    }

    private void CreateStorageItem(ItemData newItemData)
    {
        if (CharacterData == null) return;

        var newItem = ItemDataManager.CreateNewItem(CharacterData, newItemData, 3);
        StorageItemDatabaseList ??= [];
        StorageItemDatabaseList.Add(newItem);
        var frameViewModel = _itemDataManager.GetItemData(newItem);

        SetStorageFrameViewModel(frameViewModel);
    }

    private void UpdateStorageItem(ItemData existingItem, ItemData newItem)
    {
        if (CharacterData == null) return;

        StorageItemDatabaseList ??= [];

        // Check if the IDs are different
        if (existingItem.ItemId != newItem.ItemId && !existingItem.IsNewItem)
        {
            RHMessageBoxHelper.ShowOKMessage($"The slot '{newItem.SlotIndex}' is already in use.", "Info");
            return;
        }

        if (existingItem.IsNewItem)
        {
            RemoveStorageItem(existingItem.SlotIndex.ToString());
            CreateStorageItem(newItem);
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

            RemoveStorageItem(existingItem.SlotIndex.ToString());
            var frameViewModel = _itemDataManager.GetItemData(existingItem);
            StorageItemDatabaseList.Add(existingItem);
            SetStorageFrameViewModel(frameViewModel);
        }
    }

    private void SetStorageFrameViewModel(FrameViewModel frameViewModel)
    {
        StorageFrameViewModels ??= [];

        StorageFrameViewModels.Add(frameViewModel);
        OnPropertyChanged(nameof(StorageFrameViewModels));
    }

    private void RemoveStorageFrameViewModel(ItemData removedItem)
    {
        StorageFrameViewModels ??= [];

        var removedStorageItemIndex = StorageFrameViewModels.FindIndex(f => f.SlotIndex == removedItem.SlotIndex);
        if (removedStorageItemIndex != -1)
        {
            StorageFrameViewModels.RemoveAt(removedStorageItemIndex);
            OnPropertyChanged(nameof(StorageFrameViewModels));
        }

    }
    #endregion

    #region Account Storage

    private void LoadAccountStorageItems(List<ItemData> accountStorageItems)
    {
        if (accountStorageItems != null)
        {
            foreach (var accountStorageItem in accountStorageItems)
            {
                var frameViewModel = _itemDataManager.GetItemData(accountStorageItem);
                SetAccountStorageFrameViewModel(frameViewModel);
            }
        }
    }

    private void CreateAccountStorageItem(ItemData newItemData)
    {
        if (CharacterData == null) return;

        var newItem = ItemDataManager.CreateNewItem(CharacterData, newItemData, 3);
        AccountStorageItemDatabaseList ??= [];
        AccountStorageItemDatabaseList.Add(newItem);
        var frameViewModel = _itemDataManager.GetItemData(newItem);

        SetAccountStorageFrameViewModel(frameViewModel);
    }

    private void UpdateAccountStorageItem(ItemData existingItem, ItemData newItem)
    {
        if (CharacterData == null) return;

        AccountStorageItemDatabaseList ??= [];

        // Check if the IDs are different
        if (existingItem.ItemId != newItem.ItemId && !existingItem.IsNewItem)
        {
            RHMessageBoxHelper.ShowOKMessage($"The slot '{newItem.SlotIndex}' is already in use.", "Info");
            return;
        }

        if (existingItem.IsNewItem)
        {
            RemoveAccountStorageItem(existingItem.SlotIndex.ToString());
            CreateAccountStorageItem(newItem);
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

            RemoveAccountStorageItem(existingItem.SlotIndex.ToString());
            var frameViewModel = _itemDataManager.GetItemData(existingItem);
            AccountStorageItemDatabaseList.Add(existingItem);
            SetAccountStorageFrameViewModel(frameViewModel);
        }
    }

    private void SetAccountStorageFrameViewModel(FrameViewModel frameViewModel)
    {
        AccountStorageFrameViewModels ??= [];

        AccountStorageFrameViewModels.Add(frameViewModel);
        OnPropertyChanged(nameof(AccountStorageFrameViewModels));
    }

    private void RemoveAccountStorageFrameViewModel(ItemData removedItem)
    {
        AccountStorageFrameViewModels ??= [];

        var removedAccountStorageItemIndex = AccountStorageFrameViewModels.FindIndex(f => f.SlotIndex == removedItem.SlotIndex);
        if (removedAccountStorageItemIndex != -1)
        {
            AccountStorageFrameViewModels.RemoveAt(removedAccountStorageItemIndex);
            OnPropertyChanged(nameof(AccountStorageFrameViewModels));
        }

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
    private List<ItemData>? _storageItemDatabaseList;

    [ObservableProperty]
    private List<ItemData>? _deletedStorageItemDatabaseList;

    [ObservableProperty]
    private List<FrameViewModel>? _storageFrameViewModels;

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
    private List<ItemData>? _accountStorageItemDatabaseList;

    [ObservableProperty]
    private List<ItemData>? _deletedAccountStorageItemDatabaseList;

    [ObservableProperty]
    private List<FrameViewModel>? _accountStorageFrameViewModels;

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
