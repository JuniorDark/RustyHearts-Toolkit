using RHToolkit.Messages;
using RHToolkit.Models;
using RHToolkit.Models.Database;
using RHToolkit.Models.MessageBox;
using RHToolkit.Services;
using RHToolkit.Views.Windows;
using System.Windows.Controls;

namespace RHToolkit.ViewModels.Pages
{
    public partial class CharacterEditViewModel(WindowsProviderService windowsProviderService, IDatabaseService databaseService) : ObservableObject
    {
        private readonly WindowsProviderService _windowsProviderService = windowsProviderService;
        private readonly IDatabaseService _databaseService = databaseService;
        private readonly CharacterOnlineValidator _characterOnlineValidator = new(databaseService);

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
                RHMessageBox.ShowOKMessage($"Error reading Character Data: {ex.Message}", "Error");
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

        [RelayCommand]
        private async Task EditCharacter()
        {
            if (CharacterData == null) return;

            try
            {
                if (!await ValidateCharacterData(true, true)) return;

                List<ItemData> inventoryItem = await _databaseService.GetItemList(CharacterData.CharacterID, "N_InventoryItem");
                List<ItemData> equipItem = await _databaseService.GetItemList(CharacterData.CharacterID, "N_EquipItem");
                List<ItemData> accountStorage = await _databaseService.GetAccountItemList(CharacterData.AuthID);

                if (_characterWindowInstance == null)
                {
                    _windowsProviderService.Show<CharacterWindow>();
                    _characterWindowInstance = Application.Current.Windows.OfType<CharacterWindow>().FirstOrDefault();

                    if (_characterWindowInstance != null)
                    {
                        IsDeleteCharacterButtonEnabled = false;
                        _characterWindowInstance.Closed += (sender, args) =>
                        {
                            _characterWindowInstance = null;
                            IsDeleteCharacterButtonEnabled = true;
                        };
                    }
                }

                WeakReferenceMessenger.Default.Send(new CharacterDataMessage(CharacterData, "CharacterWindow"));
                WeakReferenceMessenger.Default.Send(new DatabaseItemMessage(inventoryItem, ItemStorageType.Inventory));
                WeakReferenceMessenger.Default.Send(new DatabaseItemMessage(equipItem, ItemStorageType.Equipment));
                WeakReferenceMessenger.Default.Send(new DatabaseItemMessage(accountStorage, ItemStorageType.Storage));

                _characterWindowInstance?.Focus();

            }
            catch (Exception ex)
            {
                RHMessageBox.ShowOKMessage($"Error: {ex.Message}", "Error");
            }
        }

        [RelayCommand]
        private async Task DeleteCharacter()
        {
            if (CharacterData == null) return;

            try
            {
                if (!await ValidateCharacterData()) return;

                if (RHMessageBox.ConfirmMessage($"Delete the character '{CharacterData.CharacterName}'?"))
                {
                    _characterWindowInstance?.Close();

                    await _databaseService.DeleteCharacterAsync(CharacterData.AuthID, CharacterData.CharacterID);
                    await _databaseService.GMAuditAsync(CharacterData.AccountName!, CharacterData.CharacterID, CharacterData.CharacterName!, "Delete Character", $"<font color=blue>Delete Character</font>]<br><font color=red>Character: {CharacterData.CharacterID}<br>{CharacterData.CharacterName}, GUID:{{{CharacterData.CharacterID}}}<br></font>");

                    RHMessageBox.ShowOKMessage($"Character '{CharacterData.CharacterName}' deleted.", "Success");

                    await ReadCharacterData();
                }
            }
            catch (Exception ex)
            {
                RHMessageBox.ShowOKMessage($"Error: {ex.Message}", "Error");
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
                if (await _characterOnlineValidator.IsCharacterOnlineAsync(CharacterData!.CharacterName!))
                {
                    return false;
                }
            }

            if (checkItemDatabase)
            {
                if (!ItemDataManager.GetDatabaseFilePath())
                {
                    return false;
                }
            }

            return true;
        }
        #endregion

