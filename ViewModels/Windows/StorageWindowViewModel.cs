using RHToolkit.Messages;
using RHToolkit.Models;
using RHToolkit.Models.Database;
using RHToolkit.Models.MessageBox;
using RHToolkit.Services;
using RHToolkit.ViewModels.Controls;
using System.Data;

namespace RHToolkit.ViewModels.Windows;

public partial class StorageWindowViewModel : ObservableObject, IRecipient<CharacterInfoMessage>, IRecipient<ItemDataMessage>
{
    private readonly IWindowsService _windowsService;
    private readonly IDatabaseService _databaseService;
    private readonly ItemHelper _itemHelper;

    public StorageWindowViewModel(IWindowsService windowsService, IDatabaseService databaseService, ItemHelper itemHelper)
    {
        _windowsService = windowsService;
        _databaseService = databaseService;
        _itemHelper = itemHelper;

        CurrentStoragePage = 1;
        CurrentAccountStoragePage = 1;

        WeakReferenceMessenger.Default.Register<ItemDataMessage>(this);
        WeakReferenceMessenger.Default.Register<CharacterInfoMessage>(this);
    }

    #region Commands

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
                var characterInfo = new CharacterInfo(CharacterData.CharacterID, CharacterData.AuthID, CharacterData.CharacterName!, CharacterData.AccountName!, CharacterData.Class, CharacterData.Job);

                await _databaseService.SaveInventoryItem(characterInfo, StorageItemDatabaseList, DeletedStorageItemDatabaseList, "N_InventoryItem");
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
                var characterInfo = new CharacterInfo(CharacterData.CharacterID, CharacterData.AuthID, CharacterData.CharacterName!, CharacterData.AccountName!, CharacterData.Class, CharacterData.Job);

                await _databaseService.SaveInventoryItem(characterInfo, AccountStorageItemDatabaseList, DeletedAccountStorageItemDatabaseList, "tbl_Account_Storage");
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
                var removedItemIndex = StorageItemDatabaseList.FindIndex(item => item.SlotIndex == slotIndex);

