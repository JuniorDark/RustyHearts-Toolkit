using RHToolkit.Messages;
using RHToolkit.Models;
using RHToolkit.Models.MessageBox;
using RHToolkit.Properties;
using RHToolkit.Services;
using static RHToolkit.Models.EnumService;

namespace RHToolkit.ViewModels.Windows;

public partial class CharacterWindowViewModel : ObservableObject, IRecipient<ItemDataMessage>, IRecipient<CharacterDataMessage>, IRecipient<DatabaseItemMessage>
{
    private readonly WindowsProviderService _windowsProviderService;
    private readonly IDatabaseService _databaseService;
    private readonly IGMDatabaseService _gmDatabaseService;

    public CharacterWindowViewModel(WindowsProviderService windowsProviderService, IDatabaseService databaseService, IGMDatabaseService gmDatabaseService)
    {
        _windowsProviderService = windowsProviderService;
        _databaseService = databaseService;
        _gmDatabaseService = gmDatabaseService;

        PopulateClassItems();
        PopulateLobbyItems();

        WeakReferenceMessenger.Default.Register<ItemDataMessage>(this);
        WeakReferenceMessenger.Default.Register<CharacterDataMessage>(this);
        WeakReferenceMessenger.Default.Register<DatabaseItemMessage>(this);

    }


    #region Load Item 

    public void Receive(CharacterDataMessage message)
    {
        var characterData = message.Value;
        LoadCharacterData(characterData);
    }

    private void LoadCharacterData(CharacterData characterData)
    {
        if (characterData  != null)
        {
            CharacterID = characterData.CharacterID;
            CharacterName = characterData.CharacterName;
            WindyCode = characterData.WindyCode;
            Class = characterData.Class;
            Level = characterData.Level;
            CharacterExp = characterData.Experience;
            SkillPoints = characterData.SP;
            TotalSkillPoints = characterData.TotalSP;
            Lobby = characterData.LobbyID;
            CreateTime = characterData.CreateTime;
            LastLoginTime = characterData.LastLogin;
            InventoryGold = characterData.Gold;
            RessurectionScrolls = characterData.Hearts;
            WarehouseGold = characterData.StorageGold;
            WarehouseSlots = characterData.StorageCount;
            GuildExp = characterData.GuildPoint;
            GuildName = characterData.GuildName;
            RestrictCharacter = characterData.BlockYN == "Y";
            IsConnect = characterData.IsConnect == "Y";
            IsTradeEnable = characterData.IsTradeEnable == "Y";
            IsMoveEnable = characterData.IsMoveEnable == "Y";
            IsAdmin = characterData.Permission == 100;

            string className = GetEnumDescription((CharClass)characterData.Class);
            Enum jobEnum = GetJobEnum((CharClass)characterData.Class, characterData.Job);
            string jobName = GetEnumDescription(jobEnum);

            CharacterNameText = $"Lv.{characterData.Level} {characterData.CharacterName}";
            CharacterClassText = $"<{className} - {jobName} Focus> ";
            CharSilhouetteImage = GetClassImage(characterData.Class);
            Title = $"Character Editor ({characterData.CharacterName})";

            PopulateJobItems((CharClass)characterData.Class);
            Job = characterData.Job;
        }
    }

    private static string GetClassImage(int classValue)
    {
        return classValue switch
        {
            1 => "/Assets/images/char/ui_silhouette_frantz01.png",
            2 => "/Assets/images/char/ui_silhouette_angela01.png",
            3 => "/Assets/images/char/ui_silhouette_tude01.png",
            4 => "/Assets/images/char/ui_silhouette_natasha01.png",
            101 => "/Assets/images/char/ui_silhouette_roselle01.png",
            102 => "/Assets/images/char/ui_silhouette_leila01.png",
            201 => "/Assets/images/char/ui_silhouette_edgar01.png",
            301 => "/Assets/images/char/ui_silhouette_tude_girl01.png",
            401 => "/Assets/images/char/ui_silhouette_ian01.png",
            _ => "/Assets/images/char/ui_silhouette_frantz01.png",
        };
    }

