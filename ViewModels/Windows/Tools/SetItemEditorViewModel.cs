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
    public partial class SetItemEditorViewModel : ObservableObject, IRecipient<ItemDataMessage>, IRecipient<DataRowViewMessage>
    {
        private readonly Guid _token;
        private readonly IWindowsService _windowsService;
        private readonly IFrameService _frameService;
        private readonly System.Timers.Timer _searchTimer;

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
            _searchTimer = new()
            {
                Interval = 500,
                AutoReset = false
            };
            _searchTimer.Elapsed += SearchTimerElapsed;

            WeakReferenceMessenger.Default.Register<ItemDataMessage>(this);
            WeakReferenceMessenger.Default.Register<DataRowViewMessage>(this);
        }

        #region Commands 
        [RelayCommand]
        private async Task CloseWindow(Window window)
        {
            try
            {
                await CloseFile();

                window?.Close();

            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"Error: {ex.Message}", Resources.Error);
            }
        }

        #region File

        [RelayCommand]
        private async Task LoadFile()
        {
            try
            {
                await CloseFile();

                string filter = "setitem.rh|setitem.rh|All Files (*.*)|*.*";
                bool isLoaded = await DataTableManager.LoadFile(filter, "setitem_string.rh", "nSetItemID00", "Set Item");

                if (isLoaded)
                {
                    Title = $"Set Item Editor ({DataTableManager.CurrentFileName})";
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
                            ItemId = (int)DataTableManager.SelectedItem[$"nSetItemID0{slotIndex - 1}"]
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

                if (DataTableManager.SelectedItem != null)
                {
                    switch (itemData.SlotIndex)
                    {
                        case 1:
                            SetItemID00 = itemData.ItemId;
                            break;
                        case 2:
                            SetItemID01 = itemData.ItemId;
                            break;
                        case 3:
                            SetItemID02 = itemData.ItemId;
                            break;
                        case 4:
                            SetItemID03 = itemData.ItemId;
                            break;
                        case 5:
                            SetItemID04 = itemData.ItemId;
                            break;
                        case 6:
                            SetItemID05 = itemData.ItemId;
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
                SetName = "New Set";

                if (DataTableManager.SelectedItemString != null)
                {
                    SetNameString = "New Set";
                }
            }
        }

        private void SetFrameViewModelData(int itemId, int slotIndex)
        {
            if (itemId != 0)
            {
                RemoveFrameViewModel(slotIndex);

                ItemData itemData = new()
                {
                    ItemId = itemId,
                    SlotIndex = slotIndex,
                };

                var frameViewModel = ItemDataManager.GetItemData(itemData);
                
                FrameViewModels ??= [];
                FrameViewModels.Add(frameViewModel);
                OnPropertyChanged(nameof(FrameViewModels));
            }
            else
            {
                RemoveFrameViewModel(slotIndex);
            }

        }

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
                IsSelectedItemVisible = Visibility.Visible;
                SetID = (int)selectedItem["nID"];
                SetName = (string)selectedItem["wszName"];

                SetItemID00 = (int)selectedItem["nSetItemID00"];
                SetItemID01 = (int)selectedItem["nSetItemID01"];
                SetItemID02 = (int)selectedItem["nSetItemID02"];
                SetItemID03 = (int)selectedItem["nSetItemID03"];
                SetItemID04 = (int)selectedItem["nSetItemID04"];
                SetItemID05 = (int)selectedItem["nSetItemID05"];

                SetOption00 = (int)selectedItem["nSetOption00"];
                SetOption01 = (int)selectedItem["nSetOption01"];
                SetOption02 = (int)selectedItem["nSetOption02"];
                SetOption03 = (int)selectedItem["nSetOption03"];
                SetOption04 = (int)selectedItem["nSetOption04"];

                SetOptionValue00 = (int)selectedItem["nSetOptionvlue00"];
                SetOptionValue01 = (int)selectedItem["nSetOptionvlue01"];
                SetOptionValue02 = (int)selectedItem["nSetOptionvlue02"];
                SetOptionValue03 = (int)selectedItem["nSetOptionvlue03"];
                SetOptionValue04 = (int)selectedItem["nSetOptionvlue04"];

                FormatSetEffect();

                if (DataTableManager.DataTableString != null && DataTableManager.SelectedItemString != null)
                {
                    SetNameString = (string)DataTableManager.SelectedItemString["wszName"];
                }
                else
                {
                    SetNameString = "Missing Name String";
                }
            }
            else
            {
                IsSelectedItemVisible = Visibility.Hidden;
            }

            _isUpdatingSelectedItem = false;

            OnCanExecuteSelectedItemCommandChanged();
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
                        SetItemID00 = 0;
                        break;
                    case 2:
                        SetItemID01 = 0;
                        break;
                    case 3:
                        SetItemID02 = 0;
                        break;
                    case 4:
                        SetItemID03 = 0;
                        break;
                    case 5:
                        SetItemID04 = 0;
                        break;
                    case 6:
                        SetItemID05 = 0;
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

        #region Filter

        private void ApplyFileDataFilter()
        {
            if (DataTableManager.DataTable != null)
            {
                List<string> filterParts = [];

                // Text search filter
                if (!string.IsNullOrEmpty(SearchText))
                {
                    string searchText = SearchText.ToLower();
                    filterParts.Add($"(CONVERT(nID, 'System.String') LIKE '%{searchText}%' OR wszName LIKE '%{searchText}%' OR CONVERT(nSetItemID00, 'System.String') LIKE '%{searchText}%' OR CONVERT(nSetItemID01, 'System.String') LIKE '%{searchText}%' OR CONVERT(nSetItemID02, 'System.String') LIKE '%{searchText}%' OR CONVERT(nSetItemID03, 'System.String') LIKE '%{searchText}%' OR CONVERT(nSetItemID04, 'System.String') LIKE '%{searchText}%' OR CONVERT(nSetItemID05, 'System.String') LIKE '%{searchText}%' OR CONVERT(nSetOption00, 'System.String') LIKE '%{searchText}%' OR CONVERT(nSetOption01, 'System.String') LIKE '%{searchText}%' OR CONVERT(nSetOption02, 'System.String') LIKE '%{searchText}%' OR CONVERT(nSetOption03, 'System.String') LIKE '%{searchText}%' OR CONVERT(nSetOption04, 'System.String') LIKE '%{searchText}%')");
                }

                string filter = string.Join(" AND ", filterParts);

                DataTableManager.DataTable.DefaultView.RowFilter = filter;
            }
        }

        [ObservableProperty]
        private string? _searchText;
        partial void OnSearchTextChanged(string? value)
        {
            _searchTimer.Stop();
            _searchTimer.Start();
        }

        private void SearchTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            if (DataTableManager.DataTable != null)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    _searchTimer.Stop();
                    ApplyFileDataFilter();
                });
            }

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
        private List<FrameViewModel>? _frameViewModels;

        private bool _isUpdatingSelectedItem = false;

        [ObservableProperty]
        private int _setID;
        partial void OnSetIDChanged(int value)
        {
            if (!_isUpdatingSelectedItem && DataTableManager.SelectedItem != null)
            {
                DataTableManager.SelectedItem["nID"] = value;

                if (DataTableManager.SelectedItemString != null)
                {
                    DataTableManager.SelectedItemString["nID"] = value;
                }
            }
        }

        [ObservableProperty]
        private string? _setName;
        partial void OnSetNameChanged(string? value)
        {
            if (!_isUpdatingSelectedItem && DataTableManager.SelectedItem != null)
            {
                DataTableManager.SelectedItem["wszName"] = value;
            }
        }

        [ObservableProperty]
        private string? _setNameString;
        partial void OnSetNameStringChanged(string? value)
        {
            if (!_isUpdatingSelectedItem && DataTableManager.SelectedItemString != null)
            {
                DataTableManager.SelectedItemString["wszName"] = value;
            }
        }

        [ObservableProperty]
        private int _setItemID00;
        partial void OnSetItemID00Changed(int value)
        {
            if (!_isUpdatingSelectedItem && DataTableManager.SelectedItem != null)
            {
                DataTableManager.SelectedItem["nSetItemID00"] = value;
            }

            SetFrameViewModelData(value, 1);
            UpdateSetOptions();
        }

        [ObservableProperty]
        private int _setItemID01;
        partial void OnSetItemID01Changed(int value)
        {
            if (!_isUpdatingSelectedItem && DataTableManager.SelectedItem != null)
            {
                DataTableManager.SelectedItem["nSetItemID01"] = value;
            }

            SetFrameViewModelData(value, 2);
            UpdateSetOptions();
        }

        [ObservableProperty]
        private int _setItemID02;
        partial void OnSetItemID02Changed(int value)
        {
            if (!_isUpdatingSelectedItem && DataTableManager.SelectedItem != null)
            {
                DataTableManager.SelectedItem["nSetItemID02"] = value;
            }

            SetFrameViewModelData(value, 3);
            UpdateSetOptions();
        }

        [ObservableProperty]
        private int _setItemID03;
        partial void OnSetItemID03Changed(int value)
        {
            if (!_isUpdatingSelectedItem && DataTableManager.SelectedItem != null)
            {
                DataTableManager.SelectedItem["nSetItemID03"] = value;
            }

            SetFrameViewModelData(value, 4);
            UpdateSetOptions();
        }

        [ObservableProperty]
        private int _setItemID04;
        partial void OnSetItemID04Changed(int value)
        {
            if (!_isUpdatingSelectedItem && DataTableManager.SelectedItem != null)
            {
                DataTableManager.SelectedItem["nSetItemID04"] = value;
            }

            SetFrameViewModelData(value, 5);
            UpdateSetOptions();
        }

        [ObservableProperty]
        private int _setItemID05;
        partial void OnSetItemID05Changed(int value)
        {
            if (!_isUpdatingSelectedItem && DataTableManager.SelectedItem != null)
            {
                DataTableManager.SelectedItem["nSetItemID05"] = value;
            }

            SetFrameViewModelData(value, 6);
            UpdateSetOptions();
        }

        [ObservableProperty]
        private int _setOption00;
        partial void OnSetOption00Changed(int value)
        {
            if (!_isUpdatingSelectedItem && DataTableManager.SelectedItem != null)
            {
                DataTableManager.SelectedItem["nSetOption00"] = value;

                if (value == 0)
                {
                    SetOptionValue00 = 0;
                }
                FormatSetEffect();
            }
        }

        [ObservableProperty]
        private int _setOption01;
        partial void OnSetOption01Changed(int value)
        {
            if (!_isUpdatingSelectedItem && DataTableManager.SelectedItem != null)
            {
                DataTableManager.SelectedItem["nSetOption01"] = value;

                if (value == 0)
                {
                    SetOptionValue01 = 0;
                }
                FormatSetEffect();
            }
        }

        [ObservableProperty]
        private int _setOption02;
        partial void OnSetOption02Changed(int value)
        {
            if (!_isUpdatingSelectedItem && DataTableManager.SelectedItem != null)
            {
                DataTableManager.SelectedItem["nSetOption02"] = value;

                if (value == 0)
                {
                    SetOptionValue02 = 0;
                }
                FormatSetEffect();
            }
        }

        [ObservableProperty]
        private int _setOption03;
        partial void OnSetOption03Changed(int value)
        {
            if (!_isUpdatingSelectedItem && DataTableManager.SelectedItem != null)
            {
                DataTableManager.SelectedItem["nSetOption03"] = value;

                if (value == 0)
                {
                    SetOptionValue03 = 0;
                }
                FormatSetEffect();
            }
            
        }

        [ObservableProperty]
        private int _setOption04;
        partial void OnSetOption04Changed(int value)
        {
            if (!_isUpdatingSelectedItem && DataTableManager.SelectedItem != null)
            {
                DataTableManager.SelectedItem["nSetOption04"] = value;

                if (value == 0)
                {
                    SetOptionValue04 = 0;
                }
                FormatSetEffect();
            }
        }

        [ObservableProperty]
        private int _setOptionValue00;
        partial void OnSetOptionValue00Changed(int value)
        {
            if (!_isUpdatingSelectedItem && DataTableManager.SelectedItem != null)
            {
                DataTableManager.SelectedItem["nSetOptionvlue00"] = value;
                FormatSetEffect();
            }
        }

        [ObservableProperty]
        private int _setOptionValue01;
        partial void OnSetOptionValue01Changed(int value)
        {
            if (!_isUpdatingSelectedItem && DataTableManager.SelectedItem != null)
            {
                DataTableManager.SelectedItem["nSetOptionvlue01"] = value;
                FormatSetEffect();
            }
        }

        [ObservableProperty]
        private int _setOptionValue02;
        partial void OnSetOptionValue02Changed(int value)
        {
            if (!_isUpdatingSelectedItem && DataTableManager.SelectedItem != null)
            {
                DataTableManager.SelectedItem["nSetOptionvlue02"] = value;
                FormatSetEffect();
            }
        }

        [ObservableProperty]
        private int _setOptionValue03;
        partial void OnSetOptionValue03Changed(int value)
        {
            if (!_isUpdatingSelectedItem && DataTableManager.SelectedItem != null)
            {
                DataTableManager.SelectedItem["nSetOptionvlue03"] = value;
                FormatSetEffect();
            }
        }

        [ObservableProperty]
        private int _setOptionValue04;
        partial void OnSetOptionValue04Changed(int value)
        {
            if (!_isUpdatingSelectedItem && DataTableManager.SelectedItem != null)
            {
                DataTableManager.SelectedItem["nSetOptionvlue04"] = value;
                FormatSetEffect();
            }
        }

        [ObservableProperty]
        private bool _isEnabled = true;

        [ObservableProperty]
        private bool _isSetOption00Enabled = false;

        [ObservableProperty]
        private bool _isSetOption01Enabled = false;

        [ObservableProperty]
        private bool _isSetOption02Enabled = false;

        [ObservableProperty]
        private bool _isSetOption03Enabled = false;

        [ObservableProperty]
        private bool _isSetOption04Enabled = false;

        #endregion

        #endregion

        #region Properties Helper
        private void FormatSetEffect()
        {
            string setEffect01 = _frameService.GetOptionName(SetOption00, SetOptionValue00);
            string setEffect02 = _frameService.GetOptionName(SetOption01, SetOptionValue01);
            string setEffect03 = _frameService.GetOptionName(SetOption02, SetOptionValue02);
            string setEffect04 = _frameService.GetOptionName(SetOption03, SetOptionValue03);
            string setEffect05 = _frameService.GetOptionName(SetOption04, SetOptionValue04);

            string setEffect = $"{Resources.SetEffect}\n";
            if (SetOption00 != 0)
                setEffect += $"{Resources.Set2}: {setEffect01}\n";
            if (SetOption01 != 0)
                setEffect += $"{Resources.Set3}: {setEffect02}\n";
            if (SetOption02 != 0)
                setEffect += $"{Resources.Set4}: {setEffect03}\n";
            if (SetOption03 != 0)
                setEffect += $"{Resources.Set5}: {setEffect04}\n";
            if (SetOption04 != 0)
                setEffect += $"{Resources.Set6}: {setEffect05}\n";

            SetEffectText = setEffect;
        }

        private void UpdateSetOptions()
        {
            int nonZeroCount = 0;

            if (SetItemID00 != 0) nonZeroCount++;
            if (SetItemID01 != 0) nonZeroCount++;
            if (SetItemID02 != 0) nonZeroCount++;
            if (SetItemID03 != 0) nonZeroCount++;
            if (SetItemID04 != 0) nonZeroCount++;
            if (SetItemID05 != 0) nonZeroCount++;

            IsSetOption00Enabled = nonZeroCount > 1;
            IsSetOption01Enabled = nonZeroCount > 2;
            IsSetOption02Enabled = nonZeroCount > 3;
            IsSetOption03Enabled = nonZeroCount > 4;
            IsSetOption04Enabled = nonZeroCount > 5;
        }

        #endregion
    }
}
