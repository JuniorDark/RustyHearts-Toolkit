using RHToolkit.Models.MessageBox;
using RHToolkit.Models.SQLite;
using RHToolkit.Services;
using RHToolkit.ViewModels.Controls;
using System.ComponentModel;
using static RHToolkit.Models.EnumService;

namespace RHToolkit.Models.Database;

public partial class SkillDataManager : ObservableObject
{
    private readonly IGMDatabaseService _gmDatabaseService;
    private readonly CachedDataManager _cachedDataManager;
    private readonly System.Timers.Timer _skillFilterUpdateTimer;

    public SkillDataManager(CachedDataManager cachedDataManager, IGMDatabaseService gmDatabaseService, SkillDataViewModel skillDataViewModel)
    {
        _gmDatabaseService = gmDatabaseService;
        _cachedDataManager = cachedDataManager;
        _skillDataViewModel = skillDataViewModel;
        _skillFilterUpdateTimer = new()
        {
            Interval = 500,
            AutoReset = false
        };
        _skillFilterUpdateTimer.Elapsed += SkillFilterUpdateTimerElapsed;

        PopulateSkillDataItems();

        _skillDataView = new CollectionViewSource { Source = SkillDataItems }.View;
        _skillDataView.Filter = FilterSkills;

        PopulateSkillTypeItemsFilter();
    }

    [ObservableProperty]
    private SkillDataViewModel _skillDataViewModel;

    #region SkillData

    /// <summary>
    /// Checks if the provided skill ID is invalid.
    /// </summary>
    /// <param name="skillID">The skill ID to check.</param>
    /// <returns>True if the skill ID is invalid, otherwise false.</returns>
    public bool IsInvalidID(int skillID)
    {
        return _cachedDataManager.CachedSkillDataList == null || !_cachedDataManager.CachedSkillDataList.Any(skill => skill.SkillID == skillID);
    }

    /// <summary>
    /// Retrieves the SkillDataViewModel for the specified skill type, skill ID, and skill level.
    /// </summary>
    /// <param name="skillType">The type of the skill.</param>
    /// <param name="skillID">The ID of the skill.</param>
    /// <param name="skillLevel">The level of the skill.</param>
    /// <returns>The SkillDataViewModel for the specified skill.</returns>
    public SkillDataViewModel GetSkillDataViewModel(SkillType skillType, int skillID, int skillLevel)
    {
        var characterSkillType = GetCharacterSkillType(skillType);

        // Find the corresponding SkillData in the _cachedSkillDataList
        SkillData? cachedItem = _cachedDataManager.CachedSkillDataList?
        .FirstOrDefault(i => i.CharacterSkillType == characterSkillType && i.SkillID == skillID && i.SkillLevel == skillLevel);

        SkillData skillData = new();

        if (cachedItem != null)
        {
            skillData = new()
            {
                ID = cachedItem.ID,
                SkillID = cachedItem.SkillID,
                SkillName = cachedItem.SkillName ?? "",
                IconName = cachedItem.IconName ?? "icon_empty_sprite",
                Description1 = cachedItem.Description1 ?? "",
                Description2 = cachedItem.Description2 ?? "",
                Description3 = cachedItem.Description3 ?? "",
                Description4 = cachedItem.Description4 ?? "",
                Description5 = cachedItem.Description5 ?? "",
                SkillType = cachedItem.SkillType ?? "",
                SkillLevel = cachedItem.SkillLevel,
                CharacterSkillType = skillType,
                CharacterType = cachedItem.CharacterType,
                RequiredLevel = cachedItem.RequiredLevel,
                MPCost = cachedItem.MPCost,
                SPCost = cachedItem.SPCost,
                Cooltime = cachedItem.Cooltime,
            };
        }
        else
        {
            if (skillID != 0)
            {
                skillData = new()
                {
                    SkillID = skillID,
                    SkillName = string.Format(Resources.UnknownSkill, skillID),
                    IconName = "question_icon",
                };
            }

        }

        var skillDataViewModel = new SkillDataViewModel(_gmDatabaseService)
        {
            SkillData = skillData
        };

        return skillDataViewModel;
    }

