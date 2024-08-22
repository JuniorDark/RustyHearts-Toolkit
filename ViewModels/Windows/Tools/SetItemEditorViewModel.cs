﻿using Microsoft.Win32;
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
    public partial class SetItemEditorViewModel : ObservableObject, IRecipient<ItemDataMessage>, IRecipient<DataRowViewMessage>
    {
        private readonly Guid _token;
        private readonly IWindowsService _windowsService;
        private readonly IFrameService _frameService;
        private readonly System.Timers.Timer _filterUpdateTimer;

        public SetItemEditorViewModel(IWindowsService windowsService, IFrameService frameService, ItemDataManager itemDataManager)
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
        private async Task LoadFile()
        {
            try
            {
                await CloseFile();

                bool isLoaded = await DataTableManager.LoadFileFromPath("setitem.rh", "setitem_string.rh", "nSetItemID00", "Set Item");

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

                string filter = "SetItem.rh|" +
                                "setitem.rh|" +
                                "All Files (*.*)|*.*";

                OpenFileDialog openFileDialog = new()
                {
                    Filter = filter,
                    FilterIndex = 1
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    string fileName = Path.GetFileName(openFileDialog.FileName);

                    bool isLoaded = await DataTableManager.LoadFileAs(openFileDialog.FileName, "setitem_string.rh", "nSetItemID00", "Set Item");

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
            Title = $"Set Item Editor ({DataTableManager.CurrentFileName})";
            OpenMessage = "";
            IsVisible = Visibility.Visible;
            OnCanExecuteFileCommandChanged();
        }

        [RelayCommand(CanExecute = nameof(CanExecuteFileCommand))]
        private void OpenSearchDialog(string? parameter)
        {
            try
            {
                Window? shopEditorWindow = Application.Current.Windows.OfType<SetItemEditorWindow>().FirstOrDefault();
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
            Title = $"Set Item Editor";
            OpenMessage = "Open a file";
            SearchText = "";
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

        #region Add SetItem

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
                            ItemId = SetItems[slotIndex].SetItemID
                        };

                        var token = _token;

                        _windowsService.OpenItemWindow(token, "SetItem", itemData);
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
            if (message.Recipient == "SetItemEditorWindow" && message.Token == _token)
            {
                var itemData = message.Value;

                if (DataTableManager.SelectedItem != null && itemData.SlotIndex >= 0 && itemData.SlotIndex <= 5)
                {
                    UpdateDropGroupItem(itemData.ItemId, itemData.SlotIndex);
                }
            }
        }

        private void UpdateDropGroupItem(int itemId, int slotIndex)
        {
            if (itemId != 0)
            {
                var frameViewModel = ItemDataManager.GetFrameViewModel(itemId, slotIndex);
                SetItems[slotIndex].SetItemID = itemId;
                SetItems[slotIndex].FrameViewModel = frameViewModel;
                OnPropertyChanged(nameof(SetItems));
            }
        }

        [RelayCommand(CanExecute = nameof(CanExecuteFileCommand))]
        private void CreateItem()
        {
            try
            {
                if (DataTableManager.DataTable != null)
                {
                    DataTableManager.AddNewRow();
                    SetName = "New Set";

                    if (DataTableManager.SelectedItemString != null)
                    {
                        SetNameString = "New Set";
                    }
                }
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"Error: {ex.Message}", "Error");
            }
        }
        #endregion

        #region Remove SetItem

        [RelayCommand]
        private void RemoveItem(string parameter)
        {
            try
            {
                if (int.TryParse(parameter, out int slotIndex))
                {
                    RemoveSetItem(slotIndex);
                }
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"Error: {ex.Message}", "Error");
            }
        }

        private void RemoveSetItem(int slotIndex)
        {
            if (slotIndex >= 0 && slotIndex <= 5 && SetItems[slotIndex].SetItemID != 0)
            {
                DataTableManager.StartGroupingEdits();
                SetItems[slotIndex].SetItemID = 0;
                SetItems[slotIndex].FrameViewModel = null;
                OnPropertyChanged(nameof(SetItems));
                DataTableManager.EndGroupingEdits();
            }
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
            SetItems?.Clear();
            SetOptions?.Clear();

            if (selectedItem != null)
            {
                SetID = (int)selectedItem["nID"];
                SetName = (string)selectedItem["wszName"];

                SetItems = [];
                SetOptions = [];

                for (int i = 0; i < 6; i++)
                {
                    var item = new SetItem
                    {
                        SetItemID = (int)selectedItem[$"nSetItemID{i:00}"],
                        FrameViewModel = ItemDataManager.GetFrameViewModel((int)selectedItem[$"nSetItemID{i:00}"], i)
                    };

                    SetItems.Add(item);

                    SetItemPropertyChanged(item, i);
                }

                for (int i = 0; i < 5; i++)
                {
                    var item = new SetItem
                    {
                        SetOption = (int)selectedItem[$"nSetOption{i:00}"],
                        SetOptionValue = (int)selectedItem[$"nSetOptionvlue{i:00}"]
                    };

                    SetOptions.Add(item);

                    SetOptionPropertyChanged(item, i);
                }

                FormatSetEffect();
                UpdateSetOptions();

                if (DataTableManager.DataTableString != null && DataTableManager.SelectedItemString != null)
                {
                    SetNameString = (string)DataTableManager.SelectedItemString["wszName"];
                }
                else
                {
                    SetNameString = "Missing Name String";
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

        private void SetItemPropertyChanged(SetItem item, int index)
        {
            item.PropertyChanged += (s, e) =>
            {
                if (s is SetItem setItem)
                {
                    OnSetItemChanged(setItem.SetItemID, $"nSetItemID{index:00}");
                }
            };
        }

        private void SetOptionPropertyChanged(SetItem item, int index)
        {
            item.PropertyChanged += (s, e) =>
            {
                if (s is SetItem setItem)
                {
                    OnSetOptionChanged(setItem.SetOption, $"nSetOption{index:00}");
                    OnSetOptionValueChanged(setItem.SetOptionValue, $"nSetOptionvlue{index:00}");
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

            columns.Insert(0, "CONVERT(nID, 'System.String')");

            for (int i = 0; i <= 5; i++)
            {
                string columnName = $"nSetItemID{i:00}";
                columns.Add($"CONVERT({columnName}, 'System.String')");
            }
            for (int i = 0; i <= 4; i++)
            {
                string columnName = $"nSetOption{i:00}";
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

        #region Properties
        [ObservableProperty]
        private string _title = $"Set Item Editor";

        [ObservableProperty]
        private string? _openMessage = "Open a file";

        [ObservableProperty]
        private string? _setEffectText;

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

        [ObservableProperty]
        private int _optionMinValue;

        [ObservableProperty]
        private int _optionMaxValue = 10000;

        #region SelectedItem

        [ObservableProperty]
        private ObservableCollection<SetItem> _setItems = [];

        [ObservableProperty]
        private ObservableCollection<SetItem> _setOptions = [];

        [ObservableProperty]
        private int _setID;
        partial void OnSetIDChanged(int value)
        {
            UpdateSelectedItemValue(value, "nID");
        }

        [ObservableProperty]
        private string? _setName;
        partial void OnSetNameChanged(string? value)
        {
            UpdateSelectedItemValue(value, "wszName");
        }

        [ObservableProperty]
        private string? _setNameString;
        partial void OnSetNameStringChanged(string? value)
        {
            UpdateSelectedItemStringValue(value, "wszName");
        }

        private void OnSetItemChanged(int newValue, string column)
        {
            if (!_isUpdatingSelectedItem)
            {
                DataTableManager.UpdateSelectedItemValue(newValue, column);
            }

            UpdateSetOptions();
        }
        
        private void OnSetOptionChanged(int newValue, string column)
        {
            if (!_isUpdatingSelectedItem)
            {
                DataTableManager.UpdateSelectedItemValue(newValue, column);

                FormatSetEffect();
            }
        }

        private void OnSetOptionValueChanged(int newValue, string column)
        {
            if (!_isUpdatingSelectedItem)
            {
                DataTableManager.UpdateSelectedItemValue(newValue, column);
                FormatSetEffect();
            }
        }

        [ObservableProperty]
        private bool _isEnabled = true;

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

        private void UpdateSelectedItemStringValue(object? newValue, string column)
        {
            if (_isUpdatingSelectedItem)
                return;

            DataTableManager.UpdateSelectedItemStringValue(newValue, column);
        }

        private void FormatSetEffect()
        {
            string setEffect01 = _frameService.GetOptionName(SetOptions[0].SetOption, SetOptions[0].SetOptionValue);
            string setEffect02 = _frameService.GetOptionName(SetOptions[1].SetOption, SetOptions[1].SetOptionValue);
            string setEffect03 = _frameService.GetOptionName(SetOptions[2].SetOption, SetOptions[2].SetOptionValue);
            string setEffect04 = _frameService.GetOptionName(SetOptions[3].SetOption, SetOptions[3].SetOptionValue);
            string setEffect05 = _frameService.GetOptionName(SetOptions[4].SetOption, SetOptions[4].SetOptionValue);

            string setEffect = $"{Resources.SetEffect}\n";
            if (SetOptions[0].SetOption != 0)
                setEffect += $"{Resources.Set2}: {setEffect01}\n";
            if (SetOptions[1].SetOption != 0)
                setEffect += $"{Resources.Set3}: {setEffect02}\n";
            if (SetOptions[2].SetOption != 0)
                setEffect += $"{Resources.Set4}: {setEffect03}\n";
            if (SetOptions[3].SetOption != 0)
                setEffect += $"{Resources.Set5}: {setEffect04}\n";
            if (SetOptions[4].SetOption != 0)
                setEffect += $"{Resources.Set6}: {setEffect05}\n";

            SetEffectText = setEffect;
        }

        private void UpdateSetOptions()
        {
            int nonZeroCount = SetItems.Count(item => item.SetItemID != 0);

            for (int i = 0; i < SetOptions.Count; i++)
            {
                bool shouldEnable = nonZeroCount > (i + 1);
                SetOptions[i].IsSetOptionEnabled = shouldEnable;

                if (!shouldEnable)
                {
                    SetOptions[i].SetOption = 0;
                    SetOptions[i].SetOptionValue = 0;
                }
            }
            OnPropertyChanged(nameof(SetOptions));
        }

        #endregion
    }
}
