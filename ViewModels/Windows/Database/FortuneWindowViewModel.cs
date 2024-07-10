using RHToolkit.Messages;
using RHToolkit.Models;
using RHToolkit.Models.MessageBox;
using RHToolkit.Properties;
using RHToolkit.Services;
using System.Data;

namespace RHToolkit.ViewModels.Windows;

public partial class FortuneWindowViewModel : ObservableObject, IRecipient<CharacterDataMessage>
{
    private readonly IDatabaseService _databaseService;
    private readonly IGMDatabaseService _gmDatabaseService;

    public FortuneWindowViewModel(IDatabaseService databaseService, IGMDatabaseService gmDatabaseService)
    {
        _databaseService = databaseService;
        _gmDatabaseService = gmDatabaseService;

        PopulateFortuneItems();
        PopulateFortuneDescItems();

        WeakReferenceMessenger.Default.Register(this);
    }

    #region Read Fortune

    public async void Receive(CharacterDataMessage message)
    {
        if (Token == Guid.Empty)
        {
            Token = message.Token;
        }

        if (message.Recipient == "FortuneWindow" && message.Token == Token)
        {
            var characterData = message.Value;

            CharacterData = null;
            CharacterData = characterData;

            Title = $"Character Fortune ({characterData.CharacterName})";

            await ReadFortune();
        }
    }

