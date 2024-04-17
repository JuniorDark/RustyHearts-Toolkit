namespace RHToolkit.Models
{
    public class NewCharacterData
    {
        public string? CharacterName { get; set; }
        public int Class { get; set; }
        public int Job { get; set; }
        public int Level { get; set; }
        public long Experience { get; set; }
        public int SP { get; set; }
        public int TotalSP { get; set; }
        public int Fatigue { get; set; }
        public int LobbyID { get; set; }
        public int Gold { get; set; }
        public int Hearts { get; set; }
        public required string BlockYN { get; set; }
        public int StorageGold { get; set; }
        public int StorageCount { get; set; }
        public required string IsTradeEnable { get; set; }
        public int Permission { get; set; }
        public int GuildPoint { get; set; }
        public required string IsMoveEnable { get; set; }
    }

}
