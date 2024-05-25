using RHToolkit.Messages;
using RHToolkit.Models;
using RHToolkit.Models.MessageBox;
using RHToolkit.Properties;
using RHToolkit.Services;
using RHToolkit.Utilities;
using System.Data;

namespace RHToolkit.ViewModels.Windows;

public partial class TitleWindowViewModel : ObservableObject, IRecipient<CharacterDataMessage>
{
    private readonly IDatabaseService _databaseService;
    private readonly IGMDatabaseService _gmDatabaseService;

    public TitleWindowViewModel(IDatabaseService databaseService, IGMDatabaseService gmDatabaseService)
    {
        _databaseService = databaseService;
        _gmDatabaseService = gmDatabaseService;

        PopulateTitleItems();

        WeakReferenceMessenger.Default.Register(this);
    }

    #region Read Title

    [ObservableProperty]
    private CharacterData? _characterData;

    public async void Receive(CharacterDataMessage message)
    {
        if (message.Recipient == "TitleWindow")
        {
            var characterData = message.Value;
            CharacterData = null;
            CharacterData = characterData;

            Title = $"Character Title ({characterData.CharacterName})";

            await ReadTitle(characterData.CharacterID);
        }
    }

    [ObservableProperty]
    private DataTable? _titleData;

    private async Task ReadTitle(Guid characterId)
    {
        TitleData = null;
        TitleData = await _databaseService.ReadCharacterTitleListAsync(characterId);

        EquippedTitle = null;
        EquippedTitle = await _databaseService.ReadCharacterEquipTitleAsync(characterId);

        if (EquippedTitle != null)
        {
            EquippedTitleUid = (Guid)EquippedTitle["ID"];
            EquippedTitleId = (int)EquippedTitle["title_code"];
            EquippedTitleName = _gmDatabaseService.GetTitleName(EquippedTitleId);
            CurrentTitleText = $"{EquippedTitleName}";
            IsUnequipTitleButtonEnabled = true;
        }
        else
        {
            CurrentTitleText = $"No Title";
            IsUnequipTitleButtonEnabled = false;
        }

    }

    private string GetTitleDesc(int titleId, bool includeCategoryAndDuration)
    {
        try
        {
            (int titleCategory, int remainTime, int nAddEffectID00, int nAddEffectID01, int nAddEffectID02, int nAddEffectID03, int nAddEffectID04, int nAddEffectID05, string titleDesc) = _gmDatabaseService.GetTitleInfo(titleId);

            string formattedtitleCategory = titleCategory == 0 ? "Normal" : "Special";
            string formattedRemainTime = DateTimeFormatter.FormatRemainTime(remainTime);

            StringBuilder description = new();

            if (includeCategoryAndDuration)
            {
                description.AppendLine($"Title Category: {formattedtitleCategory}");
                description.AppendLine($"Title Duration: {formattedRemainTime}");
            }

            if (nAddEffectID00 == 0)
            {
                description.Append("Title Effect: None");
            }
            else
            {
                description.Append("Title Effect:");

                if (!string.IsNullOrEmpty(_gmDatabaseService.GetAddEffectName(nAddEffectID00)))
                    description.AppendLine().Append(_gmDatabaseService.GetAddEffectName(nAddEffectID00));
                if (!string.IsNullOrEmpty(_gmDatabaseService.GetAddEffectName(nAddEffectID01)))
                    description.AppendLine().Append(_gmDatabaseService.GetAddEffectName(nAddEffectID01));
                if (!string.IsNullOrEmpty(_gmDatabaseService.GetAddEffectName(nAddEffectID02)))
                    description.AppendLine().Append(_gmDatabaseService.GetAddEffectName(nAddEffectID02));
                if (!string.IsNullOrEmpty(_gmDatabaseService.GetAddEffectName(nAddEffectID03)))
                    description.AppendLine().Append(_gmDatabaseService.GetAddEffectName(nAddEffectID03));
                if (!string.IsNullOrEmpty(_gmDatabaseService.GetAddEffectName(nAddEffectID04)))
                    description.AppendLine().Append(_gmDatabaseService.GetAddEffectName(nAddEffectID04));
                if (!string.IsNullOrEmpty(_gmDatabaseService.GetAddEffectName(nAddEffectID05)))
                    description.AppendLine().Append(_gmDatabaseService.GetAddEffectName(nAddEffectID05));
            }

            return description.ToString();
        }
        catch (Exception ex)
        {
            RHMessageBox.ShowOKMessage("Error: " + ex.Message);
            return string.Empty;
        }
    }

    #endregion