                if (removedItem != null)
                {
                    // Remove the item with the specified SlotIndex from ItemDatabaseList
                    StorageItemDatabaseList?.Remove(removedItem);

                    if (!removedItem.IsNewItem)
                    {
                        DeletedStorageItemDatabaseList ??= [];
                        DeletedStorageItemDatabaseList.Add(removedItem);
                    }

                    RemoveFrameViewModel(removedItem);
                }

            }
        }
    }

    private void RemoveFrameViewModel(ItemData removedItem)
    {
        StorageFrameViewModels ??= [];

        var removedStorageItemIndex = StorageFrameViewModels.FindIndex(f => f.SlotIndex == removedItem.SlotIndex);
        StorageFrameViewModels.RemoveAt(removedStorageItemIndex);
        OnPropertyChanged(nameof(StorageFrameViewModels));
    }
    #endregion

    #region Remove Account Storage Item

    [RelayCommand]
    private void RemoveAccountStorageItem(string parameter)
    {
        if (int.TryParse(parameter, out int slotIndex) )
        {
            if (AccountStorageItemDatabaseList != null)
            {
                // Find the existing item in ItemDatabaseList
                var removedItem = AccountStorageItemDatabaseList.FirstOrDefault(item => item.SlotIndex == slotIndex);
                var removedItemIndex = AccountStorageItemDatabaseList.FindIndex(item => item.SlotIndex == slotIndex);

                if (removedItem != null)
                {
                    // Remove the item with the specified SlotIndex from ItemDatabaseList
                    AccountStorageItemDatabaseList?.Remove(removedItem);

                    if (!removedItem.IsNewItem)
                    {
                        DeletedAccountStorageItemDatabaseList ??= [];
                        DeletedAccountStorageItemDatabaseList.Add(removedItem);
                    }

                    RemoveAccountFrameViewModel(removedItem);
                }

            }
        }
    }

    private void RemoveAccountFrameViewModel(ItemData removedItem)
    {
        AccountStorageFrameViewModels ??= [];

        var removedAccountStorageItemIndex = AccountStorageFrameViewModels.FindIndex(f => f.SlotIndex == removedItem.SlotIndex);
        AccountStorageFrameViewModels.RemoveAt(removedAccountStorageItemIndex);
        OnPropertyChanged(nameof(AccountStorageFrameViewModels));
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

    #region Account Page
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

    #region Messenger

    #region Load CharacterData

    public async void Receive(CharacterInfoMessage message)
    {
        if (Token == Guid.Empty)
        {
            Token = message.Token;
        }

        if (message.Recipient == "StorageWindow" && message.Token == Token)
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

        if (int.TryParse(parameter, out int slotIndex))
        {
            ItemData? itemData = StorageItemDatabaseList?.FirstOrDefault(i => i.SlotIndex == slotIndex) ?? new ItemData { SlotIndex = slotIndex };
            var characterInfo = new CharacterInfo(CharacterData.CharacterID, CharacterData.AuthID, CharacterData.CharacterName!, CharacterData.AccountName!, CharacterData.Class, CharacterData.Job);

            _windowsService.OpenItemWindow(characterInfo.CharacterID, "StorageItem", itemData, characterInfo);
        }
    }

    [RelayCommand]
    private void AddAccountStorageItem(string parameter)
    {
        if (CharacterData == null) return;

        if (int.TryParse(parameter, out int slotIndex))
        {
            ItemData? itemData = AccountStorageItemDatabaseList?.FirstOrDefault(i => i.SlotIndex == slotIndex) ?? new ItemData { SlotIndex = slotIndex };
            var characterInfo = new CharacterInfo(CharacterData.CharacterID, CharacterData.AuthID, CharacterData.CharacterName!, CharacterData.AccountName!, CharacterData.Class, CharacterData.Job);

            _windowsService.OpenItemWindow(characterInfo.CharacterID, "AccountStorageItem", itemData, characterInfo);
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
            var existingItem = StorageItemDatabaseList.FirstOrDefault(item => item.SlotIndex == newItemData.SlotIndex && item.PageIndex == newItemData.PageIndex);

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
            var existingItem = AccountStorageItemDatabaseList.FirstOrDefault(item => item.SlotIndex == newItemData.SlotIndex && item.PageIndex == newItemData.PageIndex);

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

    #region Item Methods

    private void CreateStorageItem(ItemData newItemData)
    {
        if (CharacterData == null) return;

        var newItem = ItemHelper.CreateNewItem(CharacterData, newItemData, 21);

        StorageItemDatabaseList ??= [];
        StorageItemDatabaseList.Add(newItem);

        var frameViewModel = _itemHelper.GetItemData(newItem);

        SetStorageFrameViewModel(frameViewModel);
    }

    private void UpdateStorageItem(ItemData existingItem, ItemData newItemData)
    {
        if (CharacterData == null) return;

        StorageItemDatabaseList ??= [];

        // Check if the IDs are different
        if (existingItem.ItemId != newItemData.ItemId)
        {
            RHMessageBoxHelper.ShowOKMessage($"The slot '{newItemData.SlotIndex}' is already in use.", "Info");
            return;
        }
        else
        {
            StorageItemDatabaseList.Remove(existingItem);
            RemoveFrameViewModel(existingItem);

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

            StorageItemDatabaseList.Add(existingItem);

            var frameViewModel = _itemHelper.GetItemData(existingItem);

            SetStorageFrameViewModel(frameViewModel);
        }
    }

    private void LoadStorageItems(List<ItemData> storageItems)
    {
        if (storageItems != null)
        {
            foreach (var storageItem in storageItems)
            {
                var frameViewModel = _itemHelper.GetItemData(storageItem);
                SetStorageFrameViewModel(frameViewModel);
            }
        }
    }

    private void SetStorageFrameViewModel(FrameViewModel frameViewModel)
    {
        StorageFrameViewModels ??= [];

        StorageFrameViewModels.Add(frameViewModel);
        OnPropertyChanged(nameof(StorageFrameViewModels));
    }

    private void CreateAccountStorageItem(ItemData newItemData)
    {
        if (CharacterData == null) return;

        var newItem = ItemHelper.CreateNewItem(CharacterData, newItemData, 3);

        AccountStorageItemDatabaseList ??= [];
        AccountStorageItemDatabaseList.Add(newItem);

        var frameViewModel = _itemHelper.GetItemData(newItem);

        SetAccountStorageFrameViewModel(frameViewModel);
    }

    private void UpdateAccountStorageItem(ItemData existingItem, ItemData newItemData)
    {
        if (CharacterData == null) return;

        AccountStorageItemDatabaseList ??= [];

        // Check if the IDs are different
        if (existingItem.ItemId != newItemData.ItemId)
        {
            RHMessageBoxHelper.ShowOKMessage($"The slot '{newItemData.SlotIndex}' is already in use.", "Info");
            return;
        }
        else
        {
            AccountStorageItemDatabaseList.Remove(existingItem);
            RemoveFrameViewModel(existingItem);

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

            AccountStorageItemDatabaseList.Add(existingItem);

            var frameViewModel = _itemHelper.GetItemData(existingItem);

            SetAccountStorageFrameViewModel(frameViewModel);
        }
    }

    private void LoadAccountStorageItems(List<ItemData> accountStorageItems)
    {
        if (accountStorageItems != null)
        {
            foreach (var accountStorageItem in accountStorageItems)
            {
                var frameViewModel = _itemHelper.GetItemData(accountStorageItem);
                SetAccountStorageFrameViewModel(frameViewModel);
            }
        }
    }

    private void SetAccountStorageFrameViewModel(FrameViewModel frameViewModel)
    {
        AccountStorageFrameViewModels ??= [];

        AccountStorageFrameViewModels.Add(frameViewModel);
        OnPropertyChanged(nameof(AccountStorageFrameViewModels));
    }

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
    private string _title = "Character Inventory";

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
        NextStoragePageCommand.NotifyCanExecuteChanged();
        PreviousStoragePageCommand.NotifyCanExecuteChanged();
    }

    public string AccountStoragePageText => $"{CurrentAccountStoragePage}/5";

    #endregion

    #endregion
}
