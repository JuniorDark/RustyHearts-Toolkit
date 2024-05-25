using RHToolkit.Messages;
using RHToolkit.Models;
using RHToolkit.Models.MessageBox;
using RHToolkit.Properties;
using RHToolkit.Services;
using System.Data;
using static RHToolkit.Models.EnumService;

namespace RHToolkit.ViewModels.Windows;

public partial class SanctionWindowViewModel : ObservableObject, IRecipient<CharacterDataMessage>
{
    private readonly IDatabaseService _databaseService;

    public SanctionWindowViewModel(IDatabaseService databaseService)
    {
        _databaseService = databaseService;

        PopulateSanctionCountItems();
        PopulateSanctionPeriodItems();
        PopulateSanctionTypeItems();

        WeakReferenceMessenger.Default.Register(this);
    }

    #region Read Sanction

    [ObservableProperty]
    private CharacterData? _characterData;

    public async void Receive(CharacterDataMessage message)
    {
        if (message.Recipient == "SanctionWindow")
        {
            var characterData = message.Value;
            CharacterData = null;
            CharacterData = characterData;

            Title = $"Character Sanction ({characterData.CharacterName})";

            await ReadSanction();
        }
    }

    [ObservableProperty]
    private DataTable? _sanctionData;

    private async Task ReadSanction()
    {
        if (CharacterData == null) return;

        try
        {
            SanctionData = null;
            SanctionData = await _databaseService.ReadCharacterSanctionListAsync(CharacterData.CharacterID);
        }
        catch (Exception ex)
        {
            RHMessageBox.ShowOKMessage($"Error: {ex.Message}");
        }
    }

    #endregion

    #region Add/Remove Sanction

