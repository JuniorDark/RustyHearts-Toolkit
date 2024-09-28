using Microsoft.Win32;
using RHToolkit.Messages;
using RHToolkit.Models;
using RHToolkit.Models.Database;
using RHToolkit.Models.Editor;
using RHToolkit.Models.MessageBox;
using RHToolkit.Properties;
using RHToolkit.Services;
using RHToolkit.ViewModels.Windows.Tools.VM;
using RHToolkit.Views.Windows;
using System.Data;
using System.Windows.Controls;
using static RHToolkit.Models.EnumService;

namespace RHToolkit.ViewModels.Windows
{
    public partial class NPCShopEditorViewModel : ObservableObject, IRecipient<ItemDataMessage>, IRecipient<DataRowViewMessage>
    {
        private readonly Guid _token;
        private readonly IWindowsService _windowsService;
        private readonly System.Timers.Timer _filterUpdateTimer;

        public NPCShopEditorViewModel(IWindowsService windowsService, ItemDataManager itemDataManager)
        {
            _token = Guid.NewGuid();
            _windowsService = windowsService;
            _itemDataManager = itemDataManager;
            DataTableManager = new()
            {
                Token = _token
            };
            _filterUpdateTimer = new()
            {
                Interval = 400,
                AutoReset = false
            };
            _filterUpdateTimer.Elapsed += FilterUpdateTimerElapsed;

            WeakReferenceMessenger.Default.Register<ItemDataMessage>(this);
            WeakReferenceMessenger.Default.Register<DataRowViewMessage>(this);
        }

        #region Commands 

        #region File

        [RelayCommand]
        private async Task LoadFile(string? parameter)
        {
            try
            {
                await CloseFile();

                if (int.TryParse(parameter, out int dropGroupType))
                {
                    string? fileName = GetFileNameFromShopType(dropGroupType);
                    if (fileName == null) return;

                    SetShopProperties(dropGroupType);
                    string columnName = GetColumnName(fileName);

                    bool isLoaded = await DataTableManager.LoadFileFromPath(fileName, null, columnName, "NPCShop");

                    if (isLoaded)
                    {
                        IsLoaded();
                    }
                }
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"Error: {ex.Message}", Resources.Error);
            }
        }

