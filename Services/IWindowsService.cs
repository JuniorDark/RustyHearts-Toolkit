using RHToolkit.Models;

namespace RHToolkit.Services
{
    public interface IWindowsService
    {
        void OpenCharacterWindow(CharacterData characterData);
        void OpenDropGroupListWindow(Guid token, int id, EnumService.ItemDropGroupType dropGroupType);
        void OpenEquipmentWindow(CharacterData characterData);
        void OpenFortuneWindow(CharacterData characterData);
        void OpenInventoryWindow(CharacterData characterData);
        void OpenItemMixWindow(Guid token, string group, string? messageType);
        void OpenItemWindow(Guid token, string messageType, ItemData itemData, CharacterData? characterData = null);
        void OpenNpcShopWindow(Guid token, NameID shopID, NameID? shopTitle);
        void OpenRareCardRewardWindow(Guid token, int id, string? messageType);
        void OpenSanctionWindow(CharacterData characterData);
        void OpenStorageWindow(CharacterData characterData);
        void OpenTitleWindow(CharacterData characterData);
    }
}