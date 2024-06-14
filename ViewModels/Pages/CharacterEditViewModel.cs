using RHToolkit.Models;
using RHToolkit.Models.Database;
using RHToolkit.Models.MessageBox;
using RHToolkit.Services;
using System.Windows.Controls;

namespace RHToolkit.ViewModels.Pages
{
    public partial class CharacterEditViewModel(IDatabaseService databaseService, ISqLiteDatabaseService sqLiteDatabaseService, IWindowsService windowsService) : ObservableObject
    {
        private readonly IWindowsService _windowsService = windowsService;
        private readonly IDatabaseService _databaseService = databaseService;
        private readonly ISqLiteDatabaseService _sqLiteDatabaseService = sqLiteDatabaseService;

        #region Read Character Data

        [RelayCommand]
        private async Task ReadCharacterData()
        {
            if (string.IsNullOrWhiteSpace(SearchText)) return;

            try
            {
                if (!await ValidateCharacterData(false)) return;

                string filter = IsConnectFilter();
                CharacterDataList = null;
                CharacterDataList = await _databaseService.GetCharacterDataListAsync(SearchText, filter);

            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"Error reading Character Data: {ex.Message}", "Error");
            }
        }

        private string IsConnectFilter()
        {
            if (OnlineCheckBoxChecked && !OfflineCheckBoxChecked)
            {
                return "Y";
            }
            else if (!OnlineCheckBoxChecked && OfflineCheckBoxChecked)
            {
                return "N";
            }
            else
            {
                return string.Empty;
            }
        }
        #endregion

        #region Character Window

        [RelayCommand(CanExecute = nameof(CanExecuteCommand))]
        private async Task EditCharacter()
        {
            if (CharacterData == null) return;

            try
            {
                if (!await ValidateCharacterData(true, true)) return;

                OpenWindow(_windowsService.OpenCharacterWindow, "Character");
                OnCanExecuteWindowCommandChanged();
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"Error: {ex.Message}", "Error");
            }
        }

        [RelayCommand(CanExecute = nameof(CanExecuteWindowCommand))]
        private async Task DeleteCharacter()
        {
            if (CharacterData == null) return;

            try
            {
                if (!await ValidateCharacterData()) return;

                if (RHMessageBoxHelper.ConfirmMessage($"Delete the character '{CharacterData.CharacterName}'?"))
                {
                    await _databaseService.DeleteCharacterAsync(CharacterData.AuthID, CharacterData.CharacterID);
                    await _databaseService.GMAuditAsync(CharacterData.AccountName!, CharacterData.CharacterID, CharacterData.CharacterName!, "Delete Character", $"<font color=blue>Delete Character</font>]<br><font color=red>Character: {CharacterData.CharacterID}<br>{CharacterData.CharacterName}, GUID:{{{CharacterData.CharacterID}}}<br></font>");

                    RHMessageBoxHelper.ShowOKMessage($"Character '{CharacterData.CharacterName}' deleted.", "Success");

                    await ReadCharacterData();
                }
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"Error: {ex.Message}", "Error");
            }
        }

        private async Task<bool> ValidateCharacterData(bool checkCharacterOnline = true, bool checkItemDatabase = false)
        {
            if (!SqlCredentialValidator.ValidateCredentials())
            {
                return false;
            }

            if (checkCharacterOnline)
            {
                if (await _databaseService.IsCharacterOnlineAsync(CharacterData!.CharacterName!))
                {
                    return false;
                }
            }

            if (checkItemDatabase)
            {
                if (!_sqLiteDatabaseService.ValidateDatabase())
                {
                    return false;
                }
            }

            return true;
        }

        private bool CanExecuteCommand()
        {
            return CharacterData != null;
        }

        private bool CanExecuteWindowCommand()
        {
            return CharacterData != null && WindowsService.OpenWindowsCount == 0;
        }

        private void OnCanExecuteCommandChanged()
        {
            EditCharacterCommand.NotifyCanExecuteChanged();
        }

        private void OnCanExecuteWindowCommandChanged()
        {
            DeleteCharacterCommand.NotifyCanExecuteChanged();
            OpenEquipmentWindowCommand.NotifyCanExecuteChanged();
            OpenTitleWindowCommand.NotifyCanExecuteChanged();
            OpenSanctionWindowCommand.NotifyCanExecuteChanged();
            OpenFortuneWindowCommand.NotifyCanExecuteChanged();
        }

        #endregion

        #region Windows Buttons
        private void OpenWindow(Action<CharacterInfo> openWindowAction, string errorMessage)
        {
            if (CharacterData == null) return;

            try
            {
                var characterInfo = new CharacterInfo(CharacterData.CharacterID, CharacterData.AuthID, CharacterData.CharacterName!, CharacterData.AccountName!, CharacterData.Class, CharacterData.Job);
                openWindowAction(characterInfo);
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
            OpenWindow(_windowsService.OpenEquipmentWindow, "Equipment");
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

        #region Properties

        [ObservableProperty]
        private List<CharacterData>? _characterDataList;

        [ObservableProperty]
        private CharacterData? _characterData;
        partial void OnCharacterDataChanged(CharacterData? value)
        {
            IsButtonPanelVisible = value == null ? Visibility.Hidden : Visibility.Visible;
            SearchMessage = value == null ? "No data found." : "";
            OnCanExecuteCommandChanged();
            OnCanExecuteWindowCommandChanged();
        }

        [ObservableProperty]
        private string? _searchText;

        [ObservableProperty]
        private string? _searchMessage = "Search for a character.";

        [ObservableProperty]
        private Visibility _isButtonPanelVisible = Visibility.Hidden;

        [ObservableProperty]
        private bool? _selectAllCheckBoxChecked = null;

        [ObservableProperty]
        private bool _onlineCheckBoxChecked = false;

        [ObservableProperty]
        private bool _offlineCheckBoxChecked = true;

        [RelayCommand]
        private void OnSelectAllChecked(object sender)
        {
            if (sender is not CheckBox checkBox)
            {
                return;
            }

            checkBox.IsChecked ??=
                !OnlineCheckBoxChecked || !OfflineCheckBoxChecked;

            if (checkBox.IsChecked == true)
            {
                OnlineCheckBoxChecked = true;
                OfflineCheckBoxChecked = true;
            }
            else if (checkBox.IsChecked == false)
            {
                OnlineCheckBoxChecked = false;
                OfflineCheckBoxChecked = false;
            }
        }

        [RelayCommand]
        private void OnSingleChecked(string option)
        {
            bool allChecked = OnlineCheckBoxChecked && OfflineCheckBoxChecked;
            bool allUnchecked =
                !OnlineCheckBoxChecked && !OfflineCheckBoxChecked;

            SelectAllCheckBoxChecked = allChecked
                ? true
                : allUnchecked
                    ? false
                    : null;
        }

        #endregion
    }
}
