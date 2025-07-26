using Microsoft.Win32;
using RHToolkit.Messages;
using RHToolkit.Models;
using RHToolkit.Models.Database;
using RHToolkit.Models.Editor;
using RHToolkit.Models.MessageBox;
using RHToolkit.Services;
using RHToolkit.ViewModels.Controls;
using RHToolkit.ViewModels.Windows.Tools.VM;
using RHToolkit.Views.Windows;
using System.ComponentModel;
using System.Data;
using System.Windows.Controls;
using static RHToolkit.Models.EnumService;

namespace RHToolkit.ViewModels.Windows
{
    public partial class DropGroupEditorViewModel : ObservableObject, IRecipient<ItemDataMessage>, IRecipient<DataRowViewMessage>
    {
        private readonly Guid _token;
        private readonly IWindowsService _windowsService;
        private readonly IFrameService _frameService;
        private readonly System.Timers.Timer _filterUpdateTimer;

        public DropGroupEditorViewModel(IWindowsService windowsService, IFrameService frameService, ItemDataManager itemDataManager)
        {
            _token = Guid.NewGuid();
            _windowsService = windowsService;
            _frameService = frameService;
            _itemDataManager = itemDataManager;
            DataTableManager = new()
            {
                Token = _token
            };
            _filterUpdateTimer = new()
            {
                Interval = 500,
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
                    string? fileName = GetFileNameFromDropGroupType(dropGroupType);
                    if (fileName == null) return;

                    string columnName = GetColumnName(fileName);
                    SetDropGroupProperties(dropGroupType);

                    bool isLoaded = await DataTableManager.LoadFileFromPath(fileName, null, columnName, "DropGroup");

                    if (isLoaded)
                    {
                        IsLoaded();
                    }
                }
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
            }
        }

        [RelayCommand]
        private async Task LoadFileAs()
        {
            try
            {
                await CloseFile();

                string filter = "Item Drop Group Files .rh|" +
                                "itemdropgrouplist_f.rh;itemdropgrouplist.rh;championitemdropgrouplist.rh;" +
                                "eventworlditemdropgrouplist.rh;instanceitemdropgrouplist.rh;" +
                                "questitemdropgrouplist.rh;worldinstanceitemdropgrouplist.rh;" +
                                "worlditemdropgrouplist.rh;worlditemdropgrouplist_fatigue.rh;" +
                                "riddleboxdropgrouplist.rh;rarecarddropgrouplist.rh;rarecardrewarditemlist.rh|" +
                                "All Files (*.*)|*.*";

                OpenFileDialog openFileDialog = new()
                {
                    Filter = filter,
                    FilterIndex = 1
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    string fileName = Path.GetFileName(openFileDialog.FileName);
                    int dropGroupType = GetDropGroupTypeFromFileName(fileName);

                    if (dropGroupType == -1)
                    {
                        string message = string.Format(Resources.InvalidTableFileDesc, fileName, "Drop Group");
                        RHMessageBoxHelper.ShowOKMessage(message, Resources.Error);
                        return;
                    }

                    string columnName = GetColumnName(fileName);
                    SetDropGroupProperties(dropGroupType);

                    bool isLoaded = await DataTableManager.LoadFileAs(openFileDialog.FileName, null, columnName, "DropGroup");

                    if (isLoaded)
                    {
                        IsLoaded();
                    }
                }
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
            }
        }

        [RelayCommand]
        private async Task LoadFileFromPCK(string? parameter)
        {
            try
            {
                await CloseFile();

                if (int.TryParse(parameter, out int dropGroupType))
                {
                    string? fileName = GetFileNameFromDropGroupType(dropGroupType);
                    if (fileName == null) return;

                    string columnName = GetColumnName(fileName);
                    SetDropGroupProperties(dropGroupType);

                    bool isLoaded = await DataTableManager.LoadFileFromPCK(fileName);

                    if (isLoaded)
                    {
                        IsLoaded();
                    }
                }
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
            }
        }

        private void IsLoaded()
        {
            Title = string.Format(Resources.EditorTitleFileName, "Drop Group", DataTableManager.CurrentFileName);
            OpenMessage = "";
            IsVisible = Visibility.Visible;
            OnCanExecuteFileCommandChanged();
        }

