using Microsoft.Win32;
using RHToolkit.Messages;
using RHToolkit.Models.Database;
using RHToolkit.Models.Editor;
using RHToolkit.Models.MessageBox;
using RHToolkit.Services;
using RHToolkit.ViewModels.Windows.Tools.VM;
using RHToolkit.Views.Windows;
using System.ComponentModel;
using System.Data;
using System.Windows.Controls;

namespace RHToolkit.ViewModels.Windows
{
    public partial class AddEffectEditorViewModel : ObservableObject, IRecipient<DataRowViewMessage>
    {
        private readonly Guid _token;
        private readonly IWindowsService _windowsService;
        private readonly IGMDatabaseService _gmDatabaseService;
        private readonly System.Timers.Timer _filterUpdateTimer;

        public AddEffectEditorViewModel(IWindowsService windowsService, IGMDatabaseService gmDatabaseService, ItemDataManager itemDataManager)
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
        private async Task LoadFile()
        {
            try
            {
                await CloseFile();

                bool isLoaded = await DataTableManager.LoadFileFromPath("addeffect.rh", "addeffect_string.rh", "nSubAddEffectID", "AddEffect");

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

                string filter = "addeffect.rh|" +
                                "addeffect.rh|" +
                                "All Files (*.*)|*.*";

                OpenFileDialog openFileDialog = new()
                {
                    Filter = filter,
                    FilterIndex = 1
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    string fileName = Path.GetFileName(openFileDialog.FileName);

                    bool isLoaded = await DataTableManager.LoadFileAs(openFileDialog.FileName, "charactertitle_string.rh", "nSubAddEffectID", "AddEffect");

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

        private void IsLoaded()
        {
            Title = string.Format(Resources.EditorTitleFileName, "Add Effect", DataTableManager.CurrentFileName);
            OpenMessage = "";
            IsVisible = Visibility.Visible;
            OnCanExecuteFileCommandChanged();
        }

        [RelayCommand(CanExecute = nameof(CanExecuteFileCommand))]
        private void OpenSearchDialog(string? parameter)
        {
            try
            {
                Window? addEffectEditorWindow = Application.Current.Windows.OfType<AddEffectEditorWindow>().FirstOrDefault();
                Window owner = addEffectEditorWindow ?? Application.Current.MainWindow;
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
            Title = string.Format(Resources.EditorTitle, "Add Effect");
            OpenMessage = Resources.OpenFile;
            AddEffectValues?.Clear();
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
            columns.Add("CONVERT(wszName, 'System.String')");
            columns.Add("CONVERT(wszDesc, 'System.String')");
            columns.Add("CONVERT(szIcon, 'System.String')");
            columns.Add("CONVERT(wszDescription, 'System.String')");
            columns.Add("CONVERT(wszDescription, 'System.String')");
            columns.Add("CONVERT(szTypeName, 'System.String')");

            for (int i = 0; i < 6; i++)
            {
                columns.Add($"CONVERT(szDesc{i:00}, 'System.String')");
                columns.Add($"CONVERT(fValue{i:00}, 'System.String')");
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

                UpdateAddEffectSelectedItem(selectedItem);
            }
        }

        #region AddEffect
        private void UpdateAddEffectSelectedItem(DataRowView? selectedItem)
        {
            _isUpdatingSelectedItem = true;

            if (selectedItem != null)
            {
                AddEffectValues ??= [];

                for (int i = 0; i < 6; i++)
                {
                    string description = (string)selectedItem[$"szDesc{i:00}"];
                    double value = (float)selectedItem[$"fValue{i:00}"];

                    if (i < AddEffectValues.Count)
                    {
                        var existingItem = AddEffectValues[i];

                        existingItem.Description = description;
                        existingItem.Value = value;

                        AddEffectValuesItemPropertyChanged(existingItem);
                    }
                    else
                    {
                        var values = new AddEffect
                        {
                            Description = description,
                            Value = value
                        };

                        Application.Current.Dispatcher.Invoke(() => AddEffectValues.Add(values));
                        AddEffectValuesItemPropertyChanged(values);
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

        private void AddEffectValuesItemPropertyChanged(AddEffect item)
        {
            item.PropertyChanged -= OnAddEffectValuesItemPropertyChanged;

            item.PropertyChanged += OnAddEffectValuesItemPropertyChanged;
        }

        private void OnAddEffectValuesItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is AddEffect addEffect)
            {
                int index = AddEffectValues.IndexOf(addEffect);
                UpdateSelectedItemValue(addEffect.Description, $"szDesc{index:00}");
                UpdateSelectedItemValue(addEffect.Value, $"fValue{index:00}");
            }
        }

        #endregion

        #endregion

        #region Properties
        [ObservableProperty]
        private string _title = string.Format(Resources.EditorTitle, "Add Effect");

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
        private ItemDataManager _itemDataManager;

        #region SelectedItem

        [ObservableProperty]
        private ObservableCollection<AddEffect> _addEffectValues = [];

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