    private async Task ReadFortune()
    {
        if (CharacterData == null) return;

        try
        {
            FortuneData = null;
            FortuneData = await _databaseService.ReadCharacterFortuneAsync(CharacterData.CharacterID);
        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"Error: {ex.Message}");
        }
    }

    private void UpdateFortuneTextBox(int fortuneID1, int fortuneID2, int fortuneID3)
    {
        if (fortuneID1 == 0)
        {
            FortuneDescription = "No Fortune";
            FortuneTitle = "No Fortune";
        }
        else
        {
            (string fortuneName, string addEffectDesc00, string addEffectDesc01, string addEffectDesc02, string fortuneDesc1) = _gmDatabaseService.GetFortuneValues(fortuneID1);
            string fortuneDesc2 = _gmDatabaseService.GetFortuneDesc(fortuneID2);
            string fortuneDesc3 = _gmDatabaseService.GetFortuneDesc(fortuneID3);
            FortuneDescription = $"{fortuneDesc1}\r\n{fortuneDesc2}\r\n{fortuneDesc3}\r\n\r\nFortune Effect:\r\n{addEffectDesc00}\r\n{addEffectDesc01}\r\n{addEffectDesc02}";
            FortuneTitle = fortuneName;
        }
    }

    #endregion

    #region Save Fortune

    [RelayCommand(CanExecute = nameof(CanExecuteCommand))]
    private async Task SaveFortune()
    {
        if (CharacterData == null) return;

        try
        {
            if (await _databaseService.IsCharacterOnlineAsync(CharacterData.CharacterName!))
            {
                return;
            }

            bool fortuneChanged = HandleFortuneUpdate(SelectedFortuneID1, SelectedFortuneID2, SelectedFortuneID3);
            if (fortuneChanged)
            {
                RHMessageBoxHelper.ShowOKMessage("Fortune updated successfully!", "Success");
                await ReadFortune();
            }
        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"Error updating fortune: {ex.Message}");
        }
    }

    private bool HandleFortuneUpdate(int selectedFortuneID1, int selectedFortuneID2, int selectedFortuneID3)
    {
        const int NoFortune = 0;
        const int ActiveFortune = 1;

        if (selectedFortuneID1 != 0 && selectedFortuneID1 != FortuneID1 || selectedFortuneID2 != FortuneID2 || selectedFortuneID3 != FortuneID3)
        {
            if (RHMessageBoxHelper.ConfirmMessage($"Update {CharacterData!.CharacterName} fortune?"))
            {
                _databaseService.UpdateFortuneAsync(CharacterData, ActiveFortune, selectedFortuneID1, selectedFortuneID2, selectedFortuneID3);
                _databaseService.GMAuditAsync(CharacterData, "Character Fortune Change", $"{selectedFortuneID1}, {selectedFortuneID2}, {selectedFortuneID3}");
                return true;
            }
        }
        else if (selectedFortuneID1 != FortuneID1)
        {
            if (RHMessageBoxHelper.ConfirmMessage($"Remove {CharacterData!.CharacterName} fortune?"))
            {
                _databaseService.RemoveFortuneAsync(CharacterData, NoFortune, FortuneID1, FortuneID2, FortuneID3);
                _databaseService.GMAuditAsync(CharacterData, "Character Fortune Remove", $"{FortuneID1}, {FortuneID2}, {FortuneID3}");
                return true;
            }
        }

        return false;
    }

    private bool CanExecuteCommand()
    {
        return CharacterData != null;
    }

    #endregion

    #region Comboboxes

    [ObservableProperty]
    private List<NameID>? _fortuneItems;

    private void PopulateFortuneItems()
    {
        try
        {
            FortuneItems = _gmDatabaseService.GetFortuneItems();

            if (FortuneItems.Count > 0)
            {
                SelectedFortuneID1 = 0;
            }
        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
        }
    }

    [ObservableProperty]
    private List<NameID>? _fortuneDescItems;

    private void PopulateFortuneDescItems()
    {
        try
        {
            FortuneDescItems = _gmDatabaseService.GetFortuneDescItems();

            if (FortuneDescItems.Count > 0)
            {
                SelectedFortuneID2 = 0;
                SelectedFortuneID3 = 0;
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
    private string _title = "Character Fortune";

    [ObservableProperty]
    private string _fortuneTitle = "No Fortune";

    [ObservableProperty]
    private string _fortuneDescription = "No Fortune";

    [ObservableProperty]
    private CharacterData? _characterData;
    partial void OnCharacterDataChanged(CharacterData? value)
    {
        SaveFortuneCommand.NotifyCanExecuteChanged();
    }

    [ObservableProperty]
    private DataRow? _fortuneData;
    partial void OnFortuneDataChanged(DataRow? value)
    {
        FortuneID1 = value != null ? (int)value["type_1"] : 0;
        FortuneID2 = value != null ? (int)value["type_2"] : 0;
        FortuneID3 = value != null ? (int)value["type_3"] : 0;
        SelectedFortuneID1 = value != null ? (int)value["type_1"] : 0;
        SelectedFortuneID2 = value != null ? (int)value["type_2"] : 0;
        SelectedFortuneID3 = value != null ? (int)value["type_3"] : 0;
    }

    [ObservableProperty]
    private int _fortuneID1;
    partial void OnFortuneID1Changed(int value)
    {
        UpdateFortuneTextBox(value, FortuneID2, FortuneID3);
    }

    [ObservableProperty]
    private int _fortuneID2;
    partial void OnFortuneID2Changed(int value)
    {
        UpdateFortuneTextBox(FortuneID1, value, FortuneID3);
    }

    [ObservableProperty]
    private int _FortuneID3;
    partial void OnFortuneID3Changed(int value)
    {
        UpdateFortuneTextBox(FortuneID1, FortuneID2, value);
    }

    [ObservableProperty]
    private int _selectedFortuneID1;
    partial void OnSelectedFortuneID1Changed(int value)
    {
        UpdateFortuneTextBox(value, SelectedFortuneID2, SelectedFortuneID3);
    }

    [ObservableProperty]
    private int _selectedFortuneID2;
    partial void OnSelectedFortuneID2Changed(int value)
    {
        UpdateFortuneTextBox(SelectedFortuneID1, value, SelectedFortuneID3);
    }

    [ObservableProperty]
    private int _SelectedFortuneID3;
    partial void OnSelectedFortuneID3Changed(int value)
    {
        UpdateFortuneTextBox(SelectedFortuneID1, SelectedFortuneID2, value);
    }

    #endregion

}