    #region Add Title
    [RelayCommand]
    private async Task AddTitle()
    {
        int selectedTitleID = TitleListId;
        int selectedTitleRemainTime = _gmDatabaseService.GetTitleRemainTime(selectedTitleID);
        int selectedTitleExpireTime;
        string titleName = _gmDatabaseService.GetTitleName(selectedTitleID);

        try
        {
            if (CharacterData == null) return;

            if (TitleListId == 0)
            {
                return;
            }

            if (await _databaseService.IsCharacterOnlineAsync(CharacterData.CharacterName!))
            {
                return;
            }

            if (RHMessageBox.ConfirmMessage($"Add the title '{titleName}' to this character?"))
            {
                // Check if the character already has the same title
                if (await _databaseService.CharacterHasTitle(CharacterData.CharacterID, selectedTitleID))
                {
                    RHMessageBox.ShowOKMessage($"This character already has '{titleName}' title.", "Duplicate Title");
                    return;
                }

                // Set selectedTitleExpireTime based on selectedTitleRemainTime
                if (selectedTitleRemainTime == -1)
                {
                    selectedTitleExpireTime = 0;
                }
                else
                {
                    // Convert selectedTitleRemainTime to epoch time
                    selectedTitleExpireTime = (int)DateTime.UtcNow.AddMinutes(selectedTitleRemainTime).Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
                }

                await _databaseService.AddCharacterTitleAsync(CharacterData.CharacterID, selectedTitleID, selectedTitleRemainTime, selectedTitleExpireTime);
                await _databaseService.GMAuditAsync(CharacterData.AccountName!, CharacterData.CharacterID, CharacterData.CharacterName!, "Add Title", $"<font color=blue>Add Title</font>]<br><font color=red>Title: {selectedTitleID}<br>{CharacterData.CharacterName}, GUID:{{{CharacterData.CharacterID}}}<br></font>");

                RHMessageBox.ShowOKMessage("Title added successfully!", "Success");

                await ReadTitle(CharacterData.CharacterID);
            }
        }
        catch (Exception ex)
        {
            RHMessageBox.ShowOKMessage($"Error: {ex.Message}");
        }
    }

    #endregion

    #region Equip Title
    [RelayCommand]
    private async Task EquipTitle()
    {
        if (CharacterData == null) return;

        try
        {
            if (await _databaseService.IsCharacterOnlineAsync(CharacterData.CharacterName!))
            {
                return;
            }

            if (SelectedTitle != null)
            {
                int expireTime = (int)SelectedTitle["ExpireTime"];

                if (expireTime != 0 && DateTimeFormatter.ConvertFromEpoch(expireTime) < DateTime.UtcNow)
                {
                    RHMessageBox.ShowOKMessage($"The title '{SelectedTitleName}' has expired.", "Expirated Title");
                    return;
                }

                if (RHMessageBox.ConfirmMessage($"Equip the title '{SelectedTitleName}' to this character?"))
                {
                    await _databaseService.EquipCharacterTitleAsync(CharacterData.CharacterID, SelectedTitleUid);
                    await _databaseService.GMAuditAsync(CharacterData.AccountName!, CharacterData.CharacterID, CharacterData.CharacterName!, "Change Equip Title", $"<font color=blue>Change Equip Title</font>]<br><font color=red>Title: {SelectedTitleId}<br>{CharacterData.CharacterName}, GUID:{{{CharacterData.CharacterID}}}<br></font>");

                    RHMessageBox.ShowOKMessage("Title equiped successfully!", "Success");

                    await ReadTitle(CharacterData.CharacterID);
                }
            }

        }
        catch (Exception ex)
        {
            RHMessageBox.ShowOKMessage($"Error: {ex.Message}");
        }
    }

    #endregion

    #region Unequip Title

    [RelayCommand]
    private async Task UnequipTitle()
    {
        if (CharacterData == null) return;

        try
        {
            if (await _databaseService.IsCharacterOnlineAsync(CharacterData.CharacterName!))
            {
                return;
            }

            if (EquippedTitle != null)
            {
                if (RHMessageBox.ConfirmMessage($"Unequip the title '{EquippedTitleName}' from this character?"))
                {
                    await _databaseService.UnequipCharacterTitleAsync(EquippedTitleUid);
                    await _databaseService.GMAuditAsync(CharacterData.AccountName!, CharacterData.CharacterID, CharacterData.CharacterName!, "Change Equip Title", $"<font color=blue>Change Equip Title</font>]<br><font color=red>Title: {EquippedTitleId}<br>{CharacterData.CharacterName}, GUID:{{{CharacterData.CharacterID}}}<br></font>");

                    RHMessageBox.ShowOKMessage("Title unequiped successfully!", "Success");
                    await ReadTitle(CharacterData.CharacterID);
                }
            }
            else
            {
                RHMessageBox.ShowOKMessage("This character dont have a title equipped.", "No Title");
                return;
            }
        }
        catch (Exception ex)
        {
            RHMessageBox.ShowOKMessage($"Error: {ex.Message}");
        }
    }
    #endregion

