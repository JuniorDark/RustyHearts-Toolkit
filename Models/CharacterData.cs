﻿namespace RHToolkit.Models
{
    public class CharacterData
    {
        public Guid CharacterID { get; set; }
        public Guid AuthID { get; set; }
        public string? AccountName { get; set; }
        public int Server { get; set; }
        public string? CharacterName { get; set; }
        public int Class { get; set; }
        public int Job { get; set; }
        public int Level { get; set; }
        public long Experience { get; set; }
        public int SP { get; set; }
        public int TotalSP { get; set; }
        public int Fatigue { get; set; }
        public int LobbyID { get; set; }
        public DateTime CreateTime { get; set; }
        public DateTime LastLogin { get; set; }
        public int Gold { get; set; }
        public int Hearts { get; set; }
        public byte BlockType { get; set; }
        public string? BlockYN { get; set; }
        public string? IsConnect { get; set; }
        public int StorageGold { get; set; }
        public int StorageCount { get; set; }
        public string? IsTradeEnable { get; set; }
        public int Permission { get; set; }
        public int GuildPoint { get; set; }
        public string? IsMoveEnable { get; set; }
        public string? GuildName { get; set; }
        public bool HasGuild { get; set; }
    }

}
