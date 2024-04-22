﻿using RHToolkit.Models;

namespace RHToolkit.Services
{
    public interface IGMDatabaseService
    {
        string GetAddEffectName(int effectID);
        List<NameID> GetCategoryItems(EnumService.ItemType itemType, bool isSubCategory);
        string GetCategoryName(int categoryID);
        long GetExperienceFromLevel(int level);
        string GetFortuneDesc(int fortuneID);
        List<NameID> GetFortuneDescItems();
        (string fortuneName, string AddEffectDesc00, string AddEffectDesc01, string AddEffectDesc02, string fortuneDesc) GetFortuneValues(int fortuneID);
        List<ItemData> GetItemDataList(EnumService.ItemType itemType, string itemTableName);
        List<NameID> GetLobbyItems();
        List<NameID> GetOptionItems();
        string GetOptionName(int optionID);
        (int minValue, int maxValue) GetOptionValue(int itemID);
        (int secTime, float value, int maxValue) GetOptionValues(int optionID);
        string GetSetName(int setId);
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