namespace RHToolkit.Models
{
    public class DatabaseItem
    {
        public Guid ItemUid { get; set; }
        public Guid CharacterId { get; set; }
        public Guid AuthId { get; set; }
        public int PageIndex { get; set; }
        public int SlotIndex { get; set; }
        public int Code { get; set; }
        public int UseCount { get; set; }
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
        public int ReconNum { get; set; }
        public byte ReconState { get; set; }
        public int SocketCount { get; set; }
        public int Socket1Code { get; set; }
        public int Socket1Value { get; set; }
        public int Socket2Code { get; set; }
        public int Socket2Value { get; set; }
        public int Socket3Code { get; set; }
        public int Socket3Value { get; set; }
        public int ExpireTime { get; set; }
        public string? LockPassword { get; set; }
        public int ActivityValue { get; set; }
        public Guid LinkId { get; set; }
        public byte IsSeizure { get; set; }
        public byte Socket1Color { get; set; }
        public byte Socket2Color { get; set; }
        public byte Socket3Color { get; set; }
        public int DefermentTime { get; set; }
        public byte Rank { get; set; }
        public byte AcquireRoute { get; set; }
        public int Physical { get; set; }
        public int Magical { get; set; }
        public int DurabilityMax { get; set; }
        public int Weight { get; set; }
    }
}