        private static string? GetFileNameFromDropGroupType(int dropGroupType)
        {
            return dropGroupType switch
            {
                1 => "itemdropgrouplist_f.rh",
                2 => "itemdropgrouplist.rh",
                3 => "championitemdropgrouplist.rh",
                4 => "eventworlditemdropgrouplist.rh",
                5 => "instanceitemdropgrouplist.rh",
                6 => "questitemdropgrouplist.rh",
                7 => "worldinstanceitemdropgrouplist.rh",
                8 => "worlditemdropgrouplist.rh",
                9 => "worlditemdropgrouplist_fatigue.rh",
                10 => "riddleboxdropgrouplist.rh",
                11 => "rarecarddropgrouplist.rh",
                12 => "rarecardrewarditemlist.rh",
                _ => throw new ArgumentOutOfRangeException(nameof(dropGroupType)),
            };
        }

        private static int GetDropGroupTypeFromFileName(string fileName)
        {
            return fileName switch
            {
                "itemdropgrouplist_f.rh" => 1,
                "itemdropgrouplist.rh" => 2,
                "championitemdropgrouplist.rh" => 3,
                "eventworlditemdropgrouplist.rh" => 4,
                "instanceitemdropgrouplist.rh" => 5,
                "questitemdropgrouplist.rh" => 6,
                "worldinstanceitemdropgrouplist.rh" => 7,
                "worlditemdropgrouplist.rh" => 8,
                "worlditemdropgrouplist_fatigue.rh" => 9,
                "riddleboxdropgrouplist.rh" => 10,
                "rarecarddropgrouplist.rh" => 11,
                "rarecardrewarditemlist.rh" => 12,
                _ => -1,
            };
        }

        private static string GetColumnName(string fileName)
        {
            return fileName switch
            {
                "riddleboxdropgrouplist.rh" => "nRiddleboxGroup",
                "rarecarddropgrouplist.rh" => "nBronzeCardID",
                "rarecardrewarditemlist.rh" => "nRewardItem01",
                _ => "nDropItemCode01",
            };
        }

        private void SetDropGroupProperties(int dropGroupType)
        {
            DropGroupType = (ItemDropGroupType)dropGroupType;
            DropItemCount = DropGroupType switch
            {
                ItemDropGroupType.ChampionItemItemDropGroupList or ItemDropGroupType.InstanceItemDropGroupList or ItemDropGroupType.QuestItemDropGroupList or ItemDropGroupType.WorldInstanceItemDropGroupList => 30,
                ItemDropGroupType.ItemDropGroupListF or ItemDropGroupType.ItemDropGroupList or ItemDropGroupType.RareCardRewardItemList => 40,
                ItemDropGroupType.EventWorldItemDropGroupList or ItemDropGroupType.WorldItemDropGroupList or ItemDropGroupType.WorldItemDropGroupListF => 60,
                ItemDropGroupType.RiddleBoxDropGroupList => 25,
                _ => 1,
            };
        }

        [RelayCommand(CanExecute = nameof(CanExecuteFileCommand))]
        private void OpenSearchDialog(string? parameter)
        {
            try
            {
                Window? window = Application.Current.Windows.OfType<DropGroupEditorWindow>().FirstOrDefault();
                Window owner = window ?? Application.Current.MainWindow;
                DataTableManager.OpenSearchDialog(owner, parameter, DataGridSelectionUnit.FullRow);
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
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
            Title = string.Format(Resources.EditorTitle, "Drop Group");
            OpenMessage = Resources.OpenFile;
            DropGroupType = 0;
            DropItemCountTotal = 0;
            DropItemCountTotalValue = 0;
            DropGroupItems?.Clear();
            SearchText = string.Empty;
            IsVisible = Visibility.Hidden;
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

        #region Add Row
        [RelayCommand(CanExecute = nameof(CanExecuteFileCommand))]
        private void AddRow()
        {
            try
            {
                DataTableManager.AddNewRow();
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
            }
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
                    var itemData = new ItemData
                    {
                        SlotIndex = slotIndex,
                        OverlapCnt = DropItemCount,
                        ItemId = DropGroupItems[slotIndex].DropItemCode
                    };

                    var token = _token;

                    _windowsService.OpenItemWindow(token, "DropGroup", itemData);
                }

            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
            }
        }