    public void Receive(ItemDataMessage message)
    {
        if (message.Recipient == ViewModelType.CharacterWindowViewModel)
        {
            var itemData = message.Value;
            LoadItemData(itemData);
        }
    }

    private void LoadItemData(ItemData itemData)
    {
    }


    #endregion

    #region Comboboxes

    [ObservableProperty]
    private int _classSelectedIndex;

    [ObservableProperty]
    private List<NameID>? _classItems;

    private void PopulateClassItems()
    {
        try
        {
            ClassItems = GetEnumItems<CharClass>();

            if (ClassItems.Count > 0)
            {
                ClassSelectedIndex = 0;
            }
        }
        catch (Exception ex)
        {
            RHMessageBox.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
        }
    }

    [ObservableProperty]
    private int _jobSelectedIndex;

    [ObservableProperty]
    private List<NameID>? _jobItems;

    private void PopulateJobItems(CharClass charClass)
    {
        try
        {
            JobItems = GetEnumItems<CharClass>();

            JobItems = charClass switch
            {
                CharClass.Frantz or CharClass.Roselle or CharClass.Leila => GetEnumItems<FrantzJob>(),
                CharClass.Angela or CharClass.Edgar => GetEnumItems<AngelaJob>(),
                CharClass.Tude or CharClass.Meilin => GetEnumItems<TudeJob>(),
                CharClass.Natasha or CharClass.Ian => GetEnumItems<NatashaJob>(),
                _ => throw new ArgumentException($"Invalid character class: {charClass}"),
            };

            if (JobItems.Count > 0)
            {
                JobSelectedIndex = 0;
            }
        }
        catch (Exception ex)
        {
            RHMessageBox.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
        }
    }

    [ObservableProperty]
    private int _lobbySelectedIndex;

    [ObservableProperty]
    private List<NameID>? _lobbyItems;

    private void PopulateLobbyItems()
    {
        try
        {
            LobbyItems = _gmDatabaseService.GetLobbyItems();;

            if (LobbyItems.Count > 0)
            {
                LobbySelectedIndex = 0;
            }
        }
        catch (Exception ex)
        {
            RHMessageBox.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
        }
    }

    public void Receive(DatabaseItemMessage message)
    {
        switch (message.ItemStorageType)
        {
            case ItemStorageType.Inventory:
                // Handle inventory items
                List<DatabaseItem> inventoryItems = message.Value;
                // Do something with the inventory items...
                break;

            case ItemStorageType.Equipment:
                // Handle equipment items
                List<DatabaseItem> equipmentItems = message.Value;
                // Do something with the equipment items...
                break;

            case ItemStorageType.Storage:
                // Handle equipment items
                List<DatabaseItem> storageItems = message.Value;
                // Do something with the storage items...
                break;

            default:
                break;
        }
    }


    #endregion

    #region Character Data Properties

    [ObservableProperty]
    private string? _title;

    [ObservableProperty]
    private string? _characterNameText;

    [ObservableProperty]
    private string? _characterClassText;

    [ObservableProperty]
    private string? _charSilhouetteImage;

    [ObservableProperty]
    private string? _windyCode;

    [ObservableProperty]
    private Guid _characterID;

    [ObservableProperty]
    private DateTime _createTime;

    [ObservableProperty]
    private DateTime _lastLoginTime;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CharacterNameText))]
    private string? _characterName;

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
    [NotifyPropertyChangedFor(nameof(CharacterClassText))]
    private int _class;
    partial void OnClassChanged(int value)
    {
        JobItems?.Clear();
        PopulateJobItems((CharClass)value);
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CharacterClassText))]
    private int _job;

    [ObservableProperty]
    private int _lobby;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CharacterNameText))]
    private int _level;

    [ObservableProperty]
    private long _characterExp;

    [ObservableProperty]
    private int _skillPoints;

    [ObservableProperty]
    private int _totalSkillPoints;

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
}
