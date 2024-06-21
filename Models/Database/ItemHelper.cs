using RHToolkit.Models.SQLite;
using RHToolkit.Services;
using RHToolkit.ViewModels.Controls;

namespace RHToolkit.Models.Database;

public class ItemHelper(CachedDataManager cachedDataManager, IFrameService frameService, IGMDatabaseService gmDatabaseService)
{
    private readonly IFrameService _frameService = frameService;
    private readonly IGMDatabaseService _gmDatabaseService = gmDatabaseService;
    private readonly CachedDataManager _cachedDataManager = cachedDataManager;

    public bool IsInvalidItemID(int itemID)
    {
        return _cachedDataManager.CachedItemDataList == null || !_cachedDataManager.CachedItemDataList.Any(item => item.ItemId == itemID);
    }

    public static ItemData CreateNewItem(CharacterData characterData, ItemData newItemData, int pageIndex)
    {
        var newItem = new ItemData
        {
            CharacterId = characterData.CharacterID,
            AuthId = characterData.AuthID,
            ItemUid = Guid.NewGuid(),
            PageIndex = pageIndex, // 0 = Equip, 1 to 5 = Inventory, 11 = QuickSlot, 21 = Storage, 61 = Mail
            CreateTime = DateTime.Now,
            UpdateTime = DateTime.Now,
            ExpireTime = 0,
            RemainTime = 0,
            GCode = 1,
            LockPassword = "",
            LinkId = Guid.Empty,
            IsSeizure = 0,
            DefermentTime = 0,

            SlotIndex = newItemData.SlotIndex,
            ItemId = newItemData.ItemId,
            ItemName = newItemData.ItemName,
            IconName = newItemData.IconName,
            Durability = newItemData.Durability,
            DurabilityMax = newItemData.DurabilityMax,
            EnhanceLevel = newItemData.EnhanceLevel,
            AugmentStone = newItemData.AugmentStone,
            Rank = newItemData.Rank,
            Weight = newItemData.Weight,
            Reconstruction = newItemData.Reconstruction,
            ReconstructionMax = newItemData.ReconstructionMax,
            ItemAmount = newItemData.ItemAmount,
            Option1Code = newItemData.Option1Code,
            Option2Code = newItemData.Option2Code,
            Option3Code = newItemData.Option3Code,
            Option1Value = newItemData.Option1Value,
            Option2Value = newItemData.Option2Value,
            Option3Value = newItemData.Option3Value,
            SocketCount = newItemData.SocketCount,
            Socket1Color = newItemData.Socket1Color,
            Socket2Color = newItemData.Socket2Color,
            Socket3Color = newItemData.Socket3Color,
            Socket1Code = newItemData.Socket1Code,
            Socket2Code = newItemData.Socket2Code,
            Socket3Code = newItemData.Socket3Code,
            Socket1Value = newItemData.Socket1Value,
            Socket2Value = newItemData.Socket2Value,
            Socket3Value = newItemData.Socket3Value,
        };

        return newItem;
    }

    public FrameViewModel GetItemData(ItemData equipmentItem)
    {
        // Find the corresponding ItemData in the _cachedItemDataList
        ItemData cachedItem = _cachedDataManager.CachedItemDataList?.FirstOrDefault(item => item.ItemId == equipmentItem.ItemId) ?? new ItemData();

        ItemData itemData = new()
        {
            ItemId = cachedItem.ItemId,
            ItemName = cachedItem.ItemName ?? "",
            Description = cachedItem.Description ?? "",
            IconName = cachedItem.IconName ?? "",
            Type = cachedItem.Type,
            WeaponID00 = cachedItem.WeaponID00,
            Category = cachedItem.Category,
            SubCategory = cachedItem.SubCategory,
            LevelLimit = cachedItem.LevelLimit,
            ItemTrade = cachedItem.ItemTrade,
            OverlapCnt = cachedItem.OverlapCnt,
            Defense = cachedItem.Defense,
            MagicDefense = cachedItem.MagicDefense,
            Branch = cachedItem.Branch,
            OptionCountMax = cachedItem.OptionCountMax,
            SocketCountMax = cachedItem.SocketCountMax,
            SellPrice = cachedItem.SellPrice,
            PetFood = cachedItem.PetFood,
            JobClass = cachedItem.JobClass,
            SetId = cachedItem.SetId,
            FixOption1Code = cachedItem.FixOption1Code,
            FixOption1Value = cachedItem.FixOption1Value,
            FixOption2Code = cachedItem.FixOption2Code,
            FixOption2Value = cachedItem.FixOption2Value,

            ItemUid = equipmentItem.ItemUid,
            CharacterId = equipmentItem.CharacterId,
            AuthId = equipmentItem.AuthId,
            PageIndex = equipmentItem.PageIndex,
            SlotIndex = equipmentItem.SlotIndex,
            ItemAmount = equipmentItem.ItemAmount,
            Reconstruction = equipmentItem.Reconstruction,
            ReconstructionMax = equipmentItem.ReconstructionMax,
            AugmentStone = equipmentItem.AugmentStone,
            Rank = equipmentItem.Rank,
            AcquireRoute = equipmentItem.AcquireRoute,
            Physical = equipmentItem.Physical,
            Magical = equipmentItem.Magical,
            DurabilityMax = equipmentItem.DurabilityMax,
            Weight = equipmentItem.Weight,
            RemainTime = equipmentItem.RemainTime,
            CreateTime = equipmentItem.CreateTime,
            UpdateTime = equipmentItem.UpdateTime,
            GCode = equipmentItem.GCode,
            Durability = equipmentItem.Durability,
            EnhanceLevel = equipmentItem.EnhanceLevel,
            Option1Code = equipmentItem.Option1Code,
            Option1Value = equipmentItem.Option1Value,
            Option2Code = equipmentItem.Option2Code,
            Option2Value = equipmentItem.Option2Value,
            Option3Code = equipmentItem.Option3Code,
            Option3Value = equipmentItem.Option3Value,
            OptionGroup = equipmentItem.OptionGroup,
            SocketCount = equipmentItem.SocketCount,
            Socket1Code = equipmentItem.Socket1Code,
            Socket1Value = equipmentItem.Socket1Value,
            Socket2Code = equipmentItem.Socket2Code,
            Socket2Value = equipmentItem.Socket2Value,
            Socket3Code = equipmentItem.Socket3Code,
            Socket3Value = equipmentItem.Socket3Value,
            ExpireTime = equipmentItem.ExpireTime,
            LockPassword = equipmentItem.LockPassword,
            LinkId = equipmentItem.LinkId,
            IsSeizure = equipmentItem.IsSeizure,
            Socket1Color = equipmentItem.Socket1Color,
            Socket2Color = equipmentItem.Socket2Color,
            Socket3Color = equipmentItem.Socket3Color,
            DefermentTime = equipmentItem.DefermentTime,
        };

        var frameViewModel = new FrameViewModel(_frameService, _gmDatabaseService)
        {
            ItemData = itemData
        };

        return frameViewModel;
    }

    public static int GetRealClass(int charClass)
    {
        return charClass switch
        {
            1 or 101 or 102 => 1,
            2 or 201 => 2,
            3 or 301 => 3,
            4 or 401 => 4,
            _ => 0,
        };
    }
}