        [RelayCommand(CanExecute = nameof(CanExecuteSelectedItemCommand))]
        private void AddRareCardItem(string parameter)
        {
            try
            {
                if (int.TryParse(parameter, out int slotIndex))
                {
                    int rarecard = slotIndex switch
                    {
                        0 => DropGroupItems[0].BronzeCardCode,
                        1 => DropGroupItems[0].SilverCardCode,
                        2 => DropGroupItems[0].GoldCardCode,
                        _ => throw new ArgumentOutOfRangeException($"Invalid slotIndex: {slotIndex}"),
                    };
                    var itemData = new ItemData
                    {
                        SlotIndex = slotIndex,
                        ItemId = rarecard
                    };

                    var token = _token;

                    _windowsService.OpenItemWindow(token, "RareCard", itemData);
                }

            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
            }
        }

        public void Receive(ItemDataMessage message)
        {
            if (message.Recipient == "DropGroupEditorWindow" && message.Token == _token)
            {
                var itemData = message.Value;

                if (itemData.ItemId != 0)
                {
                    if (DropGroupType == ItemDropGroupType.RareCardDropGroupList)
                    {
                        UpdateRareCardItem(itemData);
                    }
                    else
                    {
                        if (DataTableManager.SelectedItem != null && itemData.SlotIndex >= 0 && itemData.SlotIndex < DropGroupItems.Count)
                        {
                            UpdateDropGroupItem(itemData);
                        }
                    }
                }
            }
        }

        private void UpdateDropGroupItem(ItemData itemData)
        {
            DataTableManager.StartGroupingEdits();
            var itemDataViewModel = ItemDataManager.GetItemDataViewModel(itemData.ItemId, itemData.SlotIndex, itemData.ItemAmount);
            DropGroupItems[itemData.SlotIndex].DropItemCode = itemData.ItemId;
            DropGroupItems[itemData.SlotIndex].ItemDataViewModel = itemDataViewModel;
            OnPropertyChanged(nameof(DropGroupItems));
            DataTableManager.EndGroupingEdits();
        }

