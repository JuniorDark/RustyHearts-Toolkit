using RHToolkit.Messages;
using RHToolkit.Models;
using RHToolkit.Models.Database;
using RHToolkit.Models.SQLite;
using RHToolkit.Services;
using System.Windows.Controls;
using static RHToolkit.Models.EnumService;

namespace RHToolkit.ViewModels.Windows;

public partial class SkillWindowViewModel : ObservableObject, IRecipient<CharacterDataMessage>, IRecipient<SkillDataMessage>
{
    private readonly IGMDatabaseService _gmDatabaseService;
    private readonly CachedDataManager _cachedDataManager;

    public SkillWindowViewModel(IGMDatabaseService gmDatabaseService, CachedDataManager cachedDataManager, SkillDataManager skillDataManager)
    {
        _gmDatabaseService = gmDatabaseService;
        _cachedDataManager = cachedDataManager;
        _skillDataManager = skillDataManager;

        IsButtonVisible = Visibility.Collapsed;

        WeakReferenceMessenger.Default.Register<CharacterDataMessage>(this);
        WeakReferenceMessenger.Default.Register<SkillDataMessage>(this);
    }

    #region Messenger

    #region Receive CharacterData
    [ObservableProperty]
    private CharacterData? _characterData;

    [ObservableProperty]
    private Guid? _token = Guid.Empty;

    public void Receive(CharacterDataMessage message)
    {
        if (Token == Guid.Empty)
        {
            Token = message.Token;

            CharacterData = null;
            CharacterData = message.Value;
        }
        WeakReferenceMessenger.Default.Unregister<CharacterDataMessage>(this);
    }

    #endregion

    #region Send SkillData
    [RelayCommand(CanExecute = nameof(CanExecuteCommand))]
    private void AddSkill()
    {
        if (SkillDataManager.SkillDataViewModel != null)
        {
            var skillData = SelectedItem;

            if (skillData != null && !string.IsNullOrEmpty(MessageType))
            {
                skillData.SlotIndex = SlotIndex;

                var windowMapping = new Dictionary<string, string>
                {
                    { "SkillTree", "SkillEditorWindowSkillTree" },
                    { "SkillTreeTarget", "SkillEditorWindowSkillTreeTarget" },
                    { "SkillTreeUI", "SkillEditorWindowSkillTreeUI" },
                };

                if (windowMapping.TryGetValue(MessageType, out var windowName))
                {
                    WeakReferenceMessenger.Default.Send(new SkillDataMessage(skillData, windowName, MessageType, Token));
                }
            }
        }
    }

    private bool CanExecuteCommand()
    {
        return SelectedItem != null;
    }
    #endregion

    #region Receive SkillData
    [ObservableProperty]
    private string? _messageType;

    private bool _isProcessing = false;

    public void Receive(SkillDataMessage message)
    {
        if (_isProcessing) return;

        try
        {
            _isProcessing = true;

            if (Token == Guid.Empty)
            {
                Token = message.Token;
            }

            if (message.Recipient == "SkillWindow" && message.Token == Token)
            {
                var skillData = message.Value;
                MessageType = message.MessageType;

                Dispatcher.CurrentDispatcher.BeginInvoke(() =>
                {
                    Title = GetTitle(MessageType, skillData);

                    var settings = GetVisibilitySettings(message.MessageType, skillData);

                    ApplyVisibilitySettings(settings);

                    if (skillData.SkillID != 0)
                    {
                        LoadSkillData(skillData);
                    }
                    else
                    {
                        UpdateSkillDataViewModel(skillData);
                    }

                    SlotIndex = skillData.SlotIndex;
                }, DispatcherPriority.ContextIdle);
            }
        }
        finally
        {
            _isProcessing = false;
        }
    }

