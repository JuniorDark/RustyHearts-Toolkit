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

namespace RHToolkit.ViewModels.Windows
{
    public partial class RandomRuneEditorViewModel : ObservableObject, IRecipient<ItemDataMessage>, IRecipient<DataRowViewMessage>
    {
        private readonly Guid _token;
        private readonly IWindowsService _windowsService;
        private readonly IFrameService _frameService;
        private readonly System.Timers.Timer _filterUpdateTimer;

        public RandomRuneEditorViewModel(IWindowsService windowsService, IFrameService frameService, ItemDataManager itemDataManager)
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

            PopulateChangeTypeItems();

            WeakReferenceMessenger.Default.Register<ItemDataMessage>(this);
            WeakReferenceMessenger.Default.Register<DataRowViewMessage>(this);
        }

        #region Commands 

        #region File

        [RelayCommand]
        private async Task LoadFile()
        {
            try
            {
                await CloseFile();

                bool isLoaded = await DataTableManager.LoadFileFromPath("randomrune.rh", null, "nRuneGrade", "Rune");

                if (isLoaded)
                {
                    IsLoaded();
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

                string filter = "randomrune.rh|" +
                                "randomrune.rh|" +
                                "All Files (*.*)|*.*";

                OpenFileDialog openFileDialog = new()
                {
                    Filter = filter,
                    FilterIndex = 1
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    string fileName = Path.GetFileName(openFileDialog.FileName);

                    bool isLoaded = await DataTableManager.LoadFileAs(openFileDialog.FileName, null, "nRuneGrade", "Rune");

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
            Title = $"Random Rune Editor ({DataTableManager.CurrentFileName})";
            OpenMessage = "";
            IsVisible = Visibility.Visible;
            OnCanExecuteFileCommandChanged();
        }

        [RelayCommand(CanExecute = nameof(CanExecuteFileCommand))]
        private void OpenSearchDialog(string? parameter)
        {
            try
            {
                Window? shopEditorWindow = Application.Current.Windows.OfType<RandomRuneEditorWindow>().FirstOrDefault();
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
            Title = $"Random Rune Editor";
            OpenMessage = "Open a file";
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

        #region Add RuneItem

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
                        ItemId = RuneItems[slotIndex].ItemCode,
                        ItemAmount = RuneItems[slotIndex].ItemCodeCount
                    };

                    var token = _token;

                    _windowsService.OpenItemWindow(token, "RandomRune", itemData);
                }

            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"Error: {ex.Message}", "Error");
            }
        }

        public void Receive(ItemDataMessage message)
        {
            if (message.Recipient == "RandomRuneEditorWindow" && message.Token == _token)
            {
                var itemData = message.Value;

                if (DataTableManager.SelectedItem != null && itemData.SlotIndex >= 0 && itemData.SlotIndex < 10)
                {
                    UpdateRuneItem(itemData);
                }
            }
        }

        private void UpdateRuneItem(ItemData itemData)
        {
            if (itemData.ItemId != 0)
            {
                var frameViewModel = ItemDataManager.GetFrameViewModel(itemData.ItemId, itemData.SlotIndex, itemData.ItemAmount);
                RuneItems[itemData.SlotIndex].ItemCode = itemData.ItemId;
                RuneItems[itemData.SlotIndex].ItemCodeCount = itemData.ItemAmount;
                RuneItems[itemData.SlotIndex].FrameViewModel = frameViewModel;
                OnPropertyChanged(nameof(RuneItems));
            }
        }