        [RelayCommand]
        private async Task LoadFileAs()
        {
            try
            {
                await CloseFile();

                string filter = "NPC Shop Files .rh|" +
                                "npcshop.rh;tradeshop.rh;itemmix.rh;costumemix.rh;shopitemvisiblefilter.rh;itempreview.rh|" +
                                "All Files (*.*)|*.*";

                OpenFileDialog openFileDialog = new()
                {
                    Filter = filter,
                    FilterIndex = 1
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    string fileName = Path.GetFileName(openFileDialog.FileName);
                    int shopType = GetShopTypeFromFileName(fileName);

                    if (shopType == -1)
                    {
                        RHMessageBoxHelper.ShowOKMessage($"The file '{fileName}' is not a valid Npc Shop file.", Resources.Error);
                        return;
                    }

                    SetShopProperties(shopType);
                    string columnName = GetColumnName(fileName);

                    bool isLoaded = await DataTableManager.LoadFileAs(openFileDialog.FileName, null, columnName, "NPCShop");

                    if (isLoaded)
                    {
                        IsLoaded();
                    }
                }
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"Error: {ex.Message}", Resources.Error);
            }
        }

        private void IsLoaded()
        {
            var title = GetTitleFromShopType(NpcShopType);

            Title = $"{title} Editor ({DataTableManager.CurrentFileName})";
            OpenMessage = "";
            IsVisible = Visibility.Visible;
            OnCanExecuteFileCommandChanged();
        }

        private static string? GetTitleFromShopType(NpcShopType npcShopType)
        {
            return npcShopType switch
            {
                NpcShopType.NpcShop => "NPC Shop",
                NpcShopType.TradeShop => "NPC Trade Shop",
                NpcShopType.ItemMix => "Item Craft",
                NpcShopType.CostumeMix => "Costume Craft",
                NpcShopType.ShopItemVisibleFilter => "NPC Shop Visible Filter",
                NpcShopType.ItemPreview => "NPC Shop Item Preview",
                _ => "NPC Shop",
            };
        }

        private static string? GetFileNameFromShopType(int dropGroupType)
        {
            return dropGroupType switch
            {
                1 => "npcshop.rh",
                2 => "tradeshop.rh",
                3 => "itemmix.rh",
                4 => "costumemix.rh",
                5 => "shopitemvisiblefilter.rh",
                6 => "itempreview.rh",
                _ => throw new ArgumentOutOfRangeException(nameof(dropGroupType)),
            };
        }

        private static int GetShopTypeFromFileName(string fileName)
        {
            return fileName switch
            {
                "npcshop.rh" => 1,
                "tradeshop.rh" => 2,
                "itemmix.rh" => 3,
                "costumemix.rh" => 4,
                "shopitemvisiblefilter.rh" => 5,
                "itempreview.rh" => 6,
                _ => -1,
            };
        }

        private static string GetColumnName(string fileName)
        {
            return fileName switch
            {
                "npcshop.rh" => "wszNpcName",
                "tradeshop.rh" => "nTokenID00",
                "itemmix.rh" or "costumemix.rh" => "nMixAble",
                "shopitemvisiblefilter.rh" => "nQuestID00",
                "itempreview.rh" => "nPreViewItemID",
                _ => "",
            };
        }

        private void SetShopProperties(int shopType)
        {
            NpcShopType = (NpcShopType)shopType;
        }

        [RelayCommand(CanExecute = nameof(CanExecuteFileCommand))]
        private void OpenSearchDialog(string? parameter)
        {
            try
            {
                Window? shopEditorWindow = Application.Current.Windows.OfType<NPCShopEditorWindow>().FirstOrDefault();
                Window owner = shopEditorWindow ?? Application.Current.MainWindow;
                DataTableManager.OpenSearchDialog(owner, parameter, DataGridSelectionUnit.FullRow);

            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"Error: {ex.Message}", Resources.Error);
            }
            
        }

        [RelayCommand(CanExecute = nameof(CanExecuteFileCommand))]
        public async Task<bool> CloseFile()
        {
            var close = await DataTableManager.CloseFile();

            if (close)
            {
                ClearFile();
                return true;
            }

            return false;
        }

        private void ClearFile()
        {
            Title = $"NPC Shop Editor";
            OpenMessage = "Open a file";
            IsVisible = Visibility.Hidden;
            ItemMix?.Clear();
            NpcShopItem?.Clear();
            NpcShopItems?.Clear();
            OnCanExecuteFileCommandChanged();
        }

        private bool CanExecuteFileCommand()
        {
            return DataTableManager.DataTable != null;
        }

        private bool CanExecuteSelectedItemCommand()
        {
            return DataTableManager.SelectedItem != null;
        }

        private void OnCanExecuteSelectedItemCommandChanged()
        {
            AddItemCommand.NotifyCanExecuteChanged();
        }

        private void OnCanExecuteFileCommandChanged()
        {
            CloseFileCommand.NotifyCanExecuteChanged();
            OpenSearchDialogCommand.NotifyCanExecuteChanged();
            AddItemCommand.NotifyCanExecuteChanged();
            AddRowCommand.NotifyCanExecuteChanged();
        }

        #endregion

        #region Add Item

        [RelayCommand(CanExecute = nameof(CanExecuteSelectedItemCommand))]
        private void AddItem(string parameter)
        {
            try
            {
                if (int.TryParse(parameter, out int slotIndex))
                {
                    string messageType = "";

                    switch (NpcShopType)
                    {
                        case NpcShopType.NpcShop or NpcShopType.ShopItemVisibleFilter:
                            messageType = "NpcShopItem";
                            break;
                        case NpcShopType.TradeShop:
                            messageType = "TradeShopItem";
                            break;
                        case NpcShopType.ItemMix or NpcShopType.CostumeMix:
                            messageType = "ItemMixItem";
                            break;
                    }

                    var itemData = new ItemData
                    {
                        SlotIndex = slotIndex,
                        ItemId = NpcShopItem[slotIndex].ItemCode,
                        ItemAmount = NpcShopItem[slotIndex].ItemCount
                    };

                    var token = _token;

                    _windowsService.OpenItemWindow(token, messageType, itemData);
                }

            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"Error: {ex.Message}", "Error");
            }
        }

        [RelayCommand(CanExecute = nameof(CanExecuteSelectedItemCommand))]
        private void AddItems(string parameter)
        {
            try
            {
                if (int.TryParse(parameter, out int slotIndex))
                {
                    string messageType = "";

                    switch (NpcShopType)
                    {
                        case NpcShopType.NpcShop:
                            messageType = "NpcShopItems";
                            break;
                        case NpcShopType.TradeShop:
                            messageType = "TradeShopItems";
                            break;
                        case NpcShopType.ItemMix or NpcShopType.CostumeMix:
                            messageType = "ItemMixItems";
                            break;
                        case NpcShopType.ShopItemVisibleFilter:
                            messageType = "NpcShopFilterItems";
                            break;
                    }

                    var itemData = new ItemData
                    {
                        SlotIndex = slotIndex,
                        ItemId = NpcShopItems[slotIndex].ItemCode,
                        ItemAmount = NpcShopItems[slotIndex].ItemCount
                    };

                    var token = _token;

                    _windowsService.OpenItemWindow(token, messageType, itemData);
                }

            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"Error: {ex.Message}", "Error");
            }
        }

        public void Receive(ItemDataMessage message)
        {
            if (message.Token == _token && DataTableManager.SelectedItem != null)
            {
                var itemData = message.Value;

                if (message.Recipient == "NpcShopEditorWindowItem")
                {
                    UpdateItem(itemData);
                }
                else if (message.Recipient == "NpcShopEditorWindowItems")
                {
                    UpdateItems(itemData);
                }
            }
        }

        private void UpdateItem(ItemData itemData)
        {
            if (itemData.ItemId != 0 && itemData.SlotIndex < NpcShopItem.Count)
            {
                DataTableManager.StartGroupingEdits();
                var itemDataViewModel = ItemDataManager.GetItemDataViewModel(itemData.ItemId, itemData.SlotIndex, itemData.ItemAmount);
                NpcShopItem[itemData.SlotIndex].ItemCode = itemData.ItemId;
                NpcShopItem[itemData.SlotIndex].ItemCount = itemData.ItemAmount;
                NpcShopItem[itemData.SlotIndex].ItemDataViewModel = itemDataViewModel;
                OnPropertyChanged(nameof(NpcShopItem));
                DataTableManager.EndGroupingEdits();
            }
        }

        private void UpdateItems(ItemData itemData)
        {
            if (itemData.ItemId != 0 && itemData.SlotIndex < NpcShopItems.Count)
            {
                DataTableManager.StartGroupingEdits();
                var itemDataViewModel = ItemDataManager.GetItemDataViewModel(itemData.ItemId, itemData.SlotIndex, itemData.ItemAmount);
                NpcShopItems[itemData.SlotIndex].ItemCode = itemData.ItemId;
                NpcShopItems[itemData.SlotIndex].ItemCount = itemData.ItemAmount;
                NpcShopItems[itemData.SlotIndex].ItemDataViewModel = itemDataViewModel;
                OnPropertyChanged(nameof(NpcShopItems));
                DataTableManager.EndGroupingEdits();
            }
        }

        [RelayCommand(CanExecute = nameof(CanExecuteFileCommand))]
        private void AddRow()
        {
            try
            {
                DataTableManager.StartGroupingEdits();
                DataTableManager.AddNewRow();
                switch (NpcShopType)
                {
                    case NpcShopType.NpcShop:
                        UpdateSelectedItemValue("New Npc Shop", "wszEct");
                        UpdateSelectedItemValue("Npc Name", "wszNpcName");
                        break;
                    case NpcShopType.TradeShop:
                        UpdateSelectedItemValue("New Trade Item", "wszDesc");
                        break;
                    case NpcShopType.ItemMix:
                        UpdateSelectedItemValue("New Item Craft ", "wszDesc");
                        break;
                }
                
                DataTableManager.EndGroupingEdits();
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"Error: {ex.Message}", "Error");
            }
        }
        #endregion

        #region Remove Item

        [RelayCommand]
        private void RemoveItem(string parameter)
        {
            try
            {
                if (int.TryParse(parameter, out int slotIndex))
                {
                    DataTableManager.StartGroupingEdits();
                    RemoveShopItem(slotIndex);
                    DataTableManager.EndGroupingEdits();
                }
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"Error: {ex.Message}", "Error");
            }
        }

        private void RemoveShopItem(int slotIndex)
        {
            if (NpcShopItem[slotIndex].ItemCode != 0)
            {
                NpcShopItem[slotIndex].ItemCode = 0;
                NpcShopItem[slotIndex].ItemCount = 0;
                NpcShopItem[slotIndex].ItemDataViewModel = null;
                OnPropertyChanged(nameof(NpcShopItem));
            }
        }

        [RelayCommand]
        private void RemoveItems(string parameter)
        {
            try
            {
                if (int.TryParse(parameter, out int slotIndex))
                {
                    DataTableManager.StartGroupingEdits();
                    RemoveShopItems(slotIndex);
                    DataTableManager.EndGroupingEdits();
                }
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"Error: {ex.Message}", "Error");
            }
        }

        private void RemoveShopItems(int slotIndex)
        {
            if (NpcShopItems[slotIndex].ItemCode != 0)
            {
                NpcShopItems[slotIndex].ItemCode = 0;
                NpcShopItems[slotIndex].ItemCount = 0;
                NpcShopItems[slotIndex].ItemDataViewModel = null;
                OnPropertyChanged(nameof(NpcShopItems));
            }
        }
        #endregion

        #endregion

        #region DataRowViewMessage
        public void Receive(DataRowViewMessage message)
        {
            if (message.Token == _token)
            {
                var selectedItem = message.Value;

                UpdateSelectedItem(selectedItem);
            }
        }

        private void UpdateSelectedItem(DataRowView? selectedItem)
        {
            _isUpdatingSelectedItem = true;

            if (selectedItem != null)
            {
                switch (NpcShopType)
                {
                    case NpcShopType.NpcShop:
                        UpdateNpcShop(selectedItem);
                        break;
                    case NpcShopType.TradeShop:
                        UpdateTradeShop(selectedItem);
                        break;
                    case NpcShopType.ItemMix:
                    case NpcShopType.CostumeMix:
                        UpdateItemMix(selectedItem);
                        break;
                    case NpcShopType.ShopItemVisibleFilter:
                        UpdateShopItemVisibleFilter(selectedItem);
                        break;
                    case NpcShopType.ItemPreview:
                        UpdateItemPreview(selectedItem);
                        break;
                }

                IsSelectedItemVisible = Visibility.Visible;
            }
            else
            {
                IsSelectedItemVisible = Visibility.Hidden;
            }

            _isUpdatingSelectedItem = false;
            OnCanExecuteSelectedItemCommandChanged();
        }

        #region NpcShop

        private void UpdateNpcShop(DataRowView selectedItem)
        {
            NpcShopItems ??= [];

            for (int i = 0; i < 20; i++)
            {
                int itemCode = (int)selectedItem[$"nItem{i:00}"];
                var itemDataViewModel = ItemDataManager.GetItemDataViewModel(itemCode, i, 1);

                if (i < NpcShopItems.Count)
                {
                    var existingItem = NpcShopItems[i];

                    existingItem.ItemCode = itemCode;
                    existingItem.ItemDataViewModel = itemDataViewModel;
                }
                else
                {
                    var item = new NPCShopItem
                    {
                        ItemCode = itemCode,
                        ItemDataViewModel = itemDataViewModel
                    };

                    NpcShopItems.Add(item);
                    NpcShopItemPropertyChanged(item, i);
                }
            }
        }

        private void NpcShopItemPropertyChanged(NPCShopItem item, int index)
        {
            item.PropertyChanged += (s, e) =>
            {
                if (s is NPCShopItem shopItem)
                {
                    UpdateSelectedItemValue(shopItem.ItemCode, $"nItem{index:00}");
                }
            };
        }
        #endregion

        #region TradeShop

        private void UpdateTradeShop(DataRowView selectedItem)
        {
            NpcShopItem ??= [];

            for (int i = 0; i < 1; i++)
            {
                var itemCode = (int)selectedItem[$"nItemID"];
                var itemCount = (int)selectedItem["nItemCount"];
                var itemDataViewModel = ItemDataManager.GetItemDataViewModel(itemCode, 0, itemCount);

                if (i < NpcShopItem.Count)
                {
                    var existingItem = NpcShopItem[i];

                    existingItem.ItemCode = itemCode;
                    existingItem.ItemCount = itemCount;
                    existingItem.ItemDataViewModel = itemDataViewModel;
                }
                else
                {
                    var tradeItem = new NPCShopItem
                    {
                        ItemCode = itemCode,
                        ItemCount = itemCount,
                        ItemDataViewModel = itemDataViewModel
                    };

                    NpcShopItem.Add(tradeItem);
                    TradeShopItemPropertyChanged(tradeItem);
                }
            }

            NpcShopItems ??= [];

            for (int i = 0; i < 5; i++)
            {
                var itemCode = (int)selectedItem[$"nTokenID{i:00}"];
                var itemCount = (int)selectedItem[$"nTokenCount{i:00}"];
                var itemDataViewModel = ItemDataManager.GetItemDataViewModel(itemCode, 0, itemCount);

                if (i < NpcShopItems.Count)
                {
                    var existingItem = NpcShopItems[i];

                    existingItem.ItemCode = itemCode;
                    existingItem.ItemCount = itemCount;
                    existingItem.ItemDataViewModel = itemDataViewModel;
                }
                else
                {
                    var tokenItem = new NPCShopItem
                    {
                        ItemCode = itemCode,
                        ItemCount = itemCount,
                        ItemDataViewModel = itemDataViewModel
                    };

                    NpcShopItems.Add(tokenItem);
                    TradeShopTokenItemPropertyChanged(tokenItem, i);
                }
            }
        }

        private void TradeShopItemPropertyChanged(NPCShopItem item)
        {
            item.PropertyChanged += (s, e) =>
            {
                if (s is NPCShopItem tradeItem)
                {
                    UpdateSelectedItemValue(tradeItem.ItemCode, $"nItemID");
                    UpdateSelectedItemValue(tradeItem.ItemCount, $"nItemCount");
                }
            };
        }

        private void TradeShopTokenItemPropertyChanged(NPCShopItem item, int index)
        {
            item.PropertyChanged += (s, e) =>
            {
                if (s is NPCShopItem tokenItem)
                {
                    UpdateSelectedItemValue(tokenItem.ItemCode, $"nTokenID{index:00}");
                    UpdateSelectedItemValue(tokenItem.ItemCount, $"nTokenCount{index:00}");
                }
            };
        }
        #endregion

        #region ItemMix

        private void UpdateItemMix(DataRowView selectedItem)
        {
            CraftItemID = (int)selectedItem["nID"];
            
            NpcShopItem ??= [];

            for (int i = 0; i < 1; i++)
            {
                var itemCode = (int)selectedItem[$"nID"];
                var itemDataViewModel = ItemDataManager.GetItemDataViewModel(itemCode, 0, 1);

                if (i < NpcShopItem.Count)
                {
                    var existingItem = NpcShopItem[i];

                    existingItem.ItemCode = itemCode;
                    existingItem.ItemDataViewModel = itemDataViewModel;
                }
                else
                {
                    var craftItem = new NPCShopItem
                    {
                        ItemCode = itemCode,
                        ItemDataViewModel = ItemDataManager.GetItemDataViewModel(itemCode, 0, 1)
                    };

                    NpcShopItem.Add(craftItem);
                    ItemMixItemPropertyChanged(craftItem);
                }
            }

            ItemMix ??= [];

            for (int i = 0; i < 3; i++)
            {
                var itemMixPro = (float)selectedItem[$"fItemMixPro{i:00}"];
                var itemMixCo = (int)selectedItem[$"nItemMixCo{i:00}"];

                if (i < ItemMix.Count)
                {
                    var existingItem = ItemMix[i];

                    existingItem.ItemMixPro = itemMixPro;
                    existingItem.ItemMixCo = itemMixCo;
                }
                else
                {
                    var item = new NPCShopItem
                    {
                        ItemMixPro = itemMixPro,
                        ItemMixCo = itemMixCo
                    };

                    ItemMix.Add(item);
                    ItemMixPropertyChanged(item, i);
                }
            }

            NpcShopItems ??= [];

            for (int i = 0; i < 5; i++)
            {
                var itemCode = (int)selectedItem[$"nItemCode{i:00}"];
                var itemCount = (int)selectedItem[$"nItemCount{i:00}"];
                var itemDataViewModel = ItemDataManager.GetItemDataViewModel(itemCode, 0, itemCount);

                if (i < NpcShopItems.Count)
                {
                    var existingItem = NpcShopItems[i];

                    existingItem.ItemCode = itemCode;
                    existingItem.ItemCount = itemCount;
                    existingItem.ItemDataViewModel = itemDataViewModel;
                }
                else
                {
                    var itemMaterial = new NPCShopItem
                    {
                        ItemCode = itemCode,
                        ItemCount = itemCount,
                        ItemDataViewModel = itemDataViewModel
                    };

                    NpcShopItems.Add(itemMaterial);
                    ItemMixItemsPropertyChanged(itemMaterial, i);
                }
            }
        }

        private void ItemMixPropertyChanged(NPCShopItem item, int index)
        {
            item.PropertyChanged += (s, e) =>
            {
                if (s is NPCShopItem shopItem)
                {
                    UpdateSelectedItemValue(shopItem.ItemMixPro, $"fItemMixPro{index:00}");
                    UpdateSelectedItemValue(shopItem.ItemMixCo, $"nItemMixCo{index:00}");
                }
            };
        }

        private void ItemMixItemPropertyChanged(NPCShopItem item)
        {
            item.PropertyChanged += (s, e) =>
            {
                if (s is NPCShopItem shopItem)
                {
                    CraftItemID = shopItem.ItemCode;
                }
            };
        }

        private void ItemMixItemsPropertyChanged(NPCShopItem item, int index)
        {
            item.PropertyChanged += (s, e) =>
            {
                if (s is NPCShopItem itemMixItem)
                {
                    UpdateSelectedItemValue(itemMixItem.ItemCode, $"nItemCode{index:00}");
                    UpdateSelectedItemValue(itemMixItem.ItemCount, $"nItemCount{index:00}");
                }
            };
        }
        #endregion

        #region ShopItemVisibleFilter

        private void UpdateShopItemVisibleFilter(DataRowView selectedItem)
        {
            ItemID = (int)selectedItem["nID"];

            NpcShopItem ??= [];

            for (int i = 0; i < 1; i++)
            {
                var itemCode = (int)selectedItem[$"nID"];
                var itemDataViewModel = ItemDataManager.GetItemDataViewModel(itemCode, 0, 1);

                if (i < NpcShopItem.Count)
                {
                    var existingItem = NpcShopItem[i];

                    existingItem.ItemCode = itemCode;
                    existingItem.ItemDataViewModel = itemDataViewModel;
                }
                else
                {
                    var shopItem = new NPCShopItem
                    {
                        ItemCode = itemCode,
                        ItemDataViewModel = ItemDataManager.GetItemDataViewModel(itemCode, 0, 1)
                    };

                    NpcShopItem.Add(shopItem);
                    ShopItemPropertyChanged(shopItem);
                }
            }

            QuestItems ??= [];

            for (int i = 0; i < 3; i++)
            {
                var questID = (int)selectedItem[$"nQuestID{i:00}"];
                var questCondition = (int)selectedItem[$"nQuestCondition{i:00}"];

                if (i < QuestItems.Count)
                {
                    var existingItem = QuestItems[i];

                    existingItem.QuestID = questID;
                    existingItem.QuestCondition = questCondition;
                }
                else
                {
                    var item = new NPCShopItem
                    {
                        QuestID = (int)selectedItem[$"nQuestID{i:00}"],
                        QuestCondition = (int)selectedItem[$"nQuestCondition{i:00}"]
                    };

                    QuestItems.Add(item);
                    ShopItemFilterQuestPropertyChanged(item, i);
                }
            }

            NpcShopItems ??= [];

            for (int i = 0; i < 3; i++)
            {
                var itemCode = (int)selectedItem[$"nItemID{i:00}"];
                var itemCount = (int)selectedItem[$"nItemCount{i:00}"];
                var itemDataViewModel = ItemDataManager.GetItemDataViewModel(itemCode, 0, itemCount);

                if (i < NpcShopItems.Count)
                {
                    var existingItem = NpcShopItems[i];

                    existingItem.ItemCode = itemCode;
                    existingItem.ItemCount = itemCount;
                    existingItem.ItemDataViewModel = itemDataViewModel;
                }
                else
                {
                    var item = new NPCShopItem
                    {
                        ItemCode = itemCode,
                        ItemCount = itemCount,
                        ItemDataViewModel = itemDataViewModel
                    };

                    NpcShopItems.Add(item);
                    ShopItemFilterPropertyChanged(item, i);
                }
            }
        }

        private void ShopItemPropertyChanged(NPCShopItem item)
        {
            item.PropertyChanged += (s, e) =>
            {
                if (s is NPCShopItem shopItem)
                {
                    ItemID = shopItem.ItemCode;
                }
            };
        }

        private void ShopItemFilterQuestPropertyChanged(NPCShopItem item, int index)
        {
            item.PropertyChanged += (s, e) =>
            {
                if (s is NPCShopItem quest)
                {
                    UpdateSelectedItemValue(quest.QuestID, $"nQuestID{index:00}");
                    UpdateSelectedItemValue(quest.QuestCondition, $"nQuestCondition{index:00}");
                }
            };
        }

        private void ShopItemFilterPropertyChanged(NPCShopItem item, int index)
        {
            item.PropertyChanged += (s, e) =>
            {
                if (s is NPCShopItem item)
                {
                    UpdateSelectedItemValue(item.ItemCode, $"nItemID{index:00}");
                    UpdateSelectedItemValue(item.ItemCount, $"nItemCount{index:00}");
                }
            };
        }
        #endregion

        #region ItemPreview

        private void UpdateItemPreview(DataRowView selectedItem)
        {
            PreviewItemID = (int)selectedItem["nPreViewItemID"];

            NpcShopItem ??= [];

            for (int i = 0; i < 1; i++)
            {
                var itemCode = (int)selectedItem[$"nPreViewItemID"];
                var itemDataViewModel = ItemDataManager.GetItemDataViewModel(itemCode, 0, 1);

                if (i < NpcShopItem.Count)
                {
                    var existingItem = NpcShopItem[i];

                    existingItem.ItemCode = itemCode;
                    existingItem.ItemDataViewModel = itemDataViewModel;
                }
                else
                {
                    var item = new NPCShopItem
                    {
                        ItemCode = itemCode,
                        ItemDataViewModel = itemDataViewModel
                    };

                    NpcShopItem.Add(item);
                    ItemPreviewPropertyChanged(item);
                }
            }

        }

        private void ItemPreviewPropertyChanged(NPCShopItem item)
        {
            item.PropertyChanged += (s, e) =>
            {
                if (s is NPCShopItem item)
                {
                    PreviewItemID = item.ItemCode;
                }
            };
        }

        #endregion

        #endregion

        #region Filter

        private void ApplyFilter()
        {
            List<string> filterParts = [];
            List<string> columns = [];

            columns.Add("CONVERT(nID, 'System.String')");

            switch (NpcShopType)
            {
                case NpcShopType.NpcShop:
                    columns.Add("wszNpcName");

                    for (int i = 0; i < 20; i++)
                    {
                        string columnName = $"nItem{i:00}";
                        columns.Add($"CONVERT({columnName}, 'System.String')");
                    }
                    break;
                case NpcShopType.TradeShop:
                    columns.Add("wszDesc");
                    columns.Add("CONVERT(nItemID, 'System.String')");

                    for (int i = 0; i < 5; i++)
                    {
                        string columnName = $"nTokenID{i:00}";
                        columns.Add($"CONVERT({columnName}, 'System.String')");
                    }
                    break;
                case NpcShopType.ItemMix:
                    columns.Add("wszDesc");
                    columns.Add($"CONVERT(szMixCategory, 'System.String')");
                    columns.Add($"CONVERT(nMixSubCategory, 'System.String')");
                    for (int i = 0; i < 5; i++)
                    {
                        string columnName = $"nItemCode{i:00}";
                        columns.Add($"CONVERT({columnName}, 'System.String')");
                    }
                    break;
                case NpcShopType.CostumeMix:
                    columns.Add("wszDesc");
                    for (int i = 0; i < 5; i++)
                    {
                        string columnName = $"nItemCode{i:00}";
                        columns.Add($"CONVERT({columnName}, 'System.String')");
                    }
                    break;
                case NpcShopType.ShopItemVisibleFilter:
                    columns.Add($"CONVERT(nLevel, 'System.String')");
                    break;
                case NpcShopType.ItemPreview:
                    columns.Add($"CONVERT(nPreViewItemID, 'System.String')");
                    break;
            }

            if (columns.Count > 0)
            {
                DataTableManager.ApplyFileDataFilter(filterParts, [.. columns], SearchText, MatchCase);
            }
        }

        private void TriggerFilterUpdate()
        {
            _filterUpdateTimer.Stop();
            _filterUpdateTimer.Start();
        }

        private void FilterUpdateTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                _filterUpdateTimer.Stop();
                ApplyFilter();
            });
        }

        [ObservableProperty]
        private string? _searchText;
        partial void OnSearchTextChanged(string? value)
        {
            TriggerFilterUpdate();
        }

        [ObservableProperty]
        private bool _matchCase = false;
        partial void OnMatchCaseChanged(bool value)
        {
            ApplyFilter();
        }
        #endregion

        #region Properties
        [ObservableProperty]
        private string _title = $"NPC Shop Editor";

        [ObservableProperty]
        private string? _openMessage = "Open a file";

        [ObservableProperty]
        private Visibility _isSelectedItemVisible = Visibility.Hidden;

        [ObservableProperty]
        private Visibility _isVisible = Visibility.Hidden;

        [ObservableProperty]
        private DataTableManager _dataTableManager;
        partial void OnDataTableManagerChanged(DataTableManager value)
        {
            OnCanExecuteFileCommandChanged();
        }

        [ObservableProperty]
        private ItemDataManager _itemDataManager;

        #region SelectedItem

        [ObservableProperty]
        private ObservableCollection<NPCShopItem> _npcShopItem = [];

        [ObservableProperty]
        private ObservableCollection<NPCShopItem> _npcShopItems = [];

        [ObservableProperty]
        private NpcShopType _npcShopType;

        #region ItemMix

        [ObservableProperty]
        private ObservableCollection<NPCShopItem> _itemMix = [];

        [ObservableProperty]
        private int _craftItemID;
        partial void OnCraftItemIDChanged(int value)
        {
            if (_isUpdatingSelectedItem)
                return;

            UpdateSelectedItemValue(value, "nID");
            var itemDataViewModel = ItemDataManager.GetItemDataViewModel(value, 0, 1);
            
            NpcShopItem[0].ItemCode = value;
            NpcShopItem[0].ItemDataViewModel = itemDataViewModel;

            if (itemDataViewModel != null)
            {
                UpdateSelectedItemValue(itemDataViewModel.ItemName, "wszDesc");
            }
            else
            {
                UpdateSelectedItemValue(string.Empty, "wszDesc");
            }
        }
        #endregion

        #region ShopItemVisibleFilter

        [ObservableProperty]
        private ObservableCollection<NPCShopItem> _questItems = [];

        [ObservableProperty]
        private int _itemID;
        partial void OnItemIDChanged(int value)
        {
            if (_isUpdatingSelectedItem)
                return;

            UpdateSelectedItemValue(value, "nID");
            var itemDataViewModel = ItemDataManager.GetItemDataViewModel(value, 0, 1);

            NpcShopItem[0].ItemCode = value;
            NpcShopItem[0].ItemDataViewModel = itemDataViewModel;

            if (itemDataViewModel != null)
            {
                UpdateSelectedItemValue(itemDataViewModel.ItemName, "wszItemName");
            }
            else
            {
                UpdateSelectedItemValue(string.Empty, "wszItemName");
            }
        }
        #endregion

        #region ItemPreview
        [ObservableProperty]
        private int _previewItemID;
        partial void OnPreviewItemIDChanged(int value)
        {
            if (_isUpdatingSelectedItem)
                return;

            UpdateSelectedItemValue(value, "nPreViewItemID");
            var itemDataViewModel = ItemDataManager.GetItemDataViewModel(value, 0, 1);

            NpcShopItem[0].ItemCode = value;
            NpcShopItem[0].ItemDataViewModel = itemDataViewModel;

            if (itemDataViewModel != null)
            {
                UpdateSelectedItemValue(itemDataViewModel.ItemName, "wszNote00");
            }
            else
            {
                UpdateSelectedItemValue(string.Empty, "wszNote00");
            }
        }

        #endregion

        #endregion

        #endregion

        #region Properties Helper

        private bool _isUpdatingSelectedItem = false;

        private void UpdateSelectedItemValue(object? newValue, string column)
        {
            if (_isUpdatingSelectedItem)
                return;
            DataTableManager.UpdateSelectedItemValue(newValue, column);
        }
        #endregion
    }
}
