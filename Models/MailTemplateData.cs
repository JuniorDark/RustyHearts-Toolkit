namespace RHToolkit.Models
{
    [Serializable]
    public class MailTemplateData
    {
        public bool MailTemplate { get; set; }
        public string? Sender { get; set; }
        public string? Recipient { get; set; }
        public bool SendToAll { get; set; }
        public bool SendToAllOnline { get; set; }
        public bool SendToAllOffline { get; set; }
        public string? MailContent { get; set; }
        public int AttachGold { get; set; }
        public int ItemCharge { get; set; }
        public int ReturnDays { get; set; }
        public List<int>? ItemIDs { get; set; }
        public List<int>? ItemAmounts { get; set; }
        public List<int>? Durabilities { get; set; }
        public List<int>? DurabilityMaxValues { get; set; }
        public List<int>? WeightValues { get; set; }
        public List<int>? EnchantLevels { get; set; }
        public List<int>? AugmentStoneValues { get; set; }
        public List<byte>? Ranks { get; set; }
        public List<int>? ReconNums { get; set; }
        public List<byte>? ReconStates { get; set; }
        public List<int>? Options1 { get; set; }
        public List<int>? Options2 { get; set; }
        public List<int>? Options3 { get; set; }
        public List<int>? OptionValues1 { get; set; }
        public List<int>? OptionValues2 { get; set; }
        public List<int>? OptionValues3 { get; set; }
        public List<int>? SocketCounts { get; set; }
        public List<int>? SocketColors1 { get; set; }
        public List<int>? SocketColors2 { get; set; }
        public List<int>? SocketColors3 { get; set; }
        public List<int>? SocketOptions1 { get; set; }
        public List<int>? SocketOptions2 { get; set; }
        public List<int>? SocketOptions3 { get; set; }
        public List<int>? SocketOptionValues1 { get; set; }
        public List<int>? SocketOptionValues2 { get; set; }
        public List<int>? SocketOptionValues3 { get; set; }
        

    }
}