    [RelayCommand]
    private async Task AddSanction()
    {
        try
        {
           await ProcessSanction(SanctionOperationType.Add);
        }
        catch (Exception ex)
        {
            RHMessageBox.ShowOKMessage($"Error: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ReleaseSanction()
    {
        try
        {
           await ProcessSanction(SanctionOperationType.Remove);
        }
        catch (Exception ex)
        {
            RHMessageBox.ShowOKMessage($"Error: {ex.Message}");
        }
    }

    private async Task ProcessSanction(SanctionOperationType operationType)
    {
        if (CharacterData == null) return;

        if (await _databaseService.IsCharacterOnlineAsync(CharacterData.CharacterName!))
        {
            return;
        }

        bool isSanctioned = await _databaseService.CharacterHasSanctionAsync(CharacterData.CharacterID);

        if ((operationType == SanctionOperationType.Add && isSanctioned) || (operationType == SanctionOperationType.Remove && !isSanctioned))
        {
            RHMessageBox.ShowOKMessage(operationType == SanctionOperationType.Add ? "This character is already sanctioned.\nRemove the current sanction before adding other." : "This character has no active sanctions.", "Info");
            return;
        }

        string releaser = operationType == SanctionOperationType.Add ? "" : "RHToolkit";
        string comment = operationType == SanctionOperationType.Add ? "" : SanctionComment;

        if (operationType == SanctionOperationType.Remove && string.IsNullOrWhiteSpace(SanctionComment))
        {
            RHMessageBox.ShowOKMessage("Enter a comment for the sanction removal", "Info");
            return;
        }

        try
        {
            await ProcessSanctionInternal(operationType, releaser, comment);
            await ReadSanction();
        }
        catch (Exception ex)
        {
            RHMessageBox.ShowOKMessage($"Error {(operationType == SanctionOperationType.Add ? "adding" : "removing")} sanction: {ex.Message}", "Error");
        }
    }

    private async Task ProcessSanctionInternal(SanctionOperationType operationType, string releaser, string comment)
    {
        Guid selectedSanctionUid = operationType == SanctionOperationType.Remove ? SelectedSanctionUid : Guid.Empty;
        int isApplyValue = operationType == SanctionOperationType.Remove ? SelectedIsApplySanction : 0;
        string reason = operationType == SanctionOperationType.Remove ? SelectedSanctionReason : string.Empty;

        if (operationType == SanctionOperationType.Remove && isApplyValue == 0)
        {
            RHMessageBox.ShowOKMessage("Selected sanction was already removed.", "Info");
            return;
        }

        string reasonDetails = operationType == SanctionOperationType.Add ? GetSanctionDetails() : reason;

        (int sanctionType, int sanctionPeriod, int sanctionCount) = GetSanctionDetails(operationType);

        if (RHMessageBox.ConfirmMessage((operationType == SanctionOperationType.Add ? "Sanction this character for: " : "Remove the sanction from this character for: ") + reasonDetails + "?"))
        {
            (Guid sanctionUid, _) = await _databaseService.CharacterSanctionAsync(CharacterData!.CharacterID, operationType == SanctionOperationType.Add ? Guid.NewGuid() : selectedSanctionUid, (int)operationType, releaser, comment, sanctionType, sanctionPeriod, sanctionCount);

            if (operationType == SanctionOperationType.Add)
            {
                (DateTime startTime, DateTime? endTime) = await _databaseService.GetSanctionTimesAsync(sanctionUid);
                await _databaseService.SanctionLogAsync(sanctionUid, CharacterData.CharacterID, CharacterData.AccountName!, CharacterData.CharacterName!, startTime, endTime, reasonDetails);
                await _databaseService.GMAuditAsync(CharacterData.AccountName!, CharacterData.CharacterID, CharacterData.CharacterName!, "Character Sanction", reasonDetails);
            }
            else
            {
                await _databaseService.UpdateSanctionLogAsync(sanctionUid, releaser, comment, 1);
                await _databaseService.GMAuditAsync(CharacterData.AccountName!, CharacterData.CharacterID, CharacterData.CharacterName!, "Character Sanction Release", reasonDetails);
            }

            RHMessageBox.ShowOKMessage(operationType == SanctionOperationType.Add ? "Sanction added successfully!" : "Sanction released successfully!", "Success");
        }
    }

    private (int, int, int) GetSanctionDetails(SanctionOperationType operationType)
    {
        if (operationType == SanctionOperationType.Add)
        {
            return (SanctionType, SanctionPeriod, SanctionCount);
        }
        else
        {
            return (0, 0, 0);
        }
    }

    private string GetSanctionDetails()
    {
        string sanctionTypeName = GetEnumDescription((SanctionType)SanctionType);
        string sanctionCountName = GetEnumDescription((SanctionCount)SanctionCount);
        string sanctionPeriodName = GetEnumDescription((SanctionPeriod)SanctionPeriod);
        return $"{sanctionTypeName}|{sanctionCountName}|{sanctionPeriodName}";
    }

    #endregion

    #region Comboboxes

    [ObservableProperty]
    private List<NameID>? _sanctionTypeItems;

    private void PopulateSanctionTypeItems()
    {
        try
        {
            SanctionTypeItems = GetEnumItems<SanctionType>();

            if (SanctionTypeItems.Count > 0)
            {
                SanctionType = 1;
            }
        }
        catch (Exception ex)
        {
            RHMessageBox.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
        }
    }

    [ObservableProperty]
    private List<NameID>? _sanctionCountItems;

    private void PopulateSanctionCountItems()
    {
        try
        {
            SanctionCountItems = GetEnumItems<SanctionCount>();

            if (SanctionCountItems.Count > 0)
            {
                SanctionCount = 1;
            }
        }
        catch (Exception ex)
        {
            RHMessageBox.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
        }
    }

    [ObservableProperty]
    private List<NameID>? _sanctionPeriodItems;

    private void PopulateSanctionPeriodItems()
    {
        try
        {
            SanctionPeriodItems = GetEnumItems<SanctionPeriod>();

            if (SanctionPeriodItems.Count > 0)
            {
                SanctionPeriod = 1;
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
    private string _title = "Character Sanction";

    [ObservableProperty]
    private string _sanctionComment = string.Empty;

    [ObservableProperty]
    private int _sanctionType;

    [ObservableProperty]
    private int _sanctionPeriod;

    [ObservableProperty]
    private int _sanctionCount;

    [ObservableProperty]
    private Guid _selectedSanctionUid;

    [ObservableProperty]
    private int _selectedIsApplySanction;

    [ObservableProperty]
    private string _selectedSanctionReason = string.Empty;

    [ObservableProperty]
    private DataRowView? _selectedSanction;
    partial void OnSelectedSanctionChanged(DataRowView? value)
    {
        SelectedSanctionUid = value != null ? (Guid)value["SanctionUid"] : Guid.Empty;
        SelectedIsApplySanction = value != null ? (int)value["IsApply"] : 0;
        SelectedSanctionReason = value != null ? (string)value["Reason"] : string.Empty;
        IsRemoveSanctionButtonEnabled = value != null ? true : false;
    }

    [ObservableProperty]
    private bool _isRemoveSanctionButtonEnabled = false;

    #endregion

}