        private void UpdateRareCardItem(ItemData itemData)
        {
            var itemDataViewModel = ItemDataManager.GetItemDataViewModel(itemData.ItemId, itemData.SlotIndex, itemData.ItemAmount);
            switch (itemData.SlotIndex)
            {
                case 0:
                    DropGroupItems[0].BronzeCardCode = itemData.ItemId;
                    DropGroupItems[0].BronzeCard = itemDataViewModel;
                    break;
                case 1:
                    DropGroupItems[0].SilverCardCode = itemData.ItemId;
                    DropGroupItems[0].SilverCard = itemDataViewModel;
                    break;
                case 2:
                    DropGroupItems[0].GoldCardCode = itemData.ItemId;
                    DropGroupItems[0].GoldCard = itemDataViewModel;
                    break;
            }

            OnPropertyChanged(nameof(DropGroupItems));
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
                    RemoveDropGroupItem(slotIndex);
                }
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
            }
            
        }

        private void RemoveDropGroupItem(int slotIndex)
        {
            if (slotIndex >= 0 && slotIndex < DropGroupItems.Count)
            {
                DataTableManager.StartGroupingEdits();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    var item = DropGroupItems[slotIndex];
                    if (item != null)
                    {
                        item.ItemDataViewModel = null;
                        item.DropItemCode = 0;
                        item.FDropItemCount = 0;
                        item.NDropItemCount = 0;
                    }
                });
                
                DataTableManager.EndGroupingEdits();
            }
        }

        [RelayCommand]
        private void RemoveRareCardItem(string parameter)
        {
            try
            {
                if (int.TryParse(parameter, out int slotIndex))
                {
                    switch (slotIndex)
                    {
                        case 0:
                            DropGroupItems[0].BronzeCardCode = 0;
                            DropGroupItems[0].BronzeCard = null;
                            break;
                        case 1:
                            DropGroupItems[0].SilverCardCode = 0;
                            DropGroupItems[0].SilverCard = null;
                            break;
                        case 2:
                            DropGroupItems[0].GoldCardCode = 0;
                            DropGroupItems[0].GoldCard = null;
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
            }

        }

        #endregion

        #region Open RareCardRewardItemList

        [RelayCommand]
        private void OpenRareCardRewardItemList(int parameter)
        {
            try
            {
                if (parameter != 0)
                {
                    _windowsService.OpenRareCardRewardWindow(_token, parameter, null);
                }
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
            }
        }
        #endregion

        #endregion

        #region Filter
        private void ApplyFilter()
        {
            List<string> filterParts = [];

            List<string> columns = [];

            columns.Add("CONVERT(nID, 'System.String')");

            switch (DropGroupType)
            {
                case ItemDropGroupType.RiddleBoxDropGroupList:
                    for (int i = 0; i < DropItemCount; i++)
                    {
                        string columnName = $"nItem{i:00}";
                        columns.Add($"CONVERT({columnName}, 'System.String')");
                    }
                    break;
                case ItemDropGroupType.RareCardDropGroupList:
                    columns.Add("CONVERT(nBronzeCardID, 'System.String')");
                    columns.Add("CONVERT(nSilverCardID, 'System.String')");
                    columns.Add("CONVERT(nGoldCardID, 'System.String')");
                    break;
                case ItemDropGroupType.RareCardRewardItemList:
                    for (int i = 0; i < DropItemCount; i++)
                    {
                        string columnName = $"nRewardItem{i + 1:00}";
                        columns.Add($"CONVERT({columnName}, 'System.String')");
                    }
                    break;
                default:
                    for (int i = 0; i < DropItemCount; i++)
                    {
                        string columnName = $"nDropItemCode{i + 1:00}";
                        columns.Add($"CONVERT({columnName}, 'System.String')");
                    }
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

        #region DataRowViewMessage
        public void Receive(DataRowViewMessage message)
        {
            if (message.Token == _token)
            {
                var selectedItem = message.Value;

                switch (DropGroupType)
                {
                    case ItemDropGroupType.RiddleBoxDropGroupList:
                        UpdateRiddleDropGroupSelectedItem(selectedItem);
                        break;
                    case ItemDropGroupType.RareCardDropGroupList:
                        UpdateRareCardDropGroupSelectedItem(selectedItem);
                        break;
                    case ItemDropGroupType.RareCardRewardItemList:
                        UpdateRareCardRewardItemSelectedItem(selectedItem);
                        break;
                    default:
                        UpdateDropGroupSelectedItem(selectedItem);
                        break;
                }

            }
        }

        #region DropGroup
        private void UpdateDropGroupSelectedItem(DataRowView? selectedItem)
        {
            _isUpdatingSelectedItem = true;

            if (selectedItem != null)
            {
                DropGroupItems ??= [];

                for (int i = 0; i < DropItemCount; i++)
                {
                    int dropItemCode = (int)selectedItem[$"nDropItemCode{i + 1:00}"];
                    ItemDataViewModel? itemDataViewModel = null;

                    // Check if the item at this index already exists
                    if (i < DropGroupItems.Count)
                    {
                        var existingItem = DropGroupItems[i];

                        // If the DropItemCode is different, create a new ItemDataViewModel
                        if (existingItem.DropItemCode != dropItemCode)
                        {
                            itemDataViewModel = ItemDataManager.GetItemDataViewModel(dropItemCode, i + 1, 1);
                            existingItem.ItemDataViewModel = itemDataViewModel;
                        }

                        existingItem.DropItemGroupType = DropGroupType;
                        existingItem.DropItemCode = dropItemCode;
                        existingItem.FDropItemCount = selectedItem.Row.Table.Columns.Contains($"fDropItemCount{i + 1:00}")
                            ? (float)selectedItem[$"fDropItemCount{i + 1:00}"] : 0;
                        existingItem.NDropItemCount = selectedItem.Row.Table.Columns.Contains($"nDropItemCount{i + 1:00}")
                            ? (int)selectedItem[$"nDropItemCount{i + 1:00}"] : 0;
                        existingItem.Link = selectedItem.Row.Table.Columns.Contains($"nDropItemLink{i + 1:00}")
                            ? (int)selectedItem[$"nDropItemLink{i + 1:00}"] : 0;
                        existingItem.Start = selectedItem.Row.Table.Columns.Contains($"nStart{i + 1:00}")
                            ? (int)selectedItem[$"nStart{i + 1:00}"] : 0;
                        existingItem.End = selectedItem.Row.Table.Columns.Contains($"nEnd{i + 1:00}")
                            ? (int)selectedItem[$"nEnd{i + 1:00}"] : 0;

                        ItemPropertyChanged(existingItem);
                    }
                    else
                    {
                        // If not enough items exist, add new ones
                        itemDataViewModel = ItemDataManager.GetItemDataViewModel(dropItemCode, i + 1, 1);

                        var newItem = new ItemDropGroup
                        {
                            DropItemGroupType = DropGroupType,
                            DropItemCode = dropItemCode,
                            FDropItemCount = selectedItem.Row.Table.Columns.Contains($"fDropItemCount{i + 1:00}")
                                ? (float)selectedItem[$"fDropItemCount{i + 1:00}"] : 0,
                            NDropItemCount = selectedItem.Row.Table.Columns.Contains($"nDropItemCount{i + 1:00}")
                                ? (int)selectedItem[$"nDropItemCount{i + 1:00}"] : 0,
                            Link = selectedItem.Row.Table.Columns.Contains($"nDropItemLink{i + 1:00}")
                                ? (int)selectedItem[$"nDropItemLink{i + 1:00}"] : 0,
                            Start = selectedItem.Row.Table.Columns.Contains($"nStart{i + 1:00}")
                                ? (int)selectedItem[$"nStart{i + 1:00}"] : 0,
                            End = selectedItem.Row.Table.Columns.Contains($"nEnd{i + 1:00}")
                                ? (int)selectedItem[$"nEnd{i + 1:00}"] : 0,
                            ItemDataViewModel = itemDataViewModel
                        };

                        Application.Current.Dispatcher.Invoke(() => DropGroupItems.Add(newItem));
                        ItemPropertyChanged(newItem);
                    }
                }

                CalculateDropItemCountTotal();
                IsSelectedItemVisible = Visibility.Visible;
            }
            else
            {
                IsSelectedItemVisible = Visibility.Hidden;
            }

            _isUpdatingSelectedItem = false;
            OnCanExecuteSelectedItemCommandChanged();
        }

        private void ItemPropertyChanged(ItemDropGroup item)
        {
            item.PropertyChanged -= OnItemPropertyChanged;

            item.PropertyChanged += OnItemPropertyChanged;
        }

        private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is ItemDropGroup itemDropGroup)
            {
                int index = DropGroupItems.IndexOf(itemDropGroup);
                UpdateSelectedItemValue(itemDropGroup.DropItemCode, $"nDropItemCode{index + 1:00}");
                UpdateSelectedItemValue(itemDropGroup.FDropItemCount, $"fDropItemCount{index + 1:00}");
                UpdateSelectedItemValue(itemDropGroup.NDropItemCount, $"nDropItemCount{index + 1:00}");
                UpdateSelectedItemValue(itemDropGroup.Link, $"nDropItemLink{index + 1:00}");
                UpdateSelectedItemValue(itemDropGroup.Start, $"nStart{index + 1:00}");
                UpdateSelectedItemValue(itemDropGroup.End, $"nEnd{index + 1:00}");

                CalculateDropItemCountTotal();
            }
        }

        #endregion

        #region RiddleBoxDropGroup
        private void UpdateRiddleDropGroupSelectedItem(DataRowView? selectedItem)
        {
            _isUpdatingSelectedItem = true;

            if (selectedItem != null)
            {
                DropGroupItems ??= [];

                // Update existing items or add new ones
                for (int i = 0; i < DropItemCount; i++)
                {
                    int dropItemCode = (int)selectedItem[$"nItem{i:00}"];
                    ItemDataViewModel? itemDataViewModel = null;

                    // Check if the item at this index already exists
                    if (i < DropGroupItems.Count)
                    {
                        var existingItem = DropGroupItems[i];

                        // If the DropItemCode is different, create a new ItemDataViewModel
                        if (existingItem.DropItemCode != dropItemCode)
                        {
                            itemDataViewModel = ItemDataManager.GetItemDataViewModel(dropItemCode, i, 1);
                            existingItem.ItemDataViewModel = itemDataViewModel;
                        }

                        existingItem.DropItemGroupType = DropGroupType;
                        existingItem.DropItemCode = dropItemCode;
                        existingItem.SectionStart00 = (int)selectedItem[$"nSectionStart00"];
                        existingItem.SectionEnd00 = (int)selectedItem[$"nSectionEnd00"];
                        existingItem.Probability00 = (int)selectedItem[$"nProbability00"];
                        existingItem.SectionStart01 = (int)selectedItem[$"nSectionStart01"];
                        existingItem.SectionEnd01 = (int)selectedItem[$"nSectionEnd01"];
                        existingItem.Probability01 = (int)selectedItem[$"nProbability01"];
                        existingItem.SectionStart02 = (int)selectedItem[$"nSectionStart02"];
                        existingItem.SectionEnd02 = (int)selectedItem[$"nSectionEnd02"];
                        existingItem.Probability02 = (int)selectedItem[$"nProbability02"];
                        existingItem.SectionStart03 = (int)selectedItem[$"nSectionStart03"];
                        existingItem.SectionEnd03 = (int)selectedItem[$"nSectionEnd03"];
                        existingItem.Probability03 = (int)selectedItem[$"nProbability03"];

                        RiddleDropItemPropertyChanged(existingItem);
                    }
                    else
                    {
                        itemDataViewModel = ItemDataManager.GetItemDataViewModel(dropItemCode, i , 1);

                        var newItem = new ItemDropGroup
                        {
                            DropItemGroupType = DropGroupType,
                            DropItemCode = dropItemCode,
                            ItemDataViewModel = itemDataViewModel,
                            SectionStart00 = (int)selectedItem[$"nSectionStart00"],
                            SectionEnd00 = (int)selectedItem[$"nSectionEnd00"],
                            Probability00 = (int)selectedItem[$"nProbability00"],
                            SectionStart01 = (int)selectedItem[$"nSectionStart01"],
                            SectionEnd01 = (int)selectedItem[$"nSectionEnd01"],
                            Probability01 = (int)selectedItem[$"nProbability01"],
                            SectionStart02 = (int)selectedItem[$"nSectionStart02"],
                            SectionEnd02 = (int)selectedItem[$"nSectionEnd02"],
                            Probability02 = (int)selectedItem[$"nProbability02"],
                            SectionStart03 = (int)selectedItem[$"nSectionStart03"],
                            SectionEnd03 = (int)selectedItem[$"nSectionEnd03"],
                            Probability03 = (int)selectedItem[$"nProbability03"]
                        };

                        Application.Current.Dispatcher.Invoke(() => DropGroupItems.Add(newItem));
                        RiddleDropItemPropertyChanged(newItem);
                    }
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

        private void RiddleDropItemPropertyChanged(ItemDropGroup item)
        {
            item.PropertyChanged -= OnRiddleDropItemPropertyChanged;
            item.PropertyChanged += OnRiddleDropItemPropertyChanged;
        }

        private void OnRiddleDropItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is ItemDropGroup itemDropGroup)
            {
                int index = DropGroupItems.IndexOf(itemDropGroup);
                UpdateSelectedItemValue(itemDropGroup.DropItemCode, $"nItem{index:00}");
                UpdateSelectedItemValue(itemDropGroup.SectionStart00, $"nSectionStart00");
                UpdateSelectedItemValue(itemDropGroup.SectionEnd00, $"nSectionEnd00");
                UpdateSelectedItemValue(itemDropGroup.Probability00, $"nProbability00");
                UpdateSelectedItemValue(itemDropGroup.SectionStart00, $"nSectionStart01");
                UpdateSelectedItemValue(itemDropGroup.SectionEnd00, $"nSectionEnd01");
                UpdateSelectedItemValue(itemDropGroup.Probability00, $"nProbability01");
                UpdateSelectedItemValue(itemDropGroup.SectionStart00, $"nSectionStart02");
                UpdateSelectedItemValue(itemDropGroup.SectionEnd00, $"nSectionEnd02");
                UpdateSelectedItemValue(itemDropGroup.Probability00, $"nProbability02");
                UpdateSelectedItemValue(itemDropGroup.SectionStart00, $"nSectionStart03");
                UpdateSelectedItemValue(itemDropGroup.SectionEnd00, $"nSectionEnd03");
                UpdateSelectedItemValue(itemDropGroup.Probability00, $"nProbability03");
            }
        }

        #endregion

        #region RareCardDropGroup
        private void UpdateRareCardDropGroupSelectedItem(DataRowView? selectedItem)
        {
            _isUpdatingSelectedItem = true;
            DropGroupItems?.Clear();

            if (selectedItem != null)
            {
                DropGroupItems = [];

                FNilValue = (float)selectedItem["fNil"];

                int bronzeCardCode = (int)selectedItem["nBronzeCardID"];
                var bronzeCard = ItemDataManager.GetItemDataViewModel(bronzeCardCode, 1, 1);
                double bronzeCardProbability = (float)selectedItem["fBronzeCard"];
                int silverCardCode = (int)selectedItem["nSilverCardID"];
                var silverCard = ItemDataManager.GetItemDataViewModel(silverCardCode, 1, 1);
                double silverCardProbability = (float)selectedItem["fSilverCard"];
                int goldCardCode = (int)selectedItem["nGoldCardID"];
                var goldCard = ItemDataManager.GetItemDataViewModel(goldCardCode, 1, 1);
                double goldCardProbability = (float)selectedItem["fGoldCard"];

                var newItem = new ItemDropGroup
                {
                    DropItemGroupType = DropGroupType,
                    BronzeCardCode = bronzeCardCode,
                    BronzeCard = bronzeCard,
                    BronzeCardProbability = bronzeCardProbability,
                    SilverCardCode = silverCardCode,
                    SilverCard = silverCard,
                    SilverCardProbability = silverCardProbability,
                    GoldCardCode = goldCardCode,
                    GoldCard = goldCard,
                    GoldCardProbability = goldCardProbability
                };

                DropGroupItems.Add(newItem);
                RareCardDropItemPropertyChanged(newItem);

                IsSelectedItemVisible = Visibility.Visible;
            }
            else
            {
                IsSelectedItemVisible = Visibility.Hidden;
            }

            _isUpdatingSelectedItem = false;
            OnCanExecuteSelectedItemCommandChanged();
        }

        private void RareCardDropItemPropertyChanged(ItemDropGroup item)
        {
            item.PropertyChanged -= OnRareCardDropItemPropertyChanged;

            item.PropertyChanged += OnRareCardDropItemPropertyChanged;
        }

        private void OnRareCardDropItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is ItemDropGroup rareCardDropGroup)
            {
                UpdateSelectedItemValue(rareCardDropGroup.BronzeCardCode, $"nBronzeCardID");
                UpdateSelectedItemValue(rareCardDropGroup.BronzeCardProbability, $"fBronzeCard");
                UpdateSelectedItemValue(rareCardDropGroup.SilverCardCode, $"nSilverCardID");
                UpdateSelectedItemValue(rareCardDropGroup.SilverCardProbability, $"fSilverCard");
                UpdateSelectedItemValue(rareCardDropGroup.GoldCardCode, $"nGoldCardID");
                UpdateSelectedItemValue(rareCardDropGroup.GoldCardProbability, $"fGoldCard");
                CalculateRareCardFNilValue();
            }
        }

        #endregion

        #region RareCardRewardItemList
        private void UpdateRareCardRewardItemSelectedItem(DataRowView? selectedItem)
        {
            _isUpdatingSelectedItem = true;

            if (selectedItem != null)
            {
                DropGroupItems ??= [];

                for (int i = 0; i < DropItemCount; i++)
                {
                    int rewardItemCode = (int)selectedItem[$"nRewardItem{i + 1:00}"];
                    ItemDataViewModel? itemDataViewModel = null;

                    if (i < DropGroupItems.Count)
                    {
                        var existingItem = DropGroupItems[i];

                        if (existingItem.DropItemCode != rewardItemCode)
                        {
                            itemDataViewModel = ItemDataManager.GetItemDataViewModel(rewardItemCode, i + 1, 1);
                            existingItem.ItemDataViewModel = itemDataViewModel;
                        }

                        existingItem.DropItemGroupType = DropGroupType;
                        existingItem.DropItemCode = rewardItemCode;

                        RareCardRewardItemPropertyChanged(existingItem);
                    }
                    else
                    {
                        itemDataViewModel = ItemDataManager.GetItemDataViewModel(rewardItemCode, i + 1, 1);

                        var newItem = new ItemDropGroup
                        {
                            DropItemGroupType = DropGroupType,
                            DropItemCode = rewardItemCode,
                            ItemDataViewModel = itemDataViewModel
                        };

                        Application.Current.Dispatcher.Invoke(() => DropGroupItems.Add(newItem));
                        RareCardRewardItemPropertyChanged(newItem);
                    }
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

        private void RareCardRewardItemPropertyChanged(ItemDropGroup item)
        {
            item.PropertyChanged -= OnRareCardRewardItemPropertyChanged;

            item.PropertyChanged += OnRareCardRewardItemPropertyChanged;
        }

        private void OnRareCardRewardItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is ItemDropGroup itemDropGroup)
            {
                int index = DropGroupItems.IndexOf(itemDropGroup);
                UpdateSelectedItemValue(itemDropGroup.DropItemCode, $"nRewardItem{index + 1:00}");
            }
        }

        #endregion

        #endregion

        #region Properties
        [ObservableProperty]
        private string _title = string.Format(Resources.EditorTitle, "Drop Group");

        [ObservableProperty]
        private string? _openMessage = Resources.OpenFile;

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
        private ItemDropGroupType _dropGroupType;

        [ObservableProperty]
        private int _dropItemCount;

        [ObservableProperty]
        private ObservableCollection<ItemDropGroup> _dropGroupItems = [];

        [ObservableProperty]
        private double _dropItemCountTotal;

        [ObservableProperty]
        private double _dropItemCountTotalValue;
        partial void OnDropItemCountTotalValueChanged(double value)
        {
            if (_isUpdatingDropItemCountTotal)
                return;

            _isUpdatingDropItemCountTotal = true;

            try
            {
                if (DataTableManager.SelectedItem != null)
                {
                    int itemCount = 0;

                    foreach (var item in DropGroupItems)
                    {
                        if (item.DropItemCode != 0)
                        {
                            itemCount++;
                        }
                    }

                    if (itemCount > 0)
                    {
                        double newDropValuePerItem = value / itemCount;

                        foreach (var item in DropGroupItems)
                        {
                            if (item.DropItemCode != 0)
                            {
                                if (DropGroupType == ItemDropGroupType.ItemDropGroupList)
                                {
                                    item.NDropItemCount = (int)newDropValuePerItem;
                                }
                                else
                                {
                                    item.FDropItemCount = newDropValuePerItem;
                                }
                            }
                        }
                    }

                    DataTableManager.EndGroupingEdits();
                    CalculateDropItemCountTotal();
                }
                
            }
            finally
            {
                _isUpdatingDropItemCountTotal = false;
            }
        }

        [ObservableProperty]
        private double _fNilValue;
        partial void OnFNilValueChanged(double value)
        {
            UpdateSelectedItemValue(value, "fNil");
        }

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

        #region DropGroup

        private bool _isUpdatingDropItemCountTotal = false;

        private void CalculateDropItemCountTotal()
        {
            if (DataTableManager.SelectedItem != null)
            {
                _isUpdatingDropItemCountTotal = true;

                try
                {
                    if (DropGroupType == ItemDropGroupType.ItemDropGroupList)
                    {
                        // Handle NDropItemCount
                        int totalNDropItemCount = DropGroupItems.Sum(item => item.NDropItemCount);
                        DropItemCountTotalValue = totalNDropItemCount;
                    }
                    else
                    {
                        // Handle FDropItemCount
                        double totalFDropItemCount = DropGroupItems.Sum(item => item.FDropItemCount);
                        DropItemCountTotalValue = totalFDropItemCount;
                    }
                }
                finally
                {
                    _isUpdatingDropItemCountTotal = false;
                }

                if (DropGroupType == ItemDropGroupType.ItemDropGroupList)
                {
                    // Handle NDropItemCount
                    int totalNDropItemCount = DropGroupItems.Sum(item => item.NDropItemCount);
                    DropItemCountTotal = (double)totalNDropItemCount / 1000;
                }
                else
                {
                    // Handle FDropItemCount
                    double totalFDropItemCount = DropGroupItems.Sum(item => item.FDropItemCount);
                    DropItemCountTotal = totalFDropItemCount * 100;
                }
            }

        }

        #endregion

        #region RareCard

        private void CalculateRareCardFNilValue()
        {
            if (DataTableManager.SelectedItem != null)
            {

                double currentTotal = DropGroupItems.Sum(item => item.BronzeCardProbability
                                                              + item.SilverCardProbability
                                                              + item.GoldCardProbability);

                FNilValue = 1 - currentTotal;
            }
        }
        #endregion

        #endregion
    }
}
