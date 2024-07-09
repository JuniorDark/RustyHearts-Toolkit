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

    public async void Receive(CharacterDataMessage message)
    {
        if (Token == Guid.Empty)
        {
            Token = message.Token;
        }

        if (message.Recipient == "TitleWindow" && message.Token == Token)
        {
            var characterData = message.Value;

            CharacterData = null;
            CharacterData = characterData;

            Title = $"Character Title ({CharacterData.CharacterName})";

            await ReadTitle(CharacterData.CharacterID);
        }
    }

    [ObservableProperty]
    private DataTable? _titleData;

    private async Task ReadTitle(Guid characterID)
    {
        TitleData = null;
        TitleData = await _databaseService.ReadCharacterTitleListAsync(characterID);

        EquippedTitle = null;
        EquippedTitle = await _databaseService.ReadCharacterEquipTitleAsync(characterID);

        if (EquippedTitle != null)
        {
            EquippedTitleUid = (Guid)EquippedTitle["ID"];
            EquippedTitleId = (int)EquippedTitle["title_code"];
            EquippedTitleName = _gmDatabaseService.GetTitleName(EquippedTitleId);
            CurrentTitleText = $"{EquippedTitleName}";
        }
        else
        {
            CurrentTitleText = $"No Title";
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
            RHMessageBoxHelper.ShowOKMessage("Error: " + ex.Message);
            return string.Empty;
        }
    }

    #endregion

    #region Add Title
    [RelayCommand(CanExecute = nameof(CanExecuteAddTitleCommand))]
    private async Task AddTitle()
    {
        if (CharacterData == null) return;

        try
        {
            if (await _databaseService.IsCharacterOnlineAsync(CharacterData.CharacterName!))
            {
                return;
            }

            int selectedTitleID = TitleListId;
            int selectedTitleRemainTime = _gmDatabaseService.GetTitleRemainTime(selectedTitleID);
            int selectedTitleExpireTime;
            string titleName = _gmDatabaseService.GetTitleName(selectedTitleID);

            if (RHMessageBoxHelper.ConfirmMessage($"Add the title '{titleName}' to this character?"))
            {
                // Check if the character already has the same title
                if (await _databaseService.CharacterHasTitle(CharacterData.CharacterID, selectedTitleID))
                {
                    RHMessageBoxHelper.ShowOKMessage($"This character already has '{titleName}' title.", "Duplicate Title");
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
                    selectedTitleExpireTime = (int)DateTime.Now.AddMinutes(selectedTitleRemainTime).Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
                }

                await _databaseService.AddCharacterTitleAsync(CharacterData, selectedTitleID, selectedTitleRemainTime, selectedTitleExpireTime);
                await _databaseService.GMAuditAsync(CharacterData, "Add Title", $"Title: {selectedTitleID}");
                RHMessageBoxHelper.ShowOKMessage($"Title '{titleName}' added successfully!", "Success");

                await ReadTitle(CharacterData.CharacterID);
            }
        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"Error: {ex.Message}");
        }
    }

    private bool CanExecuteAddTitleCommand()
    {
        return CharacterData != null && TitleListId != 0;
    }

    private void OnCanExecuteCommandChanged()
    {
        AddTitleCommand.NotifyCanExecuteChanged();
        EquipTitleCommand.NotifyCanExecuteChanged();
        UnequipTitleCommand.NotifyCanExecuteChanged();
        DeleteTitleCommand.NotifyCanExecuteChanged();
    }

    #endregion

    #region Equip Title
    [RelayCommand(CanExecute = nameof(CanExecuteTitleCommand))]
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

                if (expireTime != 0 && DateTimeFormatter.ConvertFromEpoch(expireTime) < DateTime.Now)
                {
                    RHMessageBoxHelper.ShowOKMessage($"The title '{SelectedTitleName}' has expired.", "Expirated Title");
                    return;
                }

                if (RHMessageBoxHelper.ConfirmMessage($"Equip the title '{SelectedTitleName}' to this character?"))
                {
                    await _databaseService.EquipCharacterTitleAsync(CharacterData, SelectedTitleUid);
                    await _databaseService.GMAuditAsync(CharacterData, "Change Equip Title", $"Title: {SelectedTitleUid}");
                    RHMessageBoxHelper.ShowOKMessage("Title equiped successfully!", "Success");

                    await ReadTitle(CharacterData.CharacterID);
                }
            }

        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"Error: {ex.Message}");
        }
    }

    private bool CanExecuteTitleCommand()
    {
        return CharacterData != null && SelectedTitle != null;
    }

    #endregion

    #region Unequip Title

    [RelayCommand(CanExecute = nameof(CanExecuteUnequipTitleCommand))]
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
                if (RHMessageBoxHelper.ConfirmMessage($"Unequip the title '{EquippedTitleName}' from this character?"))
                {
                    await _databaseService.UnequipCharacterTitleAsync(CharacterData, EquippedTitleUid);
                    await _databaseService.GMAuditAsync(CharacterData, "Change Equip Title", $"Title: {EquippedTitleUid}");
                    RHMessageBoxHelper.ShowOKMessage("Title unequiped successfully!", "Success");
                    await ReadTitle(CharacterData.CharacterID);
                }
            }
            else
            {
                RHMessageBoxHelper.ShowOKMessage("This character dont have a title equipped.", "No Title");
                return;
            }
        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"Error: {ex.Message}");
        }
    }

    private bool CanExecuteUnequipTitleCommand()
    {
        return CharacterData != null && EquippedTitle != null;
    }
    #endregion

    #region Delete Title

    [RelayCommand(CanExecute = nameof(CanExecuteTitleCommand))]
    private async Task DeleteTitle()
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
                if (RHMessageBoxHelper.ConfirmMessage($"Delete the '{SelectedTitleName}' title from this character?"))
                {
                    await _databaseService.UnequipCharacterTitleAsync(CharacterData, SelectedTitleUid);
                    await _databaseService.DeleteCharacterTitleAsync(CharacterData, SelectedTitleUid);
                    await _databaseService.GMAuditAsync(CharacterData, "Character Title Deletion", $"Deleted: {SelectedTitleUid}");
                    RHMessageBoxHelper.ShowOKMessage($"Title '{SelectedTitleName}' deleted successfully!", "Success");

                    SelectedTitle = null;

                    await ReadTitle(CharacterData.CharacterID);
                }
            }
        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"Error removing title: {ex.Message}");
        }
    }
    #endregion

    #region Comboboxes

    [ObservableProperty]
    private List<NameID>? _titleListItems;

    private void PopulateTitleItems()
    {
        try
        {
            TitleListItems = _gmDatabaseService.GetTitleItems();

            if (TitleListItems.Count > 0)
            {
                TitleListId = 0;
            }
        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
        }
    }

    #endregion

    #region Properties
    [ObservableProperty]
    private Guid? _token = Guid.Empty;

    [ObservableProperty]
    private string _title = "Character Title";

    [ObservableProperty]
    private CharacterData? _characterData;
    partial void OnCharacterDataChanged(CharacterData? value)
    {
       AddTitleCommand.NotifyCanExecuteChanged();
    }

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
    partial void OnEquippedTitleChanged(DataRow? value)
    {
        UnequipTitleCommand.NotifyCanExecuteChanged();
    }

    [ObservableProperty]
    private string? _equippedTitleName;

    [ObservableProperty]
    private Guid _equippedTitleUid;

    [ObservableProperty]
    private int _equippedTitleId;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedAddTitleText))]
    private int _TitleListId;
    partial void OnTitleListIdChanged(int value)
    {
        SelectedAddTitleText = GetTitleDesc(value, true);
        AddTitleCommand.NotifyCanExecuteChanged();
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
    private DataRowView? _selectedTitle;
    partial void OnSelectedTitleChanged(DataRowView? value)
    {
        SelectedTitleId = value != null ? (int)value["TitleId"] : 0;
        SelectedTitleUid = value != null ? (Guid)value["TitleUid"] : Guid.Empty;
        EquipTitleCommand.NotifyCanExecuteChanged();
        DeleteTitleCommand.NotifyCanExecuteChanged();
    }

    #endregion

}
