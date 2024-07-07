using RHToolkit.Messages;
using RHToolkit.Models;
using RHToolkit.Models.Database;
using RHToolkit.Models.MessageBox;
using RHToolkit.Services;
using RHToolkit.ViewModels.Controls;

namespace RHToolkit.ViewModels.Windows;

public partial class InventoryWindowViewModel : ObservableObject, IRecipient<CharacterInfoMessage>, IRecipient<ItemDataMessage>
{
    private readonly IWindowsService _windowsService;
    private readonly IDatabaseService _databaseService;
    private readonly ItemHelper _itemHelper;

    public InventoryWindowViewModel(IWindowsService windowsService, IDatabaseService databaseService, ItemHelper itemHelper)
    {
        _windowsService = windowsService;
        _databaseService = databaseService;
        _itemHelper = itemHelper;

        CurrentPage = 1;

        WeakReferenceMessenger.Default.Register<ItemDataMessage>(this);
        WeakReferenceMessenger.Default.Register<CharacterInfoMessage>(this);
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

            if (RHMessageBoxHelper.ConfirmMessage($"Save inventory changes?"))
            {
                var characterInfo = new CharacterInfo(CharacterData.CharacterID, CharacterData.AuthID, CharacterData.CharacterName!, CharacterData.AccountName!, CharacterData.Class, CharacterData.Job);

                await _databaseService.SaveInventoryItem(characterInfo, ItemDatabaseList, DeletedItemDatabaseList, "N_InventoryItem");
                RHMessageBoxHelper.ShowOKMessage("Inventory saved successfully!", "Success");
                await ReadCharacterData(CharacterData.CharacterName!);
            }

        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"Error saving inventory changes: {ex.Message}", "Error");
        }
    }

    #endregion

    #region Remove Item

    [RelayCommand]
    private void RemoveItem(string parameter)
    {
        // Split the parameter into slotIndex and pageIndex
        var parameters = parameter.Split(',');
        if (parameters.Length != 2) return;

        if (int.TryParse(parameters[0], out int slotIndex) && int.TryParse(parameters[1], out int pageIndex))
        {
            if (ItemDatabaseList != null)
            {
                // Find the existing item in ItemDatabaseList
                var removedItem = ItemDatabaseList.FirstOrDefault(item => item.SlotIndex == slotIndex && item.PageIndex == pageIndex);
                var removedItemIndex = ItemDatabaseList.FindIndex(item => item.SlotIndex == slotIndex && item.PageIndex == pageIndex);

                if (removedItem != null)
                {
                    // Remove the item with the specified SlotIndex from ItemDatabaseList
                    ItemDatabaseList?.Remove(removedItem);

                    if (!removedItem.IsNewItem)
                    {
                        DeletedItemDatabaseList ??= [];
                        DeletedItemDatabaseList.Add(removedItem);
                    }

                    RemoveFrameViewModel(removedItem);
                }

            }
        }
    }

    private void RemoveFrameViewModel(ItemData removedItem)
    {
        EquipmentFrameViewModels ??= [];
        ConsumeFrameViewModels ??= [];
        OtherFrameViewModels ??= [];
        QuestFrameViewModels ??= [];
        CostumeFrameViewModels ??= [];
        HiddenFrameViewModels ??= [];

        switch (removedItem.PageIndex)
        {
            case 1:
                var removedEquipmentItemIndex = EquipmentFrameViewModels.FindIndex(f => f.SlotIndex == removedItem.SlotIndex);
                EquipmentFrameViewModels.RemoveAt(removedEquipmentItemIndex);
                OnPropertyChanged(nameof(EquipmentFrameViewModels));
                break;
            case 2:
                var removedConsumeItemIndex = ConsumeFrameViewModels.FindIndex(f => f.SlotIndex == removedItem.SlotIndex);
                ConsumeFrameViewModels.RemoveAt(removedConsumeItemIndex);
                OnPropertyChanged(nameof(ConsumeFrameViewModels));
                break;
            case 3:
                var removedOtherItemIndex = OtherFrameViewModels.FindIndex(f => f.SlotIndex == removedItem.SlotIndex);
                OtherFrameViewModels.RemoveAt(removedOtherItemIndex);
                OnPropertyChanged(nameof(OtherFrameViewModels));
                break;
            case 4:
                var removedQuestItemIndex = QuestFrameViewModels.FindIndex(f => f.SlotIndex == removedItem.SlotIndex);
                QuestFrameViewModels.RemoveAt(removedQuestItemIndex);
                OnPropertyChanged(nameof(QuestFrameViewModels));
                break;
            case 5:
                var removedCostumeItemIndex = CostumeFrameViewModels.FindIndex(f => f.SlotIndex == removedItem.SlotIndex);
                CostumeFrameViewModels.RemoveAt(removedCostumeItemIndex);
                OnPropertyChanged(nameof(CostumeFrameViewModels));
                break;
            case 6:
                var removedHiddenItemIndex = HiddenFrameViewModels.FindIndex(f => f.SlotIndex == removedItem.SlotIndex);
                HiddenFrameViewModels.RemoveAt(removedHiddenItemIndex);
                OnPropertyChanged(nameof(HiddenFrameViewModels));
                break;
            default: break;
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

    public async void Receive(CharacterInfoMessage message)
    {
        if (Token == Guid.Empty)
        {
            Token = message.Token;
        }

        if (message.Recipient == "InventoryWindow" && message.Token == Token)
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
                Title = $"Character Inventory ({characterData.CharacterName})";
                List<ItemData> inventoryItems = await _databaseService.GetItemList(characterData.CharacterID, "N_InventoryItem");
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
        ItemDatabaseList = null;
        DeletedItemDatabaseList = null;
        EquipmentFrameViewModels?.Clear();
        ConsumeFrameViewModels?.Clear();
        OtherFrameViewModels?.Clear();
        QuestFrameViewModels?.Clear();
        CostumeFrameViewModels?.Clear();
        OnPropertyChanged(nameof(EquipmentFrameViewModels));
        OnPropertyChanged(nameof(ConsumeFrameViewModels));
        OnPropertyChanged(nameof(OtherFrameViewModels));
        OnPropertyChanged(nameof(QuestFrameViewModels));
        OnPropertyChanged(nameof(CostumeFrameViewModels));

    }
    #endregion

    #region Send ItemData

    [RelayCommand]
    private void AddItem(string parameter)
    {
        if (CharacterData == null) return;

        // Split the parameter into slotIndex and pageIndex
        var parameters = parameter.Split(',');
        if (parameters.Length != 2) return;

        if (int.TryParse(parameters[0], out int slotIndex) && int.TryParse(parameters[1], out int pageIndex))
        {
            ItemData? itemData = ItemDatabaseList?.FirstOrDefault(i => i.SlotIndex == slotIndex && i.PageIndex == pageIndex) ?? new ItemData { SlotIndex = slotIndex, PageIndex = pageIndex };
            var characterInfo = new CharacterInfo(CharacterData.CharacterID, CharacterData.AuthID, CharacterData.CharacterName!, CharacterData.AccountName!, CharacterData.Class, CharacterData.Job);

            _windowsService.OpenItemWindow(characterInfo.CharacterID, "InventoryItem", itemData, characterInfo);
        }
    }

    #endregion

    #region Receive ItemData
    public void Receive(ItemDataMessage message)
    {
        if (message.Recipient == "InventoryWindow" && message.Token == Token)
        {
            var newItemData = message.Value;

            ItemDatabaseList ??= [];

            // Find the existing item in ItemDatabaseList
            var existingItem = ItemDatabaseList.FirstOrDefault(item => item.SlotIndex == newItemData.SlotIndex && item.PageIndex == newItemData.PageIndex);

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

        var newItem = ItemHelper.CreateNewItem(CharacterData, newItemData, newItemData.PageIndex);

        ItemDatabaseList ??= [];
        ItemDatabaseList.Add(newItem);

        var frameViewModel = _itemHelper.GetItemData(newItem);

        SetFrameViewModel(frameViewModel);
    }

    private void UpdateItem(ItemData existingItem, ItemData newItemData)
    {
        if (CharacterData == null) return;

        ItemDatabaseList ??= [];

        // Check if the IDs are different
        if (existingItem.ItemId != newItemData.ItemId)
        {
            RHMessageBoxHelper.ShowOKMessage($"The slot '{newItemData.SlotIndex}' is already in use.", "Info");
            return;
        }
        else
        {
            ItemDatabaseList.Remove(existingItem);
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

            ItemDatabaseList.Add(existingItem);

            var frameViewModel = _itemHelper.GetItemData(existingItem);

            SetFrameViewModel(frameViewModel);
        }
    }

    private void LoadInventoryItems(List<ItemData> inventoryItems)
    {
        if (inventoryItems != null)
        {
            foreach (var inventoryItem in inventoryItems)
            {
                var frameViewModel = _itemHelper.GetItemData(inventoryItem);
                SetFrameViewModel(frameViewModel);
            }
        }
    }

    private void SetFrameViewModel(FrameViewModel frameViewModel)
    {
        EquipmentFrameViewModels ??= [];
        ConsumeFrameViewModels ??= [];
        OtherFrameViewModels ??= [];
        QuestFrameViewModels ??= [];
        CostumeFrameViewModels ??= [];
        HiddenFrameViewModels ??= [];

        switch (frameViewModel.PageIndex)
        {
            case 1:
                EquipmentFrameViewModels.Add(frameViewModel);
                OnPropertyChanged(nameof(EquipmentFrameViewModels));
                break;
            case 2:
                ConsumeFrameViewModels.Add(frameViewModel);
                OnPropertyChanged(nameof(ConsumeFrameViewModels));
                break;
            case 3:
                OtherFrameViewModels.Add(frameViewModel);
                OnPropertyChanged(nameof(OtherFrameViewModels));
                break;
            case 4:
                QuestFrameViewModels.Add(frameViewModel);
                OnPropertyChanged(nameof(QuestFrameViewModels));
                break;
            case 5:
                CostumeFrameViewModels.Add(frameViewModel);
                OnPropertyChanged(nameof(CostumeFrameViewModels));
                break;
            case 6:
                HiddenFrameViewModels.Add(frameViewModel);
                OnPropertyChanged(nameof(HiddenFrameViewModels));
                break;
            default: break;
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
    private string _title = "Character Inventory";

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
    private List<ItemData>? _itemDatabaseList;

    [ObservableProperty]
    private List<ItemData>? _deletedItemDatabaseList;

    [ObservableProperty]
    private List<FrameViewModel>? _equipmentFrameViewModels;

    [ObservableProperty]
    private List<FrameViewModel>? _consumeFrameViewModels;

    [ObservableProperty]
    private List<FrameViewModel>? _otherFrameViewModels;

    [ObservableProperty]
    private List<FrameViewModel>? _questFrameViewModels;

    [ObservableProperty]
    private List<FrameViewModel>? _costumeFrameViewModels;

    [ObservableProperty]
    private List<FrameViewModel>? _hiddenFrameViewModels;

    #endregion

    #endregion
}
