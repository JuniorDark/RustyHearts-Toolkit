using Microsoft.Win32;
using RHToolkit.Messages;
using RHToolkit.Models;
using RHToolkit.Models.Database;
using RHToolkit.Models.Editor;
using RHToolkit.Models.MessageBox;
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
                Interval = 500,
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
                RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
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
                RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
            }
        }

        private void IsLoaded()
        {
            Title = string.Format(Resources.EditorTitleFileName, "Random Rune", DataTableManager.CurrentFileName);
            OpenMessage = "";
            IsVisible = Visibility.Visible;
            OnCanExecuteFileCommandChanged();
        }

        [RelayCommand(CanExecute = nameof(CanExecuteFileCommand))]
        private void OpenSearchDialog(string? parameter)
        {
            try
            {
                Window? window = Application.Current.Windows.OfType<RandomRuneEditorWindow>().FirstOrDefault();
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
            Title = string.Format(Resources.EditorTitle, "Random Rune");
            OpenMessage = Resources.OpenFile;
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
                RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
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
                var itemDataViewModel = ItemDataManager.GetItemDataViewModel(itemData.ItemId, itemData.SlotIndex, itemData.ItemAmount);
                RuneItems[itemData.SlotIndex].ItemCode = itemData.ItemId;
                RuneItems[itemData.SlotIndex].ItemCodeCount = itemData.ItemAmount;
                RuneItems[itemData.SlotIndex].ItemDataViewModel = itemDataViewModel;
                OnPropertyChanged(nameof(RuneItems));
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
                RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
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
                RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
            }
        }

        private void RemoveRuneItem(int slotIndex)
        {
            if (slotIndex >= 0 && slotIndex < 10 && RuneItems[slotIndex].ItemCode != 0)
            {
                var item = RuneItems[slotIndex];

                DataTableManager.StartGroupingEdits();
                item.ItemCode = 0;
                item.ItemCount = 0;
                item.ItemCodeCount = 0;
                item.ItemDataViewModel = null;
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

            if (selectedItem != null)
            {
                RuneItems ??= [];

                for (int i = 0; i < 10; i++)
                {
                    var itemCode = (int)selectedItem[$"nItemCode{i:00}"];
                    var itemCodeCount = (int)selectedItem[$"nItemCodeCount{i:00}"];
                    var itemCount = (int)selectedItem[$"nItemCount{i:00}"];
                    var itemDataViewModel = ItemDataManager.GetItemDataViewModel(itemCode, i, itemCodeCount);
                    var isEnabled = (int)selectedItem[$"nItemCode{i:00}"] != 0;

                    if (i < RuneItems.Count)
                    {
                        var existingItem = RuneItems[i];

                        existingItem.ItemCode = itemCode;
                        existingItem.ItemCodeCount = itemCodeCount;
                        existingItem.ItemCount = itemCount;
                        existingItem.ItemDataViewModel = itemDataViewModel;
                        existingItem.IsEnabled = isEnabled;
                    }
                    else
                    {
                        var item = new RuneItem
                        {
                            ItemCode = itemCode,
                            ItemCodeCount = itemCodeCount,
                            ItemCount = itemCount,
                            ItemDataViewModel = itemDataViewModel,
                            IsEnabled = isEnabled
                        };

                        RuneItems.Add(item);

                        SetItemPropertyChanged(item, i);
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

        private void SetItemPropertyChanged(RuneItem item, int index)
        {
            item.PropertyChanged += (s, e) =>
            {
                if (s is RuneItem runeItem)
                {
                    OnRuneItemCodeChanged(runeItem.ItemCode, $"nItemCode{index:00}", index);
                    OnRuneItemCodeCountChanged(runeItem.ItemCodeCount, $"nItemCodeCount{index:00}", index);
                    OnRuneItemCountChanged(runeItem.ItemCount, $"nItemCount{index:00}", index);
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
                    new NameID { ID = -1, Name = Resources.All },
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
        private string _title = string.Format(Resources.EditorTitle, "Random Rune");

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
        private ObservableCollection<RuneItem> _runeItems = [];

        private void OnRuneItemCodeChanged(int newValue, string column, int index)
        {
            if (_isUpdatingSelectedItem)
                return;

            DataTableManager.UpdateSelectedItemValue(newValue, column);
            IsEnabled(index);
        }

        private void OnRuneItemCodeCountChanged(int newValue, string column, int index)
        {
            if (_isUpdatingSelectedItem) return;

            var itemDataViewModel = RuneItems[index].ItemDataViewModel;
            if (itemDataViewModel != null)
            {
                itemDataViewModel.ItemAmount = newValue;
                RuneItems[index].ItemDataViewModel = itemDataViewModel;
            }

            UpdateSelectedItemValue(newValue, column);
        }

        private void OnRuneItemCountChanged(int newValue, string column, int index)
        {
            if (_isUpdatingSelectedItem) return;
            UpdateSelectedItemValue(newValue, column);
            CalculateMaxCount();
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
            UpdateSelectedItemValue(totalRuneItemCount, "nMaxCount");
        }

        private void IsEnabled(int index)
        {
            RuneItems[index].IsEnabled = RuneItems[index].ItemCode != 0;
            OnPropertyChanged(nameof(RuneItems));
        }

        #endregion
    }
}
