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
using System.ComponentModel;
using System.Data;
using System.Windows.Controls;
using static RHToolkit.Models.EnumService;

namespace RHToolkit.ViewModels.Windows
{
    public partial class WorldEditorViewModel : ObservableObject, IRecipient<DataRowViewMessage>
    {
        private readonly Guid _token;
        private readonly IWindowsService _windowsService;
        private readonly IGMDatabaseService _gmDatabaseService;
        private readonly System.Timers.Timer _filterUpdateTimer;

        public WorldEditorViewModel(IWindowsService windowsService, IGMDatabaseService gmDatabaseService, ItemDataManager itemDataManager)
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

            PopulateListItems();

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

                if (int.TryParse(parameter, out int type))
                {
                    string? fileName = GetFileNameFromFileType(type);
                    if (fileName == null) return;
                    int fileType = GetFileTypeFromFileName(fileName);
                    string columnName = GetColumnName(fileName);
                    string? stringFileName = GetStringFileName(fileType);

                    WorldType = (WorldType)fileType;

                    bool isLoaded = await DataTableManager.LoadFileFromPath(fileName, stringFileName, columnName, "World");

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

                string filter = "World Files .rh|" +
                                "world.rh;mapselect_curtis.rh;dungeoninfolist.rh;|" +
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
                        RHMessageBoxHelper.ShowOKMessage($"The file '{fileName}' is not a valid World file.", Resources.Error);
                        return;
                    }

                    string columnName = GetColumnName(fileName);
                    string? stringFileName = GetStringFileName(fileType);

                    WorldType = (WorldType)fileType;

                    bool isLoaded = await DataTableManager.LoadFileAs(openFileDialog.FileName, stringFileName, columnName, "World");

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
            Title = $"World Editor ({DataTableManager.CurrentFileName})";
            OpenMessage = "";
            IsVisible = Visibility.Visible;
            OnCanExecuteFileCommandChanged();
        }

        private static string? GetFileNameFromFileType(int type)
        {
            return type switch
            {
                1 => "world.rh",
                2 => "mapselect_curtis.rh",
                3 => "dungeoninfolist.rh",
                _ => throw new ArgumentOutOfRangeException(nameof(type)),
            };
        }

        private static int GetFileTypeFromFileName(string fileName)
        {
            return fileName switch
            {
                "world.rh" => 1,
                "mapselect_curtis.rh" => 2,
                "dungeoninfolist.rh" => 3,
                _ => -1,
            };
        }

        private static string GetColumnName(string fileName)
        {
            return fileName switch
            {
                "world.rh" => "nWorldLevel",
                "mapselect_curtis.rh" => "nSelectType00",
                "dungeoninfolist.rh" => "nNotShow",
                _ => "",
            };
        }

        private static string? GetStringFileName(int type)
        {
            return type switch
            {
                1 => "world_string.rh",
                _ => null,
            };
        }

        [RelayCommand(CanExecute = nameof(CanExecuteFileCommand))]
        private void OpenSearchDialog(string? parameter)
        {
            try
            {
                Window? enemyEditorWindow = Application.Current.Windows.OfType<WorldEditorWindow>().FirstOrDefault();
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
            Title = $"World Editor";
            OpenMessage = "Open a file";
            WorldData?.Clear();
            WorldPreload?.Clear();
            SearchText = string.Empty;
            IsVisible = Visibility.Hidden;
            OnCanExecuteFileCommandChanged();
        }

        private bool CanExecuteFileCommand()
        {
            return DataTableManager.DataTable != null;
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

        #region Open EventWorldItemDropGroupList

        [RelayCommand]
        private void OpenEventWorldItemDropGroupList(int parameter)
        {
            try
            {
                if (parameter != 0)
                {
                    _windowsService.OpenDropGroupListWindow(_token, parameter, ItemDropGroupType.EventWorldItemDropGroupList);
                }
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"Error: {ex.Message}", "Error");
            }
        }
        #endregion

        #region Open WorldItemDropGroupList

        [RelayCommand]
        private void OpenWorldItemDropGroupList(int parameter)
        {
            try
            {
                if (parameter != 0)
                {
                    _windowsService.OpenDropGroupListWindow(_token, parameter, ItemDropGroupType.WorldItemDropGroupListF);
                }
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"Error: {ex.Message}", "Error");
            }
        }
        #endregion

        #region Open WorldInstanceItemDropGroupList

