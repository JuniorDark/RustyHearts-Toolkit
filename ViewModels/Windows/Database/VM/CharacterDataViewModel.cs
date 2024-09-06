using RHToolkit.Models;
using RHToolkit.Models.Database;
using RHToolkit.Models.MessageBox;
using RHToolkit.Properties;
using RHToolkit.Services;
using static RHToolkit.Models.EnumService;

namespace RHToolkit.ViewModels.Windows.Database.VM;

public partial class CharacterDataViewModel : ObservableObject
{
    private readonly IGMDatabaseService _gmDatabaseService;

    public CharacterDataViewModel(IGMDatabaseService gmDatabaseService)
    {
        _gmDatabaseService = gmDatabaseService;

        PopulateClassItems();
        PopulateLobbyItems();
    }

    public void LoadCharacterData(CharacterData characterData)
    {
        CharacterData = characterData;
        CharacterName = characterData.CharacterName;
        Class = characterData.Class;
        Job = characterData.Job;
        Level = characterData.Level;
        CharacterExp = characterData.Experience;
        TotalSkillPoints = characterData.TotalSP;
        SkillPoints = characterData.SP;
        Lobby = characterData.LobbyID;
        InventoryGold = characterData.Gold;
        RessurectionScrolls = characterData.Hearts;
        WarehouseGold = characterData.StorageGold;
        WarehouseSlots = characterData.StorageCount;
        GuildExp = characterData.GuildPoint;
        GuildName = characterData.GuildName;
        HasGuild = characterData.HasGuild;
        RestrictCharacter = characterData.BlockYN == "Y";
        IsConnect = characterData.IsConnect == "Y";
        IsTradeEnable = characterData.IsTradeEnable == "Y";
        IsMoveEnable = characterData.IsMoveEnable == "Y";
        IsAdmin = characterData.Permission == 100;
    }

    public NewCharacterData GetCharacterChanges()
    {
        return new NewCharacterData
        {
            Level = Level,
            Experience = CharacterExp,
            SP = SkillPoints,
            TotalSP = TotalSkillPoints,
            LobbyID = Lobby,
            Gold = InventoryGold,
            Hearts = RessurectionScrolls,
            StorageGold = WarehouseGold,
            StorageCount = WarehouseSlots,
            GuildPoint = GuildExp,
            Permission = IsAdmin == true ? 100 : 0,
            BlockYN = RestrictCharacter == true ? "Y" : "N",
            IsTradeEnable = IsTradeEnable == true ? "Y" : "N",
            IsMoveEnable = IsMoveEnable == true ? "Y" : "N",
        };
    }

    public bool IsNameNotAllowed(string characterName)
    {
        return _gmDatabaseService.IsNameInNickFilter(characterName);
    }

    #region Comboboxes
    private void PopulateClassItems()
    {
        try
        {
            ClassItems = GetEnumItems<CharClass>();

            if (ClassItems.Count > 0)
            {
                Class = 1;
            }
        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
        }
    }

    private void PopulateLobbyItems()
    {
        try
        {
            LobbyItems = _gmDatabaseService.GetLobbyItems();

            if (LobbyItems.Count > 0)
            {
                Lobby = 1;
            }
        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
        }
    }

    #endregion

    #region Properties

    #region Character

    [ObservableProperty]
    private CharacterData? _characterData;

    [ObservableProperty]
    private List<NameID>? _classItems;

    [ObservableProperty]
    private List<NameID>? _jobItems;

    [ObservableProperty]
    private List<NameID>? _lobbyItems;

    [ObservableProperty]
    private bool _isButtonEnabled = true;

    [ObservableProperty]
    private string? _characterName;

    [ObservableProperty]
    private bool _hasGuild;

    [ObservableProperty]
    private bool _isConnect;

    [ObservableProperty]
    private bool _restrictCharacter;

    [ObservableProperty]
    private bool _isMoveEnable;

    [ObservableProperty]
    private bool _isTradeEnable;

    [ObservableProperty]
    private bool _isAdmin;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Job))]
    private int _class;
    partial void OnClassChanged(int value)
    {
        JobItems?.Clear();
        JobItems = CharacterDataManager.GetJobItems((CharClass)value);
        if (CharacterData != null)
        {
            Job = 0;
            Job = CharacterData.Job;
        }
    }

    [ObservableProperty]
    private int _job;

    [ObservableProperty]
    private int _lobby;

    [ObservableProperty]
    private int _level;
    partial void OnLevelChanged(int value)
    {
        long experience = _gmDatabaseService.GetExperienceFromLevel(value);
        CharacterExp = experience;
    }

    [ObservableProperty]
    private long _characterExp;

    [ObservableProperty]
    private int _skillPoints;

    [ObservableProperty]
    private int _totalSkillPoints;
    partial void OnTotalSkillPointsChanged(int value)
    {
        if (SkillPoints > value)
        {
            SkillPoints = value;
        }
    }

    [ObservableProperty]
    private int _inventoryGold;

    [ObservableProperty]
    private int _ressurectionScrolls;

    [ObservableProperty]
    private int _warehouseSlots;

    [ObservableProperty]
    private int _warehouseGold;

    [ObservableProperty]
    private int _guildExp;

    [ObservableProperty]
    private string? _guildName;
    #endregion

    #endregion
}
