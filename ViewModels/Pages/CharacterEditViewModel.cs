using RHToolkit.Messages;
using RHToolkit.Models;
using RHToolkit.Models.MessageBox;
using RHToolkit.Services;
using RHToolkit.Views.Windows;

namespace RHToolkit.ViewModels.Pages
{
    public partial class CharacterEditViewModel(WindowsProviderService windowsProviderService, IDatabaseService databaseService) : ObservableObject
    {
        private readonly WindowsProviderService _windowsProviderService = windowsProviderService;
        private readonly IDatabaseService _databaseService = databaseService;

        
        private CharacterWindow? _characterWindowInstance;

        [RelayCommand]
        private async Task OnOpenCharacterWindow()
        {
            if (string.IsNullOrEmpty(CharacterName))
            {
                RHMessageBox.ShowOKMessage("Enter a character name.", "Empty Name");
                return;
            }

            CharacterData? characterData = await _databaseService.GetCharacterDataAsync(CharacterName);

            if (characterData == null)
            {
                RHMessageBox.ShowOKMessage($"The character '{CharacterName}' does not exist.", "Invalid Name");
                return;

            }
            else
            {
                List<DatabaseItem> inventoryItem = await _databaseService.GetItemList(characterData.CharacterID, "N_InventoryItem");
                List<DatabaseItem> equipItem = await _databaseService.GetItemList(characterData.CharacterID, "N_EquipItem");
                List<DatabaseItem> accountStorage = await _databaseService.GetAccountItemList(characterData.AuthID);

                if (_characterWindowInstance == null)
                {
                    _windowsProviderService.Show<CharacterWindow>();
                    _characterWindowInstance = Application.Current.Windows.OfType<CharacterWindow>().FirstOrDefault();
                    _characterWindowInstance.Closed += (sender, args) => _characterWindowInstance = null;
                }

                WeakReferenceMessenger.Default.Send(new CharacterDataMessage(characterData));
                WeakReferenceMessenger.Default.Send(new DatabaseItemMessage(inventoryItem, ItemStorageType.Inventory));
                WeakReferenceMessenger.Default.Send(new DatabaseItemMessage(equipItem, ItemStorageType.Equipment));
                WeakReferenceMessenger.Default.Send(new DatabaseItemMessage(accountStorage, ItemStorageType.Storage));

            }


        }

        [ObservableProperty]
        private string? _characterName;

    }
}
