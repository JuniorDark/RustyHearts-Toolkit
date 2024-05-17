using RHToolkit.Models;
using RHToolkit.Models.MessageBox;
using RHToolkit.Services;

namespace RHToolkit.ViewModels.Pages
{
    public partial class CharacterRestoreViewModel(IDatabaseService databaseService) : ObservableObject
    {
        private readonly IDatabaseService _databaseService = databaseService;

        #region Character Data
        [ObservableProperty]
        private List<CharacterData>? _characterDataList;

        [RelayCommand]
        private async Task ReadDeleteCharacterData()
        {
            if (string.IsNullOrWhiteSpace(SearchText)) return;

            try
            {
                CharacterDataList = await _databaseService.GetCharacterDataListAsync(SearchText, string.Empty, true);
            }
            catch (Exception ex)
            {
                RHMessageBox.ShowOKMessage($"Error reading Character Data: {ex.Message}", "Error");
            }
        }

        #endregion

        #region Character Restore

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsRestoreCharacterButtonEnabled))]
        private CharacterData? _characterData;
        partial void OnCharacterDataChanged(CharacterData? value)
        {
            IsRestoreCharacterButtonEnabled = value == null ? false : true;
        }

        [RelayCommand]
        private async Task RestoreCharacter()
        {
            try
            {
                if (CharacterData == null) return;

                if (RHMessageBox.ConfirmMessage($"Restore the character '{CharacterData.CharacterName}'?"))
                {
                    await _databaseService.RestoreCharacterAsync(CharacterData.CharacterID);
                    await _databaseService.GMAuditAsync(CharacterData.AccountName!, CharacterData.CharacterID, CharacterData.CharacterName!, "Restore Character", $"<font color=blue>Restore Character</font>]<br><font color=red>Character: {CharacterData.CharacterID}<br>{CharacterData.CharacterName}, GUID:{{{CharacterData.CharacterID}}}<br></font>");

                    RHMessageBox.ShowOKMessage($"Character '{CharacterData.CharacterName}' restored.", "Success");

                    await ReadDeleteCharacterData();
                }
            }
            catch (Exception ex)
            {
                RHMessageBox.ShowOKMessage($"Error: {ex.Message}", "Error");
            }
        }
        #endregion

        #region Properties
        [ObservableProperty]
        private string? _searchText;

        [ObservableProperty]
        private bool _isRestoreCharacterButtonEnabled = false;

        [ObservableProperty]
        private bool _isFirstTimeInitialized = true;

        #endregion
    }
}
