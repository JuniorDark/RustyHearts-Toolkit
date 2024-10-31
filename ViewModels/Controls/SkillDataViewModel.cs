using RHToolkit.Models;
using RHToolkit.Properties;
using RHToolkit.Services;
using static RHToolkit.Models.EnumService;

namespace RHToolkit.ViewModels.Controls;

public partial class SkillDataViewModel(IGMDatabaseService gmDatabaseService) : ObservableObject
{
    private readonly IGMDatabaseService _gmDatabaseService = gmDatabaseService;

    public void UpdateSkillData(SkillData? skillData)
    {
        if (skillData != null)
        {
            SlotIndex = skillData.SlotIndex;
            ID = skillData.ID;
            SkillID = skillData.SkillID;
            SkillName = skillData.SkillName;
            IconName = skillData.IconName;
            Description1 = skillData.Description1;
            Description2 = skillData.Description2;
            Description3 = skillData.Description3;
            Description4 = skillData.Description4;
            Description5 = skillData.Description5;
            SkillLevel = skillData.SkillLevel;
            RequiredLevel = skillData.RequiredLevel;
            MpCost = skillData.MPCost;
            SpCost = skillData.SPCost;
            Cooltime = skillData.Cooltime;
            SkillType = skillData.SkillType;
            CharacterType = skillData.CharacterType;
            CharacterSkillType = skillData.CharacterSkillType;
        }
    }

    #region Properties

    [ObservableProperty]
    private SkillData? _skillData;
    partial void OnSkillDataChanged(SkillData? value)
    {
        UpdateSkillData(value);
    }

    [ObservableProperty]
    private int _slotIndex;

    [ObservableProperty]
    private int _ID;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RequiredSkillText))]
    private int _skillID;

    [ObservableProperty]
    private int _skillLevel;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LevelText))]
    private int _requiredLevel;

    public string LevelText => $"{Resources.RequiredLevel}: {RequiredLevel}";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MPCostText))]
    private double _mpCost;

    public string MPCostText => MpCost > 0 ? $"MP Cost {MpCost}" : "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SkillPointText))]
    private int _spCost;

    public string SkillPointText => $"Skill Point {SpCost}";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CooldownText))]
    private double _cooltime;

    public string CooldownText => Cooltime > 0 ? $"Cooldown {Cooltime:F2} Seconds" : "";

    [ObservableProperty]
    private string? _skillName;

    [ObservableProperty]
    private string? _iconName;

    [ObservableProperty]
    private string? _description1;

    [ObservableProperty]
    private string? _description2;

    [ObservableProperty]
    private string? _description3;

    [ObservableProperty]
    private string? _description4;

    [ObservableProperty]
    private string? _description5;

    [ObservableProperty]
    private string? _skillType;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SkillClassText))]
    [NotifyPropertyChangedFor(nameof(SkillFocusText))]
    [NotifyPropertyChangedFor(nameof(RequiredSkillText))]
    public SkillType _characterSkillType;

    public string SkillClassText =>  GetEnumDescription(CharacterSkillType);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SkillFocusText))]
    public string? _characterType;

    public string SkillFocusText => CharacterSkillType != EnumService.SkillType.None ? GetSkillJob(CharacterSkillType, CharacterType) : "";

    public string RequiredSkillText => CharacterSkillType != EnumService.SkillType.None ? _gmDatabaseService.FormatPreviousSkill(CharacterSkillType, SkillID) : "";

    #endregion
}