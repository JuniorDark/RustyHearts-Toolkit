using RHToolkit.Models;
using static RHToolkit.Models.EnumService;

namespace RHToolkit.Services
{
    public interface IGMDatabaseService
    {
        List<NameID> GetOptionItems();
        (int minValue, int maxValue) GetOptionValue(int itemID);
        List<NameID> GetTitleItems();
        List<NameID> GetFortuneDescItems();
        List<NameID> GetLobbyItems();
        List<NameID> GetCategoryItems(ItemType itemType, bool isSubCategory);
        string GetCategoryName(int categoryID);
        string GetSubCategoryName(int categoryID);
        string GetSetName(int setId);
        string GetOptionName(int optionID);
        string GetFortuneDesc(int fortuneID);
        string GetTitleName(int titleID);
        string GetAddEffectName(int effectID);
        int GetTitleCategory(int titleID);
        int GetTitleRemainTime(int titleID);
        bool IsNameInNickFilter(string characterName);
        long GetExperienceFromLevel(int level);
        (int secTime, float value, int maxValue) GetOptionValues(int optionID);
        (int nPhysicalAttackMin, int nPhysicalAttackMax, int nMagicAttackMin, int nMagicAttackMax) GetWeaponStats(int jbClass, int weaponID);
        (string fortuneName, string AddEffectDesc00, string AddEffectDesc01, string AddEffectDesc02, string fortuneDesc) GetFortuneValues(int fortuneID);
        (int titleCategory, int remainTime, int nAddEffectID00, int nAddEffectID01, int nAddEffectID02, int nAddEffectID03, int nAddEffectID04, int nAddEffectID05, string titleDesc) GetTitleInfo(int titleID);
    }
}