    /// <summary>
    /// Retrieves the SkillData for the specified skill type, skill ID, and skill level.
    /// </summary>
    /// <param name="skillType">The type of the skill.</param>
    /// <param name="skillID">The ID of the skill.</param>
    /// <param name="skillLevel">The level of the skill.</param>
    /// <returns>The SkillData for the specified skill.</returns>
    public SkillData GetSkillData(SkillType skillType, int skillID, int skillLevel)
    {
        var characterSkillType = GetCharacterSkillType(skillType);

        // Find the corresponding SkillData in the _cachedSkillDataList
        SkillData? cachedItem = _cachedDataManager.CachedSkillDataList?
        .FirstOrDefault(i => i.CharacterSkillType == characterSkillType && i.SkillID == skillID && i.SkillLevel == skillLevel);

        SkillData skillData = new();

        if (cachedItem != null)
        {
            skillData = new()
            {
                CharacterSkillType = skillType,
                ID = cachedItem.ID,
                SkillID = cachedItem.SkillID,
                SkillName = cachedItem.SkillName ?? "",
                IconName = cachedItem.IconName ?? "icon_empty_sprite",
                Description1 = cachedItem.Description1 ?? "",
                Description2 = cachedItem.Description2 ?? "",
                Description3 = cachedItem.Description3 ?? "",
                Description4 = cachedItem.Description4 ?? "",
                Description5 = cachedItem.Description5 ?? "",
                SkillType = cachedItem.SkillType ?? "",
                SkillLevel = cachedItem.SkillLevel,
                CharacterType = cachedItem.CharacterType,
                RequiredLevel = cachedItem.RequiredLevel,
                MPCost = cachedItem.MPCost,
                SPCost = cachedItem.SPCost,
                Cooltime = cachedItem.Cooltime,
            };
        }
        else
        {
            if (skillID != 0)
            {
                skillData = new()
                {
                    SkillID = skillID,
                    SkillName = string.Format(Resources.UnknownSkill, skillID),
                    IconName = "question_icon",
                };
            }

        }

        return skillData;
    }

    #endregion

    #region Skill Data List

    [ObservableProperty]
    private List<SkillData>? _skillDataItems;

    /// <summary>
    /// Populates the SkillDataItems list with cached skill data.
    /// </summary>
    private void PopulateSkillDataItems()
    {
        try
        {
            SkillDataItems = _cachedDataManager.CachedSkillDataList;
        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
        }
    }

    #endregion

    #region CollectionView Filter

    [ObservableProperty]
    private ICollectionView _skillDataView;

    /// <summary>
    /// Filters the skills based on the current filter criteria.
    /// </summary>
    /// <param name="obj">The skill data object to filter.</param>
    /// <returns>True if the skill matches the filter criteria, otherwise false.</returns>
    private bool FilterSkills(object obj)
    {
        if (obj is SkillData skill)
        {
            //combobox filter 
            if (CharacterSkillTypeFilter != 0 && CharacterSkillTypeFilter != 0 && skill.CharacterSkillType != (SkillType)CharacterSkillTypeFilter)
                return false;
            if (SkillTypeFilter != null && SkillTypeFilter.ID != 0 && skill.SkillType != SkillTypeFilter.Name)
                return false;
            if (CharacterTypeFilter != null && CharacterTypeFilter.ID != 0 && skill.CharacterTypeValue != CharacterTypeFilter.ID)
                return false;
            if (SkillLevelFilter != 0 && skill.SkillLevel != SkillLevelFilter)
                return false;

            // text search filter
            if (!string.IsNullOrEmpty(SearchText))
            {
                string searchText = SearchText.ToLower();

                // Check if either skill ID or skill name contains the search text
                if (!string.IsNullOrEmpty(skill.SkillID.ToString()) && skill.SkillID.ToString().Contains(searchText, StringComparison.CurrentCultureIgnoreCase))
                    return true;

                if (!string.IsNullOrEmpty(skill.SkillName) && skill.SkillName.Contains(searchText, StringComparison.CurrentCultureIgnoreCase))
                    return true;

                return false;
            }
            return true;
        }
        return false;
    }

