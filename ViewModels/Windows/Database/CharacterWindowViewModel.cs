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
                Title = $"Character Editor ({characterData.CharacterName})";
                CharacterData = characterData;
                CharacterDataViewModel.LoadCharacterData(characterData);
                await EquipmentWindowViewModel.LoadCharacterData(characterData.CharacterName!);
            }
            else
            {
                RHMessageBoxHelper.ShowOKMessage($"The character '{characterName}' does not exist.", "Invalid Character");
                return;
            }
        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"Error reading Character Data: {ex.Message}", "Error");
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
                RHMessageBoxHelper.ShowOKMessage("There are no changes to save.", "Info");
                return;
            }

            string changes = CharacterDataManager.GenerateCharacterDataMessage(CharacterData, newCharacterData, "changes");

            if (RHMessageBoxHelper.ConfirmMessage($"Save the following changes to the character {CharacterData.CharacterName}?\n\n{changes}"))
            {
                string auditMessage = CharacterDataManager.GenerateCharacterDataMessage(CharacterData, newCharacterData, "audit");
                newCharacterData.CharacterID = CharacterData.CharacterID;
                await _databaseService.UpdateCharacterDataAsync(newCharacterData);
                await _databaseService.GMAuditAsync(CharacterData, "Character Information Change", auditMessage);
                RHMessageBoxHelper.ShowOKMessage("Character changes saved.", "Success");
                await ReadCharacterData(CharacterData.CharacterName!);
            }
        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"Error saving character changes: {ex.Message}", "Error");
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
            RHMessageBoxHelper.ShowOKMessage($"The character name '{newCharacterName}' is on the nick filter and its not allowed.", "Error");
            return;
        }

        if (!ValidateCharacterName(newCharacterName))
        {
            RHMessageBoxHelper.ShowOKMessage("Invalid character name.", "Error");
            return;
        }

        if (RHMessageBoxHelper.ConfirmMessage($"Change the character name from '{CharacterData.CharacterName}' to '{newCharacterName}'?"))
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
                    RHMessageBoxHelper.ShowOKMessage($"The character name '{newCharacterName}' already exists.", "Error");
                    return;
                }

                await _databaseService.GMAuditAsync(CharacterData, "Character Name Change", $"Old Name:{CharacterData.CharacterName}, New Name: {newCharacterName}");

                RHMessageBoxHelper.ShowOKMessage("Character name updated successfully!", "Success");
                CharacterData.CharacterName = newCharacterName;
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"Error updating character name: {ex.Message}", "Error");
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

            if (RHMessageBoxHelper.ConfirmMessage($"EXPERIMENTAL FEATURE\n\nThis will reset all character skills and unequip the character weapon/costumes and send via mail.\n\nAre you sure you want to change character '{CharacterData.CharacterName}' class from '{GetEnumDescription((CharClass)CharacterData.Class)}' to '{GetEnumDescription((CharClass)CharacterDataViewModel.Class)}'?"))
            {
                await _databaseService.UpdateCharacterClassAsync(CharacterData, CharacterDataViewModel.Class);
                await _databaseService.GMAuditAsync(CharacterData, "Character Class Change", $"Old Class: {CharacterData.Class} => New Class: {CharacterDataViewModel.Class}");
                RHMessageBoxHelper.ShowOKMessage("Character class changed successfully!", "Success");
                await ReadCharacterData(CharacterData.CharacterName!);
            }
        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"Error changing character class: {ex.Message}", "Error");
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

            if (RHMessageBoxHelper.ConfirmMessage($"This will reset all character skills.\nAre you sure you want to change character '{CharacterData.CharacterName}' focus?"))
            {
                await _databaseService.UpdateCharacterJobAsync(CharacterData, CharacterDataViewModel.Job);
                await _databaseService.GMAuditAsync(CharacterData, "Character Job Change", $"Old Job: {CharacterData.Job} => New Job: {CharacterDataViewModel.Job}");
                RHMessageBoxHelper.ShowOKMessage("Character focus changed successfully!", "Success");
                await ReadCharacterData(CharacterData.CharacterName!);
            }
        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"Error changing character focus: {ex.Message}", "Error");
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
            RHMessageBoxHelper.ShowOKMessage($"Error reading Character {errorMessage}: {ex.Message}", "Error");
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
    private string _title = "Character Editor";

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
