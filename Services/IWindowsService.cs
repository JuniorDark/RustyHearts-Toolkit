using RHToolkit.Models;

namespace RHToolkit.Services
{
    public interface IWindowsService
    {
        void OpenCharacterWindow(CharacterData characterData);
        void OpenEquipmentWindow(CharacterData characterData);
        void OpenInventoryWindow(CharacterData characterData);
        void OpenStorageWindow(CharacterData characterData);
        void OpenFortuneWindow(CharacterData characterData);
        void OpenItemWindow(Guid token, string messageType, ItemData itemData, CharacterData? characterData = null);
        void OpenSanctionWindow(CharacterData characterData);
        void OpenTitleWindow(CharacterData characterData);
        void OpenNpcShopWindow(Guid token, NameID shopID, NameID? shopTitle);
        void OpenItemMixWindow(Guid token, string group, string? messageType);
    }
}