        [RelayCommand(CanExecute = nameof(CanExecuteFileCommand))]
        private void AddRow()
        {
            try
            {
                DataTableManager.AddNewRow();
                RuneDesc = "New";
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"Error: {ex.Message}", "Error");
            }
        }
        #endregion

        #region Remove RuneItem

        [RelayCommand]
        private void RemoveItem(string parameter)
        {
            try
            {
                if (int.TryParse(parameter, out int slotIndex))
                {
                    RemoveRuneItem(slotIndex);
                }
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"Error: {ex.Message}", "Error");
            }
        }

        private void RemoveRuneItem(int slotIndex)
        {
            if (slotIndex >= 0 && slotIndex < 10 && RuneItems[slotIndex].ItemCode != 0)
            {
                DataTableManager.StartGroupingEdits();
                RuneItems[slotIndex].ItemCode = 0;
                RuneItems[slotIndex].ItemCount = 0;
                RuneItems[slotIndex].ItemCodeCount = 0;
                RuneItems[slotIndex].FrameViewModel = null;
                OnPropertyChanged(nameof(RuneItems));
                DataTableManager.EndGroupingEdits();
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
            RuneItems?.Clear();

            if (selectedItem != null)
            {
                RuneId = (int)selectedItem["nid"];
                RuneDesc = (string)selectedItem["wszDisc"];
                ChangeType = (int)selectedItem["nChangeType"] == 1;
                RandomRuneGroup = (int)selectedItem["nRandomRuneGroup"];
                RuneGrade = (int)selectedItem["nRuneGrade"];
                RandomGroupProbability = (float)selectedItem["fRandomGroupProbability"];
                MaxCount = (int)selectedItem["nMaxCount"];

                RuneItems = [];

                for (int i = 0; i < 10; i++)
                {
                    var item = new RuneItem
                    {
                        ItemCode = (int)selectedItem[$"nItemCode{i:00}"],
                        ItemCodeCount = (int)selectedItem[$"nItemCodeCount{i:00}"],
                        ItemCount = (int)selectedItem[$"nItemCount{i:00}"],
                        FrameViewModel = ItemDataManager.GetFrameViewModel((int)selectedItem[$"nItemCode{i:00}"], i, (int)selectedItem[$"nItemCodeCount{i:00}"]),
                        IsEnabled = (int)selectedItem[$"nItemCode{i:00}"] != 0
                    };

                    RuneItems.Add(item);

                    SetItemPropertyChanged(item, i);
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

        private void SetItemPropertyChanged(RuneItem item, int index)
        {
            item.PropertyChanged += (s, e) =>
            {
                if (s is RuneItem runeItem)
                {
                    OnRuneItemCodeChanged(runeItem.ItemCode, $"nItemCode{index:00}", index);
                    OnRuneItemCountChanged(runeItem.ItemCodeCount, $"nItemCodeCount{index:00}", index);
                    UpdateSelectedItemValue(runeItem.ItemCount, $"nItemCount{index:00}");
                }
            };
        }

        #endregion

        #region Filter

        private void ApplyFilter()
        {
            List<string> filterParts = [];

            // Category filters
            if (ChangeTypeFilter != -1)
            {
                filterParts.Add($"nChangeType = {ChangeTypeFilter}");
            }

            List<string> columns = [];

            columns.Add("CONVERT(nid, 'System.String')");
            columns.Add("wszDisc");
            columns.Add("CONVERT(nRandomRuneGroup, 'System.String')");
            columns.Add("CONVERT(nRuneGrade, 'System.String')");

            for (int i = 0; i < 10; i++)
            {
                string columnName = $"nItemCode{i:00}";
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

        #region Comboboxes
        [ObservableProperty]
        private List<NameID>? _changeTypeItemsFilter;

        private void PopulateChangeTypeItems()
        {
            ChangeTypeItemsFilter =
                [
                    new NameID { ID = -1, Name = "All" },
                    new NameID { ID = 0, Name = "0" },
                    new NameID { ID = 1, Name = "1" }
                ];

            ChangeTypeFilter = -1;
        }

        [ObservableProperty]
        private int _changeTypeFilter;

        partial void OnChangeTypeFilterChanged(int value)
        {
            TriggerFilterUpdate();
        }

        #endregion

        #region Properties
        [ObservableProperty]
        private string _title = $"Random Rune Editor";

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
        private ObservableCollection<RuneItem> _runeItems = [];

        [ObservableProperty]
        private int _runeId;
        partial void OnRuneIdChanged(int value)
        {
            UpdateSelectedItemValue(value, "nid");
        }

        [ObservableProperty]
        private string? _runeDesc;
        partial void OnRuneDescChanged(string? value)
        {
            UpdateSelectedItemValue(value, "wszDisc");
        }

        [ObservableProperty]
        private bool _changeType;
        partial void OnChangeTypeChanged(bool value)
        {
            UpdateSelectedItemValue(value, "nChangeType");
        }

        [ObservableProperty]
        private int _randomRuneGroup;
        partial void OnRandomRuneGroupChanged(int value)
        {
            UpdateSelectedItemValue(value, "nRandomRuneGroup");
        }

        [ObservableProperty]
        private int _runeGrade;
        partial void OnRuneGradeChanged(int value)
        {
            UpdateSelectedItemValue(value, "nRuneGrade");
        }

        [ObservableProperty]
        private double _randomGroupProbability;
        partial void OnRandomGroupProbabilityChanged(double value)
        {
            UpdateSelectedItemValue(value, "fRandomGroupProbability");
        }

        [ObservableProperty]
        private int _MaxCount;

        partial void OnMaxCountChanged(int value)
        {
            if (_isUpdating) return;

            _isUpdating = true;

            try
            {
                DataTableManager.StartGroupingEdits();
                UpdateSelectedItemValue(value, "nMaxCount");
                RecalculateItemCount(value);
                DataTableManager.EndGroupingEdits();
            }
            finally
            {
                _isUpdating = false;
            }
        }

        private void OnRuneItemCodeChanged(int newValue, string column, int index)
        {
            if (_isUpdatingSelectedItem)
                return;

            DataTableManager.UpdateSelectedItemValue(newValue, column);
            IsEnabled(index);
        }

        private bool _isUpdating = false;

        private void OnRuneItemCountChanged(int newValue, string column, int index)
        {
            if (_isUpdating) return;

            _isUpdating = true;

            try
            {
                var frameViewModel = RuneItems[index].FrameViewModel;
                if (frameViewModel != null)
                {
                    frameViewModel.ItemAmount = newValue;
                    RuneItems[index].FrameViewModel = frameViewModel;
                }
                
                UpdateSelectedItemValue(newValue, column);
                CalculateMaxCount();
            }
            finally
            {
                _isUpdating = false;
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

        private void CalculateMaxCount()
        {
            // Sum all ItemCount values
            int totalRuneItemCount = RuneItems.Sum(item => item.ItemCount);

            if (MaxCount != totalRuneItemCount)
            {
                MaxCount = totalRuneItemCount;
            }
        }

        private void RecalculateItemCount(int value)
        {
            if (DataTableManager.SelectedItem != null)
            {
                int currentTotal = RuneItems.Sum(item => item.ItemCount);
                if (currentTotal > 0)
                {
                    int scale = value / currentTotal;
                    foreach (var item in RuneItems)
                    {
                        if (item.ItemCode > 0)
                        {
                            item.ItemCount *= scale;
                        }
                    }
                }
            }

            OnPropertyChanged(nameof(RuneItems));
        }

        private void IsEnabled(int index)
        {
            RuneItems[index].IsEnabled = RuneItems[index].ItemCode != 0;
            OnPropertyChanged(nameof(RuneItems));
        }

        #endregion
    }
}
