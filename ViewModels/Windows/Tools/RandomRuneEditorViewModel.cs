using RHToolkit.Messages;
using RHToolkit.Models;
using RHToolkit.Models.Database;
using RHToolkit.Models.Editor;
using RHToolkit.Models.MessageBox;
using RHToolkit.Properties;
using RHToolkit.Services;
using RHToolkit.ViewModels.Controls;
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
        private async Task LoadFile(string? parameter)
        {
            try
            {
                await CloseFile();

                string filter = "randomrune.rh|randomrune.rh|All Files (*.*)|*.*";
                bool isLoaded = await DataTableManager.LoadFile(filter, null, "nRuneGrade", "Rune");

                if (isLoaded)
                {
                    Title = $"Random Rune Editor ({DataTableManager.CurrentFileName})";
                    OpenMessage = "";
                    IsVisible = Visibility.Visible;
                    OnCanExecuteFileCommandChanged();
                }
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"Error: {ex.Message}", Resources.Error);
            }
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
            CreateItemCommand.NotifyCanExecuteChanged();
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
                    if (DataTableManager.SelectedItem != null)
                    {
                        var itemData = new ItemData
                        {
                            SlotIndex = slotIndex,
                            ItemId = (int)DataTableManager.SelectedItem[$"nItemCode0{slotIndex - 1}"]
                        };

                        var token = _token;

                        _windowsService.OpenItemWindow(token, "RandomRune", itemData);
                    }
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

                if (DataTableManager.SelectedItem != null)
                {
                    switch (itemData.SlotIndex)
                    {
                        case 1:
                            RuneItemCode00 = itemData.ItemId;
                            break;
                        case 2:
                            RuneItemCode01 = itemData.ItemId;
                            break;
                        case 3:
                            RuneItemCode02 = itemData.ItemId;
                            break;
                        case 4:
                            RuneItemCode03 = itemData.ItemId;
                            break;
                        case 5:
                            RuneItemCode04 = itemData.ItemId;
                            break;
                        case 6:
                            RuneItemCode05 = itemData.ItemId;
                            break;
                        case 7:
                            RuneItemCode06 = itemData.ItemId;
                            break;
                        case 8:
                            RuneItemCode07 = itemData.ItemId;
                            break;
                        case 9: 
                            RuneItemCode08 = itemData.ItemId;
                            break;
                        case 10:
                            RuneItemCode09 = itemData.ItemId;
                            break;
                    }
                }
            }
        }

        [RelayCommand(CanExecute = nameof(CanExecuteFileCommand))]
        private void CreateItem()
        {
            if (DataTableManager.DataTable != null)
            {
                DataTableManager.AddNewRow();
                RuneDesc = "New";
            }
        }

        private void SetFrameViewModelData(int itemId, int slotIndex)
        {
            RemoveFrameViewModel(slotIndex);

            if (itemId != 0)
            {
                ItemData itemData = new()
                {
                    ItemId = itemId,
                    SlotIndex = slotIndex
                };

                var frameViewModel = ItemDataManager.GetItemData(itemData);
                
                FrameViewModels ??= [];
                FrameViewModels.Add(frameViewModel);
                OnPropertyChanged(nameof(FrameViewModels));
            }

        }
        #endregion

        #region Remove Item

        [RelayCommand]
        private void RemoveItem(string parameter)
        {
            if (int.TryParse(parameter, out int slotIndex))
            {
                switch (slotIndex)
                {
                    case 1:
                        RuneItemCount00 = 0;
                        RuneItemCode00 = 0;
                        break;
                    case 2:
                        RuneItemCount01 = 0;
                        RuneItemCode01 = 0;
                        break;
                    case 3:
                        RuneItemCount02 = 0;
                        RuneItemCode02 = 0;
                        break;
                    case 4:
                        RuneItemCount03 = 0;
                        RuneItemCode03 = 0;
                        break;
                    case 5:
                        RuneItemCount04 = 0;
                        RuneItemCode04 = 0;
                        break;
                    case 6:
                        RuneItemCount05 = 0;
                        RuneItemCode05 = 0;
                        break;
                    case 7:
                        RuneItemCount06 = 0;
                        RuneItemCode06 = 0;
                        break;
                    case 8:
                        RuneItemCount07 = 0;
                        RuneItemCode07 = 0;
                        break;
                    case 9:
                        RuneItemCount08 = 0;
                        RuneItemCode08 = 0;
                        break;
                    case 10:
                        RuneItemCount09 = 0;
                        RuneItemCode09 = 0;
                        break;
                }

                RemoveFrameViewModel(slotIndex);
            }
        }

        private void RemoveFrameViewModel(int slotIndex)
        {
            if (FrameViewModels != null)
            {
                var removedItemIndex = FrameViewModels.FindIndex(i => i.SlotIndex == slotIndex);
                if (removedItemIndex != -1)
                {
                    FrameViewModels.RemoveAt(removedItemIndex);
                }
                OnPropertyChanged(nameof(FrameViewModels));
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
            FrameViewModels?.Clear();

            if (selectedItem != null)
            {
                IsSelectedItemVisible = Visibility.Visible;
                RuneId = (int)selectedItem["nid"];
                RuneDesc = (string)selectedItem["wszDisc"];
                ChangeType = (int)selectedItem["nChangeType"];
                RandomRuneGroup = (int)selectedItem["nRandomRuneGroup"];
                RuneGrade = (int)selectedItem["nRuneGrade"];
                RandomGroupProbability = (float)selectedItem["fRandomGroupProbability"];
                MaxCount = (int)selectedItem["nMaxCount"];

                RuneItemCode00 = (int)selectedItem["nItemCode00"];
                RuneItemCode01 = (int)selectedItem["nItemCode01"];
                RuneItemCode02 = (int)selectedItem["nItemCode02"];
                RuneItemCode03 = (int)selectedItem["nItemCode03"];
                RuneItemCode04 = (int)selectedItem["nItemCode04"];
                RuneItemCode05 = (int)selectedItem["nItemCode05"];
                RuneItemCode06 = (int)selectedItem["nItemCode06"];
                RuneItemCode07 = (int)selectedItem["nItemCode07"];
                RuneItemCode08 = (int)selectedItem["nItemCode08"];
                RuneItemCode09 = (int)selectedItem["nItemCode09"];

                RuneItemCount00 = (int)selectedItem["nItemCount00"];
                RuneItemCount01 = (int)selectedItem["nItemCount01"];
                RuneItemCount02 = (int)selectedItem["nItemCount02"];
                RuneItemCount03 = (int)selectedItem["nItemCount03"];
                RuneItemCount04 = (int)selectedItem["nItemCount04"];
                RuneItemCount05 = (int)selectedItem["nItemCount05"];
                RuneItemCount06 = (int)selectedItem["nItemCount06"];
                RuneItemCount07 = (int)selectedItem["nItemCount07"];
                RuneItemCount08 = (int)selectedItem["nItemCount08"];
                RuneItemCount09 = (int)selectedItem["nItemCount09"];
            }
            else
            {
                IsSelectedItemVisible = Visibility.Hidden;
            }

            _isUpdatingSelectedItem = false;

            OnCanExecuteSelectedItemCommandChanged();
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

            string[] columns =
                    [
                        "CONVERT(nid, 'System.String')",
                        "wszDisc",
                        "CONVERT(nRandomRuneGroup, 'System.String')",
                        "CONVERT(nRuneGrade, 'System.String')",
                        "CONVERT(nItemCode00, 'System.String')",
                        "CONVERT(nItemCode01, 'System.String')",
                        "CONVERT(nItemCode02, 'System.String')",
                        "CONVERT(nItemCode03, 'System.String')",
                        "CONVERT(nItemCode04, 'System.String')",
                        "CONVERT(nItemCode05, 'System.String')",
                        "CONVERT(nItemCode06, 'System.String')",
                        "CONVERT(nItemCode07, 'System.String')",
                        "CONVERT(nItemCode08, 'System.String')",
                        "CONVERT(nItemCode09, 'System.String')"
                    ];

            DataTableManager.ApplyFileDataFilter(filterParts, columns, SearchText, MatchCase);
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
        private List<NameID>? _changeTypeItems;
        [ObservableProperty]
        private List<NameID>? _changeTypeItemsFilter;

        private void PopulateChangeTypeItems()
        {
            ChangeTypeItems =
                [
                    new NameID { ID = 0, Name = "0" },
                    new NameID { ID = 1, Name = "1" }
                ];
            ChangeTypeItemsFilter =
                [
                    new NameID { ID = -1, Name = "All" },
                    new NameID { ID = 0, Name = "0" },
                    new NameID { ID = 1, Name = "1" }
                ];

            ChangeTypeFilter = -1;
            ChangeType = 0;
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
        private List<FrameViewModel>? _frameViewModels;

        private bool _isUpdatingSelectedItem = false;

        [ObservableProperty]
        private int _runeId;
        partial void OnRuneIdChanged(int value)
        {
            if (!_isUpdatingSelectedItem && DataTableManager.SelectedItem != null)
            {
                DataTableManager.SelectedItem["nid"] = value;
            }
        }

        [ObservableProperty]
        private string? _runeDesc;
        partial void OnRuneDescChanged(string? value)
        {
            if (!_isUpdatingSelectedItem && DataTableManager.SelectedItem != null)
            {
                DataTableManager.SelectedItem["wszDisc"] = value;
            }
        }

        [ObservableProperty]
        private int _changeType;
        partial void OnChangeTypeChanged(int value)
        {
            if (!_isUpdatingSelectedItem && DataTableManager.SelectedItem != null)
            {
                DataTableManager.SelectedItem["nChangeType"] = value;
            }
        }

        [ObservableProperty]
        private int _randomRuneGroup;
        partial void OnRandomRuneGroupChanged(int value)
        {
            if (!_isUpdatingSelectedItem && DataTableManager.SelectedItem != null)
            {
                DataTableManager.SelectedItem["nRandomRuneGroup"] = value;
            }
        }

        [ObservableProperty]
        private int _runeGrade;
        partial void OnRuneGradeChanged(int value)
        {
            if (!_isUpdatingSelectedItem && DataTableManager.SelectedItem != null)
            {
                DataTableManager.SelectedItem["nRuneGrade"] = value;
            }
        }

        [ObservableProperty]
        private double _randomGroupProbability;
        partial void OnRandomGroupProbabilityChanged(double value)
        {
            if (!_isUpdatingSelectedItem && DataTableManager.SelectedItem != null)
            {
                DataTableManager.SelectedItem["fRandomGroupProbability"] = value;
            }
        }

        [ObservableProperty]
        private int _MaxCount;
        partial void OnMaxCountChanged(int value)
        {
            if (!_isUpdatingSelectedItem && DataTableManager.SelectedItem != null)
            {
                DataTableManager.SelectedItem["nMaxCount"] = value;
            }
        }

        [ObservableProperty]
        private int _runeItemCode00;
        partial void OnRuneItemCode00Changed(int value) => OnRuneItemCodeChanged(0, value);

        [ObservableProperty]
        private int _runeItemCode01;
        partial void OnRuneItemCode01Changed(int value) => OnRuneItemCodeChanged(1, value);

        [ObservableProperty]
        private int _runeItemCode02;
        partial void OnRuneItemCode02Changed(int value) => OnRuneItemCodeChanged(2, value);

        [ObservableProperty]
        private int _runeItemCode03;
        partial void OnRuneItemCode03Changed(int value) => OnRuneItemCodeChanged(3, value);

        [ObservableProperty]
        private int _runeItemCode04;
        partial void OnRuneItemCode04Changed(int value) => OnRuneItemCodeChanged(4, value);

        [ObservableProperty]
        private int _runeItemCode05;
        partial void OnRuneItemCode05Changed(int value) => OnRuneItemCodeChanged(5, value);

        [ObservableProperty]
        private int _runeItemCode06;
        partial void OnRuneItemCode06Changed(int value) => OnRuneItemCodeChanged(6, value);

        [ObservableProperty]
        private int _runeItemCode07;
        partial void OnRuneItemCode07Changed(int value) => OnRuneItemCodeChanged(7, value);

        [ObservableProperty]
        private int _runeItemCode08;
        partial void OnRuneItemCode08Changed(int value) => OnRuneItemCodeChanged(8, value);

        [ObservableProperty]
        private int _runeItemCode09;
        partial void OnRuneItemCode09Changed(int value) => OnRuneItemCodeChanged(9, value);

        private void OnRuneItemCodeChanged(int index, int value)
        {
            if (!_isUpdatingSelectedItem && DataTableManager.SelectedItem != null)
            {
                DataTableManager.SelectedItem[$"nItemCode{index:00}"] = value;
            }

            SetFrameViewModelData(value, index + 1);
        }

        [ObservableProperty]
        private int _runeItemCount00;
        partial void OnRuneItemCount00Changed(int value) => OnRuneItemCountChanged(0, value);

        [ObservableProperty]
        private int _runeItemCount01;
        partial void OnRuneItemCount01Changed(int value) => OnRuneItemCountChanged(1, value);

        [ObservableProperty]
        private int _runeItemCount02;
        partial void OnRuneItemCount02Changed(int value) => OnRuneItemCountChanged(2, value);

        [ObservableProperty]
        private int _runeItemCount03;
        partial void OnRuneItemCount03Changed(int value) => OnRuneItemCountChanged(3, value);

        [ObservableProperty]
        private int _runeItemCount04;
        partial void OnRuneItemCount04Changed(int value) => OnRuneItemCountChanged(4, value);

        [ObservableProperty]
        private int _runeItemCount05;
        partial void OnRuneItemCount05Changed(int value) => OnRuneItemCountChanged(5, value);

        [ObservableProperty]
        private int _runeItemCount06;
        partial void OnRuneItemCount06Changed(int value) => OnRuneItemCountChanged(6, value);

        [ObservableProperty]
        private int _runeItemCount07;
        partial void OnRuneItemCount07Changed(int value) => OnRuneItemCountChanged(7, value);

        [ObservableProperty]
        private int _runeItemCount08;
        partial void OnRuneItemCount08Changed(int value) => OnRuneItemCountChanged(8, value);

        [ObservableProperty]
        private int _runeItemCount09;
        partial void OnRuneItemCount09Changed(int value) => OnRuneItemCountChanged(9, value);

        private void OnRuneItemCountChanged(int index, int value)
        {
            if (!_isUpdatingSelectedItem && DataTableManager.SelectedItem != null)
            {
                DataTableManager.SelectedItem[$"nItemCount{index:00}"] = value;

                RecalculateMaxCount();
            }
        }

        #endregion

        #endregion

        #region Properties Helper

        private void RecalculateMaxCount()
        {
            // Sum all RuneItemCount values
            int totalRuneItemCount = RuneItemCount00 + RuneItemCount01 + RuneItemCount02 + RuneItemCount03 + RuneItemCount04 + RuneItemCount05 + RuneItemCount06 + RuneItemCount07 + RuneItemCount08 + RuneItemCount09;

            MaxCount = totalRuneItemCount;
        }

        #endregion
    }
}
