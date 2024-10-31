using RHToolkit.Models;
using static RHToolkit.Models.EnumService;

namespace RHToolkit.Services
{
    public interface IGMDatabaseService
    {
        string FormatPreviousSkill(SkillType skillType, int skillID);
        List<NameID> GetAddEffectItems();
        string GetAddEffectName(int effectID);
        (float defenseValue, int defensePlus) GetArmorEnhanceValue(int enhanceValue);
        List<NameID> GetAuctionCategoryItems();
        List<NameID> GetCategoryItems(ItemType itemType, bool isSubCategory);
        string GetCategoryName(int categoryID);
        List<NameID> GetCostumeMixGroupItems();
        List<int> GetCostumeMixItems(string groupIDs);
        List<NameID> GetCostumePackItems();
        List<NameID> GetCostumePartItems(int jobClass);
        List<DropGroupList> GetDropGroupListItems(string tableName, int dropItemCount);
        List<string> GetEnemyNameItems();
        long GetExperienceFromLevel(int level);
        List<NameID> GetFielMeshItems();
        string GetFortuneDesc(int fortuneID);
        List<NameID> GetFortuneDescItems();
        List<NameID> GetFortuneItems();
        (string fortuneName, string AddEffectDesc00, string AddEffectDesc01, string AddEffectDesc02, string fortuneDesc) GetFortuneValues(int fortuneID);
        List<ItemData> GetItemDataList(ItemType itemType, string itemTableName);
        List<ItemData> GetItemDataLists();
        List<NameID> GetItemMixGroupItems();
        List<int> GetItemMixItems(string groupIDs);
        ObservableCollection<ItemMixData> GetItemMixList();
        List<NameID> GetLobbyItems();
        List<NameID> GetMissionItems();
        List<NameID> GetNpcDialogItems();
        List<NameID> GetNpcInstanceItems();
        List<NameID> GetNpcListItems();
        List<NameID> GetNpcShopItems();
        List<int> GetNpcShopItems(int shopID);
        List<NameID> GetNpcShopsItems();
        string GetOptionGroupName(int optionID);
        List<NameID> GetOptionItems();
        string GetOptionName(int optionID);
        (int secTime, float value) GetOptionValues(int optionID);
        List<NameID> GetPetEatItems();
        List<NameID> GetPetRebirthItems();
        List<NameID> GetQuestGroupItems();
        List<NameID> GetQuestListItems();
        List<DropGroupList> GetRareCardDropGroupListItems();
        List<RareCardReward> GetRareCardRewardItems();
        List<NameID> GetRiddleGroupItems();
        (int nSetOption00, int nSetOptionvlue00, int nSetOption01, int nSetOptionvlue01, int nSetOption02, int nSetOptionvlue02, int nSetOption03, int nSetOptionvlue03, int nSetOption04, int nSetOptionvlue04) GetSetInfo(int setID);
        List<NameID> GetSetItemItems();
        string GetSetName(int setId);
        List<SkillData> GetSkillDataList(SkillType skillType, string skillTableName);
        List<SkillData> GetSkillDataLists();
        List<NameID> GetSkillListItems(int jobClass);
        string GetSkillName(SkillType characterSkillType, int skillID, int skillLevel);
        (int beforeSkillID00, int beforeSkillLevel00, int beforeSkillID01, int beforeSkillLevel01, int beforeSkillID02, int beforeSkillLevel02) GetSkillTreeValues(SkillType characterSkillType, int skillID);
        string GetString(int stringID);
        List<NameID> GetStringItems();
        string GetSubCategory02Name(int categoryID);
        List<NameID> GetSubCategoryItems();
        string GetSubCategoryName(int categoryID);
        int GetTitleCategory(int titleID);
        (int titleCategory, int remainTime, int nAddEffectID00, int nAddEffectID01, int nAddEffectID02, int nAddEffectID03, int nAddEffectID04, int nAddEffectID05, string titleDesc) GetTitleInfo(int titleID);
        List<NameID> GetTitleItems();
        List<NameID> GetTitleListItems();
        string GetTitleName(int titleID);
        int GetTitleRemainTime(int titleID);
        List<NameID> GetTradeItemGroupItems();
        List<NameID> GetTradeShopGroupItems();
        List<int> GetTradeShopItems(int shopID);
        List<NameID> GetUnionPackageItems();
        (float weaponValue, int weaponPlus) GetWeaponEnhanceValue(int enhanceValue);
        (int nPhysicalAttackMin, int nPhysicalAttackMax, int nMagicAttackMin, int nMagicAttackMax) GetWeaponStats(int jbClass, int weaponID);
        List<NameID> GetWorldNameItems();
        bool IsNameInNickFilter(string characterName);
    }
}