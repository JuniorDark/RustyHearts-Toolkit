using Microsoft.Win32;
using RHToolkit.Messages;
using RHToolkit.Models;
using RHToolkit.Models.Database;
using RHToolkit.Models.Editor;
using RHToolkit.Models.MessageBox;
using RHToolkit.Properties;
using RHToolkit.Services;
using RHToolkit.Views.Windows;
using System.Data;
using System.Windows.Controls;

namespace RHToolkit.ViewModels.Windows
{
    public partial class NpcEditorViewModel : ObservableObject, IRecipient<DataRowViewMessage>
    {
        private readonly Guid _token;
        private readonly IWindowsService _windowsService;
        private readonly IGMDatabaseService _gmDatabaseService;
        private readonly System.Timers.Timer _filterUpdateTimer;

        public NpcEditorViewModel(IWindowsService windowsService, IGMDatabaseService gmDatabaseService, ItemDataManager itemDataManager)
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
                Interval = 400,
                AutoReset = false
            };
            _filterUpdateTimer.Elapsed += FilterUpdateTimerElapsed;
            PopulateListItems();
            WeakReferenceMessenger.Default.Register(this);
        }

        #region Commands 

        #region File

        [RelayCommand]
        private async Task LoadFile()
        {
            try
            {
                await CloseFile();

                bool isLoaded = await DataTableManager.LoadFileFromPath("npcinstance.rh", "npcinstance_string.rh", "nNPCRole", "Npc");

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

                string filter = "npcinstance.rh|" +
                                "npcinstance.rh|" +
                                "All Files (*.*)|*.*";

                OpenFileDialog openFileDialog = new()
                {
                    Filter = filter,
                    FilterIndex = 1
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    string fileName = Path.GetFileName(openFileDialog.FileName);

                    bool isLoaded = await DataTableManager.LoadFileAs(openFileDialog.FileName, "npcinstance_string.rh", "nNPCRole", "Npc");

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
            Title = $"NPC Editor ({DataTableManager.CurrentFileName})";
            OpenMessage = "";
            IsVisible = Visibility.Visible;
            OnCanExecuteFileCommandChanged();
        }

        [RelayCommand(CanExecute = nameof(CanExecuteFileCommand))]
        private void OpenSearchDialog(string? parameter)
        {
            try
            {
                Window? shopEditorWindow = Application.Current.Windows.OfType<NpcEditorWindow>().FirstOrDefault();
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
            Title = $"NPC Editor";
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

        private void OnCanExecuteFileCommandChanged()
        {
            CloseFileCommand.NotifyCanExecuteChanged();
            OpenSearchDialogCommand.NotifyCanExecuteChanged();
            AddRowCommand.NotifyCanExecuteChanged();
        }

        #endregion

        #region Open Shop Window

        [RelayCommand]
        private void OpenShopWindow(NameID parameter)
        {
            try
            {
                if (parameter != null && parameter.ID != 0)
                {
                    var shopTitle = parameter.Type == "NpcShop" ? SelectedShopTitle : SelectedTradeShopTitle;

                    _windowsService.OpenNpcShopWindow(_token, parameter, shopTitle);
                }
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"Error: {ex.Message}", "Error");
            }
        }
        #endregion

        #region Open ItemMix Window

        [RelayCommand]
        private void OpenItemMixWindow(string parameter)
        {
            try
            {
                if (parameter != null)
                {
                    _windowsService.OpenItemMixWindow(_token, parameter, "ItemMix");
                }
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"Error: {ex.Message}", "Error");
            }
        }

        [RelayCommand]
        private void OpenCostumeMixWindow(string parameter)
        {
            try
            {
                if (parameter != null)
                {
                    _windowsService.OpenItemMixWindow(_token, parameter, "CostumeMix");
                }
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"Error: {ex.Message}", "Error");
            }
        }
        #endregion

        #region Add Row

        [RelayCommand(CanExecute = nameof(CanExecuteFileCommand))]
        private void AddRow()
        {
            try
            {
                DataTableManager.StartGroupingEdits();
                DataTableManager.AddNewRow();
                if (DataTableManager.SelectedItem != null)
                {
                    DataTableManager.SelectedItem["szName"] = "new_npc";
                    DataTableManager.SelectedItem["wszDesc"] = "New Npc";
                }

                if (DataTableManager.SelectedItemString != null)
                {
                    DataTableManager.SelectedItemString["wszDesc"] = "New Npc";
                }
                DataTableManager.EndGroupingEdits();
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"Error: {ex.Message}", "Error");
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
            QuestGroup?.Clear();
            RandomQuestGroup?.Clear();
            ShopID?.Clear();

            if (selectedItem != null)
            {
                NpcRole = (int)selectedItem["nNPCRole"];

                QuestGroup = [];
                RandomQuestGroup = [];
                ShopID = [];

                for (int i = 0; i < 5; i++)
                {
                    var item = new NpcInstance
                    {
                        RandomQuestGroup = (int)selectedItem[$"nRandomQuestGroup{i:00}"],
                        RandomProbability = (float)selectedItem[$"fRandomProbability{i:00}"]
                    };

                    RandomQuestGroup.Add(item);

                    RandomQuestGroupPropertyChanged(item, i);
                }

                for (int i = 0; i < 10; i++)
                {
                    var item = new NpcInstance
                    {
                        QuestGroup = (int)selectedItem[$"nQuestGroup{i:00}"],
                        MissionGroup = (int)selectedItem[$"nMissionGroup{i:00}"]
                    };

                    QuestGroup.Add(item);

                    QuestGroupPropertyChanged(item, i);
                }

                for (int i = 0; i < 13; i++)
                {
                    var item = new NpcInstance
                    {
                        ShopID = (int)selectedItem[$"nShopID{i:00}"]
                    };

                    ShopID.Add(item);

                    ShopIDPropertyChanged(item, i);
                }

                IsSelectedItemVisible = Visibility.Visible;
            }
            else
            {
                IsSelectedItemVisible = Visibility.Hidden;
            }

            _isUpdatingSelectedItem = false;
        }

        private void RandomQuestGroupPropertyChanged(NpcInstance item, int index)
        {
            item.PropertyChanged += (s, e) =>
            {
                if (s is NpcInstance randomQuest)
                {
                    UpdateSelectedItemValue(randomQuest.RandomQuestGroup, $"nRandomQuestGroup{index:00}");
                    UpdateSelectedItemValue(randomQuest.RandomProbability, $"fRandomProbability{index:00}");
                }
            };
        }

        private void QuestGroupPropertyChanged(NpcInstance item, int index)
        {
            item.PropertyChanged += (s, e) =>
            {
                if (s is NpcInstance quest)
                {
                    UpdateSelectedItemValue(quest.QuestGroup, $"nQuestGroup{index:00}");
                    UpdateSelectedItemValue(quest.MissionGroup, $"nMissionGroup{index:00}");
                }
            };
        }

        private void ShopIDPropertyChanged(NpcInstance item, int index)
        {
            item.PropertyChanged += (s, e) =>
            {
                if (s is NpcInstance shopID)
                {
                    UpdateSelectedItemValue(shopID.ShopID, $"nShopID{index:00}");
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
            columns.Add("CONVERT(nNpcID, 'System.String')");
            columns.Add("szSpriteName");

            for (int i = 0; i < 13; i++)
            {
                string columnName = $"nShopID{i:00}";
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
        private List<NameID>? _questGroupItems;

        [ObservableProperty]
        private List<NameID>? _stringItems;

        [ObservableProperty]
        private List<NameID>? _itemMixItems;

        [ObservableProperty]
        private List<NameID>? _costumeMixItems;

        [ObservableProperty]
        private List<NameID>? _tradeShopItems;

        [ObservableProperty]
        private List<NameID>? _npcShopsItems;

        [ObservableProperty]
        private List<NameID>? _npcListItems;

        [ObservableProperty]
        private List<NameID>? _npcDialogItems;

        private void PopulateListItems()
        {
            try
            {
                QuestGroupItems = _gmDatabaseService.GetQuestGroupItems();
                StringItems = _gmDatabaseService.GetStringItems();
                ItemMixItems = _gmDatabaseService.GetItemMixGroupItems();
                CostumeMixItems = _gmDatabaseService.GetCostumeMixGroupItems();
                TradeShopItems = _gmDatabaseService.GetTradeItemGroupItems();
                NpcShopsItems = _gmDatabaseService.GetNpcShopsItems();
                NpcListItems = _gmDatabaseService.GetNpcListItems();
                NpcDialogItems = _gmDatabaseService.GetNpcDialogItems();
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"Error: {ex.Message}", "Error");
            }
            
        }

        #endregion

        #region Properties
        [ObservableProperty]
        private string _title = $"NPC Editor";

        [ObservableProperty]
        private string? _openMessage = "Open a file";

        [ObservableProperty]
        private string? _titleEffectText;

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
        private ObservableCollection<NpcInstance> _questGroup = [];

        [ObservableProperty]
        private ObservableCollection<NpcInstance> _randomQuestGroup = [];

        [ObservableProperty]
        private ObservableCollection<NpcInstance> _shopID = [];

        [ObservableProperty]
        private NameID? _selectedShopTitle;

        [ObservableProperty]
        private NameID? _selectedTradeShopTitle;

        [ObservableProperty]
        private NameID? _selectedItemMix;

        [ObservableProperty]
        private NameID? _selectedCostumeMix;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(NpcRoleText))]
        private int _npcRole;

        public string? NpcRoleText => NpcRole != 0 ? $"<{_gmDatabaseService.GetString(NpcRole)}>" : string.Empty;
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
