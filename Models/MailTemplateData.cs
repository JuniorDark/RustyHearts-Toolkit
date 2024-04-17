namespace RHToolkit.Models
{
    [Serializable]
    public class MailTemplateData
    {
        public string? Sender { get; set; }
        public string? Recipient { get; set; }
        public bool SendToAll { get; set; }
        public string? Content { get; set; }
        public int Gold { get; set; }
        public int ReqGold { get; set; }
        public List<int>? ItemTypes { get; set; }
        public List<int>? ItemIDs { get; set; }
        public List<int>? ItemAmounts { get; set; }
        public List<int>? Durabilities { get; set; }
        public List<int>? EnchantLevels { get; set; }
        public List<int>? Ranks { get; set; }
        public List<int>? ReconNums { get; set; }
        public List<int>? ReconStates { get; set; }
        public List<int>? OptionCodes1 { get; set; }
        public List<int>? OptionCodes2 { get; set; }
        public List<int>? OptionCodes3 { get; set; }
        public List<int>? OptionValues1 { get; set; }
        public List<int>? OptionValues2 { get; set; }
        public List<int>? OptionValues3 { get; set; }
        public List<int>? SocketCounts { get; set; }
        public List<int>? SocketColors1 { get; set; }
        public List<int>? SocketColors2 { get; set; }
        public List<int>? SocketColors3 { get; set; }
        public List<int>? SocketCodes1 { get; set; }
        public List<int>? SocketCodes2 { get; set; }
        public List<int>? SocketCodes3 { get; set; }
        public List<int>? SocketValues1 { get; set; }
        public List<int>? SocketValues2 { get; set; }
        public List<int>? SocketValues3 { get; set; }
        public List<int>? DurabilityMaxValues { get; set; }
        public List<int>? WeightValues { get; set; }

    }
}
