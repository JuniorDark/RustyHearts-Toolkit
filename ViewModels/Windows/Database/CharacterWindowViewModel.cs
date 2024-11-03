using RHToolkit.Messages;
using RHToolkit.Models;
using RHToolkit.Models.Database;
using RHToolkit.Models.MessageBox;
using RHToolkit.Services;
using RHToolkit.ViewModels.Windows.Database.VM;
using static RHToolkit.Models.EnumService;

namespace RHToolkit.ViewModels.Windows;

public partial class CharacterWindowViewModel : ObservableObject, IRecipient<CharacterDataMessage>
{
    private readonly IWindowsService _windowsService;
    private readonly IDatabaseService _databaseService;

    public CharacterWindowViewModel(IWindowsService windowsService, IDatabaseService databaseService, CharacterDataViewModel characterDataViewModel, EquipmentWindowViewModel equipmentWindowViewModel)
    {
        _windowsService = windowsService;
        _databaseService = databaseService;
        _characterDataViewModel = characterDataViewModel;
        _equipmentWindowViewModel = equipmentWindowViewModel;
        WeakReferenceMessenger.Default.Register(this);
    }

    #region Messenger

    #region Load Character

    public async void Receive(CharacterDataMessage message)
    {
        if (Token == Guid.Empty)
        {
            Token = message.Token;
        }

        if (message.Recipient == "CharacterWindow" && message.Token == Token)
        {
            var characterData = message.Value;

            await ReadCharacterData(characterData.CharacterName!);
        }
    }

    private async Task ReadCharacterData(string characterName)
    {
        try
        {
            CharacterData? characterData = await _databaseService.GetCharacterDataAsync(characterName);

            if (characterData != null)
            {
                CharacterData = null;
                Title = string.Format(Resources.EditorTitleFileName, Resources.Character, characterData.CharacterName);
                CharacterData = characterData;
                CharacterDataViewModel.LoadCharacterData(characterData);
                await EquipmentWindowViewModel.LoadCharacterData(characterData.CharacterName!);
            }
            else
            {
                RHMessageBoxHelper.ShowOKMessage(string.Format(Resources.InexistentCharacterMessage, characterName), Resources.InvalidCharacter);
                return;
            }
        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
        }
    }

    #endregion

    #endregion

    #region Commands

