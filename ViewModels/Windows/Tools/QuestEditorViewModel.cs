using Microsoft.Win32;
using RHToolkit.Messages;
using RHToolkit.Models;
using RHToolkit.Models.Database;
using RHToolkit.Models.Editor;
using RHToolkit.Models.MessageBox;
using RHToolkit.Properties;
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
    public partial class QuestEditorViewModel : ObservableObject, IRecipient<ItemDataMessage>, IRecipient<DataRowViewMessage>
    {
        private readonly Guid _token;
        private readonly IWindowsService _windowsService;
        private readonly IGMDatabaseService _gmDatabaseService;
        private readonly System.Timers.Timer _filterUpdateTimer;
        private readonly System.Timers.Timer _questListFilterUpdateTimer;

        public QuestEditorViewModel(IWindowsService windowsService, IGMDatabaseService gmDatabaseService, ItemDataManager itemDataManager)
        {
            _token = Guid.NewGuid();
            _windowsService = windowsService;
            _gmDatabaseService = gmDatabaseService;
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

            _questListFilterUpdateTimer = new()
            {
                Interval = 500,
                AutoReset = false
            };
            _questListFilterUpdateTimer.Elapsed += QuestListFilterUpdateTimerElapsed;

            PopulateListItems();

            WeakReferenceMessenger.Default.Register<ItemDataMessage>(this);
            WeakReferenceMessenger.Default.Register<DataRowViewMessage>(this);

            _questListView = CollectionViewSource.GetDefaultView(QuestListItems);
            _questListView.Filter = FilterQuestList;
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
                    string? fileName = GetFileNameFromQuestType(dropGroupType);
                    if (fileName == null) return;
                    int questType = GetQuestTypeFromFileName(fileName);
                    string columnName = GetColumnName(fileName);
                    string? stringFileName = GetStringFileName(questType);

                    QuestType = (QuestType)questType;

                    bool isLoaded = await DataTableManager.LoadFileFromPath(fileName, stringFileName, columnName, "Quest");

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

                string filter = "Quest Files .rh|" +
                                "quest.rh;quest_acquire.rh;mission.rh;party_mission.rh;" +
                                "missionreward.rh;questcomplete.rh;questgroup.rh;questgroupcomplete.rh;" +
                                "questgrouprequest.rh;questgroupstring.rh;questrequest.rh;queststring.rh;" +
                                "quest_acquire_string.rh|" +
                                "All Files (*.*)|*.*";

                OpenFileDialog openFileDialog = new()
                {
                    Filter = filter,
                    FilterIndex = 1
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    string fileName = Path.GetFileName(openFileDialog.FileName);
                    int questType = GetQuestTypeFromFileName(fileName);

                    if (questType == -1)
                    {
                        RHMessageBoxHelper.ShowOKMessage($"The file '{fileName}' is not a valid Quest file.", Resources.Error);
                        return;
                    }

                    string columnName = GetColumnName(fileName);
                    string? stringFileName = GetStringFileName(questType);

                    QuestType = (QuestType)questType;

                    bool isLoaded = await DataTableManager.LoadFileAs(openFileDialog.FileName, stringFileName, columnName, "Quest");

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
            Title = $"Quest Editor ({DataTableManager.CurrentFileName})";
            OpenMessage = "";
            IsVisible = Visibility.Visible;
            OnCanExecuteFileCommandChanged();
        }

        private static string? GetFileNameFromQuestType(int questType)
        {
            return questType switch
            {
                1 => "mission.rh",
                2 => "missionreward.rh",
                3 => "party_mission.rh",
                4 => "quest.rh",
                5 => "queststring.rh",
                6 => "questcomplete.rh",
                7 => "questgroup.rh",
                8 => "questgroupstring.rh",
                9 => "questgroupcomplete.rh",
                10 => "questgrouprequest.rh",
                11 => "questrequest.rh",
                12 => "quest_acquire.rh",
                13 => "quest_acquire_string.rh",
                _ => throw new ArgumentOutOfRangeException(nameof(questType)),
            };
        }

        private static int GetQuestTypeFromFileName(string fileName)
        {
            return fileName switch
            {
                "mission.rh" => 1,
                "missionreward.rh" => 2,
                "party_mission.rh" => 3,
                "quest.rh" => 4,
                "queststring.rh" => 5,
                "questcomplete.rh" => 6,
                "questgroup.rh" => 7,
                "questgroupstring.rh" => 8,
                "questgroupcomplete.rh" => 9,
                "questgrouprequest.rh" => 10,
                "questrequest.rh" => 11,
                "quest_acquire.rh" => 12,
                "quest_acquire_string.rh" => 13,
                _ => -1,
            };
        }

        private static string GetColumnName(string fileName)
        {
            return fileName switch
            {
                "quest.rh" or "quest_acquire.rh" => "nAutoNextQuest",
                "party_mission.rh" => "nDayMission",
                "mission.rh" => "nMissionStringID",
                "missionreward.rh" => "fRatio00",
                "questcomplete.rh" or "questgroupcomplete.rh" or "questgrouprequest.rh" or "questrequest.rh" => "szSprite00",
                "questgroup.rh" => "wszNpcNameTitle",
                "questgroupstring.rh" or "queststring.rh" or "quest_acquire_string.rh" => "wszAccept",
                _ => "",
            };
        }

        private static string? GetStringFileName(int questType)
        {
            return questType switch
            {
                4 => "queststring.rh",
                12 => "quest_acquire_string.rh",
                _ => null,
            };
        }

        [RelayCommand(CanExecute = nameof(CanExecuteFileCommand))]
        private void OpenSearchDialog(string? parameter)
        {
            try
            {
                Window? questEditorWindow = Application.Current.Windows.OfType<QuestEditorWindow>().FirstOrDefault();
                Window owner = questEditorWindow ?? Application.Current.MainWindow;
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
            Title = $"Quest Editor";
            OpenMessage = "Open a file";
            QuestItem?.Clear();
            QuestItems?.Clear();
            QuestRewardItems?.Clear();
            QuestItemsValue?.Clear();
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
            AddQuestItemCommand.NotifyCanExecuteChanged();
            AddQuestItemsCommand.NotifyCanExecuteChanged();
            AddQuestRewardItemCommand.NotifyCanExecuteChanged();
        }

        private void OnCanExecuteFileCommandChanged()
        {
            CloseFileCommand.NotifyCanExecuteChanged();
            OpenSearchDialogCommand.NotifyCanExecuteChanged();
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
                RHMessageBoxHelper.ShowOKMessage($"Error: {ex.Message}", "Error");
            }
        }
        #endregion

        #region Add Item
        [RelayCommand(CanExecute = nameof(CanExecuteSelectedItemCommand))]
        private void AddQuestItem(string parameter)
        {
            try
            {
                if (int.TryParse(parameter, out int slotIndex))
                {
                    var itemData = new ItemData
                    {
                        SlotIndex = slotIndex,
                        ItemId = QuestItem[slotIndex].ItemID,
                        ItemAmount = QuestItem[slotIndex].ItemCount,
                    };

                    _windowsService.OpenItemWindow(_token, "QuestItem", itemData);
                }

            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"Error: {ex.Message}", "Error");
            }
        }

        [RelayCommand(CanExecute = nameof(CanExecuteSelectedItemCommand))]
        private void AddQuestItems(string parameter)
        {
            try
            {
                if (int.TryParse(parameter, out int slotIndex))
                {
                    var itemData = new ItemData
                    {
                        SlotIndex = slotIndex,
                        ItemId = QuestItems[slotIndex].ItemID,
                        ItemAmount = QuestItems[slotIndex].ItemCount,
                    };

                    string messageType = "";

                    switch (QuestType)
                    {
                        case QuestType.PartyMission:
                            messageType = "PartyMissionItem";
                            break;
                        case QuestType.Quest or QuestType.QuestAcquire:
                            messageType = "QuestItems";
                            break;
                    }

                    _windowsService.OpenItemWindow(_token, messageType, itemData);
                }

            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"Error: {ex.Message}", "Error");
            }
        }

        [RelayCommand(CanExecute = nameof(CanExecuteSelectedItemCommand))]
        private void AddQuestRewardItem(string parameter)
        {
            try
            {
                if (int.TryParse(parameter, out int slotIndex))
                {
                    var itemData = new ItemData
                    {
                        SlotIndex = slotIndex,
                        ItemId = QuestRewardItems[slotIndex].ItemID,
                        ItemAmount = QuestRewardItems[slotIndex].ItemCount,
                    };

                    _windowsService.OpenItemWindow(_token, "QuestRewardItem", itemData);
                }

            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"Error: {ex.Message}", "Error");
            }
        }

        public void Receive(ItemDataMessage message)
        {
            if (message.Recipient == "QuestEditorWindow" && message.Token == _token)
            {
                var itemData = message.Value;
                var messageType = message.MessageType;

                if (itemData.ItemId != 0)
                {
                    switch (messageType)
                    {
                        case "QuestItem":
                            UpdateQuestItem(itemData);
                            break;
                        case "QuestItems" or "PartyMissionItem":
                            UpdateQuestItems(itemData);
                            break;
                        case "QuestRewardItem":
                            UpdateQuestRewardItem(itemData);
                            break;
                    }
                }
            }
        }

        private void UpdateQuestItem(ItemData itemData)
        {
            DataTableManager.StartGroupingEdits();
            var item = QuestItem[itemData.SlotIndex];
            var itemDataViewModel = ItemDataManager.GetItemDataViewModel(itemData.ItemId, itemData.SlotIndex, itemData.ItemAmount);
            item.ItemID = itemData.ItemId;
            item.ItemCount = itemData.ItemAmount;
            item.ItemDataViewModel = itemDataViewModel;
            OnPropertyChanged(nameof(QuestItem));
            DataTableManager.EndGroupingEdits();
        }

        private void UpdateQuestItems(ItemData itemData)
        {
            DataTableManager.StartGroupingEdits();
            var item = QuestItems[itemData.SlotIndex];
            var itemDataViewModel = ItemDataManager.GetItemDataViewModel(itemData.ItemId, itemData.SlotIndex, itemData.ItemAmount);
            item.ItemID = itemData.ItemId;
            item.ItemCount = itemData.ItemAmount;
            item.ItemDataViewModel = itemDataViewModel;
            OnPropertyChanged(nameof(QuestItems));
            DataTableManager.EndGroupingEdits();
        }

        private void UpdateQuestRewardItem(ItemData itemData)
        {
            DataTableManager.StartGroupingEdits();
            var item = QuestRewardItems[itemData.SlotIndex];
            var itemDataViewModel = ItemDataManager.GetItemDataViewModel(itemData.ItemId, itemData.SlotIndex, itemData.ItemAmount);
            item.ItemID = itemData.ItemId;
            item.ItemCount = itemData.ItemAmount;
            item.ItemDataViewModel = itemDataViewModel;
            OnPropertyChanged(nameof(QuestRewardItems));
            DataTableManager.EndGroupingEdits();
        }

        #endregion

        #region Remove Item

        [RelayCommand]
        private void RemoveQuestItem(string parameter)
        {
            try
            {
                if (int.TryParse(parameter, out int slotIndex))
                {
                    RemoveQuestItem(slotIndex);
                }
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"Error: {ex.Message}", "Error");
            }
            
        }

        private void RemoveQuestItem(int slotIndex)
        {
            if (slotIndex >= 0 && slotIndex < QuestItem.Count)
            {
                DataTableManager.StartGroupingEdits();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    var item = QuestItem[slotIndex];
                    if (item != null)
                    {
                        item.ItemDataViewModel = null;
                        item.ItemID = 0;
                        item.ItemCount = 0;
                    }
                });
                
                DataTableManager.EndGroupingEdits();
            }
        }

        [RelayCommand]
        private void RemoveQuestItems(string parameter)
        {
            try
            {
                if (int.TryParse(parameter, out int slotIndex))
                {
                    RemoveQuestItems(slotIndex);
                }
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"Error: {ex.Message}", "Error");
            }

        }

        private void RemoveQuestItems(int slotIndex)
        {
            if (slotIndex >= 0 && slotIndex < QuestItems.Count)
            {
                DataTableManager.StartGroupingEdits();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    var item = QuestItems[slotIndex];
                    if (item != null)
                    {
                        item.ItemDataViewModel = null;
                        item.ItemID = 0;
                        item.ItemCount = 0;
                    }
                });

                DataTableManager.EndGroupingEdits();
            }
        }

        [RelayCommand]
        private void RemoveQuestRewardItem(string parameter)
        {
            try
            {
                if (int.TryParse(parameter, out int slotIndex))
                {
                    RemoveQuestRewardItem(slotIndex);
                }
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"Error: {ex.Message}", "Error");
            }

        }

        private void RemoveQuestRewardItem(int slotIndex)
        {
            if (slotIndex >= 0 && slotIndex < QuestRewardItems.Count)
            {
                DataTableManager.StartGroupingEdits();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    var item = QuestRewardItems[slotIndex];
                    if (item != null)
                    {
                        item.ItemDataViewModel = null;
                        item.ItemID = 0;
                        item.ItemCount = 0;
                    }
                });

                DataTableManager.EndGroupingEdits();
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

            switch (QuestType)
            {
                case QuestType.PartyMission:
                    for (int i = 0; i < 3; i++)
                    {
                        string columnName = $"nItemID{i + 1:00}";
                        columns.Add($"CONVERT({columnName}, 'System.String')");
                    }
                    break;
                case QuestType.Quest or QuestType.QuestAcquire:
                    for (int i = 0; i < 3; i++)
                    {
                        string columnName = $"nGetItem{i:00}";
                        columns.Add($"CONVERT({columnName}, 'System.String')");
                    }
                    columns.Add("CONVERT(nQuestItemID, 'System.String')");
                    columns.Add("CONVERT(nSelItemValue00, 'System.String')");
                    columns.Add("CONVERT(nSelItemValue02, 'System.String')");
                    columns.Add("CONVERT(nSelItemValue04, 'System.String')");
                    columns.Add("CONVERT(nSelItemValue06, 'System.String')");
                    columns.Add("CONVERT(nSelItemValue08, 'System.String')");
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

        #region Comboboxes

        [ObservableProperty]
        private List<NameID>? _worldItems;

        [ObservableProperty]
        private List<NameID>? _missionItems;

        [ObservableProperty]
        private List<NameID>? _npcInstanceItems;

        [ObservableProperty]
        private List<NameID>? _questListItems;

        private void PopulateListItems()
        {
            NpcInstanceItems = _gmDatabaseService.GetNpcInstanceItems();
            WorldItems = _gmDatabaseService.GetWorldNameItems();
            MissionItems = _gmDatabaseService.GetMissionItems();
            QuestListItems = _gmDatabaseService.GetQuestListItems();
        }

        #region QuestList Filter
        [ObservableProperty]
        private ICollectionView _questListView;

        private readonly List<int> selectedQuest = [];
        private bool FilterQuestList(object obj)
        {
            if (obj is NameID quest)
            {
                if (quest.ID == 0)
                    return true;

                if (QuestItems != null && QuestItems.Count >= 19)
                {
                    selectedQuest.Add(QuestItems[0].QuestID);
                    selectedQuest.Add(QuestItems[1].QuestID);
                    selectedQuest.Add(QuestItems[2].QuestID);
                    selectedQuest.Add(QuestItems[3].QuestID);
                    selectedQuest.Add(QuestItems[4].QuestID);
                    selectedQuest.Add(QuestItems[5].QuestID);
                    selectedQuest.Add(QuestItems[6].QuestID);
                    selectedQuest.Add(QuestItems[7].QuestID);
                    selectedQuest.Add(QuestItems[8].QuestID);
                    selectedQuest.Add(QuestItems[9].QuestID);
                    selectedQuest.Add(QuestItems[10].QuestID);
                    selectedQuest.Add(QuestItems[11].QuestID);
                    selectedQuest.Add(QuestItems[12].QuestID);
                    selectedQuest.Add(QuestItems[13].QuestID);
                    selectedQuest.Add(QuestItems[14].QuestID);
                    selectedQuest.Add(QuestItems[15].QuestID);
                    selectedQuest.Add(QuestItems[16].QuestID);
                    selectedQuest.Add(QuestItems[17].QuestID);
                    selectedQuest.Add(QuestItems[18].QuestID);
                    selectedQuest.Add(QuestItems[19].QuestID);

                    if (selectedQuest.Contains(quest.ID))
                        return true;
                }

                // text search filter
                if (!string.IsNullOrEmpty(QuestListSearch))
                {
                    string searchText = QuestListSearch.ToLower();

                    // Check if either quest ID or quest Name contains the search text
                    if (!string.IsNullOrEmpty(quest.ID.ToString()) && quest.ID.ToString().Contains(searchText, StringComparison.CurrentCultureIgnoreCase))
                        return true;

                    if (!string.IsNullOrEmpty(quest.Name) && quest.Name.Contains(searchText, StringComparison.CurrentCultureIgnoreCase))
                        return true;

                    return false;
                }

                return true;
            }
            return false;
        }

        [ObservableProperty]
        private string? _questListSearch;
        partial void OnQuestListSearchChanged(string? value)
        {
            _questListFilterUpdateTimer.Stop();
            _questListFilterUpdateTimer.Start();
        }

        private void QuestListFilterUpdateTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(QuestListView.Refresh);
        }
        #endregion

        #endregion

        #region DataRowViewMessage
        public void Receive(DataRowViewMessage message)
        {
            if (message.Token == _token)
            {
                var selectedItem = message.Value;

                switch (QuestType)
                {
                    case QuestType.Mission:
                        UpdateMissionSelectedItem(selectedItem);
                        break;
                    case QuestType.MissionReward:
                        UpdateMissionRewardSelectedItem(selectedItem);
                        break;
                    case QuestType.PartyMission:
                        UpdatePartyMissionSelectedItem(selectedItem);
                        break;
                    case QuestType.Quest or QuestType.QuestAcquire:
                        UpdateQuestSelectedItem(selectedItem);
                        break;
                    case QuestType.QuestComplete or QuestType.QuestGroupComplete or QuestType.QuestGroupRequest or QuestType.QuestRequest:
                        UpdateQuestCompleteSelectedItem(selectedItem);
                        break;
                    case QuestType.QuestGroup:
                        UpdateQuestGroupSelectedItem(selectedItem);
                        break;
                    default:
                        UpdateSelectedItem(selectedItem);
                        break;
                }

            }
        }

        #region SelectedItem
        private void UpdateSelectedItem(DataRowView? selectedItem)
        {
            _isUpdatingSelectedItem = true;

            if (selectedItem != null)
            {
                IsSelectedItemVisible = Visibility.Visible;
            }
            else
            {
                IsSelectedItemVisible = Visibility.Hidden;
            }

            _isUpdatingSelectedItem = false;
            OnCanExecuteSelectedItemCommandChanged();
        }

        #endregion

        #region Quest
        private void UpdateQuestSelectedItem(DataRowView? selectedItem)
        {
            _isUpdatingSelectedItem = true;

            if (selectedItem != null)
            {
                QuestItem?.Clear();

                QuestItem ??= [];

                int itemCode = (int)selectedItem[$"nQuestItemID"];
                int itemCount = (int)selectedItem[$"nQuestItemCount"];
                ItemDataViewModel? itemDataViewModel = null;

                itemDataViewModel = ItemDataManager.GetItemDataViewModel(itemCode, 0, itemCount);

                var newItem = new Quest
                {
                    ItemID = itemCode,
                    ItemCount = itemCount,
                    ItemDataViewModel = itemDataViewModel
                };

                Application.Current.Dispatcher.Invoke(() => QuestItem.Add(newItem));
                QuestItemPropertyChanged(newItem);

                QuestItems ??= [];

                for (int i = 0; i < 3; i++)
                {
                    int getItemCode = (int)selectedItem[$"nGetItem{i:00}"];
                    int getItemCount = (int)selectedItem[$"nGetItemCount{i:00}"];
                    ItemDataViewModel? getItemItemDataViewModel = null;

                    if (i < QuestItems.Count)
                    {
                        var existingItem = QuestItems[i];

                        if (existingItem.ItemID != getItemCode)
                        {
                            getItemItemDataViewModel = ItemDataManager.GetItemDataViewModel(getItemCode, i , getItemCount);
                            existingItem.ItemDataViewModel = getItemItemDataViewModel;
                            existingItem.ItemID = getItemCode;
                            existingItem.ItemCount = getItemCount;
                        }

                        QuestGetItemPropertyChanged(existingItem);
                    }
                    else
                    {
                        getItemItemDataViewModel = ItemDataManager.GetItemDataViewModel(getItemCode, i, getItemCount);

                        var getItem = new Quest
                        {
                            ItemID = getItemCode,
                            ItemCount = getItemCount,
                            ItemDataViewModel = getItemItemDataViewModel
                        };

                        Application.Current.Dispatcher.Invoke(() => QuestItems.Add(getItem));
                        QuestGetItemPropertyChanged(getItem);
                    }
                }

                QuestRewardItems ??= [];

                for (int i = 0; i < 10; i += 2) 
                {
                    int selectItemCode = (int)selectedItem[$"nSelItemValue{i:00}"];
                    int selectItemCount = (int)selectedItem[$"nSelItemValue{i + 1:00}"];

                    var selectItemDataViewModel = ItemDataManager.GetItemDataViewModel(selectItemCode, i / 2, selectItemCount);

                    if ((i / 2) < QuestRewardItems.Count)
                    {
                        var existingItem = QuestRewardItems[i / 2];
                        if (existingItem.ItemID != selectItemCode || existingItem.ItemCount != selectItemCount)
                        {
                            existingItem.ItemID = selectItemCode;
                            existingItem.ItemCount = selectItemCount;
                            existingItem.ItemDataViewModel = selectItemDataViewModel;
                        }

                        QuestSelectItemPropertyChanged(existingItem);
                    }
                    else
                    {
                        var selectItem = new Quest
                        {
                            ItemID = selectItemCode,
                            ItemCount = selectItemCount,
                            ItemDataViewModel = selectItemDataViewModel
                        };

                        Application.Current.Dispatcher.Invoke(() => QuestRewardItems.Add(selectItem));
                        QuestSelectItemPropertyChanged(selectItem);
                    }
                }

                QuestItemsValue ??= [];

                for (int i = 0; i < 10; i++)
                {
                    int value = (int)selectedItem[$"nValue{i:00}"];

                    if (i < QuestItemsValue.Count)
                    {
                        var existingItem = QuestItemsValue[i];

                        if (existingItem.Value != value)
                        {
                            existingItem.Value = value;
                        }

                        QuestValueItemPropertyChanged(existingItem);
                    }
                    else
                    {
                        var newItemValue = new Quest
                        {
                            Value = value,
                        };

                        QuestItemsValue.Add(newItemValue);
                        QuestValueItemPropertyChanged(newItem);
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

        private void QuestItemPropertyChanged(Quest item)
        {
            item.PropertyChanged -= OnQuestItemPropertyChanged;

            item.PropertyChanged += OnQuestItemPropertyChanged;
        }

        private void OnQuestItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is Quest questItem)
            {
                UpdateSelectedItemValue(questItem.ItemID, $"nQuestItemID");
                UpdateSelectedItemValue(questItem.ItemCount, $"nQuestItemCount");
            }
        }

        private void QuestGetItemPropertyChanged(Quest item)
        {
            item.PropertyChanged -= OnQuestGetItemPropertyChanged;

            item.PropertyChanged += OnQuestGetItemPropertyChanged;
        }

        private void OnQuestGetItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is Quest questItem)
            {
                int index = QuestItems.IndexOf(questItem);
                UpdateSelectedItemValue(questItem.ItemID, $"nGetItem{index:00}");
                UpdateSelectedItemValue(questItem.ItemCount, $"nGetItemCount{index:00}");
            }
        }

        private void QuestValueItemPropertyChanged(Quest item)
        {
            item.PropertyChanged -= OnQuestValueItemPropertyChanged;

            item.PropertyChanged += OnQuestValueItemPropertyChanged;
        }

        private void OnQuestValueItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is Quest questItem)
            {
                int index = QuestItemsValue.IndexOf(questItem);
                UpdateSelectedItemValue(questItem.ItemID, $"nValue{index:00}");
            }
        }

        private void QuestSelectItemPropertyChanged(Quest item)
        {
            item.PropertyChanged -= OnQuestSelectItemPropertyChanged;

            item.PropertyChanged += OnQuestSelectItemPropertyChanged;
        }

        private void OnQuestSelectItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is Quest questItem)
            {
                int index = QuestRewardItems.IndexOf(questItem);
                UpdateSelectedItemValue(questItem.ItemID, $"nSelItemValue{index * 2:00}");
                UpdateSelectedItemValue(questItem.ItemCount, $"nSelItemValue{index * 2 + 1:00}");
            }
        }

        #endregion

        #region Mission
        private void UpdateMissionSelectedItem(DataRowView? selectedItem)
        {
            _isUpdatingSelectedItem = true;

            if (selectedItem != null)
            {
                QuestItems ??= [];

                for (int i = 0; i < 10; i++)
                {
                    int value = (int)selectedItem[$"nValue{i:00}"];

                    if (i < QuestItems.Count)
                    {
                        var existingItem = QuestItems[i];

                        if (existingItem.Value != value)
                        {
                            existingItem.Value = value;
                        }

                        MissionItemPropertyChanged(existingItem);
                    }
                    else
                    {
                        var newItem = new Quest
                        {
                            Value = value
                        };

                        QuestItems.Add(newItem);
                        MissionItemPropertyChanged(newItem);
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

        private void MissionItemPropertyChanged(Quest item)
        {
            item.PropertyChanged -= OnMissionItemPropertyChanged;

            item.PropertyChanged += OnMissionItemPropertyChanged;
        }

        private void OnMissionItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is Quest questItem)
            {
                int index = QuestItems.IndexOf(questItem);
                UpdateSelectedItemValue(questItem.Value, $"nValue{index:00}");
            }
        }

        #endregion

        #region Mission Reward
        private void UpdateMissionRewardSelectedItem(DataRowView? selectedItem)
        {
            _isUpdatingSelectedItem = true;

            if (selectedItem != null)
            {
                QuestItems ??= [];

                for (int i = 0; i < 10; i++)
                {
                    int itemCode = (int)selectedItem[$"nItemCode{i:00}"];
                    double ratio = (float)selectedItem[$"fRatio{i:00}"];
                    ItemDataViewModel? itemDataViewModel = null;

                    if (i < QuestItems.Count)
                    {
                        var existingItem = QuestItems[i];

                        if (existingItem.ItemID != itemCode)
                        {
                            itemDataViewModel = ItemDataManager.GetItemDataViewModel(itemCode, i, 1);
                            existingItem.ItemDataViewModel = itemDataViewModel;
                            existingItem.ItemID = itemCode;
                            existingItem.Ratio = ratio;
                        }

                        MissionRewardItemPropertyChanged(existingItem);
                    }
                    else
                    {
                        itemDataViewModel = ItemDataManager.GetItemDataViewModel(itemCode, i + 1, 1);

                        var newItem = new Quest
                        {
                            ItemID = itemCode,
                            Ratio = ratio,
                            ItemDataViewModel = itemDataViewModel
                        };

                        Application.Current.Dispatcher.Invoke(() => QuestItems.Add(newItem));
                        MissionRewardItemPropertyChanged(newItem);
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

        private void MissionRewardItemPropertyChanged(Quest item)
        {
            item.PropertyChanged -= OnMissionRewardItemPropertyChanged;

            item.PropertyChanged += OnMissionRewardItemPropertyChanged;
        }

        private void OnMissionRewardItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is Quest questItem)
            {
                int index = QuestItems.IndexOf(questItem);
                UpdateSelectedItemValue(questItem.ItemID, $"nItemCode{index:00}");
                UpdateSelectedItemValue(questItem.ItemID, $"fRatio{index:00}");
            }
        }

        #endregion

        #region PartyMission
        private void UpdatePartyMissionSelectedItem(DataRowView? selectedItem)
        {
            _isUpdatingSelectedItem = true;

            if (selectedItem != null)
            {
                QuestItems ??= [];

                for (int i = 0; i < 3; i++)
                {
                    int itemCode = (int)selectedItem[$"nItemID{i + 1:00}"];
                    int itemCount = (int)selectedItem[$"nItemCnt{i + 1:00}"];
                    ItemDataViewModel? itemDataViewModel = null;

                    if (i < QuestItems.Count)
                    {
                        var existingItem = QuestItems[i];

                        if (existingItem.ItemID != itemCode)
                        {
                            itemDataViewModel = ItemDataManager.GetItemDataViewModel(itemCode, i + 1, itemCount);
                            existingItem.ItemDataViewModel = itemDataViewModel;
                            existingItem.ItemID = itemCode;
                            existingItem.ItemCount = itemCount;
                        }

                        PartyMissionItemPropertyChanged(existingItem);
                    }
                    else
                    {
                        itemDataViewModel = ItemDataManager.GetItemDataViewModel(itemCode, i + 1, itemCount);

                        var newItem = new Quest
                        {
                            ItemID = itemCode,
                            ItemCount = itemCount,
                            ItemDataViewModel = itemDataViewModel
                        };

                        Application.Current.Dispatcher.Invoke(() => QuestItems.Add(newItem));
                        PartyMissionItemPropertyChanged(newItem);
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

        private void PartyMissionItemPropertyChanged(Quest item)
        {
            item.PropertyChanged -= OnPartyMissionItemPropertyChanged;

            item.PropertyChanged += OnPartyMissionItemPropertyChanged;
        }

        private void OnPartyMissionItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is Quest questItem)
            {
                int index = QuestItems.IndexOf(questItem);
                UpdateSelectedItemValue(questItem.ItemID, $"nItemID{index + 1:00}");
            }
        }

        #endregion

        #region Quest Complete
        private void UpdateQuestCompleteSelectedItem(DataRowView? selectedItem)
        {
            _isUpdatingSelectedItem = true;

            if (selectedItem != null)
            {
                QuestItems ??= [];

                for (int i = 0; i < 20; i++)
                {
                    string sprite = (string)selectedItem[$"szSprite{i:00}"];
                    int npcID = (int)selectedItem[$"nNPCID{i:00}"];
                    string text = (string)selectedItem[$"wszText{i:00}"];

                    if (i < QuestItems.Count)
                    {
                        var existingItem = QuestItems[i];

                        existingItem.NpcID = npcID;
                        existingItem.Sprite = sprite;
                        existingItem.Text = text;

                        QuestCompleteItemPropertyChanged(existingItem);
                    }
                    else
                    {
                        var newItem = new Quest
                        {
                            NpcID = npcID,
                            Sprite = sprite,
                            Text = text
                        };

                        Application.Current.Dispatcher.Invoke(() => QuestItems.Add(newItem));
                        QuestCompleteItemPropertyChanged(newItem);
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

        private void QuestCompleteItemPropertyChanged(Quest item)
        {
            item.PropertyChanged -= OnQuestCompleteItemPropertyChanged;

            item.PropertyChanged += OnQuestCompleteItemPropertyChanged;
        }

        private void OnQuestCompleteItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is Quest questItem)
            {
                int index = QuestItems.IndexOf(questItem);
                UpdateSelectedItemValue(questItem.Sprite, $"szSprite{index:00}");
                UpdateSelectedItemValue(questItem.NpcID, $"nNPCID{index:00}");
            }
        }

        #endregion

        #region Quest Group
        private void UpdateQuestGroupSelectedItem(DataRowView? selectedItem)
        {
            _isUpdatingSelectedItem = true;

            if (selectedItem != null)
            {
                QuestItems ??= [];

                for (int i = 0; i < 20; i++)
                {
                    int questID = (int)selectedItem[$"nQuest{i:00}"];

                    if (i < QuestItems.Count)
                    {
                        var existingItem = QuestItems[i];

                        existingItem.QuestID = questID;

                        QuestGroupItemPropertyChanged(existingItem);
                    }
                    else
                    {
                        var newItem = new Quest
                        {
                            QuestID = questID
                        };

                        Application.Current.Dispatcher.Invoke(() => QuestItems.Add(newItem));
                        QuestGroupItemPropertyChanged(newItem);
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

        private void QuestGroupItemPropertyChanged(Quest item)
        {
            item.PropertyChanged -= OnQuestGroupItemPropertyChanged;

            item.PropertyChanged += OnQuestGroupItemPropertyChanged;
        }

        private void OnQuestGroupItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is Quest questItem)
            {
                int index = QuestItems.IndexOf(questItem);
                UpdateSelectedItemValue(questItem.QuestID, $"nQuest{index:00}");
            }
        }

        #endregion

        #endregion

        #region Properties
        [ObservableProperty]
        private string _title = $"Quest Editor";

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
        private QuestType _questType;

        [ObservableProperty]
        private ObservableCollection<Quest> _questItem = [];

        [ObservableProperty]
        private ObservableCollection<Quest> _questItems = [];

        [ObservableProperty]
        private ObservableCollection<Quest> _questRewardItems = [];

        [ObservableProperty]
        private ObservableCollection<Quest> _questItemsValue = [];

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
