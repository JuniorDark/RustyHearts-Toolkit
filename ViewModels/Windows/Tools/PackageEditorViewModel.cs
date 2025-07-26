using Microsoft.Win32;
using RHToolkit.Messages;
using RHToolkit.Models;
using RHToolkit.Models.Database;
using RHToolkit.Models.Editor;
using RHToolkit.Models.MessageBox;
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
                Interval = 500,
                AutoReset = false
            };
            _filterUpdateTimer.Elapsed += FilterUpdateTimerElapsed;

            PopulateListItems();

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
                    if (fileName == null) return;

                    string? stringFileName = GetStringFileName(unionPackageType);
                    PackageTeplateType = unionPackageType;
                    string columnName = GetColumnName(fileName);

                    bool isLoaded = await DataTableManager.LoadFileFromPath(fileName, stringFileName, columnName, "Package");

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

                string filter = "Package Files|" +
                                "unionpackage.rh;unionpackage_local.rh;conditionselectitem.rh|" +
                                "All Files (*.*)|*.*";

                OpenFileDialog openFileDialog = new()
                {
                    Filter = filter,
                    FilterIndex = 1
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    string fileName = Path.GetFileName(openFileDialog.FileName);
                    int fileType = GetFileTypeFromFileName(fileName);

                    if (fileType == -1)
                    {
                        string message = string.Format(Resources.InvalidTableFileDesc, fileName, "Package");
                        RHMessageBoxHelper.ShowOKMessage(message, Resources.Error);
                        return;
                    }

                    string? stringFileName = GetStringFileName(fileType);
                    string? columnName = GetColumnName(fileName);
                    PackageTeplateType = fileType;
                    bool isLoaded = await DataTableManager.LoadFileAs(openFileDialog.FileName, stringFileName, columnName, "Package");

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

                if (int.TryParse(parameter, out int unionPackageType))
                {
                    string? fileName = GetFileName(unionPackageType);
                    if (fileName == null) return;

                    string? stringFileName = GetStringFileName(unionPackageType);
                    PackageTeplateType = unionPackageType;
                    string columnName = GetColumnName(fileName);

                    bool isLoaded = await DataTableManager.LoadFileFromPCK(fileName, stringFileName);

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

        private static string GetFileName(int unionPackageType)
        {
            return unionPackageType switch
            {
                1 => "unionpackage.rh",
                2 => "unionpackage_local.rh",
                3 => "conditionselectitem.rh",
                _ => throw new ArgumentOutOfRangeException(nameof(unionPackageType)),
            };
        }

        private static string? GetStringFileName(int unionPackageType)
        {
            return unionPackageType switch
            {
                1 => "unionpackage_string.rh",
                2 => "unionpackage_local_string.rh",
                3 => null,
                _ => throw new ArgumentOutOfRangeException(nameof(unionPackageType)),
            };
        }

        private static string GetColumnName(string fileName)
        {
            return fileName switch
            {
                "unionpackage.rh" or "unionpackage_local.rh" => "nPackageType",
                "conditionselectitem.rh" => "nTakeItemType",
                _ => "",
            };
        }

        private static int GetFileTypeFromFileName(string fileName)
        {
            return fileName switch
            {
                "unionpackage.rh" => 1,
                "unionpackage_local.rh" => 2,
                "conditionselectitem.rh" => 3,
                _ => -1,
            };
        }

        private void IsLoaded()
        {
            Title = string.Format(Resources.EditorTitleFileName, "Package", DataTableManager.CurrentFileName);
            OpenMessage = "";
            IsVisible = Visibility.Visible;
            OnCanExecuteFileCommandChanged();
        }

        [RelayCommand(CanExecute = nameof(CanExecuteFileCommand))]
        private void OpenSearchDialog(string? parameter)
        {
            try
            {
                Window? window = Application.Current.Windows.OfType<PackageEditorWindow>().FirstOrDefault();
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
            Title = string.Format(Resources.EditorTitle, "Package");
            OpenMessage = Resources.OpenFile;
            PackageTeplateType = 0;
            PackageItems?.Clear();
            PackageEffects?.Clear();
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

                    _windowsService.OpenItemWindow(token, PackageTeplateType == 3 ? "PackageCondition" : "Package", itemData);
                }

            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
            }
        }

        public void Receive(ItemDataMessage message)
        {
            if (message.Recipient == "PackageEditorWindow" && message.Token == _token)
            {
                var itemData = message.Value;

                if (itemData.SlotIndex >= 0 && itemData.SlotIndex < 12)
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
                if (PackageTeplateType != 3)
                {
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
                }
                DataTableManager.EndGroupingEdits();
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
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
                RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
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

                if (PackageTeplateType == 3)
                {
                    UpdateConditionSelectedItem(selectedItem);
                }
                else
                {
                    UpdatePackageSelectedItem(selectedItem);
                }
            }
        }

        #region Package
        private void UpdatePackageSelectedItem(DataRowView? selectedItem)
        {
            _isUpdatingSelectedItem = true;

            if (selectedItem != null)
            {
                PackageType = (int)selectedItem["nPackageType"];
                EffectRemainTime = (int)selectedItem["nEffectRemainTime"];

                PackageItems ??= [];

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

                PackageEffects?.Clear();
                PackageEffects = [];

                for (int i = 0; i < 6; i++)
                {
                    var effectCode = (int)selectedItem[$"nEffectCode{i:00}"];
                    var effectValue = (float)selectedItem[$"fEffectValue{i:00}"];
                    var stringID = (int)selectedItem[$"nStringID{i:00}"];

                    var item = new PackageItem
                    {
                        EffectCode = effectCode,
                        EffectValue = effectValue,
                        StringID = stringID
                    };

                    PackageEffects.Add(item);
                    EffectPropertyChanged(item, i);
                    UpdateEffectValueRanges(i, item.EffectCode);
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

        #region ConditionSelectItem
        private void UpdateConditionSelectedItem(DataRowView? selectedItem)
        {
            _isUpdatingSelectedItem = true;

            if (selectedItem != null)
            {
                PackageItems ??= [];

                for (int i = 0; i < 6; i++)
                {
                    var itemCode = (int)selectedItem[$"nItemID{i:00}"];
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

                        ConditionItemPropertyChanged(item, i);
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

        private void ConditionItemPropertyChanged(PackageItem item, int index)
        {
            item.PropertyChanged += (s, e) =>
            {
                if (s is PackageItem packageItem)
                {
                    UpdateSelectedItemValue(packageItem.ItemCode, $"nItemID{index:00}");
                    UpdateSelectedItemValue(packageItem.ItemCount, $"nItemCount{index:00}");
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

            columns.Add("CONVERT(nID, 'System.String')");
            
            switch (PackageTeplateType)
            {
                case 1 or 2:
                    columns.Add("wszName");
                    for (int i = 0; i < 12; i++)
                    {
                        string columnName = $"nItemCode{i:00}";
                        columns.Add($"CONVERT({columnName}, 'System.String')");
                    }
                    break;
                case 3:
                    columns.Add("szName");
                    columns.Add("CONVERT(nGroup, 'System.String')");
                    columns.Add("CONVERT(nRandomGroup, 'System.String')");
                    for (int i = 0; i < 6; i++)
                    {
                        string columnName = $"nItemID{i:00}";
                        columns.Add($"CONVERT({columnName}, 'System.String')");
                    }
                    break;
                default:
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
        private List<NameID>? _packageEffectItems;

        [ObservableProperty]
        private List<NameID>? _jobItems;

        [ObservableProperty]
        private List<NameID>? _packageTypeItems;

        private void PopulateListItems()
        {
            PackageTypeItems =
                [
                    new NameID { ID = 0, Name = Resources.EffectPackage },
                    new NameID { ID = 2, Name = Resources.ItemPackage }
                ];

            PackageEffectItems =
                [
                    new NameID { ID = 0, Name = Resources.None },
                    new NameID { ID = 1, Name = Resources.PackageEffectEXPBonus },
                    new NameID { ID = 2, Name = Resources.PackageEffectAuctionSlot },
                    new NameID { ID = 3, Name = Resources.PackageEffectRepurchaseCostDecrease },
                    new NameID { ID = 4, Name = Resources.PackageEffectGuildEXPBonus },
                    new NameID { ID = 5, Name = Resources.PackageEffectShopItem },
                    new NameID { ID = 6, Name = Resources.PackageEffectItemLevel },
                    new NameID { ID = 7, Name = Resources.PackageEffectGoldCard },
                    new NameID { ID = 8, Name = Resources.PackageEffectBonusCardSelection },
                    new NameID { ID = 9, Name = Resources.PackageEffectRepairCostReduction },
                    //new NameID { ID = 10, Name = "Increases PvP experience points ?" },
                    //new NameID { ID = 11, Name = "Maximizes package effects ?" },
                    new NameID { ID = 12, Name = Resources.PackageEffectUnlimitedWeight },
                    new NameID { ID = 13, Name = Resources.PackageEffectWeightExtraInventory },
                    new NameID { ID = 14, Name = Resources.PackageEffectWeaponUpgrade },
                    new NameID { ID = 15, Name = Resources.PackageEffectTwinPet },
                    //new NameID { ID = 16, Name = "Unknown" },
                    new NameID { ID = 17, Name = Resources.PackageEffectResetSkills }
                ];
            JobItems =
                [
                    new NameID { ID = 0, Name = Resources.BaseSkill },
                    new NameID { ID = 1, Name = Resources.Focus1 },
                    new NameID { ID = 2, Name = Resources.Focus2 },
                    new NameID { ID = 3, Name = Resources.Focus3 }
                ];
        }

        #endregion

        #region Properties
        [ObservableProperty]
        private string _title = string.Format(Resources.EditorTitle, "Package");

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
        private int _packageTeplateType;

        [ObservableProperty]
        private ItemDataManager _itemDataManager;

        #region SelectedItem

        [ObservableProperty]
        private ObservableCollection<PackageItem> _packageItems = [];

        [ObservableProperty]
        private ObservableCollection<PackageItem> _packageEffects = [];

        [ObservableProperty]
        private string? _packageEffectText;

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

        private void FormatPackageEffect()
        {
            string effectRemainTime = DateTimeFormatter.FormatMinutesToDate(EffectRemainTime);
            bool hasEffect = false;
            StringBuilder packageEffect = new($"{Resources.PackageEffect}\n");

            for (int i = 0; i < PackageEffects.Count; i++)
            {
                if (PackageEffects[i].EffectCode != 0)
                {
                    hasEffect = true;
                    string packageEffectStr = _frameService.GetString(GetPackageStringId(PackageEffects[i].EffectCode));
                    packageEffect.AppendLine($"{Resources.Effect}: {packageEffectStr} ({FormatEffectValue(PackageEffects[i].EffectCode, PackageEffects[i].EffectValue)})");
                }
            }

            if (!hasEffect)
            {
                packageEffect.AppendLine(Resources.NoEffect);
            }
            else if (EffectRemainTime != 0)
            {
                packageEffect.AppendLine($"{Resources.Duration}: {effectRemainTime}");
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