    #region Remove Title

    [RelayCommand]
    private async Task RemoveTitle()
    {
        if (CharacterData == null) return;

        try
        {
            if (await _databaseService.IsCharacterOnlineAsync(CharacterData.CharacterName!))
            {
                return;
            }

            if (SelectedTitle != null)
            {
                if (RHMessageBox.ConfirmMessage($"Delete the '{SelectedTitleName}' title from this character?"))
                {
                    await _databaseService.UnequipCharacterTitleAsync(SelectedTitleUid);
                    await _databaseService.RemoveCharacterTitleAsync(CharacterData.CharacterID, SelectedTitleUid);
                    await _databaseService.GMAuditAsync(CharacterData.AccountName!, CharacterData.CharacterID, CharacterData.CharacterName!, "Character Title Deletion", $"<font color=blue>Character Title Deletion</font>]<br><font color=red>Deleted: {SelectedTitleId}<br>{CharacterData.CharacterName}, GUID:{{{CharacterData.CharacterID}}}<br></font>");

                    RHMessageBox.ShowOKMessage($"Title '{SelectedTitleName}' deleted successfully!", "Success");

                    SelectedTitle = null;

                    await ReadTitle(CharacterData.CharacterID);
                }
            }
        }
        catch (Exception ex)
        {
            RHMessageBox.ShowOKMessage($"Error removing title: {ex.Message}");
        }
    }
    #endregion

    #region Comboboxes

    [ObservableProperty]
    private int _titleListSelectedIndex;

    [ObservableProperty]
    private List<NameID>? _titleListItems;

    private void PopulateTitleItems()
    {
        try
        {
            TitleListItems = _gmDatabaseService.GetTitleItems();

            if (TitleListItems.Count > 0)
            {
                TitleListSelectedIndex = 0;
            }
        }
        catch (Exception ex)
        {
            RHMessageBox.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
        }
    }

    #endregion

    #region Properties
    [ObservableProperty]
    private string _title = "Character Title";

    [ObservableProperty]
    private string? _currentTitleText;

    [ObservableProperty]
    private string? _selectedTitleText;

    [ObservableProperty]
    private string? _selectedAddTitleText;

    [ObservableProperty]
    private Guid _selectedTitleUid;

    [ObservableProperty]
    private string? _selectedTitleName;

    [ObservableProperty]
    private DataRow? _equippedTitle;

    [ObservableProperty]
    private string? _equippedTitleName;

    [ObservableProperty]
    private Guid _equippedTitleUid;

    [ObservableProperty]
    private int _equippedTitleId;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedAddTitleText))]
    [NotifyPropertyChangedFor(nameof(IsAddTitleButtonEnabled))]
    private int _TitleListId;
    partial void OnTitleListIdChanged(int value)
    {
        SelectedAddTitleText = GetTitleDesc(value, true);
        IsAddTitleButtonEnabled = value == 0 ? false : true;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedTitleText))]
    [NotifyPropertyChangedFor(nameof(SelectedTitleName))]
    private int _selectedTitleId;
    partial void OnSelectedTitleIdChanged(int value)
    {
        SelectedTitleText = GetTitleDesc(value, false);
        SelectedTitleName = _gmDatabaseService.GetTitleName(value);
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedTitleId))]
    [NotifyPropertyChangedFor(nameof(IsEquipTitleButtonEnabled))]
    [NotifyPropertyChangedFor(nameof(IsRemoveTitleButtonEnabled))]
    private DataRowView? _selectedTitle;
    partial void OnSelectedTitleChanged(DataRowView? value)
    {
        SelectedTitleId = value != null ? (int)value["TitleId"] : 0;
        SelectedTitleUid = value != null ? (Guid)value["TitleUid"] : Guid.Empty;
        IsEquipTitleButtonEnabled = value != null ? true : false;
        IsRemoveTitleButtonEnabled = value != null ? true : false;
    }

    [ObservableProperty]
    private bool _isEquipTitleButtonEnabled = false;

    [ObservableProperty]
    private bool _isRemoveTitleButtonEnabled = false;

    [ObservableProperty]
    private bool _isUnequipTitleButtonEnabled = false;

    [ObservableProperty]
    private bool _isAddTitleButtonEnabled = false;

    #endregion

}
