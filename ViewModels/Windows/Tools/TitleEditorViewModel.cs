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
using System.ComponentModel;
using System.Data;
using System.Windows.Controls;

namespace RHToolkit.ViewModels.Windows
{
    public partial class TitleEditorViewModel : ObservableObject, IRecipient<DataRowViewMessage>
    {
        private readonly Guid _token;
        private readonly IWindowsService _windowsService;
        private readonly IGMDatabaseService _gmDatabaseService;
        private readonly System.Timers.Timer _filterUpdateTimer;
        private readonly System.Timers.Timer _addEffectFilterUpdateTimer;

        public TitleEditorViewModel(IWindowsService windowsService, IGMDatabaseService gmDatabaseService, ItemDataManager itemDataManager)
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

            _addEffectFilterUpdateTimer = new()
            {
                Interval = 500,
                AutoReset = false
            };
            _addEffectFilterUpdateTimer.Elapsed += AddEffectFilterUpdateTimerElapsed;

            PopulateTitleItems();
            WeakReferenceMessenger.Default.Register(this);

            _addEffectView = CollectionViewSource.GetDefaultView(_itemDataManager.AddEffectItems);
            _addEffectView.Filter = FilterAddEffect;
        }

        #region Commands 

        #region File

        [RelayCommand]
        private async Task LoadFile()
        {
            try
            {
                await CloseFile();

                bool isLoaded = await DataTableManager.LoadFileFromPath("charactertitle.rh", "charactertitle_string.rh", "nTitleType", "Title");

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

                string filter = "charactertitle.rh|" +
                                "charactertitle.rh|" +
                                "All Files (*.*)|*.*";

                OpenFileDialog openFileDialog = new()
                {
                    Filter = filter,
                    FilterIndex = 1
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    string fileName = Path.GetFileName(openFileDialog.FileName);

                    bool isLoaded = await DataTableManager.LoadFileAs(openFileDialog.FileName, "charactertitle_string.rh", "nTitleType", "Title");

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

                bool isLoaded = await DataTableManager.LoadFileFromPCK("charactertitle.rh", "charactertitle_string.rh");

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

        private void IsLoaded()
        {
            Title = string.Format(Resources.EditorTitleFileName, "Titile", DataTableManager.CurrentFileName);
            OpenMessage = "";
            IsVisible = Visibility.Visible;
            OnCanExecuteFileCommandChanged();
        }

        [RelayCommand(CanExecute = nameof(CanExecuteFileCommand))]
        private void OpenSearchDialog(string? parameter)
        {
            try
            {
                Window? window = Application.Current.Windows.OfType<TitleEditorWindow>().FirstOrDefault();
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
            Title = string.Format(Resources.EditorTitle, "Title");
            OpenMessage = Resources.OpenFile;
            SearchText = string.Empty;
            AddEffectSearch = string.Empty;
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
                    DataTableManager.SelectedItem["wszTitleName"] = "New Title";
                }
                if (DataTableManager.SelectedItemString != null)
                {
                    DataTableManager.SelectedItemString["wszTitleName"] = "New Title"; ;
                }
                DataTableManager.EndGroupingEdits();
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
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
                TitleEffects ??= [];
                TitleOptions ??= [];

                for (int i = 0; i < 6; i++)
                {
                    var addEffectID = (int)selectedItem[$"nAddEffectID{i:00}"];

                    if (i < TitleEffects.Count)
                    {
                        var existingItem = TitleEffects[i];

                        existingItem.AddEffectID = addEffectID;
                    }
                    else
                    {
                        var item = new TitleEffect
                        {
                            AddEffectID = addEffectID
                        };

                        TitleEffects.Add(item);
                        TitleEffectPropertyChanged(item, i);
                    }
                }

                for (int i = 0; i < 3; i++)
                {
                    var dummyName = (string)selectedItem[$"szDummyName{i:00}"];
                    var mdataName = (string)selectedItem[$"szMdataName{i:00}"];
                    var motionName = (string)selectedItem[$"szMotionName{i:00}"];
                    var offSet = (string)selectedItem[$"szOffSet{i:00}"];

                    if (i < TitleOptions.Count)
                    {
                        var existingItem = TitleOptions[i];

                        existingItem.DummyName = dummyName;
                        existingItem.MdataName = mdataName;
                        existingItem.MotionName = motionName;
                        existingItem.OffSet = offSet;
                    }
                    else
                    {
                        var item = new TitleEffect
                        {
                            DummyName = dummyName,
                            MdataName = mdataName,
                            MotionName = motionName,
                            OffSet = offSet
                        };

                        TitleOptions.Add(item);
                        TitleStringPropertyChanged(item, i);
                    }
                }

                FormatTitleEffect();

                IsSelectedItemVisible = Visibility.Visible;
            }
            else
            {
                IsSelectedItemVisible = Visibility.Hidden;
            }

            _isUpdatingSelectedItem = false;
        }

        private void TitleEffectPropertyChanged(TitleEffect item, int index)
        {
            item.PropertyChanged += (s, e) =>
            {
                if (s is TitleEffect effect)
                {
                    OnTitleEffectChanged(effect.AddEffectID, $"nAddEffectID{index:00}");
                }
            };
        }

        private void TitleStringPropertyChanged(TitleEffect item, int index)
        {
            item.PropertyChanged += (s, e) =>
            {
                if (s is TitleEffect item)
                {
                    UpdateSelectedItemValue(item.DummyName, $"szDummyName{index:00}");
                    UpdateSelectedItemValue(item.MdataName, $"szMdataName{index:00}");
                    UpdateSelectedItemValue(item.MotionName, $"szMotionName{index:00}");
                    UpdateSelectedItemValue(item.OffSet, $"szOffSet{index:00}");
                }
            };
        }

        #endregion

        #region Filter

        private void ApplyFilter()
        {
            List<string> filterParts = [];

            List<string> columns = [];

            columns.Add("CONVERT(nID, 'System.String')");

            for (int i = 0; i < 6; i++)
            {
                string columnName = $"nAddEffectID{i:00}";
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

        #region AddEffect Filter
        [ObservableProperty]
        private ICollectionView _addEffectView;

        private readonly List<int> selectedAddEffect = [];
        private bool FilterAddEffect(object obj)
        {
            if (obj is NameID addEffect)
            {
                if (addEffect.ID == 0)
                    return true;

                if (TitleEffects != null && TitleEffects.Count >= 6)
                {
                    selectedAddEffect.Add(TitleEffects[0].AddEffectID);
                    selectedAddEffect.Add(TitleEffects[1].AddEffectID);
                    selectedAddEffect.Add(TitleEffects[2].AddEffectID);
                    selectedAddEffect.Add(TitleEffects[3].AddEffectID);
                    selectedAddEffect.Add(TitleEffects[4].AddEffectID);
                    selectedAddEffect.Add(TitleEffects[5].AddEffectID);

                    if (selectedAddEffect.Contains(addEffect.ID))
                        return true;
                }

                // text search filter
                if (!string.IsNullOrEmpty(AddEffectSearch))
                {
                    string searchText = AddEffectSearch.ToLower();

                    // Check if either option ID or option Name contains the search text
                    if (!string.IsNullOrEmpty(addEffect.ID.ToString()) && addEffect.ID.ToString().Contains(searchText, StringComparison.CurrentCultureIgnoreCase))
                        return true;

                    if (!string.IsNullOrEmpty(addEffect.Name) && addEffect.Name.Contains(searchText, StringComparison.CurrentCultureIgnoreCase))
                        return true;

                    return false;
                }

                return true;
            }
            return false;
        }

        [ObservableProperty]
        private string? _addEffectSearch;
        partial void OnAddEffectSearchChanged(string? value)
        {
            _addEffectFilterUpdateTimer.Stop();
            _addEffectFilterUpdateTimer.Start();
        }

        private void AddEffectFilterUpdateTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(AddEffectView.Refresh);
        }
        #endregion

        #endregion

        #region Comboboxes

        [ObservableProperty]
        private List<NameID>? _titleTypeItems;

        [ObservableProperty]
        private List<NameID>? _titleCategoryItems;

        private void PopulateTitleItems()
        {
            TitleTypeItems =
                [
                    new NameID { ID = 0, Name = Resources.TitleNormal },
                    new NameID { ID = 1, Name = Resources.TitleAnimated }
                ];

            TitleCategoryItems =
                [
                    new NameID { ID = 0, Name = Resources.TitleNormal },
                    new NameID { ID = 1, Name = Resources.TitleSpecial }
                ];
        }

        #endregion

        #region Properties
        [ObservableProperty]
        private string _title = string.Format(Resources.EditorTitle, "Title");

        [ObservableProperty]
        private string? _openMessage = Resources.OpenFile;

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
        private ObservableCollection<TitleEffect> _titleEffects = [];

        [ObservableProperty]
        private ObservableCollection<TitleEffect> _titleOptions = [];

        private void OnTitleEffectChanged(int newValue, string column)
        {
            UpdateSelectedItemValue(newValue, column);
            FormatTitleEffect();
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

        private void FormatTitleEffect()
        {
            StringBuilder titleEffect = new($"[{Resources.TitleEffect}]\n");

            for (int i = 0; i < TitleEffects.Count; i++)
            {
                if (TitleEffects[i].AddEffectID != 0)
                {
                    string effect = _gmDatabaseService.GetAddEffectName(TitleEffects[i].AddEffectID);
                    titleEffect.AppendLine($"{effect}");
                }
            }

            int remainTime = (int)DataTableManager.SelectedItem!["nRemainTime"];

            string formattedRemainTime = DateTimeFormatter.FormatRemainTime(remainTime);
            titleEffect.AppendLine($"\n{Resources.TitleDuration}: {formattedRemainTime}");
            TitleEffectText = titleEffect.ToString();
        }

        #endregion
    }
}
