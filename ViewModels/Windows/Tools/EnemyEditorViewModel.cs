using Microsoft.Win32;
using RHToolkit.Messages;
using RHToolkit.Models.Database;
using RHToolkit.Models.Editor;
using RHToolkit.Models.MessageBox;
using RHToolkit.Properties;
using RHToolkit.Services;
using RHToolkit.ViewModels.Windows.Tools.VM;
using RHToolkit.Views.Windows;
using System.ComponentModel;
using System.Data;
using System.Windows.Controls;
using static RHToolkit.Models.EnumService;

namespace RHToolkit.ViewModels.Windows
{
    public partial class EnemyEditorViewModel : ObservableObject, IRecipient<DataRowViewMessage>
    {
        private readonly Guid _token;
        private readonly IWindowsService _windowsService;
        private readonly IGMDatabaseService _gmDatabaseService;
        private readonly System.Timers.Timer _filterUpdateTimer;

        public EnemyEditorViewModel(IWindowsService windowsService, IGMDatabaseService gmDatabaseService, ItemDataManager itemDataManager)
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

            WeakReferenceMessenger.Default.Register(this);
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
                    string? fileName = GetFileNameFromEnemyType(dropGroupType);
                    if (fileName == null) return;
                    int enemyType = GetEnemyTypeFromFileName(fileName);
                    string columnName = GetColumnName(fileName);
                    string? stringFileName = GetStringFileName(enemyType);

                    EnemyType = (EnemyType)enemyType;

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

                string filter = "Enemy Files .rh|" +
                                "enemy.rh;|" +
                                "All Files (*.*)|*.*";

                OpenFileDialog openFileDialog = new()
                {
                    Filter = filter,
                    FilterIndex = 1
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    string fileName = Path.GetFileName(openFileDialog.FileName);
                    int enemyType = GetEnemyTypeFromFileName(fileName);

                    if (enemyType == -1)
                    {
                        RHMessageBoxHelper.ShowOKMessage($"The file '{fileName}' is not a valid Enemy file.", Resources.Error);
                        return;
                    }

                    string columnName = GetColumnName(fileName);
                    string? stringFileName = GetStringFileName(enemyType);

                    EnemyType = (EnemyType)enemyType;

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
            Title = $"Enemy Editor ({DataTableManager.CurrentFileName})";
            OpenMessage = "";
            IsVisible = Visibility.Visible;
            OnCanExecuteFileCommandChanged();
        }

        private static string? GetFileNameFromEnemyType(int questType)
        {
            return questType switch
            {
                1 => "enemy.rh",
                _ => throw new ArgumentOutOfRangeException(nameof(questType)),
            };
        }

        private static int GetEnemyTypeFromFileName(string fileName)
        {
            return fileName switch
            {
                "enemy.rh" => 1,
                _ => -1,
            };
        }

        private static string GetColumnName(string fileName)
        {
            return fileName switch
            {
                "enemy.rh" => "fEnemyExp",
                _ => "",
            };
        }

        private static string? GetStringFileName(int questType)
        {
            return questType switch
            {
                1 => "enemy_string.rh",
                _ => null,
            };
        }

        [RelayCommand(CanExecute = nameof(CanExecuteFileCommand))]
        private void OpenSearchDialog(string? parameter)
        {
            try
            {
                Window? enemyEditorWindow = Application.Current.Windows.OfType<EnemyEditorWindow>().FirstOrDefault();
                Window owner = enemyEditorWindow ?? Application.Current.MainWindow;
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
            Title = $"Enemy Editor";
            OpenMessage = "Open a file";
            EnemyDropGroups?.Clear();
            EnemyConditionResistance?.Clear();
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

        #region Open QuestItemDropGroupItemList

        [RelayCommand]
        private void OpenQuestItemDropGroupItemList(int parameter)
        {
            try
            {
                if (parameter != 0)
                {
                    _windowsService.OpenDropGroupListWindow(_token, parameter, ItemDropGroupType.QuestItemDropGroupList);
                }
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"Error: {ex.Message}", "Error");
            }
        }
        #endregion

        #region Open ChampionItemDropGroupItemList

        [RelayCommand]
        private void OpenChampionItemDropGroupItemList(int parameter)
        {
            try
            {
                if (parameter != 0)
                {
                    _windowsService.OpenDropGroupListWindow(_token, parameter, ItemDropGroupType.ChampionItemItemDropGroupList);
                }
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"Error: {ex.Message}", "Error");
            }
        }
        #endregion

