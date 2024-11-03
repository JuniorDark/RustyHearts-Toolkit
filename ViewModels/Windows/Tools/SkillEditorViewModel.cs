using Microsoft.Win32;
using RHToolkit.Messages;
using RHToolkit.Models;
using RHToolkit.Models.Database;
using RHToolkit.Models.Editor;
using RHToolkit.Models.MessageBox;
using RHToolkit.Services;
using RHToolkit.ViewModels.Controls;
using RHToolkit.ViewModels.Windows.Tools.VM;
using RHToolkit.Views.Windows;
using System.ComponentModel;
using System.Data;
using System.Windows.Controls;
using static RHToolkit.Models.EnumService;

namespace RHToolkit.ViewModels.Windows
{
    public partial class SkillEditorViewModel : ObservableObject, IRecipient<DataRowViewMessage>, IRecipient<SkillDataMessage>
    {
        private readonly Guid _token;
        private readonly IWindowsService _windowsService;
        private readonly IGMDatabaseService _gmDatabaseService;
        private readonly System.Timers.Timer _filterUpdateTimer;

        public SkillEditorViewModel(IWindowsService windowsService, IGMDatabaseService gmDatabaseService, SkillDataManager skillDataManager, SkillDataViewModel skillDataViewModel)
        {
            _token = Guid.NewGuid();
            _windowsService = windowsService;
            _gmDatabaseService = gmDatabaseService;
            _skillDataManager = skillDataManager;
            _skillDataViewModel = skillDataViewModel;
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

            WeakReferenceMessenger.Default.Register<SkillDataMessage>(this);
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

                string? fileName = parameter;
                if (fileName == null) return;

                int fileType = GetFileTypeFromFileName(fileName);
                string columnName = GetColumnName(fileName);
                string? stringFileName = GetStringFileName(fileType);

                SkillType = (SkillType)fileType;

                bool isLoaded = await DataTableManager.LoadFileFromPath(fileName, stringFileName, columnName, "Skill");

                if (isLoaded)
                {
                    Title = GetTitle(fileName);
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

                string filter = "Skill Files .rh|" +
                                "angelaskill.rh;frantzskill.rh;natashaskill.rh;tudeskill.rh;" +
                                "angelaskilltree.rh;frantzskilltree.rh;natashaskilltree.rh;tudeskilltree.rh;" +
                                "angelaavatar01skilltree.rh;frantzavatar01skilltree.rh;frantzavatar02skilltree.rh;natashaavatar01skilltree.rh;tudeavatar01skilltree.rh;" +
                                "angelamyskillui.rh;frantzmyskillui.rh;natashamyskillui.rh;tudemyskillui.rh;" +
                                "angelaavatar01myskillui.rh;frantzavatar01myskillui.rh;frantzavatar02myskillui.rh;natasha_avatar01_myskillui.rh;tudeavatar01myskillui.rh|" +
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
                        string message = string.Format(Resources.InvalidTableFileDesc, fileName, "Skill");
                        RHMessageBoxHelper.ShowOKMessage(message, Resources.Error);
                        return;
                    }

                    string columnName = GetColumnName(fileName);
                    string? stringFileName = GetStringFileName(fileType);

                    SkillType = (SkillType)fileType;

                    bool isLoaded = await DataTableManager.LoadFileAs(openFileDialog.FileName, stringFileName, columnName, "Skill");

                    if (isLoaded)
                    {
                        Title = GetTitle(fileName);
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
            OpenMessage = "";
            IsVisible = Visibility.Visible;
            OnCanExecuteFileCommandChanged();
        }

        private static int GetFileTypeFromFileName(string fileName)
        {
            return fileName switch
            {
                "frantzskill.rh" => 1,
                "angelaskill.rh" => 2,
                "tudeskill.rh" => 3,
                "natashaskill.rh" => 4,
                "frantzskilltree.rh" or "frantzavatar01skilltree.rh" or "frantzavatar02skilltree.rh" => 5,
                "angelaskilltree.rh" or "angelaavatar01skilltree.rh" => 6,
                "tudeskilltree.rh" or "tudeavatar01skilltree.rh" => 7,
                "natashaskilltree.rh" or "natashaavatar01skilltree.rh" => 8,
                "frantzmyskillui.rh" or "frantzavatar01myskillui.rh" or "frantzavatar02myskillui.rh" => 9,
                "angelamyskillui.rh" or "angelaavatar01myskillui.rh" => 10,
                "tudemyskillui.rh" or "tudeavatar01myskillui.rh" => 11,
                "natashamyskillui.rh" or "natasha_avatar01_myskillui.rh" => 12,
                _ => -1,
            };
        }

        private static string GetColumnName(string fileName)
        {
            return fileName switch
            {
                "frantzskill.rh" or "angelaskill.rh" or "tudeskill.rh" or "natashaskill.rh" => "nSkillLevel",
                "frantzskilltree.rh" or "angelaskilltree.rh" or "tudeskilltree.rh" or "natashaskilltree.rh" or "frantzavatar01skilltree.rh" or "frantzavatar02skilltree.rh" or "angelaavatar01skilltree.rh" or "tudeavatar01skilltree.rh" or "natashaavatar01skilltree.rh" => "nBeforeSkillID00",
                "frantzmyskillui.rh" or "angelamyskillui.rh" or "tudemyskillui.rh" or "natashamyskillui.rh" or "frantzavatar01myskillui.rh" or "frantzavatar02myskillui.rh" or "angelaavatar01myskillui.rh" or "tudeavatar01myskillui.rh" or "natasha_avatar01_myskillui.rh" => "nTier",
                _ => "",
            };
        }

        private static string? GetStringFileName(int type)
        {
            return type switch
            {
                1 => "frantzskill_string.rh",
                2 => "angelaskill_string.rh",
                3 => "tudeskill_string.rh",
                4 => "natashaskill_string.rh",
                _ => null,
            };
        }

        private string GetTitle(string fileName)
        {
            string skill = string.Format(Resources.EditorTitleFileName, "Skill", DataTableManager.CurrentFileName);
            string skillTree = string.Format(Resources.EditorTitleFileName, Resources.SkillTreeEditor, DataTableManager.CurrentFileName);
            string skillTreeUI = string.Format(Resources.EditorTitleFileName, Resources.SkillTreeUIEditor, DataTableManager.CurrentFileName);

            return fileName switch
            {
                "frantzskill.rh" or "angelaskill.rh" or "tudeskill.rh" or "natashaskill.rh" => skill,
                "frantzskilltree.rh" or "angelaskilltree.rh" or "tudeskilltree.rh" or "natashaskilltree.rh" or "frantzavatar01skilltree.rh" or "frantzavatar02skilltree.rh" or "angelaavatar01skilltree.rh" or "tudeavatar01skilltree.rh" or "natashaavatar01skilltree.rh" => skillTree,
                "frantzmyskillui.rh" or "angelamyskillui.rh" or "tudemyskillui.rh" or "natashamyskillui.rh" or "frantzavatar01myskillui.rh" or "frantzavatar02myskillui.rh" or "angelaavatar01myskillui.rh" or "tudeavatar01myskillui.rh" or "natasha_avatar01_myskillui.rh" => skillTreeUI,
                _ => "",
            };
        }

        [RelayCommand(CanExecute = nameof(CanExecuteFileCommand))]
        private void OpenSearchDialog(string? parameter)
        {
            try
            {
                Window? window = Application.Current.Windows.OfType<SkillEditorWindow>().FirstOrDefault();
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
            Title = string.Format(Resources.EditorTitle, "Skill");
            OpenMessage = Resources.OpenFile;
            SkillItems?.Clear();
            SkillMotion?.Clear();
            SkillParam?.Clear();
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
                DataTableManager.AddNewRow();
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
            }
        }
        #endregion

        #region Add Skill

        [RelayCommand(CanExecute = nameof(CanExecuteSelectedItemCommand))]
        private void AddSkill(string parameter)
        {
            try
            {
                if (int.TryParse(parameter, out int slotIndex))
                {
                    var characterSkillType = GetCharacterSkillType(SkillType);

                    var skillData = new SkillData
                    {
                        SlotIndex = slotIndex,
                        SkillID = SkillDataViewModel != null ? SkillDataViewModel.SkillID : 0,
                        SkillLevel = SkillDataViewModel != null ? SkillDataViewModel.SkillLevel : 0,
                        CharacterSkillType = characterSkillType,
                    };

                    _windowsService.OpenSkillWindow(_token, "SkillTreeTarget", skillData);
                }

            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
            }
        }

        [RelayCommand(CanExecute = nameof(CanExecuteSelectedItemCommand))]
        private void AddSkillTree(string parameter)
        {
            try
            {
                if (int.TryParse(parameter, out int slotIndex))
                {
                    var characterSkillType = GetCharacterSkillType(SkillType);

                    var skillData = new SkillData
                    {
                        SlotIndex = slotIndex,
                        SkillID = SkillItems[slotIndex].SkillID,
                        SkillLevel = SkillItems[slotIndex].SkillLevel,
                        CharacterSkillType = characterSkillType,
                    };

                    _windowsService.OpenSkillWindow(_token, "SkillTree", skillData);
                }

            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
            }
        }

        [RelayCommand(CanExecute = nameof(CanExecuteSelectedItemCommand))]
        private void AddSkillUI(string parameter)
        {
            try
            {
                if (int.TryParse(parameter, out int slotIndex))
                {
                    var characterSkillType = GetCharacterSkillType(SkillType);

                    var skillData = new SkillData
                    {
                        SlotIndex = slotIndex,
                        SkillID = SkillDataViewModel != null ? SkillDataViewModel.SkillID : 0,
                        SkillLevel = SkillDataViewModel != null ? SkillDataViewModel.SkillLevel : 1,
                        CharacterSkillType = characterSkillType,
                    };

                    _windowsService.OpenSkillWindow(_token, "SkillTreeUI", skillData);
                }

            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
            }
        }

        public void Receive(SkillDataMessage message)
        {
            if (message.Token == _token && DataTableManager.SelectedItem != null)
            {
                var skillData = message.Value;

                switch (message.Recipient)
                {
                    case "SkillEditorWindowSkillTreeTarget":
                        UpdateSkill(skillData);
                        break;
                    case "SkillEditorWindowSkillTree":
                        UpdateSkillTree(skillData);
                        break;
                    case "SkillEditorWindowSkillTreeUI":
                        UpdateSkillUI(skillData);
                        break;
                }

            }
        }

        private void UpdateSkill(SkillData skillData)
        {
            if (skillData.SkillID != 0)
            {
                DataTableManager.StartGroupingEdits();
                var skillDataViewModel = SkillDataManager.GetSkillDataViewModel(SkillType, skillData.SkillID, skillData.SkillLevel);
                SkillDataViewModel = skillDataViewModel;
                UpdateSelectedItemValue(skillData.SkillID, $"nSkillID");
                UpdateSelectedItemValue(skillData.SkillLevel, $"nSkillLevel");
                DataTableManager.EndGroupingEdits();
            }
        }

        private void UpdateSkillTree(SkillData skillData)
        {
            if (skillData.SkillID != 0 && skillData.SlotIndex < SkillItems.Count)
            {
                var skill = SkillItems[skillData.SlotIndex];

                DataTableManager.StartGroupingEdits();
                var skillDataViewModel = SkillDataManager.GetSkillDataViewModel(SkillType, skillData.SkillID, skillData.SkillLevel);
                skill.SkillID = skillData.SkillID;
                skill.SkillLevel = skillData.SkillLevel;
                skill.SkillDataViewModel = skillDataViewModel;
                OnPropertyChanged(nameof(SkillItems));
                DataTableManager.EndGroupingEdits();
            }
        }

        private void UpdateSkillUI(SkillData skillData)
        {
            if (skillData.SkillID != 0)
            {
                DataTableManager.StartGroupingEdits();
                var skillDataViewModel = SkillDataManager.GetSkillDataViewModel(SkillType, skillData.SkillID, skillData.SkillLevel);
                SkillDataViewModel = skillDataViewModel;
                UpdateSelectedItemValue(skillData.SkillID, $"nSkillID");
                UpdateSelectedItemValue(skillData.SkillName, $"wsznote");
                DataTableManager.EndGroupingEdits();
            }
        }

        #endregion

        #region Remove Skill

        [RelayCommand]
        private void RemoveSkill(string parameter)
        {
            try
            {
                if (int.TryParse(parameter, out int slotIndex))
                {
                    if (SkillDataViewModel != null)
                    {
                        DataTableManager.StartGroupingEdits();
                        UpdateSelectedItemValue(0, $"nSkillID");
                        UpdateSelectedItemValue(0, $"nSkillLevel");
                        DataTableManager.EndGroupingEdits();
                        SkillDataViewModel = null;
                    }
                }
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
            }
        }

        [RelayCommand]
        private void RemoveSkillTree(string parameter)
        {
            try
            {
                if (int.TryParse(parameter, out int slotIndex))
                {
                    DataTableManager.StartGroupingEdits();
                    var skill = SkillItems[slotIndex];

                    if (skill.SkillID != 0)
                    {
                        skill.SkillID = 0;
                        skill.SkillLevel = 0;
                        skill.SkillDataViewModel = null;
                        OnPropertyChanged(nameof(SkillItems));
                    }
                    DataTableManager.EndGroupingEdits();
                }
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
            }
        }

        [RelayCommand]
        private void RemoveSkillUI(string parameter)
        {
            try
            {
                if (int.TryParse(parameter, out int slotIndex))
                {
                    if (SkillDataViewModel != null)
                    {
                        DataTableManager.StartGroupingEdits();
                        UpdateSelectedItemValue(0, $"nSkillID");
                        DataTableManager.EndGroupingEdits();
                        SkillDataViewModel = null;
                    }
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

            switch (SkillType)
            {
                case SkillType.SkillFrantz or SkillType.SkillAngela or SkillType.SkillTude or SkillType.SkillNatasha:
                    columns.Add("CONVERT(nSkillID, 'System.String')");
                    columns.Add("CONVERT(szCharacterType, 'System.String')");
                    columns.Add("CONVERT(szSkillType, 'System.String')");
                    columns.Add("CONVERT(szIcon, 'System.String')");
                    columns.Add("CONVERT(szTarget, 'System.String')");
                    columns.Add("CONVERT(szParamName01, 'System.String')");
                    columns.Add("CONVERT(szParamName02, 'System.String')");
                    columns.Add("CONVERT(szParamName03, 'System.String')");
                    columns.Add("CONVERT(szParamName04, 'System.String')");
                    break;
                case SkillType.SkillTreeFrantz or SkillType.SkillTreeAngela or SkillType.SkillTreeTude or SkillType.SkillTreeNatasha:
                    columns.Add("CONVERT(nSkillID, 'System.String')");
                    columns.Add("CONVERT(nBeforeSkillID00, 'System.String')");
                    columns.Add("CONVERT(nBeforeSkillID01, 'System.String')");
                    columns.Add("CONVERT(nBeforeSkillID02, 'System.String')");
                    break;
                case SkillType.SkillUIFrantz or SkillType.SkillUIAngela or SkillType.SkillUITude or SkillType.SkillUINatasha:
                    columns.Add("CONVERT(nSkillID, 'System.String')");
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

        #region Skill

        [ObservableProperty]
        private List<string>? _characterTypeItems;

        [ObservableProperty]
        private List<string>? _skillTypeItems;

        [ObservableProperty]
        private List<string>? _skillActiveTypeItems;

        [ObservableProperty]
        private List<string>? _skillTargetItems;

        [ObservableProperty]
        private List<string>? _stateTrueItems;

        [ObservableProperty]
        private List<string>? _stateFalseItems;

        [ObservableProperty]
        private List<string>? _paramNameItems;

        private void PopulateListItems()
        {
            CharacterTypeItems =
            [
                "TYPE_ALL",
                "TYPE_A",
                "TYPE_B",
                "TYPE_C"
            ];

            SkillTypeItems =
            [
                "ACTIVE",
                "BUFF",
                "PASSIVE"
            ];

            SkillActiveTypeItems =
            [
                "",
                "ACTIVE_NORMAL",
                "ACTIVE_SPECIAL",
                "BUFF"
            ];

            SkillTargetItems =
            [
                "ONESELF",
                "ENEMY",
                "PARTY"
            ];

            StateTrueItems =
            [
                "RUN",
                "WALK",
                "ATTACK",
                "GUARD",
                "STAY",
                "DAMAGE",
                "BIG_DAMAGE",
                "MIDAIR"
            ];

            StateFalseItems =
            [
                "MIDAIR",
                "DOWN_ATTACK",
                "CAUGHT"
            ];

            ParamNameItems =
            [
                "",
                "ADDEFFECTID",
                "ADD_DAMAGE",
                "ADD_HP",
                "ADD_MAX_HP_%",
                "ADD_MAX_MP_%",
                "ANTI_CONDITION_BLEEDING",
                "ANTI_CONDITION_CHAOS",
                "ANTI_CONDITION_COLD",
                "ANTI_CONDITION_DARKNESS",
                "ANTI_CONDITION_FIRE",
                "ANTI_CONDITION_FREEZING",
                "ATTRIBUTE_DARKNESS",
                "ATTRIBUTE_EARTH",
                "ATTRIBUTE_FIRE",
                "ATTRIBUTE_LIGHT",
                "ATTRIBUTE_LIGHTNING",
                "ATTRIBUTE_TOXICATION",
                "ATTRIBUTE_WATER",
                "ATTRIBUTE_WINDY",
                "BLOODSUCKING",
                "CONDITION_BLEEDING",
                "CONDITION_FREEZING",
                "CONDITION_HOLD",
                "CONDITION_LIGHTNING",
                "CONDITION_PROBABILITY",
                "CONDITION_SLEEP",
                "CONDITION_SLOW",
                "CONDITION_STONE",
                "CONDITION_STUN",
                "CONDITION_TOXICATION",
                "CRITICAL",
                "DAMAGE",
                "MAGIC_DAMAGE",
                "MAGIC_STRIKING_POWER_%",
                "RESISTANCE_DARKNESS",
                "RESISTANCE_EARTH",
                "RESISTANCE_FIRE",
                "RESISTANCE_LIGHT",
                "RESISTANCE_LIGHTNING",
                "RESISTANCE_TOXICATION",
                "RESISTANCE_WATER",
                "RESISTANCE_WINDY",
                "RES_CONDITION_ALL",
                "RES_CONDITION_SLEEP",
                "RES_CONDITION_TOXICATION",
                "STRIKING_POWER_%",
            ];

        }
        #endregion

        #region SkillTreeUI
        [ObservableProperty]
        private List<NameID>? _jobItems;

        private void PopulateJobItems(SkillType skillType)
        {
            try
            {
                JobItems = skillType switch
                {
                    SkillType.SkillUIFrantz => GetEnumItems<FrantzJob>(false),
                    SkillType.SkillUIAngela => GetEnumItems<AngelaJob>(false),
                    SkillType.SkillUITude => GetEnumItems<TudeJob>(false),
                    SkillType.SkillUINatasha => GetEnumItems<NatashaJob>(false),
                    _ => null,
                };

                if (JobItems != null && JobItems.Count > 0)
                {
                    Job = JobItems.First().ID;
                    OnPropertyChanged(nameof(JobItems));
                }
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
            }
        }

        [ObservableProperty]
        private List<NameID>? _skillClassItems;

        private void PopulateSkillClassItems(SkillType skillType, int job)
        {
            try
            {
                SkillClassItems = GetSkillClassList(skillType, job);
                var existingItem = SkillClassItems?.FirstOrDefault(item => item.ID == SkillClass);
                if (existingItem != null)
                {
                    SkillClass = existingItem.ID;
                }
                else
                {
                    SkillClass = 0;
                }
                OnPropertyChanged(nameof(SkillClassItems));
                OnPropertyChanged(nameof(SkillClass));
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

                switch (SkillType)
                {
                    case SkillType.SkillFrantz or SkillType.SkillAngela or SkillType.SkillTude or SkillType.SkillNatasha:
                        UpdateSkillSelectedItem(selectedItem);
                        break;
                    case SkillType.SkillTreeFrantz or SkillType.SkillTreeAngela or SkillType.SkillTreeTude or SkillType.SkillTreeNatasha:
                        UpdateSkillTreeSelectedItem(selectedItem);
                        break;
                    case SkillType.SkillUIFrantz or SkillType.SkillUIAngela or SkillType.SkillUITude or SkillType.SkillUINatasha:
                        UpdateSkillUISelectedItem(selectedItem);
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

        #region Skill
        private void UpdateSkillSelectedItem(DataRowView? selectedItem)
        {
            _isUpdatingSelectedItem = true;

            if (selectedItem != null)
            {
                if (SkillType == SkillType.SkillFrantz)
                {
                    int totalDamage = (int)selectedItem[$"nTotalDamage"];
                    int weaponDamageSkill = (int)selectedItem[$"nWeaponeDamage_Skill"];

                    TotalDamage = totalDamage.ToString();
                    WeaponDamageSkill = weaponDamageSkill.ToString();
                }
                else
                {
                    TotalDamage = (string)selectedItem[$"szTotalDamage"];
                    WeaponDamageSkill = (string)selectedItem[$"szWeaponeDamage_Skill"];
                }
                
                SkillMotion ??= [];

                for (int i = 1; i <= 4; i++)
                {
                    string skillMotion = (string)selectedItem[$"szSkillMotion{i}"];
                    int skillCategory = (int)selectedItem[$"nSkillMotionCategory{i}"];
                    string skillMotionSG = (string)selectedItem[$"szSkillMotion{i}SG"];
                    int skillCategorySG = (int)selectedItem[$"nSkillMotionCategory{i}SG"];

                    if (i - 1 < SkillMotion.Count)
                    {
                        var existingItem = SkillMotion[i - 1];
                        existingItem.SkillMotion = skillMotion;
                        existingItem.SkillMotionCategory = skillCategory;
                        existingItem.SkillMotionSG = skillMotionSG;
                        existingItem.SkillMotionCategorySG = skillCategorySG;

                        SkillMotionItemPropertyChanged(existingItem);
                    }
                    else
                    {
                        var skillMotionData = new Skill
                        {
                            SkillMotion = skillMotion,
                            SkillMotionCategory = skillCategory,
                            SkillMotionSG = skillMotionSG,
                            SkillMotionCategorySG = skillCategorySG
                        };

                        Application.Current.Dispatcher.Invoke(() => SkillMotion.Add(skillMotionData));
                        SkillMotionItemPropertyChanged(skillMotionData);
                    }
                }

                SkillParam ??= [];

                for (int i = 0; i < 4; i++)
                {
                    string paramName = (string)selectedItem[$"szParamName{i + 1:00}"];
                    double paramValue = (float)selectedItem[$"fParamValue{i + 1:00}"];

                    if (i < SkillParam.Count)
                    {
                        var existingItem = SkillParam[i];

                        existingItem.ParamName = paramName;
                        existingItem.ParamValue = paramValue;

                        SkillParamItemPropertyChanged(existingItem);
                    }
                    else
                    {
                        var skillData = new Skill
                        {
                            ParamName = paramName,
                            ParamValue = paramValue
                        };

                        Application.Current.Dispatcher.Invoke(() => SkillParam.Add(skillData));
                        SkillParamItemPropertyChanged(skillData);
                    }
                }

                SkillData skill = new()
                {
                    SkillName = DataTableManager.SelectedItemString != null ? (string)DataTableManager.SelectedItemString["wszName"] : "",
                    Description1 = DataTableManager.SelectedItemString != null ? (string)DataTableManager.SelectedItemString["wszDescription"] : "",
                    Description2 = DataTableManager.SelectedItemString != null ? (string)DataTableManager.SelectedItemString["wszDescription1"] : "",
                    Description3 = DataTableManager.SelectedItemString != null ? (string)DataTableManager.SelectedItemString["wszDescription2"] : "",
                    Description4 = DataTableManager.SelectedItemString != null ? (string)DataTableManager.SelectedItemString["wszDescription3"] : "",
                    Description5 = DataTableManager.SelectedItemString != null ? (string)DataTableManager.SelectedItemString["wszDescription4"] : "",
                    IconName = (string)selectedItem["szIcon"],
                    SkillID = (int)selectedItem["nSkillID"],
                    SkillLevel = (int)selectedItem["nSkillLevel"],
                    RequiredLevel = (int)selectedItem["nLearnLevel"],
                    SPCost = (int)selectedItem["nCost"],
                    MPCost = (float)selectedItem["fMP"],
                    Cooltime = (float)selectedItem["fCoolTime"],
                    CharacterType = (string)selectedItem["szCharacterType"],
                    CharacterSkillType = GetCharacterSkillType(SkillType),

                };

                SkillDataViewModel ??= new(_gmDatabaseService);

                SkillDataViewModel.UpdateSkillData(skill);

                IsSelectedItemVisible = Visibility.Visible;
            }
            else
            {
                IsSelectedItemVisible = Visibility.Hidden;
            }

            _isUpdatingSelectedItem = false;
        }

        private void SkillMotionItemPropertyChanged(Skill item)
        {
            item.PropertyChanged -= OnSkillMotionItemPropertyChanged;

            item.PropertyChanged += OnSkillMotionItemPropertyChanged;
        }

        private void OnSkillMotionItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is Skill skillMotion)
            {
                int index = SkillMotion.IndexOf(skillMotion);
                UpdateSelectedItemValue(skillMotion.SkillMotion, $"szSkillMotion{index}");
                UpdateSelectedItemValue(skillMotion.SkillMotionCategory, $"nSkillMotionCategory{index}");
                UpdateSelectedItemValue(skillMotion.SkillMotionSG, $"szSkillMotion{index}SG");
                UpdateSelectedItemValue(skillMotion.SkillMotionCategorySG, $"SkillMotionCategory{index}SG");
            }
        }

        private void SkillParamItemPropertyChanged(Skill item)
        {
            item.PropertyChanged -= OnSkillParamItemPropertyChanged;

            item.PropertyChanged += OnSkillParamItemPropertyChanged;
        }

        private void OnSkillParamItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is Skill skillParam)
            {
                int index = SkillParam.IndexOf(skillParam);
                UpdateSelectedItemValue(skillParam.ParamName, $"szParamName{index + 1:00}");
                UpdateSelectedItemValue(skillParam.ParamValue, $"fParamValue{index + 1:00}");
            }
        }

        #endregion

        #region SkillTree
        private void UpdateSkillTreeSelectedItem(DataRowView? selectedItem)
        {
            _isUpdatingSelectedItem = true;

            if (selectedItem != null)
            {
                int skillID = (int)selectedItem[$"nSkillID"];
                int skillLevel = (int)selectedItem[$"nSkillLevel"];
                var characterSkillType = GetCharacterSkillType(SkillType);

                if (skillID != 0)
                {
                    var targetSkill = SkillDataManager.GetSkillData(characterSkillType, skillID, skillLevel);
                    SkillDataViewModel ??= new(_gmDatabaseService);
                    SkillDataViewModel.UpdateSkillData(targetSkill);
                }
                else
                {
                    SkillDataViewModel = null;
                }

                SkillItems ??= [];

                for (int i = 0; i < 3; i++)
                {
                    int beforeSkillID = (int)selectedItem[$"nBeforeSkillID{i:00}"];
                    int beforeSkillLevel = (int)selectedItem[$"nBeforeSkillLevel{i:00}"];
                    SkillDataViewModel? skillDataViewModel = null;

                    if (i < SkillItems.Count)
                    {
                        var existingItem = SkillItems[i];

                        if (existingItem.SkillID != beforeSkillID)
                        {
                            skillDataViewModel = SkillDataManager.GetSkillDataViewModel(characterSkillType, beforeSkillID, beforeSkillLevel);
                            existingItem.SkillDataViewModel = skillDataViewModel;
                            existingItem.SkillID = beforeSkillID;
                            existingItem.SkillLevel = beforeSkillLevel;
                        }

                        SkillTreeItemPropertyChanged(existingItem);
                    }
                    else
                    {
                        skillDataViewModel = SkillDataManager.GetSkillDataViewModel(characterSkillType, beforeSkillID, beforeSkillLevel);

                        Skill skill = new()
                        {
                            SkillDataViewModel = beforeSkillID != 0 ? skillDataViewModel: null,
                            SkillID = beforeSkillID,
                            SkillLevel = beforeSkillLevel,
                        };

                        Application.Current.Dispatcher.Invoke(() => SkillItems.Add(skill));
                        SkillTreeItemPropertyChanged(skill);
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

        private void SkillTreeItemPropertyChanged(Skill item)
        {
            item.PropertyChanged -= OnSkillTreeItemPropertyChanged;

            item.PropertyChanged += OnSkillTreeItemPropertyChanged;
        }

        private void OnSkillTreeItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is Skill skillItem)
            {
                int index = SkillItems.IndexOf(skillItem);
                UpdateSelectedItemValue(skillItem.SkillID, $"nBeforeSkillID{index:00}");
                UpdateSelectedItemValue(skillItem.SkillLevel, $"nBeforeSkillLevel{index:00}");
            }
        }

        #endregion

        #region SkillUI
        private void UpdateSkillUISelectedItem(DataRowView? selectedItem)
        {
            _isUpdatingSelectedItem = true;

            if (selectedItem != null)
            {
                SkillClass = (int)selectedItem[$"nSkillClass"];
                Job = (int)selectedItem[$"nJob"];
                Column = (int)selectedItem[$"nColumn"];
                Tier = (int)selectedItem[$"nTier"];

                int skillID = (int)selectedItem[$"nSkillID"];
                var characterSkillType = GetCharacterSkillType(SkillType);

                if (skillID != 0)
                {
                    var skillData = SkillDataManager.GetSkillData(characterSkillType, skillID, 1);
                    SkillDataViewModel ??= new(_gmDatabaseService);
                    SkillDataViewModel.UpdateSkillData(skillData);
                }
                else
                {
                    SkillDataViewModel = null;
                }

                IsSelectedItemVisible = Visibility.Visible;
            }
            else
            {
                IsSelectedItemVisible = Visibility.Hidden;
            }

            _isUpdatingSelectedItem = false;
        }

        #endregion

        #endregion

        #region Properties

        [ObservableProperty]
        private string _title = string.Format(Resources.EditorTitle, "Skill");

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
        private SkillDataManager _skillDataManager;

        [ObservableProperty]
        private SkillDataViewModel? _skillDataViewModel;

        #region SelectedItem

        [ObservableProperty]
        private SkillType _skillType;
        partial void OnSkillTypeChanged(SkillType value)
        {
            PopulateJobItems(value);
        }

        [ObservableProperty]
        private ObservableCollection<Skill> _skillItems = [];

        [ObservableProperty]
        private ObservableCollection<Skill> _skillParam = [];

        [ObservableProperty]
        private ObservableCollection<Skill> _skillMotion = [];

        #region Skill

        [ObservableProperty]
        private string? _totalDamage;
        partial void OnTotalDamageChanged(string? value)
        {
            if (SkillType == SkillType.SkillFrantz)
            {
                if (int.TryParse(value, out int parsedValue))
                {
                    UpdateSelectedItemValue(parsedValue, "nTotalDamage");
                }
            }
            else
            {
                UpdateSelectedItemValue(value, "szTotalDamage");
            }
        }

        [ObservableProperty]
        private string? _weaponDamageSkill;
        partial void OnWeaponDamageSkillChanged(string? value)
        {
            if (SkillType == SkillType.SkillFrantz)
            {
                if (int.TryParse(value, out int parsedValue))
                {
                    UpdateSelectedItemValue(parsedValue, "nWeaponeDamage_Skill");
                }
            }
            else
            {
                UpdateSelectedItemValue(value, "szWeaponeDamage_Skill");
            }
        }

        #endregion

        #region SkillUI

        [ObservableProperty]
        private int _job = -1;
        partial void OnJobChanged(int value)
        {
            PopulateSkillClassItems(SkillType, value);
            UpdateSelectedItemValue(value, "nJob");
        }

        [ObservableProperty]
        private int _skillClass;
        partial void OnSkillClassChanged(int value)
        {
            SkillTreeColumnMax = value == 0 ? 1 : 4;
            SkillClassName = GetSkillClassName(SkillType, Job, value);
            SkillTreeImage = GetskillTreeImage(value);
            UpdateSelectedItemValue(value, "nSkillClass");
        }

        [ObservableProperty]
        private string? _skillClassName;

        [ObservableProperty]
        private string _skillTreeImage = "/Assets/images/skill/lb_base81_skill_tree_bg02.png";

        [ObservableProperty]
        private int _column;
        partial void OnColumnChanged(int value)
        {
            SkillTreeColumn = value > 0 ? value - 1 : 0;
            UpdateSelectedItemValue(value, "nColumn");
        }

        [ObservableProperty]
        private int _tier;
        partial void OnTierChanged(int value)
        {
            SkillTreeRow = value > 0 ? value - 1 : 0;
            UpdateSelectedItemValue(value, "nTier");
        }

        [ObservableProperty]
        private int _skillTreeColumn;

        [ObservableProperty]
        private int _skillTreeColumnMax;

        [ObservableProperty]
        private int _skillTreeRow;

        #endregion

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

        private static string GetskillTreeImage(int skillClass)
        {
            return skillClass switch
            {
                1 => "/Assets/images/skill/lb_base81_skill_tree_bg02.png",
                2 => "/Assets/images/skill/lb_base81_skill_tree_bg03.png",
                3 => "/Assets/images/skill/lb_base81_skill_tree_bg04.png",
                _ => "/Assets/images/skill/lb_base81_skill_tree_bg02.png",
            };
        }
        #endregion
    }
}
