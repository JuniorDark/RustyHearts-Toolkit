using RHToolkit.Messages;
using RHToolkit.Models;
using RHToolkit.Models.Database;
using RHToolkit.Models.MessageBox;
using RHToolkit.Services;
using RHToolkit.Views.Windows;
using System.Windows.Controls;

namespace RHToolkit.ViewModels.Pages
{
    public partial class CharacterEditViewModel(WindowsProviderService windowsProviderService, IDatabaseService databaseService, ISqLiteDatabaseService sqLiteDatabaseService) : ObservableObject
    {
        private readonly WindowsProviderService _windowsProviderService = windowsProviderService;
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
        private CharacterWindow? _characterWindowInstance;

        [RelayCommand(CanExecute = nameof(CanExecuteCommand))]
        private async Task EditCharacter()
        {
            if (CharacterData == null) return;

            try
            {
                if (!await ValidateCharacterData(true, true)) return;

                if (_characterWindowInstance == null)
                {
                    _windowsProviderService.Show<CharacterWindow>();
                    _characterWindowInstance = Application.Current.Windows.OfType<CharacterWindow>().FirstOrDefault();

                    if (_characterWindowInstance != null)
                    {
                        _characterWindowInstance.Closed += (sender, args) =>
                        {
                            _characterWindowInstance = null;
                            DeleteCharacterCommand.NotifyCanExecuteChanged();
                        };
                    }
                }

                var characterInfo = new CharacterInfo(CharacterData.CharacterID, CharacterData.AuthID, CharacterData.CharacterName!, CharacterData.AccountName!, CharacterData.Class, CharacterData.Job);
                WeakReferenceMessenger.Default.Send(new CharacterInfoMessage(characterInfo, "CharacterWindow"));

                _characterWindowInstance?.Focus();

                DeleteCharacterCommand.NotifyCanExecuteChanged();
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"Error: {ex.Message}", "Error");
            }
        }


        [RelayCommand(CanExecute = nameof(CanExecuteDeleteCommand))]
        private async Task DeleteCharacter()
        {
            if (CharacterData == null) return;

            try
            {
                if (!await ValidateCharacterData()) return;

                if (RHMessageBoxHelper.ConfirmMessage($"Delete the character '{CharacterData.CharacterName}'?"))
                {
                    _characterWindowInstance?.Close();

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

        private bool CanExecuteDeleteCommand()
        {
            return CharacterData != null && _characterWindowInstance == null;
        }

        private void OnCanExecuteCommandChanged()
        {
            EditCharacterCommand.NotifyCanExecuteChanged();
            DeleteCharacterCommand.NotifyCanExecuteChanged();
            OpenTitleWindowCommand.NotifyCanExecuteChanged();
            OpenSanctionWindowCommand.NotifyCanExecuteChanged();
            OpenFortuneWindowCommand.NotifyCanExecuteChanged();

        }
        #endregion

        #region Buttons

        #region Title
        private TitleWindow? _titleWindowInstance;

        [RelayCommand(CanExecute = nameof(CanExecuteCommand))]
        private void OpenTitleWindow()
        {
            if (CharacterData == null) return;

            if (!SqlCredentialValidator.ValidateCredentials())
            {
                return;
            }

            try
            {
                if (_titleWindowInstance == null)
                {
                    _windowsProviderService.Show<TitleWindow>();
                    _titleWindowInstance = Application.Current.Windows.OfType<TitleWindow>().FirstOrDefault();

                    if (_titleWindowInstance != null)
                    {
                        _titleWindowInstance.Closed += (sender, args) => _titleWindowInstance = null;
                    }
                }

                var characterInfo = new CharacterInfo(CharacterData.CharacterID, CharacterData.AuthID, CharacterData.CharacterName!, CharacterData.AccountName!, CharacterData.Class, CharacterData.Job);
                WeakReferenceMessenger.Default.Send(new CharacterInfoMessage(characterInfo, "TitleWindow"));

                _titleWindowInstance?.Focus();
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"Error reading Character Title: {ex.Message}", "Error");
            }
        }
        #endregion

        #region Sanction
        private SanctionWindow? _sanctionWindowInstance;

        [RelayCommand(CanExecute = nameof(CanExecuteCommand))]
        private void OpenSanctionWindow()
        {
            if (CharacterData == null) return;

            if (!SqlCredentialValidator.ValidateCredentials())
            {
                return;
            }

            try
            {
                if (_sanctionWindowInstance == null)
                {
                    _windowsProviderService.Show<SanctionWindow>();
                    _sanctionWindowInstance = Application.Current.Windows.OfType<SanctionWindow>().FirstOrDefault();

                    if (_sanctionWindowInstance != null)
                    {
                        _sanctionWindowInstance.Closed += (sender, args) => _sanctionWindowInstance = null;
                    }
                }

                var characterInfo = new CharacterInfo(CharacterData.CharacterID, CharacterData.AuthID, CharacterData.CharacterName!, CharacterData.AccountName!, CharacterData.Class, CharacterData.Job);
                WeakReferenceMessenger.Default.Send(new CharacterInfoMessage(characterInfo, "SanctionWindow"));
                _sanctionWindowInstance?.Focus();
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"Error reading Character Sanction: {ex.Message}", "Error");
            }
        }
        #endregion

        #region Fortune
        private FortuneWindow? _fortuneWindowInstance;

        [RelayCommand(CanExecute = nameof(CanExecuteCommand))]
        private void OpenFortuneWindow()
        {
            if (CharacterData == null) return;

            if (!SqlCredentialValidator.ValidateCredentials())
            {
                return;
            }

            try
            {
                if (_fortuneWindowInstance == null)
                {
                    _windowsProviderService.Show<FortuneWindow>();
                    _fortuneWindowInstance = Application.Current.Windows.OfType<FortuneWindow>().FirstOrDefault();

                    if (_fortuneWindowInstance != null)
                    {
                        _fortuneWindowInstance.Closed += (sender, args) => _fortuneWindowInstance = null;
                    }
                }

                var characterInfo = new CharacterInfo(CharacterData.CharacterID, CharacterData.AuthID, CharacterData.CharacterName!, CharacterData.AccountName!, CharacterData.Class, CharacterData.Job);
                WeakReferenceMessenger.Default.Send(new CharacterInfoMessage(characterInfo, "FortuneWindow"));
                _fortuneWindowInstance?.Focus();
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"Error reading Character Fortune: {ex.Message}", "Error");
            }
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