        [RelayCommand]
        private void OpenWorldInstanceItemDropGroupList(int parameter)
        {
            try
            {
                if (parameter != 0)
                {
                    _windowsService.OpenDropGroupListWindow(_token, parameter, ItemDropGroupType.WorldInstanceItemDropGroupList);
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

            switch (WorldType)
            {
                case WorldType.World:
                    columns.Add("CONVERT(szName, 'System.String')");
                    columns.Add("CONVERT(szCategory, 'System.String')");
                    columns.Add("CONVERT(nRootVillageID, 'System.String')");
                    columns.Add("CONVERT(nWorldItemDropGroupList, 'System.String')");
                    columns.Add("CONVERT(nEventWorldItemDropGroupList, 'System.String')");
                    columns.Add("CONVERT(nWorldInstanceItemDropGroupList, 'System.String')");
                    break;
                case WorldType.MapSelectCurtis:
                    columns.Add("CONVERT(szMapSwfdata, 'System.String')");

                    for (int i = 0; i < 10; i++)
                    {
                        columns.Add($"CONVERT(nWorldID{i:00}, 'System.String')");
                        columns.Add($"CONVERT(szBigSprite{i:00}, 'System.String')");
                    }
                    break;
                case WorldType.DungeonInfoList:
                    columns.Add("CONVERT(nCategory00, 'System.String')");
                    columns.Add("CONVERT(nCategory01, 'System.String')");
                    columns.Add("CONVERT(nDungeonID, 'System.String')");
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
        private List<NameID>? _worldItems;

        [ObservableProperty]
        private List<string>? _enemyItems;

        [ObservableProperty]
        private List<NameID>? _difficultyItems;

        private void PopulateListItems()
        {
            DifficultyItems =
            [
                new NameID { ID = 1, Name = "Normal" },
                new NameID { ID = 2, Name = "Hard" },
                new NameID { ID = 3, Name = "Very Hard" },
                new NameID { ID = 4, Name = "Blood" }
            ];
            WorldItems = _gmDatabaseService.GetWorldNameItems();
            EnemyItems = _gmDatabaseService.GetEnemyNameItems();
        }

        #endregion

        #region DataRowViewMessage
        public void Receive(DataRowViewMessage message)
        {
            if (message.Token == _token)
            {
                var selectedItem = message.Value;

                switch (WorldType)
                {
                    case WorldType.World:
                        UpdateWorldSelectedItem(selectedItem);
                        break;
                    case WorldType.MapSelectCurtis:
                        UpdateMapSelectSelectedItem(selectedItem);
                        break;
                    default:
                        UpdateSelectedItem(selectedItem);
                        break;
                }

            }
        }

        #region SelectedItem
        private void UpdateSelectedItem(DataRowView? selectedItem)
        {
            _isUpdatingSelectedItem = true;

            if (selectedItem != null)
            {
                IsSelectedItemVisible = Visibility.Visible;
            }
            else
            {
                IsSelectedItemVisible = Visibility.Hidden;
            }

            _isUpdatingSelectedItem = false;
        }

        #endregion

        #region World
        private void UpdateWorldSelectedItem(DataRowView? selectedItem)
        {
            _isUpdatingSelectedItem = true;

            if (selectedItem != null)
            {
                WorldPreload ??= [];

                for (int i = 0; i < 4; i++)
                {
                    string preloadShoot = (string)selectedItem[$"szPreloadShoot{i + 1:00}"];

                    if (i < WorldPreload.Count)
                    {
                        var existingItem = WorldPreload[i];

                        existingItem.PreloadShoot = preloadShoot;

                        WorldPreloadItemPropertyChanged(existingItem);
                    }
                    else
                    {
                        var preload = new WorldData
                        {
                            PreloadShoot = preloadShoot,
                        };

                        Application.Current.Dispatcher.Invoke(() => WorldPreload.Add(preload));
                        WorldPreloadItemPropertyChanged(preload);
                    }
                }

                WorldData ??= [];

                for (int i = 0; i < 3; i++)
                {
                    int stageClearTime = (int)selectedItem[$"nStageClearTime{i:00}"];
                    int stageClearPoint = (int)selectedItem[$"nStageClearPoint{i:00}"];

                    if (i < WorldData.Count)
                    {
                        var existingItem = WorldData[i];

                        existingItem.StageClearTime = stageClearTime;
                        existingItem.StageClearPoint = stageClearPoint;

                        WorldStageClearItemPropertyChanged(existingItem);
                    }
                    else
                    {
                        var preload = new WorldData
                        {
                            StageClearTime = stageClearTime,
                            StageClearPoint = stageClearPoint
                        };

                        Application.Current.Dispatcher.Invoke(() => WorldData.Add(preload));
                        WorldStageClearItemPropertyChanged(preload);
                    }
                }

                IsSelectedItemVisible = Visibility.Visible;
            }
            else
            {
                IsSelectedItemVisible = Visibility.Hidden;
            }

            _isUpdatingSelectedItem = false;
        }

        private void WorldPreloadItemPropertyChanged(WorldData item)
        {
            item.PropertyChanged -= OnWorldPreloadItemPropertyChanged;

            item.PropertyChanged += OnWorldPreloadItemPropertyChanged;
        }

        private void OnWorldPreloadItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is WorldData worldPreload)
            {
                int index = WorldPreload.IndexOf(worldPreload);
                UpdateSelectedItemValue(worldPreload.PreloadShoot, $"szPreloadShoot{index + 1:00}");
            }
        }

        private void WorldStageClearItemPropertyChanged(WorldData item)
        {
            item.PropertyChanged -= OnWorldStageClearItemPropertyChanged;

            item.PropertyChanged += OnWorldStageClearItemPropertyChanged;
        }

        private void OnWorldStageClearItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is WorldData worldData)
            {
                int index = WorldData.IndexOf(worldData);
                UpdateSelectedItemValue(worldData.StageClearTime, $"nStageClearTime{index:00}");
                UpdateSelectedItemValue(worldData.StageClearPoint, $"nStageClearPoint{index:00}");
            }
        }

