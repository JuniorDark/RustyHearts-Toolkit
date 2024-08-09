using RHToolkit.Messages;
using RHToolkit.Models;
using RHToolkit.Models.Database;
using RHToolkit.Models.Editor;
using RHToolkit.Models.MessageBox;
using RHToolkit.Properties;
using RHToolkit.Services;
using RHToolkit.Utilities;
using RHToolkit.ViewModels.Controls;
using RHToolkit.Views.Windows;
using System.Data;
using System.Windows.Controls;

namespace RHToolkit.ViewModels.Windows
{
    public partial class PackageEditorViewModel : ObservableObject, IRecipient<ItemDataMessage>, IRecipient<DataRowViewMessage>
    {
        private readonly Guid _token;
        private readonly IWindowsService _windowsService;
        private readonly IFrameService _frameService;
        private readonly System.Timers.Timer _filterUpdateTimer;

        public PackageEditorViewModel(IWindowsService windowsService, IFrameService frameService, ItemDataManager itemDataManager)
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
                Interval = 300,
                AutoReset = false
            };
            _filterUpdateTimer.Elapsed += FilterUpdateTimerElapsed;

            PopulatePackageTypeItems();
            PopulatePackageEffectItems();

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
        private async Task LoadFile(string? parameter)
        {
            try
            {
                await CloseFile();

                string filter = parameter == "1" ? "unionpackage.rh|unionpackage.rh|All Files (*.*)|*.*" : "unionpackage_local.rh|unionpackage_local.rh|All Files (*.*)|*.*";
                string stringTableName = parameter == "1" ? "unionpackage_string.rh" : "unionpackage_local_string.rh";
                bool isLoaded = await DataTableManager.LoadFile(filter, stringTableName, "nPackageType", "Package");

                if (isLoaded)
                {
                    Title = $"Package Editor ({DataTableManager.CurrentFileName})";
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
            Title = $"Package Editor";
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
                            ItemId = (int)DataTableManager.SelectedItem[$"nItemCode0{slotIndex - 1}"],
                            ItemAmount = (int)DataTableManager.SelectedItem[$"nItemCount0{slotIndex - 1}"]
                        };

                        var token = _token;

                        _windowsService.OpenItemWindow(token, "Package", itemData);
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
            if (message.Recipient == "PackageEditorWindow" && message.Token == _token)
            {
                var itemData = message.Value;

                if (DataTableManager.SelectedItem != null)
                {
                    switch (itemData.SlotIndex)
                    {
                        case 1:
                            PackageItemCount00 = itemData.ItemAmount;
                            PackageItemCode00 = itemData.ItemId;
                            break;
                        case 2:
                            PackageItemCount01 = itemData.ItemAmount;
                            PackageItemCode01 = itemData.ItemId;
                            break;
                        case 3:
                            PackageItemCount02 = itemData.ItemAmount;
                            PackageItemCode02 = itemData.ItemId;
                            break;
                        case 4:
                            PackageItemCount03 = itemData.ItemAmount;
                            PackageItemCode03 = itemData.ItemId;
                            break;
                        case 5:
                            PackageItemCount04 = itemData.ItemAmount;
                            PackageItemCode04 = itemData.ItemId;
                            break;
                        case 6:
                            PackageItemCode05 = itemData.ItemId;
                            PackageItemCount05 = itemData.ItemAmount;
                            break;
                        case 7:
                            PackageItemCount06 = itemData.ItemAmount;
                            PackageItemCode06 = itemData.ItemId;
                            break;
                        case 8:
                            PackageItemCount07 = itemData.ItemAmount;
                            PackageItemCode07 = itemData.ItemId;
                            break;
                        case 9: 
                            PackageItemCount08 = itemData.ItemAmount;
                            PackageItemCode08 = itemData.ItemId;
                            break;
                        case 10:
                            PackageItemCount05 = itemData.ItemAmount;
                            PackageItemCode09 = itemData.ItemId;
                            break;
                        case 11:
                            PackageItemCount10 = itemData.ItemAmount;
                            PackageItemCode10 = itemData.ItemId;
                            break;
                        case 12:
                            PackageItemCount11 = itemData.ItemAmount;
                            PackageItemCode11 = itemData.ItemId;
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
                PackageName = "New Package";
                PackageDesc = "New Package Description";

                if (DataTableManager.SelectedItemString != null)
                {
                    PackageNameString = "New Package";
                    PackageDescString = "New Package Description";
                }
            }
        }

        private void SetFrameViewModelData(int itemId, int itemAmount, int slotIndex)
        {
            if (itemId != 0)
            {
                RemoveFrameViewModel(slotIndex);

                ItemData itemData = new()
                {
                    ItemId = itemId,
                    ItemAmount = itemAmount,
                    SlotIndex = slotIndex
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
                        PackageItemCount00 = 0;
                        PackageItemCode00 = 0;
                        break;
                    case 2:
                        PackageItemCount01 = 0;
                        PackageItemCode01 = 0;
                        break;
                    case 3:
                        PackageItemCount02 = 0;
                        PackageItemCode02 = 0;
                        break;
                    case 4:
                        PackageItemCount03 = 0;
                        PackageItemCode03 = 0;
                        break;
                    case 5:
                        PackageItemCount04 = 0;
                        PackageItemCode04 = 0;
                        break;
                    case 6:
                        PackageItemCount05 = 0;
                        PackageItemCode05 = 0;
                        break;
                    case 7:
                        PackageItemCount06 = 0;
                        PackageItemCode06 = 0;
                        break;
                    case 8:
                        PackageItemCount07 = 0;
                        PackageItemCode07 = 0;
                        break;
                    case 9:
                        PackageItemCount08 = 0;
                        PackageItemCode08 = 0;
                        break;
                    case 10:
                        PackageItemCount09 = 0;
                        PackageItemCode09 = 0;
                        break;
                    case 11:
                        PackageItemCount10 = 0;
                        PackageItemCode10 = 0;
                        break;
                    case 12:
                        PackageItemCount11 = 0;
                        PackageItemCode11 = 0;
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
                IsSelectedItemVisible = Visibility.Visible;
                PackageID = (int)selectedItem["nID"];
                PackageName = (string)selectedItem["wszName"];
                PackageDesc = (string)selectedItem["wszDesc"];
                PackageIcon = (string)selectedItem["szPackageIcon"];
                PackageType = (int)selectedItem["nPackageType"];

                EffectRemainTime = (int)selectedItem["nEffectRemainTime"];

                EffectCode00 = (int)selectedItem["nEffectCode00"];
                EffectValue00 = (float)selectedItem["fEffectValue00"];

                EffectCode01 = (int)selectedItem["nEffectCode01"];
                EffectValue01 = (float)selectedItem["fEffectValue01"];

                EffectCode02 = (int)selectedItem["nEffectCode02"];
                EffectValue02 = (float)selectedItem["fEffectValue02"];

                EffectCode03 = (int)selectedItem["nEffectCode03"];
                EffectValue03 = (float)selectedItem["fEffectValue03"];

                EffectCode04 = (int)selectedItem["nEffectCode04"];
                EffectValue04 = (float)selectedItem["fEffectValue04"];

                EffectCode05 = (int)selectedItem["nEffectCode05"];
                EffectValue05 = (float)selectedItem["fEffectValue05"];

                RuneGroup00 = (int)selectedItem["nRuneGroup00"];
                RuneGroup01 = (int)selectedItem["nRuneGroup01"];
                RuneGroup02 = (int)selectedItem["nRuneGroup02"];

                PackageItemCount00 = (int)selectedItem["nItemCount00"];
                PackageItemCount01 = (int)selectedItem["nItemCount01"];
                PackageItemCount02 = (int)selectedItem["nItemCount02"];
                PackageItemCount03 = (int)selectedItem["nItemCount03"];
                PackageItemCount04 = (int)selectedItem["nItemCount04"];
                PackageItemCount05 = (int)selectedItem["nItemCount05"];
                PackageItemCount06 = (int)selectedItem["nItemCount06"];
                PackageItemCount07 = (int)selectedItem["nItemCount07"];
                PackageItemCount08 = (int)selectedItem["nItemCount08"];
                PackageItemCount09 = (int)selectedItem["nItemCount09"];
                PackageItemCount10 = (int)selectedItem["nItemCount10"];
                PackageItemCount11 = (int)selectedItem["nItemCount11"];

                PackageItemCode00 = (int)selectedItem["nItemCode00"];
                PackageItemCode01 = (int)selectedItem["nItemCode01"];
                PackageItemCode02 = (int)selectedItem["nItemCode02"];
                PackageItemCode03 = (int)selectedItem["nItemCode03"];
                PackageItemCode04 = (int)selectedItem["nItemCode04"];
                PackageItemCode05 = (int)selectedItem["nItemCode05"];
                PackageItemCode06 = (int)selectedItem["nItemCode06"];
                PackageItemCode07 = (int)selectedItem["nItemCode07"];
                PackageItemCode08 = (int)selectedItem["nItemCode08"];
                PackageItemCode09 = (int)selectedItem["nItemCode09"];
                PackageItemCode10 = (int)selectedItem["nItemCode10"];
                PackageItemCode11 = (int)selectedItem["nItemCode11"];

                if (DataTableManager.DataTableString != null && DataTableManager.SelectedItemString != null)
                {
                    PackageNameString = (string)DataTableManager.SelectedItemString["wszName"];
                    PackageDescString = (string)DataTableManager.SelectedItemString["wszDesc"];
                }
                else
                {
                    PackageNameString = "Missing Name String";
                    PackageDescString = "Missing Desc String";
                }

                FormatPackageEffect();
            }
            else
            {
                IsSelectedItemVisible = Visibility.Hidden;
            }

            _isUpdatingSelectedItem = false;

            OnCanExecuteSelectedItemCommandChanged();
        }

        #endregion

        #endregion

        #region Filter

        private void ApplyFilter()
        {
            if (DataTableManager.DataTable != null)
            {
                List<string> filterParts = [];

                // Category filters
                if (PackageTypeFilter != -1)
                {
                    filterParts.Add($"nPackageType = {PackageTypeFilter}");
                }

                // Text search filter
                if (!string.IsNullOrEmpty(SearchText))
                {
                    string searchText = SearchText.ToLower();
                    filterParts.Add($"(CONVERT(nID, 'System.String') LIKE '%{searchText}%' OR wszName LIKE '%{searchText}%' OR CONVERT(nItemCode00, 'System.String') LIKE '%{searchText}%' OR CONVERT(nItemCode01, 'System.String') LIKE '%{searchText}%' OR CONVERT(nItemCode02, 'System.String') LIKE '%{searchText}%' OR CONVERT(nItemCode03, 'System.String') LIKE '%{searchText}%' OR CONVERT(nItemCode04, 'System.String') LIKE '%{searchText}%' OR CONVERT(nItemCode05, 'System.String') LIKE '%{searchText}%' OR CONVERT(nItemCode06, 'System.String') LIKE '%{searchText}%' OR CONVERT(nItemCode07, 'System.String') LIKE '%{searchText}%' OR CONVERT(nItemCode08, 'System.String') LIKE '%{searchText}%' OR CONVERT(nItemCode09, 'System.String') LIKE '%{searchText}%' OR CONVERT(nItemCode10, 'System.String') LIKE '%{searchText}%' OR CONVERT(nItemCode11, 'System.String') LIKE '%{searchText}%')");
                }

                string filter = string.Join(" AND ", filterParts);

                DataTableManager.FilterText = filter;

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
        #endregion

        #region Comboboxes

        [ObservableProperty]
        private List<NameID>? _packageEffectItems;

        private void PopulatePackageEffectItems()
        {
            PackageEffectItems =
                [
                    new NameID { ID = 0, Name = "None" },
                    new NameID { ID = 1, Name = "EXP Bonus" },
                    new NameID { ID = 2, Name = "Extra Auction Slots" },
                    new NameID { ID = 3, Name = "Repurchase Cost Decrease" },
                    new NameID { ID = 4, Name = "Guild EXP Bonus" },
                    //new NameID { ID = 5, Name = "Potions available at the shops ?" },
                    new NameID { ID = 6, Name = "Can Use High Level Items" },
                    new NameID { ID = 7, Name = "Increases Gold Card Drop Rate" },
                    new NameID { ID = 8, Name = "Bonus Card Selection" },
                    new NameID { ID = 9, Name = "Repair Cost Reduction" },
                    //new NameID { ID = 10, Name = "Increases PvP experience points ?" },
                    //new NameID { ID = 11, Name = "Maximizes package effects ?" },
                    new NameID { ID = 12, Name = "Unlimited Inventory Weight" },
                    new NameID { ID = 13, Name = "Unlimited Inventory Weight /Extea Inventory Slots" },
                    new NameID { ID = 14, Name = "Increases Weapon Upgrade Effect" },
                    new NameID { ID = 15, Name = "Can use Twin Pet" },
                    //new NameID { ID = 16, Name = "Unknown" },
                    new NameID { ID = 17, Name = "Can Reset Skills" }
                ];

        }

        [ObservableProperty]
        private List<NameID>? _packageTypeItems;

        [ObservableProperty]
        private List<NameID>? _packageTypeItemsFilter;

        private void PopulatePackageTypeItems()
        {
            PackageTypeItems =
                [
                    new NameID { ID = 0, Name = "Effect Package" },
                    new NameID { ID = 2, Name = "Item Package" }
                ];

            PackageTypeItemsFilter =
                [
                    new NameID { ID = -1, Name = "All" },
                    new NameID { ID = 0, Name = "Effect Package" },
                    new NameID { ID = 2, Name = "Item Package" }
                ];

            PackageTypeFilter = -1;
        }

        [ObservableProperty]
        private int _packageTypeFilter;

        partial void OnPackageTypeFilterChanged(int value)
        {
            TriggerFilterUpdate();
        }

        #endregion

        #region Properties
        [ObservableProperty]
        private string _title = $"Package Editor";

        [ObservableProperty]
        private string? _openMessage = "Open a file";

        [ObservableProperty]
        private string? _packageEffectText;

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
        private int _packageID;
        partial void OnPackageIDChanged(int value)
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
        private string? _packageName;
        partial void OnPackageNameChanged(string? value)
        {
            if (!_isUpdatingSelectedItem && DataTableManager.SelectedItem != null)
            {
                DataTableManager.SelectedItem["wszName"] = value;
            }
        }

        [ObservableProperty]
        private string? _packageNameString;
        partial void OnPackageNameStringChanged(string? value)
        {
            if (!_isUpdatingSelectedItem && DataTableManager.SelectedItemString != null)
            {
                DataTableManager.SelectedItemString["wszName"] = value;
            }
        }

        [ObservableProperty]
        private string? _packageDesc;
        partial void OnPackageDescChanged(string? value)
        {
            if (!_isUpdatingSelectedItem && DataTableManager.SelectedItem != null)
            {
                DataTableManager.SelectedItem["wszDesc"] = value;
            }
        }

        [ObservableProperty]
        private string? _packageDescString;
        partial void OnPackageDescStringChanged(string? value)
        {
            if (!_isUpdatingSelectedItem && DataTableManager.SelectedItemString != null)
            {
                DataTableManager.SelectedItemString["wszDesc"] = value;
            }
        }

        [ObservableProperty]
        private string? _packageIcon;
        partial void OnPackageIconChanged(string? value)
        {
            if (!_isUpdatingSelectedItem && DataTableManager.SelectedItem != null)
            {
                DataTableManager.SelectedItem["szPackageIcon"] = value;
            }
        }

        [ObservableProperty]
        private int _packageType;
        partial void OnPackageTypeChanged(int value)
        {
            if (!_isUpdatingSelectedItem && DataTableManager.SelectedItem != null)
            {
                DataTableManager.SelectedItem["nPackageType"] = value;
                DataTableManager.SelectedItem["nOverlay"] = value == 0 ? 1 : 0;
            }

            IsPackageEffectEnabled = value == 0;
        }

        [ObservableProperty]
        private int _effectRemainTime;
        partial void OnEffectRemainTimeChanged(int value)
        {
            if (!_isUpdatingSelectedItem && DataTableManager.SelectedItem != null)
            {
                DataTableManager.SelectedItem["nEffectRemainTime"] = value;
                FormatPackageEffect();
            }

        }

        [ObservableProperty]
        private int _effectCode00;
        partial void OnEffectCode00Changed(int value) => OnEffectCodeChanged(0, value);

        [ObservableProperty]
        private int _effectCode01;
        partial void OnEffectCode01Changed(int value) => OnEffectCodeChanged(1, value);

        [ObservableProperty]
        private int _effectCode02;
        partial void OnEffectCode02Changed(int value) => OnEffectCodeChanged(2, value);

        [ObservableProperty]
        private int _effectCode03;
        partial void OnEffectCode03Changed(int value) => OnEffectCodeChanged(3, value);

        [ObservableProperty]
        private int _effectCode04;
        partial void OnEffectCode04Changed(int value) => OnEffectCodeChanged(4, value);

        [ObservableProperty]
        private int _effectCode05;
        partial void OnEffectCode05Changed(int value) => OnEffectCodeChanged(5, value);

        private void OnEffectCodeChanged(int index, int value)
        {
            if (!_isUpdatingSelectedItem && DataTableManager.SelectedItem != null)
            {
                DataTableManager.SelectedItem[$"nEffectCode{index:00}"] = value;
                DataTableManager.SelectedItem[$"nStringID{index:00}"] = GetPackageStringId(value);

                if (value == 17) // Reset Skills [Limited Period]
                {
                    DataTableManager.SelectedItem["nOverlay"] = 2;
                }

                FormatPackageEffect();
            }

            UpdateEffectValueRanges(index, value);
        }

        [ObservableProperty]
        private double _effectValue00;
        partial void OnEffectValue00Changed(double value) => OnEffectValueChanged(0, value);

        [ObservableProperty]
        private double _effectValue01;
        partial void OnEffectValue01Changed(double value) => OnEffectValueChanged(1, value);

        [ObservableProperty]
        private double _effectValue02;
        partial void OnEffectValue02Changed(double value) => OnEffectValueChanged(2, value);

        [ObservableProperty]
        private double _effectValue03;
        partial void OnEffectValue03Changed(double value) => OnEffectValueChanged(3, value);

        [ObservableProperty]
        private double _effectValue04;
        partial void OnEffectValue04Changed(double value) => OnEffectValueChanged(4, value);

        [ObservableProperty]
        private double _effectValue05;
        partial void OnEffectValue05Changed(double value) => OnEffectValueChanged(5, value);

        private void OnEffectValueChanged(int index, double value)
        {
            if (!_isUpdatingSelectedItem && DataTableManager.SelectedItem != null)
            {
                DataTableManager.SelectedItem[$"fEffectValue{index:00}"] = value;
                FormatPackageEffect();
            }
        }

        [ObservableProperty]
        private int _packageItemCode00;
        partial void OnPackageItemCode00Changed(int value) => OnPackageItemCodeChanged(0, value, PackageItemCount00);

        [ObservableProperty]
        private int _packageItemCode01;
        partial void OnPackageItemCode01Changed(int value) => OnPackageItemCodeChanged(1, value, PackageItemCount01);

        [ObservableProperty]
        private int _packageItemCode02;
        partial void OnPackageItemCode02Changed(int value) => OnPackageItemCodeChanged(2, value, PackageItemCount02);

        [ObservableProperty]
        private int _packageItemCode03;
        partial void OnPackageItemCode03Changed(int value) => OnPackageItemCodeChanged(3, value, PackageItemCount03);

        [ObservableProperty]
        private int _packageItemCode04;
        partial void OnPackageItemCode04Changed(int value) => OnPackageItemCodeChanged(4, value, PackageItemCount04);

        [ObservableProperty]
        private int _packageItemCode05;
        partial void OnPackageItemCode05Changed(int value) => OnPackageItemCodeChanged(5, value, PackageItemCount05);

        [ObservableProperty]
        private int _packageItemCode06;
        partial void OnPackageItemCode06Changed(int value) => OnPackageItemCodeChanged(6, value, PackageItemCount06);

        [ObservableProperty]
        private int _packageItemCode07;
        partial void OnPackageItemCode07Changed(int value) => OnPackageItemCodeChanged(7, value, PackageItemCount07);

        [ObservableProperty]
        private int _packageItemCode08;
        partial void OnPackageItemCode08Changed(int value) => OnPackageItemCodeChanged(8, value, PackageItemCount08);

        [ObservableProperty]
        private int _packageItemCode09;
        partial void OnPackageItemCode09Changed(int value) => OnPackageItemCodeChanged(9, value, PackageItemCount09);

        [ObservableProperty]
        private int _packageItemCode10;
        partial void OnPackageItemCode10Changed(int value) => OnPackageItemCodeChanged(10, value, PackageItemCount10);

        [ObservableProperty]
        private int _packageItemCode11;
        partial void OnPackageItemCode11Changed(int value) => OnPackageItemCodeChanged(11, value, PackageItemCount11);

        private void OnPackageItemCodeChanged(int index, int value, int itemCount)
        {
            if (!_isUpdatingSelectedItem && DataTableManager.SelectedItem != null)
            {
                DataTableManager.SelectedItem[$"nItemCode{index:00}"] = value;
            }

            SetFrameViewModelData(value, itemCount, index + 1);
        }

        [ObservableProperty]
        private int _packageItemCount00;
        partial void OnPackageItemCount00Changed(int value) => OnPackageItemCountChanged(0, value);

        [ObservableProperty]
        private int _packageItemCount01;
        partial void OnPackageItemCount01Changed(int value) => OnPackageItemCountChanged(1, value);

        [ObservableProperty]
        private int _packageItemCount02;
        partial void OnPackageItemCount02Changed(int value) => OnPackageItemCountChanged(2, value);

        [ObservableProperty]
        private int _packageItemCount03;
        partial void OnPackageItemCount03Changed(int value) => OnPackageItemCountChanged(3, value);

        [ObservableProperty]
        private int _packageItemCount04;
        partial void OnPackageItemCount04Changed(int value) => OnPackageItemCountChanged(4, value);

        [ObservableProperty]
        private int _packageItemCount05;
        partial void OnPackageItemCount05Changed(int value) => OnPackageItemCountChanged(5, value);

        [ObservableProperty]
        private int _packageItemCount06;
        partial void OnPackageItemCount06Changed(int value) => OnPackageItemCountChanged(6, value);

        [ObservableProperty]
        private int _packageItemCount07;
        partial void OnPackageItemCount07Changed(int value) => OnPackageItemCountChanged(7, value);

        [ObservableProperty]
        private int _packageItemCount08;
        partial void OnPackageItemCount08Changed(int value) => OnPackageItemCountChanged(8, value);

        [ObservableProperty]
        private int _packageItemCount09;
        partial void OnPackageItemCount09Changed(int value) => OnPackageItemCountChanged(9, value);

        [ObservableProperty]
        private int _packageItemCount10;
        partial void OnPackageItemCount10Changed(int value) => OnPackageItemCountChanged(10, value);

        [ObservableProperty]
        private int _packageItemCount11;
        partial void OnPackageItemCount11Changed(int value) => OnPackageItemCountChanged(11, value);

        private void OnPackageItemCountChanged(int index, int value)
        {
            if (!_isUpdatingSelectedItem && DataTableManager.SelectedItem != null)
            {
                DataTableManager.SelectedItem[$"nItemCount{index:00}"] = value;
            }
        }

        [ObservableProperty]
        private int _runeGroup00;
        partial void OnRuneGroup00Changed(int value) => OnRuneGroupChanged(0, value);

        [ObservableProperty]
        private int _runeGroup01;
        partial void OnRuneGroup01Changed(int value) => OnRuneGroupChanged(1, value);

        [ObservableProperty]
        private int _runeGroup02;
        partial void OnRuneGroup02Changed(int value) => OnRuneGroupChanged(2, value);

        private void OnRuneGroupChanged(int index, int value)
        {
            if (!_isUpdatingSelectedItem && DataTableManager.SelectedItem != null)
            {
                DataTableManager.SelectedItem[$"nRuneGroup{index:00}"] = value;
            }
        }

        [ObservableProperty]
        private bool _isPackageEffectEnabled = true;

        [ObservableProperty]
        private double _effectValue00Min;

        [ObservableProperty]
        private double _effectValue00Max;

        [ObservableProperty]
        private double _effectValue00Increment;

        [ObservableProperty]
        private double _effectValue01Min;

        [ObservableProperty]
        private double _effectValue01Max;

        [ObservableProperty]
        private double _effectValue01Increment;

        [ObservableProperty]
        private double _effectValue02Min;

        [ObservableProperty]
        private double _effectValue02Max;

        [ObservableProperty]
        private double _effectValue02Increment;

        [ObservableProperty]
        private double _effectValue03Min;

        [ObservableProperty]
        private double _effectValue03Max;

        [ObservableProperty]
        private double _effectValue03Increment;

        [ObservableProperty]
        private double _effectValue04Min;

        [ObservableProperty]
        private double _effectValue04Max;

        [ObservableProperty]
        private double _effectValue04Increment;

        [ObservableProperty]
        private double _effectValue05Min;

        [ObservableProperty]
        private double _effectValue05Max;

        [ObservableProperty]
        private double _effectValue05Increment;

        #endregion

        #endregion

        #region Properties Helper
        private void FormatPackageEffect()
        {
            string effectRemainTime = DateTimeFormatter.FormatMinutesToDate(EffectRemainTime);
            string packageEffect01 = _frameService.GetString(GetPackageStringId(EffectCode00));
            string packageEffect02 = _frameService.GetString(GetPackageStringId(EffectCode01));
            string packageEffect03 = _frameService.GetString(GetPackageStringId(EffectCode02));
            string packageEffect04 = _frameService.GetString(GetPackageStringId(EffectCode03));
            string packageEffect05 = _frameService.GetString(GetPackageStringId(EffectCode04));
            string packageEffect06 = _frameService.GetString(GetPackageStringId(EffectCode05));

            bool hasEffect = EffectCode00 != 0 || EffectCode01 != 0 || EffectCode02 != 0 || EffectCode03 != 0 || EffectCode04 != 0 || EffectCode05 != 0;

            static string FormatEffectValue(double value, int effectCode)
            {
                return effectCode switch
                {
                    1 or 3 or 4 or 9 => $"{value * 100:F1}%",
                    _ => $"{(int)value}",
                };
            }

            string packageEffect = hasEffect ? "Package Effect\n" : "Package Effect: No Effect\n";
            if (EffectRemainTime != 0)
                packageEffect += $"Duration: {effectRemainTime}\n";
            if (EffectCode00 != 0)
                packageEffect += $"Effect: {packageEffect01} ({FormatEffectValue(EffectValue00, EffectCode00)})\n";
            if (EffectCode01 != 0)
                packageEffect += $"Effect: {packageEffect02} ({FormatEffectValue(EffectValue01, EffectCode01)})\n";
            if (EffectCode02 != 0)
                packageEffect += $"Effect: {packageEffect03} ({FormatEffectValue(EffectValue02, EffectCode02)})\n";
            if (EffectCode03 != 0)
                packageEffect += $"Effect: {packageEffect04} ({FormatEffectValue(EffectValue03, EffectCode03)})\n";
            if (EffectCode04 != 0)
                packageEffect += $"Effect: {packageEffect05} ({FormatEffectValue(EffectValue04, EffectCode04)})\n";
            if (EffectCode05 != 0)
                packageEffect += $"Effect: {packageEffect06} ({FormatEffectValue(EffectValue05, EffectCode05)})\n";

            PackageEffectText = packageEffect;
        }

        private static int GetPackageStringId(int effectCode)
        {
            return effectCode switch
            {
                1 => 2714,
                2 => 2715,
                3 => 2716,
                4 => 2717,
                5 => 2718,
                6 => 2719,
                7 => 2720,
                8 => 2721,
                9 => 2722,
                12 => 2726,
                13 => 2727,
                14 => 2728,
                15 => 2655,
                17 => 3275,
                _ => 0,
            };
        }

        private void UpdateEffectValueRanges(int index, int effectCode)
        {
            double min, max, increment;
            switch (effectCode)
            {
                case 1 or 4 or 7:
                    min = 0.01;
                    max = 1;
                    increment = 0.01;
                    break;
                case 2:
                    min = 1;
                    max = 24;
                    increment = 1;
                    break;
                case 3:
                    min = 0.01;
                    max = 0.10;
                    increment = 0.01;
                    break;
                case 5 or 8 or 12 or 17:
                    min = 1;
                    max = 1;
                    increment = 1;
                    break;
                case 6:
                    min = 1;
                    max = 10;
                    increment = 1;
                    break;
                case 9:
                    min = 0.01;
                    max = 0.50;
                    increment = 0.01;
                    break;
                case 13:
                    min = 1;
                    max = 24;
                    increment = 1;
                    break;
                case 14:
                    min = 1;
                    max = 20;
                    increment = 1;
                    break;
                case 15:
                    min = 2;
                    max = 2;
                    increment = 2;
                    break;
                default:
                    min = 0;
                    max = 0;
                    increment = 0;
                    break;
            }

            switch (index)
            {
                case 0:
                    EffectValue00Min = min;
                    EffectValue00Max = max;
                    EffectValue00Increment = increment;
                    EffectValue00 = min;
                    break;
                case 1:
                    EffectValue01Min = min;
                    EffectValue01Max = max;
                    EffectValue01Increment = increment;
                    EffectValue01 = min;
                    break;
                case 2:
                    EffectValue02Min = min;
                    EffectValue02Max = max;
                    EffectValue02Increment = increment;
                    EffectValue02 = min;
                    break;
                case 3:
                    EffectValue03Min = min;
                    EffectValue03Max = max;
                    EffectValue03Increment = increment;
                    EffectValue03 = min;
                    break;
                case 4:
                    EffectValue04Min = min;
                    EffectValue04Max = max;
                    EffectValue04Increment = increment;
                    EffectValue04 = min;
                    break;
                case 5:
                    EffectValue05Min = min;
                    EffectValue05Max = max;
                    EffectValue05Increment = increment;
                    EffectValue05 = min;
                    break;
            }
        }

        #endregion
    }
}
