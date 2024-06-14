using RHToolkit.Models;

namespace RHToolkit.Services
{
    public interface IWindowsService
    {
        void OpenCharacterWindow(CharacterInfo characterInfo);
        void OpenEquipmentWindow(CharacterInfo characterInfo);
        void OpenFortuneWindow(CharacterInfo characterInfo);
        void OpenItemWindow(Guid token, string messageType, ItemData itemData, CharacterInfo? characterInfo = null);
        void OpenSanctionWindow(CharacterInfo characterInfo);
        void OpenTitleWindow(CharacterInfo characterInfo);
    }
}