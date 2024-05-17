using RHToolkit.Messages;
using RHToolkit.Models;
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

        #region Character Data
        [ObservableProperty]
        private List<CharacterData>? _characterDataList;

        [RelayCommand]
        private async Task ReadCharacterData()
        {
            if (string.IsNullOrWhiteSpace(SearchText)) return;

            try
            {
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

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsEditCharacterButtonEnabled))]
        [NotifyPropertyChangedFor(nameof(IsDeleteCharacterButtonEnabled))]
        private CharacterData? _characterData;
        partial void OnCharacterDataChanged(CharacterData? value)
        {
            IsEditCharacterButtonEnabled = value == null ? false : true;

            if (_characterWindowInstance == null)
            {
                IsDeleteCharacterButtonEnabled = value == null ? false : true;
            }
        }

        [RelayCommand]
        private async Task EditCharacter()
        {
            if (CharacterData == null) return;

            try
            {
                if (await _databaseService.IsCharacterOnlineAsync(CharacterData.CharacterName!))
                {
                    RHMessageBox.ShowOKMessage($"The character '{CharacterData.CharacterName}' is online. You can't edit an online character.", "Info");
                    return;
                }

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

                WeakReferenceMessenger.Default.Send(new CharacterDataMessage(CharacterData));
                WeakReferenceMessenger.Default.Send(new DatabaseItemMessage(inventoryItem, ItemStorageType.Inventory));
                WeakReferenceMessenger.Default.Send(new DatabaseItemMessage(equipItem, ItemStorageType.Equipment));
                WeakReferenceMessenger.Default.Send(new DatabaseItemMessage(accountStorage, ItemStorageType.Storage));

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
                if (await _databaseService.IsCharacterOnlineAsync(CharacterData.CharacterName!))
                {
                    RHMessageBox.ShowOKMessage($"The character '{CharacterData.CharacterName}' is online. You can't delete an online character.", "Info");
                    return;
                }

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
        #endregion

        #region Properties

        [ObservableProperty]
        private string? _searchText;

        [ObservableProperty]
        private bool _isEditCharacterButtonEnabled = false;

        [ObservableProperty]
        private bool _isDeleteCharacterButtonEnabled = false;

        [ObservableProperty]
        private bool _isFirstTimeInitialized = true;

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
