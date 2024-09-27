using Microsoft.Win32;
using RHToolkit.Messages;
using RHToolkit.Models;
using RHToolkit.Models.Database;
using RHToolkit.Models.Editor;
using RHToolkit.Models.MessageBox;
using RHToolkit.Properties;
using RHToolkit.Services;
using RHToolkit.Utilities;
using RHToolkit.ViewModels.Windows.Tools.VM;
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
                Interval = 400,
                AutoReset = false
            };
            _filterUpdateTimer.Elapsed += FilterUpdateTimerElapsed;

            PopulatePackageTypeItems();
            PopulatePackageEffectItems();

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

                if (int.TryParse(parameter, out int unionPackageType))
                {
                    string? fileName = GetFileName(unionPackageType);
                    string? stringFileName = GetStringFileName(unionPackageType);
                    if (fileName == null) return;

                    bool isLoaded = await DataTableManager.LoadFileFromPath(fileName, stringFileName, "nPackageType", "Package");

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

                string filter = "UnionPackage Files|" +
                                "unionpackage.rh;unionpackage_local.rh|" +
                                "All Files (*.*)|*.*";

                OpenFileDialog openFileDialog = new()
                {
                    Filter = filter,
                    FilterIndex = 1
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    string selectedFileName = Path.GetFileName(openFileDialog.FileName);

                    int unionPackageType = selectedFileName switch
                    {
                        "unionpackage.rh" => 1,
                        "unionpackage_local.rh" => 2,
                        _ => throw new Exception($"The file '{selectedFileName}' is not a valid unionpackage file."),
                    };

                    string? fileName = GetFileName(unionPackageType);
                    string? stringFileName = GetStringFileName(unionPackageType);

                    bool isLoaded = await DataTableManager.LoadFileAs(openFileDialog.FileName, stringFileName, "nPackageType", "Package");

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


        private static string? GetFileName(int unionPackageType)
        {
            return unionPackageType switch
            {
                1 => "unionpackage.rh",
                2 => "unionpackage_local.rh",
                _ => throw new ArgumentOutOfRangeException(nameof(unionPackageType)),
            };
        }

        private static string? GetStringFileName(int unionPackageType)
        {
            return unionPackageType switch
            {
                1 => "unionpackage_string.rh",
                2 => "unionpackage_local_string.rh",
                _ => throw new ArgumentOutOfRangeException(nameof(unionPackageType)),
            };
        }

        private void IsLoaded()
        {
            Title = $"Package Editor ({DataTableManager.CurrentFileName})";
            OpenMessage = "";
            IsVisible = Visibility.Visible;
            OnCanExecuteFileCommandChanged();
        }

        [RelayCommand(CanExecute = nameof(CanExecuteFileCommand))]
        private void OpenSearchDialog(string? parameter)
        {
            try
            {
                Window? shopEditorWindow = Application.Current.Windows.OfType<PackageEditorWindow>().FirstOrDefault();
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
                        ItemId = PackageItems[slotIndex].ItemCode,
                        ItemAmount = PackageItems[slotIndex].ItemCount
                    };

                    var token = _token;

                    _windowsService.OpenItemWindow(token, "Package", itemData);
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

                if (DataTableManager.SelectedItem != null && itemData.SlotIndex >= 0 && itemData.SlotIndex < 10)
                {
                    UpdatePackageItem(itemData);
                }
            }
        }

        private void UpdatePackageItem(ItemData itemData)
        {
            if (itemData.ItemId != 0)
            {
                DataTableManager.StartGroupingEdits();
                var itemDataViewModel = ItemDataManager.GetItemDataViewModel(itemData.ItemId, itemData.SlotIndex, itemData.ItemAmount);
                PackageItems[itemData.SlotIndex].ItemCode = itemData.ItemId;
                PackageItems[itemData.SlotIndex].ItemCount = itemData.ItemAmount;
                PackageItems[itemData.SlotIndex].ItemDataViewModel = itemDataViewModel;
                OnPropertyChanged(nameof(PackageItems));
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
                if (DataTableManager.SelectedItem != null)
                {
                    DataTableManager.SelectedItem["wszName"] = "New Package";
                    DataTableManager.SelectedItem["wszDesc"] = "New Package Description";
                }
                if (DataTableManager.SelectedItemString != null)
                {
                    DataTableManager.SelectedItemString["wszName"] = "New Package";
                    DataTableManager.SelectedItemString["wszDesc"] = "New Package Description";
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
                    RemovePackageItem(slotIndex);
                }
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"Error: {ex.Message}", "Error");
            }
        }

        private void RemovePackageItem(int slotIndex)
        {
            if (slotIndex >= 0 && slotIndex < 12 && PackageItems[slotIndex].ItemCode != 0)
            {
                DataTableManager.StartGroupingEdits();
                PackageItems[slotIndex].ItemCode = 0;
                PackageItems[slotIndex].ItemCount = 0;
                PackageItems[slotIndex].ItemDataViewModel = null;
                OnPropertyChanged(nameof(PackageItems));
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
                PackageType = (int)selectedItem["nPackageType"];
                EffectRemainTime = (int)selectedItem["nEffectRemainTime"];

                PackageItems ??= [];
                PackageEffects ??= [];

                for (int i = 0; i < 12; i++)
                {
                    var itemCode = (int)selectedItem[$"nItemCode{i:00}"];
                    var itemCount = (int)selectedItem[$"nItemCount{i:00}"];
                    var itemDataViewModel = ItemDataManager.GetItemDataViewModel(itemCode, i, itemCount);

                    if (i < PackageItems.Count)
                    {
                        var existingItem = PackageItems[i];

                        existingItem.ItemCode = itemCode;
                        existingItem.ItemCount = itemCount;
                        existingItem.ItemDataViewModel = itemDataViewModel;
                    }
                    else
                    {
                        var item = new PackageItem
                        {
                            ItemCode = itemCode,
                            ItemCount = itemCount,
                            ItemDataViewModel = itemDataViewModel
                        };

                        PackageItems.Add(item);

                        ItemPropertyChanged(item, i);
                    }
                    
                }
                for (int i = 0; i < 6; i++)
                {
                    var effectCode = (int)selectedItem[$"nEffectCode{i:00}"];
                    var effectValue = (float)selectedItem[$"fEffectValue{i:00}"];
                    var stringID = (int)selectedItem[$"nStringID{i:00}"];

                    if (i < PackageEffects.Count)
                    {
                        var existingItem = PackageEffects[i];

                        existingItem.EffectCode = effectCode;
                        existingItem.EffectValue = effectValue;
                        existingItem.StringID = stringID;
                    }
                    else
                    {
                        var item = new PackageItem
                        {
                            EffectCode = (int)selectedItem[$"nEffectCode{i:00}"],
                            EffectValue = (float)selectedItem[$"fEffectValue{i:00}"],
                            StringID = (int)selectedItem[$"nStringID{i:00}"]
                        };

                        PackageEffects.Add(item);
                        EffectPropertyChanged(item, i);
                        UpdateEffectValueRanges(i, item.EffectCode);
                    }
                }

                FormatPackageEffect();
                IsSelectedItemVisible = Visibility.Visible;
            }
            else
            {
                IsSelectedItemVisible = Visibility.Hidden;
            }

            _isUpdatingSelectedItem = false;
            OnCanExecuteSelectedItemCommandChanged();
        }

        private void ItemPropertyChanged(PackageItem item, int index)
        {
            item.PropertyChanged += (s, e) =>
            {
                if (s is PackageItem packageItem)
                {
                    UpdateSelectedItemValue(packageItem.ItemCode, $"nItemCode{index:00}");
                    UpdateSelectedItemValue(packageItem.ItemCount, $"nItemCount{index:00}");
                }
            };
        }

        private void EffectPropertyChanged(PackageItem item, int index)
        {
            item.PropertyChanged += (s, e) =>
            {
                if (s is PackageItem packageEffect)
                {
                    OnEffectCodeChanged(packageEffect.EffectCode, index);
                    OnEffectValueChanged(packageEffect.EffectValue, index);
                }
            };
        }

        #endregion

        #region Filter

        private void ApplyFilter()
        {
            List<string> filterParts = [];

            if (PackageTypeFilter != -1)
            {
                filterParts.Add($"nPackageType = {PackageTypeFilter}");
            }

            List<string> columns = [];

            columns.Add("CONVERT(nID, 'System.String')");
            columns.Add("wszName");

            for (int i = 0; i < 12; i++)
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
                    new NameID { ID = 5, Name = "Exclusive Items available at the shops" },
                    new NameID { ID = 6, Name = "Can Use High Level Items" },
                    new NameID { ID = 7, Name = "Increases Gold Card Drop Rate" },
                    new NameID { ID = 8, Name = "Bonus Card Selection" },
                    new NameID { ID = 9, Name = "Repair Cost Reduction" },
                    //new NameID { ID = 10, Name = "Increases PvP experience points ?" },
                    //new NameID { ID = 11, Name = "Maximizes package effects ?" },
                    new NameID { ID = 12, Name = "Unlimited Inventory Weight" },
                    new NameID { ID = 13, Name = "Unlimited Inventory Weight /Extra Inventory Slots" },
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
        private ObservableCollection<PackageItem> _packageItems = [];

        [ObservableProperty]
        private ObservableCollection<PackageItem> _packageEffects = [];

        [ObservableProperty]
        private int _packageType;
        partial void OnPackageTypeChanged(int value)
        {
            DataTableManager.StartGroupingEdits();
            UpdateSelectedItemValue(value, "nPackageType");
            UpdateSelectedItemValue(value == 0 ? 1 : 0, "nOverlay");
            IsPackageEffectEnabled = value == 0;
            DataTableManager.EndGroupingEdits();
        }

        [ObservableProperty]
        private int _effectRemainTime;
        partial void OnEffectRemainTimeChanged(int value)
        {
            UpdateSelectedItemValue(value, "nEffectRemainTime");
            FormatPackageEffect();
        }

        private void OnEffectCodeChanged(int value, int index)
        {
            if (!_isUpdatingSelectedItem)
            {
                DataTableManager.StartGroupingEdits();
                UpdateSelectedItemValue(value, $"nEffectCode{index:00}");
                UpdateSelectedItemValue(GetPackageStringId(value), $"nStringID{index:00}");

                if (value == 17) // Reset Skills [Limited Period]
                {
                    UpdateSelectedItemValue(2, "nOverlay");
                }

                FormatPackageEffect();
                UpdateEffectValueRanges(index, value);
                DataTableManager.EndGroupingEdits();
            }
        }

        private void OnEffectValueChanged(double value, int index)
        {
            if (!_isUpdatingSelectedItem)
            {
                UpdateSelectedItemValue(value, $"fEffectValue{index:00}");
                FormatPackageEffect();
            }
        }

        [ObservableProperty]
        private bool _isPackageEffectEnabled = true;

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

        private void FormatPackageEffect()
        {
            string effectRemainTime = DateTimeFormatter.FormatMinutesToDate(EffectRemainTime);
            bool hasEffect = false;
            StringBuilder packageEffect = new("Package Effect\n");

            for (int i = 0; i < PackageEffects.Count; i++)
            {
                if (PackageEffects[i].EffectCode != 0)
                {
                    hasEffect = true;
                    string packageEffectStr = _frameService.GetString(GetPackageStringId(PackageEffects[i].EffectCode));
                    packageEffect.AppendLine($"Effect: {packageEffectStr} ({FormatEffectValue(PackageEffects[i].EffectCode, PackageEffects[i].EffectValue)})");
                }
            }

            if (!hasEffect)
            {
                packageEffect.AppendLine("No Effect");
            }
            else if (EffectRemainTime != 0)
            {
                packageEffect.AppendLine($"Duration: {effectRemainTime}");
            }

            PackageEffectText = packageEffect.ToString();
        }

        private static string FormatEffectValue(int effectCode, double value)
        {
            return effectCode switch
            {
                1 or 3 or 4 or 9 => $"{value * 100:F1}%",
                _ => $"{(int)value}",
            };
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
        private bool _isUpdatingEffectValueRanges = false;

        private void UpdateEffectValueRanges(int index, int effectCode)
        {
            if (_isUpdatingEffectValueRanges)
                return;

            _isUpdatingEffectValueRanges = true;

            double min, max, smallIncrement, largeIncrement;
            switch (effectCode)
            {
                case 1 or 4 or 7:
                    min = 0.01;
                    max = 1;
                    smallIncrement = 0.01;
                    largeIncrement = 0.1;
                    break;
                case 2:
                    min = 1;
                    max = 24;
                    smallIncrement = 1;
                    largeIncrement = 1;
                    break;
                case 3:
                    min = 0.01;
                    max = 0.10;
                    smallIncrement = 0.01;
                    largeIncrement = 0.01;
                    break;
                case 5 or 8 or 12 or 17:
                    min = 1;
                    max = 1;
                    smallIncrement = 1;
                    largeIncrement = 1;
                    break;
                case 6:
                    min = 1;
                    max = 10;
                    smallIncrement = 1;
                    largeIncrement = 1;
                    break;
                case 9:
                    min = 0.01;
                    max = 0.50;
                    smallIncrement = 0.01;
                    largeIncrement = 0.1;
                    break;
                case 13:
                    min = 1;
                    max = 24;
                    smallIncrement = 1;
                    largeIncrement = 2;
                    break;
                case 14:
                    min = 1;
                    max = 20;
                    smallIncrement = 1;
                    largeIncrement = 2;
                    break;
                case 15:
                    min = 2;
                    max = 2;
                    smallIncrement = 2;
                    largeIncrement = 2;
                    break;
                default:
                    min = 0;
                    max = 0;
                    smallIncrement = 0;
                    largeIncrement = 0;
                    break;
            }

            PackageEffects[index].EffectValueMin = min;
            PackageEffects[index].EffectValueMax = max;
            PackageEffects[index].EffectValueSmallIncrement = smallIncrement;
            PackageEffects[index].EffectValueLargeIncrement = largeIncrement;
            if (PackageEffects[index].EffectValue < min || PackageEffects[index].EffectValue > max)
            {
                PackageEffects[index].EffectValue = min;
            }

            _isUpdatingEffectValueRanges = false;
        }

        #endregion
    }
}
