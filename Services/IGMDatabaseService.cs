using RHToolkit.Models;

namespace RHToolkit.Services
{
    public interface IGMDatabaseService
    {
        List<NameID> GetAddEffectItems();
        string GetAddEffectName(int effectID);
        List<NameID> GetCategoryItems(EnumService.ItemType itemType, bool isSubCategory);
        string GetCategoryName(int categoryID);
        List<NameID> GetCostumeMixGroupItems();
        List<int> GetCostumeMixItems(string groupIDs);
        (float weaponValue, int weaponPlus) GetWeaponEnhanceValue(int enhanceValue);
        (float defenseValue, int defensePlus) GetArmorEnhanceValue(int enhanceValue);
        long GetExperienceFromLevel(int level);
        string GetFortuneDesc(int fortuneID);
        List<NameID> GetFortuneDescItems();
        List<NameID> GetFortuneItems();
        (string fortuneName, string AddEffectDesc00, string AddEffectDesc01, string AddEffectDesc02, string fortuneDesc) GetFortuneValues(int fortuneID);
        List<ItemData> GetItemDataList(EnumService.ItemType itemType, string itemTableName);
        List<ItemData> GetItemDataLists();
        List<NameID> GetItemMixGroupItems();
        List<int> GetItemMixItems(string groupIDs);
        ObservableCollection<ItemMixData> GetItemMixList();
        List<NameID> GetLobbyItems();
        List<NameID> GetNpcDialogItems();
        List<NameID> GetNpcListItems();
        List<NameID> GetNpcShopItems();
        List<int> GetNpcShopItems(int shopID);
        List<NameID> GetNpcShopsItems();
        string GetOptionGroupName(int optionID);
        List<NameID> GetOptionItems();
        string GetOptionName(int optionID);
        (int secTime, float value) GetOptionValues(int optionID);
        List<NameID> GetQuestGroupItems();
        List<NameID> GetQuestListItems();
        (int nSetOption00, int nSetOptionvlue00, int nSetOption01, int nSetOptionvlue01, int nSetOption02, int nSetOptionvlue02, int nSetOption03, int nSetOptionvlue03, int nSetOption04, int nSetOptionvlue04) GetSetInfo(int setID);
        string GetSetName(int setId);
        string GetString(int stringID);
        List<NameID> GetStringItems();
        string GetSubCategory02Name(int categoryID);
        List<NameID> GetSubCategoryItems();
        string GetSubCategoryName(int categoryID);
        int GetTitleCategory(int titleID);
        (int titleCategory, int remainTime, int nAddEffectID00, int nAddEffectID01, int nAddEffectID02, int nAddEffectID03, int nAddEffectID04, int nAddEffectID05, string titleDesc) GetTitleInfo(int titleID);
        List<NameID> GetTitleItems();
        string GetTitleName(int titleID);
        int GetTitleRemainTime(int titleID);
        List<NameID> GetTradeItemGroupItems();
        List<NameID> GetTradeShopGroupItems();
        List<int> GetTradeShopItems(int shopID);
        (int nPhysicalAttackMin, int nPhysicalAttackMax, int nMagicAttackMin, int nMagicAttackMax) GetWeaponStats(int jbClass, int weaponID);
        bool IsNameInNickFilter(string characterName);
    }
}