    #region Save Character
    [RelayCommand(CanExecute = nameof(CanExecuteCommand))]
    private async Task SaveCharacter()
    {
        if (CharacterData == null) return;

        if (!SqlCredentialValidator.ValidateCredentials())
        {
            return;
        }

        try
        {
            if (await _databaseService.IsCharacterOnlineAsync(CharacterData.CharacterName!))
            {
                return;
            }

            var newCharacterData = CharacterDataViewModel.GetCharacterChanges();

            if (!CharacterDataManager.HasCharacterDataChanges(CharacterData, newCharacterData))
            {
                RHMessageBoxHelper.ShowOKMessage(Resources.NoChangesMessage, Resources.Info);
                return;
            }

            string changes = CharacterDataManager.GenerateCharacterDataMessage(CharacterData, newCharacterData, "changes");

            if (RHMessageBoxHelper.ConfirmMessage($"{string.Format(Resources.EditCharacterSaveMessage, CharacterData.CharacterName)}\n\n{changes}"))
            {
                string auditMessage = CharacterDataManager.GenerateCharacterDataMessage(CharacterData, newCharacterData, "audit");
                newCharacterData.CharacterID = CharacterData.CharacterID;
                await _databaseService.UpdateCharacterDataAsync(newCharacterData);
                await _databaseService.GMAuditAsync(CharacterData, "Character Information Change", auditMessage);
                RHMessageBoxHelper.ShowOKMessage(Resources.EditCharacterSaveSuccessMessage, Resources.Success);
                await ReadCharacterData(CharacterData.CharacterName!);
            }
        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"{Resources.EditCharacterErrorMessage}: {ex.Message}", Resources.Error);
        }
    }

    private bool CanExecuteCommand()
    {
        return CharacterData != null;
    }

    private void OnCanExecuteCommandChanged()
    {
        SaveCharacterCommand.NotifyCanExecuteChanged();
        SaveCharacterNameCommand.NotifyCanExecuteChanged();
        SaveCharacterClassCommand.NotifyCanExecuteChanged();
        SaveCharacterJobCommand.NotifyCanExecuteChanged();
        OpenEquipmentWindowCommand.NotifyCanExecuteChanged();
        OpenInventoryWindowCommand.NotifyCanExecuteChanged();
        OpenStorageWindowCommand.NotifyCanExecuteChanged();
        OpenTitleWindowCommand.NotifyCanExecuteChanged();
        OpenSanctionWindowCommand.NotifyCanExecuteChanged();
        OpenFortuneWindowCommand.NotifyCanExecuteChanged();
    }
    #endregion

    #region Character Name
    [RelayCommand]
    private async Task SaveCharacterName()
    {
        string? newCharacterName = CharacterDataViewModel.CharacterName;

        if (CharacterData == null || newCharacterName == null || CharacterDataViewModel.CharacterName == newCharacterName)
        {
            return;
        }

        if (!SqlCredentialValidator.ValidateCredentials())
        {
            return;
        }

        if (CharacterDataViewModel.IsNameNotAllowed(newCharacterName))
        {
            RHMessageBoxHelper.ShowOKMessage(string.Format(Resources.CharacterEditNameNotAllowedMessage, newCharacterName), Resources.Error);
            return;
        }

        if (!ValidateCharacterName(newCharacterName))
        {
            RHMessageBoxHelper.ShowOKMessage(Resources.CharacterEditInvalidNameMessage, Resources.Error);
            return;
        }

        if (RHMessageBoxHelper.ConfirmMessage(string.Format(Resources.CharacterEditSaveNameMessage, CharacterData.CharacterName, newCharacterName)))
        {
            try
            {
                if (await _databaseService.IsCharacterOnlineAsync(CharacterData.CharacterName!))
                {
                    return;
                }

                int result = await _databaseService.UpdateCharacterNameAsync(CharacterData.CharacterID, newCharacterName);

                if (result == -1)
                {
                    RHMessageBoxHelper.ShowOKMessage(string.Format(Resources.CharacterEditAlreadyUsedNameMessage, newCharacterName), Resources.Error);
                    return;
                }

                await _databaseService.GMAuditAsync(CharacterData, "Character Name Change", $"Old Name:{CharacterData.CharacterName}, New Name: {newCharacterName}");

                RHMessageBoxHelper.ShowOKMessage(Resources.CharacterEditSaveNameSuccessMessage, Resources.Success);
                CharacterData.CharacterName = newCharacterName;
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
            }
        }

    }

    private static bool ValidateCharacterName(string characterName)
    {
        return !string.IsNullOrWhiteSpace(characterName) && characterName.Length >= 3 && characterName.Length <= 16;
    }
    #endregion

    #region Save Character Class / Job
    [RelayCommand]
    private async Task SaveCharacterClass()
    {
        if (CharacterData == null || CharacterData.Class == CharacterDataViewModel.Class) return;

        if (!SqlCredentialValidator.ValidateCredentials())
        {
            return;
        }

        try
        {
            if (await _databaseService.IsCharacterOnlineAsync(CharacterData.CharacterName!))
            {
                return;
            }

            if (RHMessageBoxHelper.ConfirmMessage(string.Format(Resources.CharacterEditorSaveClassMessage, CharacterData.CharacterName, GetEnumDescription((CharClass)CharacterData.Class), GetEnumDescription((CharClass)CharacterDataViewModel.Class))))
            {
                await _databaseService.UpdateCharacterClassAsync(CharacterData, CharacterDataViewModel.Class);
                await _databaseService.GMAuditAsync(CharacterData, "Character Class Change", $"Old Class: {CharacterData.Class} => New Class: {CharacterDataViewModel.Class}");
                RHMessageBoxHelper.ShowOKMessage(Resources.CharacterEditorSaveClassSuccessMessage, Resources.Success);
                await ReadCharacterData(CharacterData.CharacterName!);
            }
        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
        }
    }

    [RelayCommand]
    private async Task SaveCharacterJob()
    {
        if (CharacterData == null || CharacterData.Job == CharacterDataViewModel.Job) return;

        if (!SqlCredentialValidator.ValidateCredentials())
        {
            return;
        }

        try
        {
            if (await _databaseService.IsCharacterOnlineAsync(CharacterData.CharacterName!))
            {
                return;
            }

            if (RHMessageBoxHelper.ConfirmMessage(string.Format(Resources.CharacterEditorSaveFocusMessage, CharacterData.CharacterName)))
            {
                await _databaseService.UpdateCharacterJobAsync(CharacterData, CharacterDataViewModel.Job);
                await _databaseService.GMAuditAsync(CharacterData, "Character Job Change", $"Old Job: {CharacterData.Job} => New Job: {CharacterDataViewModel.Job}");
                RHMessageBoxHelper.ShowOKMessage(Resources.CharacterEditorSaveFocusSuccessMessage, Resources.Success);
                await ReadCharacterData(CharacterData.CharacterName!);
            }
        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
        }
    }
    #endregion

    #region Windows Buttons
    private void OpenWindow(Action<CharacterData> openWindowAction, string errorMessage)
    {
        if (CharacterData == null) return;

        try
        {
            openWindowAction(CharacterData);
        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {errorMessage}: {ex.Message}", Resources.Error);
        }
    }

    #region Equipment

    [RelayCommand(CanExecute = nameof(CanExecuteCommand))]
    private void OpenEquipmentWindow()
    {
        OpenWindow(_windowsService.OpenEquipmentWindow, "EquipItem");
    }

    #endregion

    #region Inventory

    [RelayCommand(CanExecute = nameof(CanExecuteCommand))]
    private void OpenInventoryWindow()
    {
        OpenWindow(_windowsService.OpenInventoryWindow, "Inventory");
    }

    #endregion

    #region Storage

    [RelayCommand(CanExecute = nameof(CanExecuteCommand))]
    private void OpenStorageWindow()
    {
        OpenWindow(_windowsService.OpenStorageWindow, "Storage");
    }

    #endregion

    #region Title

    [RelayCommand(CanExecute = nameof(CanExecuteCommand))]
    private void OpenTitleWindow()
    {
        OpenWindow(_windowsService.OpenTitleWindow, "Title");
    }

    #endregion

    #region Sanction

    [RelayCommand(CanExecute = nameof(CanExecuteCommand))]
    private void OpenSanctionWindow()
    {
        OpenWindow(_windowsService.OpenSanctionWindow, "Sanction");
    }

    #endregion

    #region Fortune

    [RelayCommand(CanExecute = nameof(CanExecuteCommand))]
    private void OpenFortuneWindow()
    {
        OpenWindow(_windowsService.OpenFortuneWindow, "Fortune");
    }

    #endregion

    #endregion

    #endregion

    #region Properties

    [ObservableProperty]
    private string _title = string.Format(Resources.EditorTitle, Resources.Character);

    [ObservableProperty]
    private Guid? _token = Guid.Empty;

    #region Character
    [ObservableProperty]
    private CharacterData? _characterData;
    partial void OnCharacterDataChanged(CharacterData? value)
    {
        OnCanExecuteCommandChanged();
    }

    [ObservableProperty]
    private CharacterDataViewModel _characterDataViewModel;

    [ObservableProperty]
    private EquipmentWindowViewModel _equipmentWindowViewModel;

    [ObservableProperty]
    private bool _isButtonEnabled = true;

    #endregion

    #endregion
}
