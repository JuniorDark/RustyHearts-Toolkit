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
                    string? fileName = GetFileNameFromDropGroupType(dropGroupType);
                    if (fileName == null) return;

                    SetDropGroupProperties(dropGroupType);

                    bool isLoaded = await DataTableManager.LoadFileFromPath(fileName, null, "nDropItemCode01", "DropGroup");

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

                string filter = "Item Drop Group Files .rh|" +
                                "itemdropgrouplist_f.rh;itemdropgrouplist.rh;championitemdropgrouplist.rh;" +
                                "eventworlditemdropgrouplist.rh;instanceitemdropgrouplist.rh;" +
                                "questitemdropgrouplist.rh;worldinstanceitemdropgrouplist.rh;" +
                                "worlditemdropgrouplist.rh;worlditemdropgrouplist_fatigue.rh|" +
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
                        RHMessageBoxHelper.ShowOKMessage($"The file '{fileName}' is not a valid Item Drop Group file.", Resources.Error);
                        return;
                    }

                    SetDropGroupProperties(dropGroupType);

                    bool isLoaded = await DataTableManager.LoadFileAs(openFileDialog.FileName, null, "nDropItemCode01", "DropGroup");

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
            Title = $"Drop Group Editor ({DataTableManager.CurrentFileName})";
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
                _ => -1,
            };
        }

        private void SetDropGroupProperties(int dropGroupType)
        {
            DropGroupType = (ItemDropGroupType)dropGroupType;
            DropItemCount = DropGroupType switch
            {
                ItemDropGroupType.ChampionItemItemDropGroupList or ItemDropGroupType.InstanceItemDropGroupList or ItemDropGroupType.QuestItemDropGroupList or ItemDropGroupType.WorldInstanceItemDropGroupList => 30,
                ItemDropGroupType.ItemDropGroupListF or ItemDropGroupType.ItemDropGroupList => 40,
                ItemDropGroupType.EventWorldItemDropGroupList or ItemDropGroupType.WorldItemDropGroupList or ItemDropGroupType.WorldItemDropGroupListF => 60,
                _ => 0,
            };
        }

        [RelayCommand(CanExecute = nameof(CanExecuteFileCommand))]
        private void OpenSearchDialog(string? parameter)
        {
            try
            {
                Window? shopEditorWindow = Application.Current.Windows.OfType<DropGroupEditorWindow>().FirstOrDefault();
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
            Title = $"Drop Group Editor";
            OpenMessage = "Open a file";
            DropGroupType = 0;
            DropItemCountTotal = 0;
            DropItemCountTotalValue = 0;
            DropGroupItems?.Clear();
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
                RHMessageBoxHelper.ShowOKMessage($"Error: {ex.Message}", "Error");
            }
        }

        public void Receive(ItemDataMessage message)
        {
            if (message.Recipient == "DropGroupEditorWindow" && message.Token == _token)
            {
                var itemData = message.Value;

                if (DataTableManager.SelectedItem != null && itemData.SlotIndex >= 0 && itemData.SlotIndex <= DropGroupItems.Count)
                {
                    UpdateDropGroupItem(itemData);
                }
            }
        }

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

        private void UpdateDropGroupItem(ItemData itemData)
        {
            if (itemData.ItemId != 0)
            {
                DataTableManager.StartGroupingEdits();
                var frameViewModel = ItemDataManager.GetFrameViewModel(itemData.ItemId, itemData.SlotIndex, itemData.ItemAmount);
                DropGroupItems[itemData.SlotIndex].DropItemCode = itemData.ItemId;
                DropGroupItems[itemData.SlotIndex].FrameViewModel = frameViewModel;
                OnPropertyChanged(nameof(DropGroupItems));
                DataTableManager.EndGroupingEdits();
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
                    RemoveDropGroupItem(slotIndex);
                }
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"Error: {ex.Message}", "Error");
            }
            
        }

        private void RemoveDropGroupItem(int slotIndex)
        {
            if (slotIndex >= 0 && slotIndex <= DropGroupItems.Count)
            {
                DataTableManager.StartGroupingEdits();
                DropGroupItems[slotIndex].FrameViewModel = null;
                DropGroupItems[slotIndex].DropItemCode = 0;
                DropGroupItems[slotIndex].FDropItemCount = 0;
                DropGroupItems[slotIndex].NDropItemCount = 0;
                OnPropertyChanged(nameof(DropGroupItems));
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

            columns.Insert(0, "CONVERT(nID, 'System.String')");

            for (int i = 1; i <= DropItemCount; i++)
            {
                string columnName = $"nDropItemCode{i:00}";
                columns.Add($"CONVERT({columnName}, 'System.String')");
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

                UpdateSelectedItem(selectedItem);
            }
        }

        private void UpdateSelectedItem(DataRowView? selectedItem)
        {
            _isUpdatingSelectedItem = true;
            DropGroupItems?.Clear();

            if (selectedItem != null)
            {
                DropGroupId = (int)selectedItem["nID"];
                MaxRange = selectedItem.Row.Table.Columns.Contains("nMaxRange")
                        ? (int)selectedItem["nMaxRange"] : 0;
                MaxCount = selectedItem.Row.Table.Columns.Contains("nMaxCount")
                        ? (int)selectedItem["nMaxCount"] : 0;
                Seed = selectedItem.Row.Table.Columns.Contains("nSeed")
                        ? (int)selectedItem["nSeed"] : 0;

                Note = selectedItem.Row.Table.Columns.Contains("wszNote")
                        ? (string)selectedItem["wszNote"] : string.Empty;
                Name = selectedItem.Row.Table.Columns.Contains("wszName")
                        ? (string)selectedItem["wszName"] : string.Empty;
                Group = selectedItem.Row.Table.Columns.Contains("nGroup")
                        ? (int)selectedItem["nGroup"] : 0;

                DropGroupItems = [];

                for (int i = 0; i < DropItemCount; i++)
                {
                    var item = new ItemDropGroup
                    {
                        DropItemGroupType = DropGroupType,
                        DropItemCode = (int)selectedItem[$"nDropItemCode{i + 1:00}"],
                        FDropItemCount = selectedItem.Row.Table.Columns.Contains($"fDropItemCount{i + 1:00}")
                        ? (float)selectedItem[$"fDropItemCount{i + 1:00}"]
                        : 0,
                        NDropItemCount = selectedItem.Row.Table.Columns.Contains($"nDropItemCount{i + 1:00}")
                        ? (int)selectedItem[$"nDropItemCount{i + 1:00}"]
                        : 0,
                        Link = selectedItem.Row.Table.Columns.Contains($"nDropItemLink{i + 1:00}")
                        ? (int)selectedItem[$"nDropItemLink{i + 1:00}"]
                        : 0,
                        Start = selectedItem.Row.Table.Columns.Contains($"nStart{i + 1:00}")
                        ? (int)selectedItem[$"nStart{i + 1:00}"]
                        : 0,
                        End = selectedItem.Row.Table.Columns.Contains($"nEnd{i + 1:00}")
                        ? (int)selectedItem[$"nEnd{i + 1:00}"]
                        : 0,
                        FrameViewModel = ItemDataManager.GetFrameViewModel((int)selectedItem[$"nDropItemCode{i + 1:00}"], i + 1, 1)
                    };

                    DropGroupItems.Add(item);

                    ItemPropertyChanged(item, i);
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

        private void ItemPropertyChanged(ItemDropGroup item, int index)
        {
            item.PropertyChanged += (s, e) =>
            {
                if (s is ItemDropGroup itemDropGroup)
                {
                    UpdateSelectedItemValue(itemDropGroup.DropItemCode, $"nDropItemCode{index + 1:00}");
                    UpdateSelectedItemValue(itemDropGroup.FDropItemCount, $"fDropItemCount{index + 1:00}");
                    UpdateSelectedItemValue(itemDropGroup.NDropItemCount, $"nDropItemCount{index + 1:00}");
                    UpdateSelectedItemValue(itemDropGroup.Link, $"nDropItemLink{index + 1:00}");
                    UpdateSelectedItemValue(itemDropGroup.Start, $"nStart{index + 1:00}");
                    UpdateSelectedItemValue(itemDropGroup.End, $"nEnd{index + 1:00}");

                    CalculateDropItemCountTotal();
                }
            };
        }

        #endregion

        #region Properties
        [ObservableProperty]
        private string _title = $"Drop Group Editor";

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
        private ItemDropGroupType _dropGroupType;

        [ObservableProperty]
        private int _dropItemCount;

        [ObservableProperty]
        private int _dropGroupId;
        partial void OnDropGroupIdChanged(int value)
        {
            UpdateSelectedItemValue(value, "nID");
        }

        [ObservableProperty]
        private int _maxRange;
        partial void OnMaxRangeChanged(int value)
        {
            UpdateSelectedItemValue(value, "nMaxRange");
        }

        [ObservableProperty]
        private int _maxCount;
        partial void OnMaxCountChanged(int value)
        {
            UpdateSelectedItemValue(value, "nMaxCount");
        }

        [ObservableProperty]
        private int _seed;
        partial void OnSeedChanged(int value)
        {
            UpdateSelectedItemValue(value, "nSeed");
        }

        [ObservableProperty]
        private int _group;
        partial void OnGroupChanged(int value)
        {
            UpdateSelectedItemValue(value, "nGroup");
        }

        [ObservableProperty]
        private string? _note;
        partial void OnNoteChanged(string? value)
        {
            UpdateSelectedItemValue(value, "wszNote");
        }

        [ObservableProperty]
        private string? _name;
        partial void OnNameChanged(string? value)
        {
            UpdateSelectedItemValue(value, "wszName");
        }

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
                    DataTableManager.StartGroupingEdits();

                    if (DropGroupType == ItemDropGroupType.ItemDropGroupList)
                    {
                        // Update NDropItemCount values proportionally
                        int currentTotal = DropGroupItems.Sum(item => item.NDropItemCount);
                        if (currentTotal > 0)
                        {
                            int scale = (int)value / currentTotal;
                            foreach (var item in DropGroupItems)
                            {
                                if (item.DropItemCode > 0)
                                {
                                    item.NDropItemCount *= scale;
                                }
                            }
                        }
                    }
                    else
                    {
                        // Update FDropItemCount values proportionally
                        double currentTotal = DropGroupItems.Sum(item => item.FDropItemCount);
                        if (currentTotal > 0)
                        {
                            double scale = value / currentTotal;
                            foreach (var item in DropGroupItems)
                            {
                                if (item.DropItemCode > 0)
                                {
                                    item.FDropItemCount *= scale;
                                }
                            }
                        }
                    }
                }
                OnPropertyChanged(nameof(DropGroupItems));
                DataTableManager.EndGroupingEdits();
                CalculateDropItemCountTotal();
            }
            finally
            {
                _isUpdatingDropItemCountTotal = false;
            }
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
    }
}
