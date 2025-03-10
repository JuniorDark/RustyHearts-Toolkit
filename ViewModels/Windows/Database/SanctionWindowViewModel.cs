﻿using RHToolkit.Messages;
using RHToolkit.Models;
using RHToolkit.Models.MessageBox;
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

    public async void Receive(CharacterDataMessage message)
    {
        if (Token == Guid.Empty)
        {
            Token = message.Token;
        }

        if (message.Recipient == "SanctionWindow" && message.Token == Token)
        {
            var characterData = message.Value;
            CharacterData = null;
            CharacterData = characterData;

            Title = string.Format(Resources.EditorTitle, Resources.CharacterSanction);

            await ReadSanction();
        }
    }

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
            RHMessageBoxHelper.ShowOKMessage($"{Resources.Error} : {ex.Message}");
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
            RHMessageBoxHelper.ShowOKMessage($"{Resources.Error} : {ex.Message}");
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
            RHMessageBoxHelper.ShowOKMessage($"{Resources.Error} : {ex.Message}");
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
            RHMessageBoxHelper.ShowOKMessage(operationType == SanctionOperationType.Add ? Resources.SanctionEditAlreadySanctionedMessage : Resources.SanctionEditNoSanctionedMessage, Resources.Info);
            return;
        }

        string releaser = operationType == SanctionOperationType.Add ? "" : "RHToolkit";
        string comment = operationType == SanctionOperationType.Add ? "" : SanctionComment;

        if (operationType == SanctionOperationType.Remove && string.IsNullOrWhiteSpace(SanctionComment))
        {
            RHMessageBoxHelper.ShowOKMessage(Resources.SanctionEditCommentMessage, Resources.Info);
            return;
        }

        try
        {
            await ProcessSanctionData(operationType, releaser, comment);
            await ReadSanction();
        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"{string.Format(Resources.SanctionEditErrorMessage, operationType == SanctionOperationType.Add ? Resources.Adding : Resources.Removing)}: {ex.Message}", Resources.Error);
        }
    }

    private async Task ProcessSanctionData(SanctionOperationType operationType, string releaser, string comment)
    {
        if (CharacterData == null) return;

        Guid selectedSanctionUid = operationType == SanctionOperationType.Remove ? SelectedSanctionUid : Guid.Empty;
        int isApplyValue = operationType == SanctionOperationType.Remove ? SelectedIsApplySanction : 0;
        string reason = operationType == SanctionOperationType.Remove ? SelectedSanctionReason : string.Empty;

        if (operationType == SanctionOperationType.Remove && isApplyValue == 0)
        {
            RHMessageBoxHelper.ShowOKMessage(Resources.SanctionEditAlreadyRemovedSanctionMessage, Resources.Info);
            return;
        }

        string reasonDetails = operationType == SanctionOperationType.Add ? GetSanctionDescription() : reason;

        (int sanctionType, int sanctionPeriod, int sanctionCount) = GetSanctionDetails(operationType);

        if (RHMessageBoxHelper.ConfirmMessage((operationType == SanctionOperationType.Add ? $"{Resources.SanctionEditAddMessage}: " : $"{Resources.SanctionEditRemoveMessage}: ") + reasonDetails + "?"))
        {
            await _databaseService.CharacterSanctionAsync(CharacterData, operationType, operationType == SanctionOperationType.Add ? Guid.NewGuid() : selectedSanctionUid, (int)operationType, reasonDetails, releaser, comment, sanctionType, sanctionPeriod, sanctionCount);

            RHMessageBoxHelper.ShowOKMessage(operationType == SanctionOperationType.Add ? Resources.SanctionEditAddSuccessMessage : Resources.SanctionEditRemoveSuccessMessage, Resources.Success);
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
        return CharacterData != null;
    }

    private bool CanExecuteReleaseSanctionCommand()
    {
        return CharacterData != null && SelectedSanction != null;
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
    private Guid? _token = Guid.Empty;

    [ObservableProperty]
    private string _title = Resources.CharacterSanction;

    [ObservableProperty]
    private CharacterData? _characterData;
    partial void OnCharacterDataChanged(CharacterData? value)
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
