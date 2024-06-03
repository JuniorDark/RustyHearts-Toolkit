namespace RHToolkit.Models;

public class CharacterInfo(Guid characterID, string characterName, string accountName)
{
    public Guid CharacterID { get; set; } = characterID;
    public string? CharacterName { get; set; } = characterName;
    public string? AccountName { get; set; } = accountName;
}
