using RHToolkit.Messages;
using RHToolkit.Models;
using RHToolkit.Models.MessageBox;
using RHToolkit.Properties;
using RHToolkit.Services;
using System.Data;
using static RHToolkit.Models.EnumService;

namespace RHToolkit.ViewModels.Windows;

public partial class SanctionWindowViewModel : ObservableObject, IRecipient<CharacterInfoMessage>
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

    public async void Receive(CharacterInfoMessage message)
    {
        if (message.Recipient == "SanctionWindow")
        {
            var characterInfo = message.Value;
            CharacterInfo = null;
            CharacterInfo = characterInfo;

            Title = $"Character Sanction ({characterInfo.CharacterName})";

            await ReadSanction();
        }
    }

    private async Task ReadSanction()
    {
        if (CharacterInfo == null) return;

        try
        {
            SanctionData = null;
            SanctionData = await _databaseService.ReadCharacterSanctionListAsync(CharacterInfo.CharacterID);
        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"Error: {ex.Message}");
        }
    }

    #endregion

    #region Add/Remove Sanction

    [RelayCommand(CanExecute = nameof(CanExecuteAddSanctionCommand))]
    private async Task AddSanction()
    {
        try
        {
           await ProcessSanction(SanctionOperationType.Add);
        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"Error: {ex.Message}");
        }
    }

    [RelayCommand(CanExecute = nameof(CanExecuteReleaseSanctionCommand))]
    private async Task ReleaseSanction()
    {
        try
        {
           await ProcessSanction(SanctionOperationType.Remove);
        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"Error: {ex.Message}");
        }
    }

    private async Task ProcessSanction(SanctionOperationType operationType)
    {
        if (CharacterInfo == null) return;

        if (await _databaseService.IsCharacterOnlineAsync(CharacterInfo.CharacterName!))
        {
            return;
        }

        bool isSanctioned = await _databaseService.CharacterHasSanctionAsync(CharacterInfo.CharacterID);

        if ((operationType == SanctionOperationType.Add && isSanctioned) || (operationType == SanctionOperationType.Remove && !isSanctioned))
        {
            RHMessageBoxHelper.ShowOKMessage(operationType == SanctionOperationType.Add ? "This character is already sanctioned.\nRemove the current sanction before adding other." : "This character has no active sanctions.", "Info");
            return;
        }

        string releaser = operationType == SanctionOperationType.Add ? "" : "RHToolkit";
        string comment = operationType == SanctionOperationType.Add ? "" : SanctionComment;

        if (operationType == SanctionOperationType.Remove && string.IsNullOrWhiteSpace(SanctionComment))
        {
            RHMessageBoxHelper.ShowOKMessage("Enter a comment for the sanction removal", "Info");
            return;
        }

        try
        {
            await ProcessSanctionData(operationType, releaser, comment);
            await ReadSanction();
        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"Error {(operationType == SanctionOperationType.Add ? "adding" : "removing")} sanction: {ex.Message}", "Error");
        }
    }

    private async Task ProcessSanctionData(SanctionOperationType operationType, string releaser, string comment)
    {
        if (CharacterInfo == null) return;

        Guid selectedSanctionUid = operationType == SanctionOperationType.Remove ? SelectedSanctionUid : Guid.Empty;
        int isApplyValue = operationType == SanctionOperationType.Remove ? SelectedIsApplySanction : 0;
        string reason = operationType == SanctionOperationType.Remove ? SelectedSanctionReason : string.Empty;

        if (operationType == SanctionOperationType.Remove && isApplyValue == 0)
        {
            RHMessageBoxHelper.ShowOKMessage("Selected sanction was already removed.", "Info");
            return;
        }

        string reasonDetails = operationType == SanctionOperationType.Add ? GetSanctionDescription() : reason;

        (int sanctionType, int sanctionPeriod, int sanctionCount) = GetSanctionDetails(operationType);

        if (RHMessageBoxHelper.ConfirmMessage((operationType == SanctionOperationType.Add ? "Sanction this character for: " : "Remove the sanction from this character for: ") + reasonDetails + "?"))
        {
            await _databaseService.CharacterSanctionAsync(CharacterInfo, operationType, operationType == SanctionOperationType.Add ? Guid.NewGuid() : selectedSanctionUid, (int)operationType, reasonDetails, releaser, comment, sanctionType, sanctionPeriod, sanctionCount);

            RHMessageBoxHelper.ShowOKMessage(operationType == SanctionOperationType.Add ? "Sanction added successfully!" : "Sanction released successfully!", "Success");
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

    private string GetSanctionDescription()
    {
        string sanctionTypeName = GetEnumDescription((SanctionType)SanctionType);
        string sanctionCountName = GetEnumDescription((SanctionCount)SanctionCount);
        string sanctionPeriodName = GetEnumDescription((SanctionPeriod)SanctionPeriod);
        return $"{sanctionTypeName}|{sanctionCountName}|{sanctionPeriodName}";
    }

    private bool CanExecuteAddSanctionCommand()
    {
        return CharacterInfo != null;
    }

    private bool CanExecuteReleaseSanctionCommand()
    {
        return CharacterInfo != null && SelectedSanction != null;
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
            RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
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
            RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
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
            RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
        }
    }

    #endregion

    #region Properties
    [ObservableProperty]
    private string _title = "Character Sanction";

    [ObservableProperty]
    private CharacterInfo? _characterInfo;
    partial void OnCharacterInfoChanged(CharacterInfo? value)
    {
        AddSanctionCommand.NotifyCanExecuteChanged();
        ReleaseSanctionCommand.NotifyCanExecuteChanged();
    }

    [ObservableProperty]
    private DataTable? _sanctionData;

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
        ReleaseSanctionCommand.NotifyCanExecuteChanged();
    }

    #endregion

}
