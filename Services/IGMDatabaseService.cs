using RHToolkit.Models;

namespace RHToolkit.Services
{
    public interface IGMDatabaseService
    {
        string GetAddEffectName(int effectID);
        List<NameID> GetCategoryItems(EnumService.ItemType itemType, bool isSubCategory);
        List<NameID> GetSubCategoryItems();
        string GetCategoryName(int categoryID);
        double GetEnhanceValue(int enhanceValue);
        long GetExperienceFromLevel(int level);
        string GetFortuneDesc(int fortuneID);
        List<NameID> GetFortuneDescItems();
        List<NameID> GetFortuneItems();
        (string fortuneName, string AddEffectDesc00, string AddEffectDesc01, string AddEffectDesc02, string fortuneDesc) GetFortuneValues(int fortuneID);
        List<ItemData> GetItemDataList(EnumService.ItemType itemType, string itemTableName);
        List<NameID> GetLobbyItems();
        string GetOptionGroupName(int optionID);
        List<NameID> GetOptionItems();
        List<NameID> GetQuestListItems();
        List<NameID> GetAddEffectItems();
        string GetOptionName(int optionID);
        (int secTime, float value) GetOptionValues(int optionID);
        (int nSetOption00, int nSetOptionvlue00, int nSetOption01, int nSetOptionvlue01, int nSetOption02, int nSetOptionvlue02, int nSetOption03, int nSetOptionvlue03, int nSetOption04, int nSetOptionvlue04) GetSetInfo(int setID);
        string GetSetName(int setId);
        string GetString(int stringID);
        string GetSubCategoryName(int categoryID);
        int GetTitleCategory(int titleID);
        (int titleCategory, int remainTime, int nAddEffectID00, int nAddEffectID01, int nAddEffectID02, int nAddEffectID03, int nAddEffectID04, int nAddEffectID05, string titleDesc) GetTitleInfo(int titleID);
        List<NameID> GetTitleItems();
        string GetTitleName(int titleID);
        int GetTitleRemainTime(int titleID);
        (int nPhysicalAttackMin, int nPhysicalAttackMax, int nMagicAttackMin, int nMagicAttackMax) GetWeaponStats(int jbClass, int weaponID);
        bool IsNameInNickFilter(string characterName);
    }
}