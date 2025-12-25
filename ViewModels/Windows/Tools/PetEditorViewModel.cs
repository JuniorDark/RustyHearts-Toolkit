using Microsoft.Win32;
using RHToolkit.Messages;
using RHToolkit.Models;
using RHToolkit.Models.Database;
using RHToolkit.Models.Editor;
using RHToolkit.Models.MessageBox;
using RHToolkit.Models.Model3D;
using RHToolkit.Models.UISettings;
using RHToolkit.Services;
using RHToolkit.Views.Windows;
using System.Data;
using System.Windows.Controls;

namespace RHToolkit.ViewModels.Windows
{
    public partial class PetEditorViewModel : ObservableObject, IRecipient<ItemDataMessage>, IRecipient<DataRowViewMessage>
    {
        private readonly Guid _token;
        private readonly IWindowsService _windowsService;
        private readonly IGMDatabaseService _gmDatabaseService;
        private readonly System.Timers.Timer _filterUpdateTimer;

        public PetEditorViewModel(IWindowsService windowsService, IGMDatabaseService gmDatabaseService, ItemDataManager itemDataManager)
        {
            _token = Guid.NewGuid();
            _windowsService = windowsService;
            _gmDatabaseService = gmDatabaseService;
            _itemDataManager = itemDataManager;

            DataTableManager = new()
            {
                Token = _token
            };

            ModelView = new ModelViewManager
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
        private async Task LoadFile()
        {
            try
            {
                await CloseFile();

                bool isLoaded = await DataTableManager.LoadFileFromPath("pet.rh", "pet_string.rh", "nPetType", "Pet");

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

                string filter = "Pet Files|" +
                                "pet.rh|" +
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
                        string message = string.Format(Resources.InvalidTableFileDesc, fileName, "Pet");
                        RHMessageBoxHelper.ShowOKMessage(message, Resources.Error);
                        return;
                    }

                    string? stringFileName = GetStringFileName(fileType);

                    bool isLoaded = await DataTableManager.LoadFileAs(openFileDialog.FileName, stringFileName, "nPetType", "Pet");

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
        private async Task LoadFileFromPCK()
        {
            try
            {
                await CloseFile();

                bool isLoaded = await DataTableManager.LoadFileFromPCK("pet.rh", "pet_string.rh");

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

        private static string? GetFileName(int petType)
        {
            return petType switch
            {
                1 => "pet.rh",
                _ => throw new ArgumentOutOfRangeException(nameof(petType)),
            };
        }

        private static string? GetStringFileName(int petType)
        {
            return petType switch
            {
                1 => "pet_string.rh",
                _ => throw new ArgumentOutOfRangeException(nameof(petType)),
            };
        }

        private static int GetFileTypeFromFileName(string fileName)
        {
            return fileName switch
            {
                "pet.rh" => 1,
                _ => -1,
            };
        }

        private void IsLoaded()
        {
            Title = string.Format(Resources.EditorTitleFileName, "Pet", DataTableManager.CurrentFileName);
            OpenMessage = "";
            IsVisible = Visibility.Visible;
            OnCanExecuteFileCommandChanged();
        }

        [RelayCommand(CanExecute = nameof(CanExecuteFileCommand))]
        private void OpenSearchDialog(string? parameter)
        {
            try
            {
                Window? window = Application.Current.Windows.OfType<PetEditorWindow>().FirstOrDefault();
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
            Title = string.Format(Resources.EditorTitle, "Pet");
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
                DataTableManager.StartGroupingEdits();
                DataTableManager.AddNewRow();
                if (DataTableManager.SelectedItem != null)
                {
                    DataTableManager.SelectedItem["wszName"] = "New Pet";
                }

                if (DataTableManager.SelectedItemString != null)
                {
                    DataTableManager.SelectedItemString["wszName"] = "New Pet";
                }
                DataTableManager.EndGroupingEdits();
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
            }
        }
        #endregion

        #region Add Item

        [RelayCommand]
        private void AddPetDeathDropItem()
        {
            try
            {
                if (DataTableManager.SelectedItem != null)
                {
                    var itemCode = (int)DataTableManager.SelectedItem[$"nDeathDropItemID"];

                    var itemData = new ItemData
                    {
                        ItemId = itemCode
                    };

                    _windowsService.OpenItemWindow(_token, "PetDeathDropItem", itemData);
                }
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
            }
        }

        public void Receive(ItemDataMessage message)
        {
            if (message.Recipient == "PetEditorWindow" && message.Token == _token)
            {
                var itemData = message.Value;

                if (DataTableManager.SelectedItem != null)
                {
                    var itemCode = itemData.ItemId;
                    UpdateSelectedItemValue(itemCode, "nDeathDropItemID");

                    var itemDataViewModel = ItemDataManager.GetItemDataViewModel(itemCode, 0, 1);

                    ItemDataManager.ItemDataViewModel = itemDataViewModel!;
                }
            }
        }

        #endregion

        #region Remove Item

        [RelayCommand]
        private void RemovePetDeathDropItem()
        {
            if (DataTableManager.SelectedItem != null)
            {
                UpdateSelectedItemValue(0, "nDeathDropItemID");
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
                var itemCode = (int)selectedItem[$"nDeathDropItemID"];
                var itemDataViewModel = ItemDataManager.GetItemDataViewModel(itemCode, 0, 1);

                ItemDataManager.ItemDataViewModel = itemDataViewModel!;

                IsSelectedItemVisible = Visibility.Visible;
            }
            else
            {
                IsSelectedItemVisible = Visibility.Hidden;
            }

            _isUpdatingSelectedItem = false;
        }

        #endregion

        #region Open Model Preview Window

        [RelayCommand]
        private async Task OpenModelPreviewWindow(string parameter)
        {
            try
            {
                if (!string.IsNullOrEmpty(parameter))
                {

                    string clientAssetsFolder = RegistrySettingsHelper.GetClientAssetsFolder();

                    if (string.IsNullOrEmpty(clientAssetsFolder) || !Directory.Exists(clientAssetsFolder))
                    {
                        var openFolderDialog = new OpenFolderDialog();

                        if (openFolderDialog.ShowDialog() == true)
                        {
                            clientAssetsFolder = openFolderDialog.FolderName;
                            RegistrySettingsHelper.SetClientAssetsFolder(clientAssetsFolder);
                        }
                        else
                        {
                            return;
                        }
                    }

                    var modelPath = Path.Combine(clientAssetsFolder, parameter.ToLower());

                    if (!File.Exists(modelPath))
                    {
                        RHMessageBoxHelper.ShowOKMessage(
                                    string.Format(Resources.FileNotFoundMessage, modelPath),
                                    Resources.Error
                                );
                        return;
                    }

                    var modelData = new ModelType
                    {
                        FilePath = modelPath,
                        Format = ModelFormat.MDATA,
                    };

                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        _windowsService.OpenModelViewWindow(_token, modelData, ModelView!);
                    });
                }
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
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
            columns.Add("wszName");
            columns.Add("szIcon");

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
        private List<NameID>? _stringItems;

        [ObservableProperty]
        private List<NameID>? _petRebirthItems;

        private void PopulateListItems()
        {
            try
            {
                PetRebirthItems = _gmDatabaseService.GetPetRebirthItems();
                StringItems = _gmDatabaseService.GetStringItems();
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
            }
            
        }
        #endregion

        #region Properties
        [ObservableProperty]
        private string _title = string.Format(Resources.EditorTitle, "Pet");

        [ObservableProperty]
        private string? _openMessage = Resources.OpenFile;

        [ObservableProperty]
        private Visibility _isSelectedItemVisible = Visibility.Hidden;

        [ObservableProperty]
        private Visibility _isVisible = Visibility.Hidden;

        [ObservableProperty]
        private ModelViewManager? _modelView;

        [ObservableProperty]
        private DataTableManager _dataTableManager;
        partial void OnDataTableManagerChanged(DataTableManager value)
        {
            OnCanExecuteFileCommandChanged();
        }

        [ObservableProperty]
        private ItemDataManager _itemDataManager;

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