        #region Open InstanceItemDropGroupItemList

        [RelayCommand]
        private void OpenInstanceItemDropGroupItemList(int parameter)
        {
            try
            {
                if (parameter != 0)
                {
                    _windowsService.OpenDropGroupListWindow(_token, parameter, ItemDropGroupType.InstanceItemDropGroupList);
                }
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"Error: {ex.Message}", "Error");
            }
        }
        #endregion

        #region Open RareCardItemDropGroupItemList

        [RelayCommand]
        private void OpenRareCardItemDropGroupItemList(int parameter)
        {
            try
            {
                if (parameter != 0)
                {
                    _windowsService.OpenDropGroupListWindow(_token, parameter, ItemDropGroupType.RareCardDropGroupList);
                }
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"Error: {ex.Message}", "Error");
            }
        }
        #endregion

        #region Open ItemDropGroupItemList

        [RelayCommand]
        private void OpenItemDropGroupItemList(int parameter)
        {
            try
            {
                if (parameter != 0)
                {
                    _windowsService.OpenDropGroupListWindow(_token, parameter, ItemDropGroupType.ItemDropGroupList);
                }
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"Error: {ex.Message}", "Error");
            }
        }
        #endregion

        #region Open ItemDropGroupItemListF

        [RelayCommand]
        private void OpenItemDropGroupItemListF(int parameter)
        {
            try
            {
                if (parameter != 0)
                {
                    _windowsService.OpenDropGroupListWindow(_token, parameter, ItemDropGroupType.ItemDropGroupListF);
                }
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"Error: {ex.Message}", "Error");
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

            switch (EnemyType)
            {
                case EnemyType.Enemy:
                    columns.Add("CONVERT(wszName, 'System.String')");
                    columns.Add("CONVERT(nQuestItemDropGroup, 'System.String')");
                    columns.Add("CONVERT(nChampionItemDropGroup, 'System.String')");
                    columns.Add("CONVERT(nEventItemDropGroup, 'System.String')");
                    columns.Add("CONVERT(nInstanceItemDropGroup, 'System.String')");
                    columns.Add("CONVERT(nRareCardItemDropGroup, 'System.String')");

                    for (int i = 0; i < 3; i++)
                    {
                        columns.Add($"CONVERT(nItemDropGroup{i:00}, 'System.String')");
                        columns.Add($"CONVERT(nItemDropGroupF{i:00}, 'System.String')");
                        columns.Add($"CONVERT(nFatigueItemDropGroup{i:00}, 'System.String')");
                        columns.Add($"CONVERT(nFatigueItemDropGroupF{i:00}, 'System.String')");
                        columns.Add($"CONVERT(nHiddenItemDropGroup{i:00}, 'System.String')");
                        columns.Add($"CONVERT(nHiddenItemDropGroupF{i:00}, 'System.String')");
                    }

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

        #region DataRowViewMessage
        public void Receive(DataRowViewMessage message)
        {
            if (message.Token == _token)
            {
                var selectedItem = message.Value;

                switch (EnemyType)
                {
                    case EnemyType.Enemy:
                        UpdateEnemySelectedItem(selectedItem);
                        break;
                }

            }
        }

        #region Quest
        private void UpdateEnemySelectedItem(DataRowView? selectedItem)
        {
            _isUpdatingSelectedItem = true;

            if (selectedItem != null)
            {
                EnemyDropGroups ??= [];

                for (int i = 0; i < 3; i++)
                {
                    int itemDropGroup = (int)selectedItem[$"nItemDropGroup{i:00}"];
                    int itemDropCount = (int)selectedItem[$"nItemDropCount{i:00}"];
                    int itemDropGroupF = (int)selectedItem[$"nItemDropGroupF{i:00}"];
                    int itemDropCountF = (int)selectedItem[$"nItemDropCountF{i:00}"];
                    int fatigueItemDropGroup = (int)selectedItem[$"nFatigueItemDropGroup{i:00}"];
                    int fatigueItemDropCount = (int)selectedItem[$"nFatigueItemDropCount{i:00}"];
                    int fatigueItemDropGroupF = (int)selectedItem[$"nFatigueItemDropGroupF{i:00}"];
                    int fatigueItemDropCountF = (int)selectedItem[$"nFatigueItemDropCountF{i:00}"];
                    int hiddenItemDropGroup = (int)selectedItem[$"nHiddenItemDropGroup{i:00}"];
                    int hiddenItemDropCount = (int)selectedItem[$"nHiddenItemDropCount{i:00}"];
                    int hiddenItemDropGroupF = (int)selectedItem[$"nHiddenItemDropGroupF{i:00}"];
                    int hiddenItemDropCountF = (int)selectedItem[$"nHiddenItemDropCountF{i:00}"];

                    if (i < EnemyDropGroups.Count)
                    {
                        var existingItem = EnemyDropGroups[i];

                        existingItem.ItemDropGroup = itemDropGroup;
                        existingItem.ItemDropCount = itemDropCount;
                        existingItem.ItemDropGroupF = itemDropGroupF;
                        existingItem.ItemDropCountF = itemDropCountF;
                        existingItem.FatigueItemDropGroup = itemDropGroup;
                        existingItem.FatigueItemDropCount = itemDropCount;
                        existingItem.FatigueItemDropGroupF = fatigueItemDropGroupF;
                        existingItem.FatigueItemDropCountF = fatigueItemDropCountF;
                        existingItem.HiddenItemDropGroup = hiddenItemDropGroup;
                        existingItem.HiddenItemDropCount = hiddenItemDropCount;
                        existingItem.HiddenItemDropGroupF = hiddenItemDropGroupF;
                        existingItem.HiddenItemDropCountF = hiddenItemDropCountF;

                        EnemyDropGroupItemPropertyChanged(existingItem);
                    }
                    else
                    {
                        var enemyDropGroup = new Enemy
                        {
                            ItemDropGroup = itemDropGroup,
                            ItemDropCount = itemDropCount,
                            ItemDropGroupF = itemDropGroupF,
                            ItemDropCountF = itemDropCountF,
                            FatigueItemDropGroup = itemDropGroup,
                            FatigueItemDropCount = itemDropCount,
                            FatigueItemDropGroupF = fatigueItemDropGroupF,
                            FatigueItemDropCountF = fatigueItemDropCountF,
                            HiddenItemDropGroup = hiddenItemDropGroup,
                            HiddenItemDropCount = hiddenItemDropCount,
                            HiddenItemDropGroupF = hiddenItemDropGroupF,
                            HiddenItemDropCountF = hiddenItemDropCountF,
                        };

                        Application.Current.Dispatcher.Invoke(() => EnemyDropGroups.Add(enemyDropGroup));
                        EnemyDropGroupItemPropertyChanged(enemyDropGroup);
                    }
                }

                EnemyConditionResistance ??= [];

                for (int i = 0; i < 10; i++)
                {
                    string conditionResistance = (string)selectedItem[$"szConditionResistance{i + 1:00}"];
                    double conditionResistanceValue = (float)selectedItem[$"fConditionResistanceValue{i + 1:00}"];

                    if (i < EnemyConditionResistance.Count)
                    {
                        var existingItem = EnemyConditionResistance[i];

                        existingItem.ConditionResistance = conditionResistance;
                        existingItem.ConditionResistanceValue = conditionResistanceValue;

                        EnemyConditionResistanceItemPropertyChanged(existingItem);
                    }
                    else
                    {
                        var enemyConditionResistance = new Enemy
                        {
                            ConditionResistance = conditionResistance,
                            ConditionResistanceValue = conditionResistanceValue
                        };

                        Application.Current.Dispatcher.Invoke(() => EnemyConditionResistance.Add(enemyConditionResistance));
                        EnemyConditionResistanceItemPropertyChanged(enemyConditionResistance);
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

        private void EnemyDropGroupItemPropertyChanged(Enemy item)
        {
            item.PropertyChanged -= OnEnemyDropGroupItemPropertyChanged;

            item.PropertyChanged += OnEnemyDropGroupItemPropertyChanged;
        }

        private void OnEnemyDropGroupItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is Enemy enemyDropGroup)
            {
                int index = EnemyDropGroups.IndexOf(enemyDropGroup);
                UpdateSelectedItemValue(enemyDropGroup.ItemDropGroup, $"nItemDropGroup{index:00}");
                UpdateSelectedItemValue(enemyDropGroup.ItemDropCount, $"nItemDropCount{index:00}");
                UpdateSelectedItemValue(enemyDropGroup.ItemDropGroupF, $"nItemDropGroupF{index:00}");
                UpdateSelectedItemValue(enemyDropGroup.ItemDropCountF, $"nItemDropCountF{index:00}");
                UpdateSelectedItemValue(enemyDropGroup.FatigueItemDropGroup, $"nFatigueItemDropGroup{index:00}");
                UpdateSelectedItemValue(enemyDropGroup.FatigueItemDropCount, $"nFatigueItemDropCount{index:00}");
                UpdateSelectedItemValue(enemyDropGroup.FatigueItemDropGroupF, $"nFatigueItemDropGroupF{index:00}");
                UpdateSelectedItemValue(enemyDropGroup.FatigueItemDropCountF, $"nFatigueItemDropCountF{index:00}");
                UpdateSelectedItemValue(enemyDropGroup.HiddenItemDropGroup, $"nHiddenItemDropGroup{index:00}");
                UpdateSelectedItemValue(enemyDropGroup.HiddenItemDropCount, $"nHiddenItemDropCount{index:00}");
                UpdateSelectedItemValue(enemyDropGroup.HiddenItemDropGroupF, $"nHiddenItemDropGroupF{index:00}");
                UpdateSelectedItemValue(enemyDropGroup.HiddenItemDropCountF, $"nHiddenItemDropCountF{index:00}");
            }
        }

        private void EnemyConditionResistanceItemPropertyChanged(Enemy item)
        {
            item.PropertyChanged -= OnEnemyConditionResistanceItemPropertyChanged;

            item.PropertyChanged += OnEnemyConditionResistanceItemPropertyChanged;
        }

        private void OnEnemyConditionResistanceItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is Enemy enemyConditionResistance)
            {
                int index = EnemyDropGroups.IndexOf(enemyConditionResistance);
                UpdateSelectedItemValue(enemyConditionResistance.ConditionResistance, $"szConditionResistance{index + 1:00}");
                UpdateSelectedItemValue(enemyConditionResistance.ConditionResistanceValue, $"fConditionResistanceValue{index + 1:00}");
            }
        }

        #endregion

        #endregion

        #region Properties
        [ObservableProperty]
        private string _title = $"Enemy Editor";

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
        private EnemyType _enemyType;

        [ObservableProperty]
        private ObservableCollection<Enemy> _enemyDropGroups = [];

        [ObservableProperty]
        private ObservableCollection<Enemy> _enemyConditionResistance = [];

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