        #region Buttons

        #region Title
        private TitleWindow? _titleWindowInstance;

        [RelayCommand]
        private async Task OnOpenTitleWindow()
        {
            try
            {
                if (!await ValidateCharacterData(false)) return;

                if (_titleWindowInstance == null)
                {
                    _windowsProviderService.Show<TitleWindow>();
                    _titleWindowInstance = Application.Current.Windows.OfType<TitleWindow>().FirstOrDefault();

                    if (_titleWindowInstance != null)
                    {
                        _titleWindowInstance.Closed += (sender, args) => _titleWindowInstance = null;
                    }
                }

                WeakReferenceMessenger.Default.Send(new CharacterDataMessage(CharacterData!, "TitleWindow"));

                _titleWindowInstance?.Focus();
            }
            catch (Exception ex)
            {
                RHMessageBox.ShowOKMessage($"Error reading Character Title: {ex.Message}", "Error");
            }
        }
        #endregion

        #region Sanction
        private SanctionWindow? _sanctionWindowInstance;

        [RelayCommand]
        private async Task OnOpenSanctionWindow()
        {
            try
            {
                if (!await ValidateCharacterData(false)) return;

                if (_sanctionWindowInstance == null)
                {
                    _windowsProviderService.Show<SanctionWindow>();
                    _sanctionWindowInstance = Application.Current.Windows.OfType<SanctionWindow>().FirstOrDefault();

                    if (_sanctionWindowInstance != null)
                    {
                        _sanctionWindowInstance.Closed += (sender, args) => _sanctionWindowInstance = null;
                    }
                }

                WeakReferenceMessenger.Default.Send(new CharacterDataMessage(CharacterData!, "SanctionWindow"));
                _sanctionWindowInstance?.Focus();
            }
            catch (Exception ex)
            {
                RHMessageBox.ShowOKMessage($"Error reading Character Sanction: {ex.Message}", "Error");
            }
        }
        #endregion

        #region Fortune
        private FortuneWindow? _fortuneWindowInstance;

        [RelayCommand]
        private async Task OnOpenFortuneWindow()
        {
            try
            {
                if (!await ValidateCharacterData(false)) return;

                if (_fortuneWindowInstance == null)
                {
                    _windowsProviderService.Show<FortuneWindow>();
                    _fortuneWindowInstance = Application.Current.Windows.OfType<FortuneWindow>().FirstOrDefault();

                    if (_fortuneWindowInstance != null)
                    {
                        _fortuneWindowInstance.Closed += (sender, args) => _fortuneWindowInstance = null;
                    }
                }

                WeakReferenceMessenger.Default.Send(new CharacterDataMessage(CharacterData!, "FortuneWindow"));
                _fortuneWindowInstance?.Focus();
            }
            catch (Exception ex)
            {
                RHMessageBox.ShowOKMessage($"Error reading Character Fortune: {ex.Message}", "Error");
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
            IsEditCharacterButtonEnabled = value == null ? false : true;
            IsButtonPanelVisible = value == null ? Visibility.Hidden : Visibility.Visible;
            IsButtonEnabled = value == null ? false : true;
            SearchMessage = value == null ? "No data found." : "";

            if (_characterWindowInstance == null)
            {
                IsDeleteCharacterButtonEnabled = value == null ? false : true;
            }
        }

        [ObservableProperty]
        private string? _searchText;

        [ObservableProperty]
        private string? _searchMessage = "Search for a character.";

        [ObservableProperty]
        private bool _isButtonEnabled = false;

        [ObservableProperty]
        private Visibility _isButtonPanelVisible = Visibility.Hidden;

        [ObservableProperty]
        private bool _isEditCharacterButtonEnabled = false;

        [ObservableProperty]
        private bool _isDeleteCharacterButtonEnabled = false;

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