        #endregion

        #region MapSelect
        private void UpdateMapSelectSelectedItem(DataRowView? selectedItem)
        {
            _isUpdatingSelectedItem = true;

            if (selectedItem != null)
            {
                WorldData ??= [];

                for (int i = 0; i < 10; i++)
                {
                    int selectType = (int)selectedItem[$"nSelectType{i:00}"];
                    int worldID = (int)selectedItem[$"nWorldID{i:00}"];
                    string menuCommand = (string)selectedItem[$"szMC{i:00}"];
                    string bigSprite = (string)selectedItem[$"szBigSprite{i:00}"];

                    if (i < WorldData.Count)
                    {
                        var existingItem = WorldData[i];

                        existingItem.SelectType = selectType;
                        existingItem.WorldID = worldID;
                        existingItem.Mc = menuCommand;
                        existingItem.BigSprite = bigSprite;

                        MapSelectItemPropertyChanged(existingItem);
                    }
                    else
                    {
                        var mapSelect = new WorldData
                        {
                            StageClearTime = selectType,
                            StageClearPoint = worldID,
                            Mc = menuCommand,
                            BigSprite = bigSprite
                        };

                        Application.Current.Dispatcher.Invoke(() => WorldData.Add(mapSelect));
                        MapSelectItemPropertyChanged(mapSelect);
                    }
                }

                IsSelectedItemVisible = Visibility.Visible;
            }
            else
            {
                IsSelectedItemVisible = Visibility.Hidden;
            }

            _isUpdatingSelectedItem = false;
        }

        private void MapSelectItemPropertyChanged(WorldData item)
        {
            item.PropertyChanged -= OnMapSelectItemPropertyChanged;

            item.PropertyChanged += OnMapSelectItemPropertyChanged;
        }

        private void OnMapSelectItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is WorldData mapSelect)
            {
                int index = WorldPreload.IndexOf(mapSelect);
                UpdateSelectedItemValue(mapSelect.SelectType, $"nSelectType{index:00}");
                UpdateSelectedItemValue(mapSelect.WorldID, $"nWorldID{index:00}");
                UpdateSelectedItemValue(mapSelect.Mc, $"szMC{index:00}");
                UpdateSelectedItemValue(mapSelect.BigSprite, $"szBigSprite{index:00}");
            }
        }

        #endregion

        #endregion

        #region Properties
        [ObservableProperty]
        private string _title = $"World Editor";

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
        private WorldType _worldType;

        [ObservableProperty]
        private ObservableCollection<WorldData> _worldData = [];

        [ObservableProperty]
        private ObservableCollection<WorldData> _worldPreload = [];


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
