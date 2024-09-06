namespace RHToolkit.Models
{
    public class ItemData
    {
        public bool IsNewItem { get; set; }
        public bool IsEditedItem { get; set; }
        public Guid ItemUid { get; set; }
        public Guid CharacterId { get; set; }
        public Guid AuthId { get; set; }
        public int PageIndex { get; set; }
        public int SlotIndex { get; set; }
        public string? ItemName { get; set; }
        public string? Description { get; set; }
        public string? IconName { get; set; }
        public int Type { get; set; }
        public int ItemId { get; set; }
        public int WeaponID00 { get; set; }
        public int Category { get; set; }
        public int SubCategory { get; set; }
        public int LevelLimit { get; set; }
        public int ItemTrade { get; set; }
        public int OverlapCnt { get; set; }
        public int ItemAmount { get; set; }
        public int Defense { get; set; }
        public int MagicDefense { get; set; }
        public int Branch { get; set; }
        public int InventoryType { get; set; }
        public int AccountStorage { get; set; }
        public int OptionCountMax { get; set; }
        public int SocketCountMax { get; set; }
        public int Reconstruction { get; set; }
        public byte ReconstructionMax { get; set; }
        public int SellPrice { get; set; }
        public int PetFood { get; set; }
        public int JobClass { get; set; }
        public int SetId { get; set; }
        public int AugmentStone { get; set; }
        public int FixOption1Code { get; set; }
        public int FixOption1Value { get; set; }
        public int FixOption2Code { get; set; }
        public int FixOption2Value { get; set; }
        public int RemainTime { get; set; }
        public DateTime CreateTime { get; set; }
        public DateTime UpdateTime { get; set; }
        public int GCode { get; set; }
        public int Durability { get; set; }
        public int EnhanceLevel { get; set; }
        public int Option1Code { get; set; }
        public int Option1Value { get; set; }
        public int Option2Code { get; set; }
        public int Option2Value { get; set; }
        public int Option3Code { get; set; }
        public int Option3Value { get; set; }
        public int OptionGroup { get; set; }
        public int SocketCount { get; set; }
        public int Socket1Code { get; set; }
        public int Socket1Value { get; set; }
        public int Socket2Code { get; set; }
        public int Socket2Value { get; set; }
        public int Socket3Code { get; set; }
        public int Socket3Value { get; set; }
        public int ExpireTime { get; set; }
        public string? LockPassword { get; set; }
        public Guid LinkId { get; set; }
        public byte IsSeizure { get; set; }
        public int Socket1Color { get; set; }
        public int Socket2Color { get; set; }
        public int Socket3Color { get; set; }
        public int DefermentTime { get; set; }
        public byte Rank { get; set; }
        public byte AcquireRoute { get; set; }
        public int Physical { get; set; }
        public int Magical { get; set; }
        public int DurabilityMax { get; set; }
        public int Weight { get; set; }
        public int TitleList { get; set; }

    }

}