    [ObservableProperty]
    private string? _searchText;
    partial void OnSearchTextChanged(string? value)
    {
        _skillFilterUpdateTimer.Stop();
        _skillFilterUpdateTimer.Start();
    }

    private void SkillFilterUpdateTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        RefreshView();
    }

    private void RefreshView()
    {
        Application.Current.Dispatcher.Invoke(SkillDataView.Refresh);
    }

    #region Comboboxes Filter

    [ObservableProperty]
    private List<NameID>? _characterSkillTypeFilterItems;

    [ObservableProperty]
    private List<NameID>? _skillTypeFilterItems;

    /// <summary>
    /// Populates the skill type filter items.
    /// </summary>
    private void PopulateSkillTypeItemsFilter()
    {
        CharacterSkillTypeFilterItems =
        [
            new NameID { ID = 0, Name = Resources.None },
            new NameID { ID = 1, Name =  Resources.Frantz },
            new NameID { ID = 2, Name =  Resources.Angela },
            new NameID { ID = 3, Name =  Resources.Tude },
            new NameID { ID = 4, Name =  Resources.Natasha }
        ];

        CharacterSkillTypeFilter = 0;

        SkillTypeFilterItems =
        [
            new NameID { ID = 0, Name = Resources.None, DisplayName = Resources.None },
            new NameID { ID = 1, Name = "ACTIVE", DisplayName =  Resources.SkillActive },
            new NameID { ID = 2, Name = "PASSIVE", DisplayName = Resources.SkillPassive },
            new NameID { ID = 3, Name = "BUFF", DisplayName = Resources.SkillBuff }
        ];

        SkillTypeFilter = SkillTypeFilterItems.First();

        PopulateSkillTypeFilterItems(0);
    }

    [ObservableProperty]
    private List<NameID>? _characterTypeFilterItems;

    /// <summary>
    /// Populates the skill type filter items based on the character skill type.
    /// </summary>
    /// <param name="characterSkillType">The character skill type.</param>
    private void PopulateSkillTypeFilterItems(int characterSkillType)
    {
        try
        {
            CharacterTypeFilterItems = characterSkillType switch
            {
                0 => GetEnumItems<GenericJob>(false),
                1 => GetEnumItems<FrantzJob>(false),
                2 => GetEnumItems<AngelaJob>(false),
                3 => GetEnumItems<TudeJob>(false),
                4 => GetEnumItems<NatashaJob>(false),
                _ => throw new ArgumentOutOfRangeException(nameof(characterSkillType), $"Invalid Character Skill Type value '{characterSkillType}'"),
            };

            if (CharacterTypeFilterItems.Count > 0)
            {
                CharacterTypeFilter = CharacterTypeFilterItems.First();
                OnPropertyChanged(nameof(CharacterTypeFilter));
            }
        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
        }
    }

    [ObservableProperty]
    private int _characterSkillTypeFilter;
    partial void OnCharacterSkillTypeFilterChanged(int value)
    {
        PopulateSkillTypeFilterItems(value);

        RefreshView();
    }

    [ObservableProperty]
    private NameID? _skillTypeFilter;
    partial void OnSkillTypeFilterChanged(NameID? value)
    {
        RefreshView();
    }

    [ObservableProperty]
    private NameID? _characterTypeFilter;
    partial void OnCharacterTypeFilterChanged(NameID? value)
    {
        RefreshView();
    }

    [ObservableProperty]
    private int _skillLevelFilter;
    partial void OnSkillLevelFilterChanged(int value)
    {
        RefreshView();
    }
    #endregion

    #endregion

}