    private VisibilitySettings GetVisibilitySettings(string? messageType, SkillData skillData)
    {
        var defaultSettings = new VisibilitySettings
        {
            IsSlotVisible = Visibility.Collapsed,
            IsButtonVisible = Visibility.Visible,
            SelectionMode = DataGridSelectionMode.Single,
            SlotIndexMin = 0,
            SlotIndexMax = 0
        };

        var visibilitySettings = new Dictionary<string, Action<VisibilitySettings>>
        {
            ["SkillTree"] = settings =>
            {
                SkillDataManager.CharacterSkillTypeFilter = (int)skillData.CharacterSkillType;
                settings.SlotIndexMax = 2;
                settings.IsSlotVisible = Visibility.Visible;
            },
            ["SkillTreeUI"] = settings =>
            {
                SkillDataManager.SkillLevelFilter = skillData.SkillLevel;
            },

            ["SkillTreeTarget"] = settings => { },
        };

        if (!string.IsNullOrEmpty(messageType) && visibilitySettings.TryGetValue(messageType, out var action))
        {
            action(defaultSettings);
        }

        return defaultSettings;
    }

    private void ApplyVisibilitySettings(VisibilitySettings settings)
    {
        SelectionMode = settings.SelectionMode;
        IsSlotVisible = settings.IsSlotVisible;
        IsButtonVisible = settings.IsButtonVisible;
        SlotIndexMin = settings.SlotIndexMin;
        SlotIndexMax= settings.SlotIndexMax;
    }

    private void UpdateSkillDataViewModel(SkillData skillData)
    {
        if (SkillDataManager.SkillDataViewModel != null)
        {
           SkillDataManager.SkillDataViewModel.SlotIndex = skillData.SlotIndex;
        }
    }

    private class VisibilitySettings
    {
        public Visibility IsSlotVisible { get; set; }
        public Visibility IsButtonVisible { get; set; }
        public DataGridSelectionMode SelectionMode { get; set; }
        public int SlotIndexMin { get; set; }
        public int SlotIndexMax { get; set; }
    }

    private static string GetTitle(string? messageType, SkillData skillData)
    {
        return (messageType ?? string.Empty) switch
        {
            "SkillTree" or "SkillTreeTarget" => $"Add {GetEnumDescription(skillData.CharacterSkillType)} Skill Tree Skill",
            "SkillTreeUI" => $"Add {GetEnumDescription(skillData.CharacterSkillType)} Skill Tree UI Skill",
            _ => "Skill List",
        };
    }

    #endregion

    #endregion

    #region Load SkillData

    private void LoadSkillData(SkillData skillData)
    {
        var skillDataViewModel = SkillDataManager.GetSkillDataViewModel(skillData.CharacterSkillType, skillData.SkillID, skillData.SkillLevel);
        SkillDataManager.SkillDataViewModel = skillDataViewModel;
        SelectedItem = SkillDataManager.SkillDataItems?.FirstOrDefault(i => i.CharacterSkillType == skillData.CharacterSkillType && i.SkillID == skillData.SkillID && i.SkillLevel == skillData.SkillLevel);
    }

    private void UpdateSkillData(SkillData skillData)
    {
        var skillDataViewModel = SkillDataManager.GetSkillDataViewModel(skillData.CharacterSkillType, skillData.SkillID, skillData.SkillLevel);
        SkillDataManager.SkillDataViewModel = skillDataViewModel;
    }

    #endregion

    #region Properties

    [ObservableProperty]
    private string _title = "Skill List";

    [ObservableProperty]
    private string _addItemText = "Add Selected Skill";

    [ObservableProperty]
    private Visibility _isSlotVisible = Visibility.Collapsed;

    [ObservableProperty]
    private Visibility _isButtonVisible = Visibility.Collapsed;

    [ObservableProperty]
    private Visibility _isItemAmountVisible = Visibility.Collapsed;

    [ObservableProperty]
    private Visibility _isOptionsVisible = Visibility.Collapsed;

    [ObservableProperty]
    private DataGridSelectionMode _selectionMode = DataGridSelectionMode.Single;

    [ObservableProperty]
    private SkillDataManager _skillDataManager;

    #region SkillData

    [ObservableProperty]
    private int _slotIndex;

    [ObservableProperty]
    private int _slotIndexMin;

    [ObservableProperty]
    private int _slotIndexMax;

    [ObservableProperty]
    private SkillData? _selectedItem;
    partial void OnSelectedItemChanged(SkillData? value)
    {
        if (value != null)
        {
            if (_isProcessing) return;
            UpdateSkillData(value);
        }

        AddSkillCommand.NotifyCanExecuteChanged();
    }

    #endregion

    #endregion
}
