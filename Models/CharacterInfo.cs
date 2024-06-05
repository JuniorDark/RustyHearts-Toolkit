namespace RHToolkit.Models;

public class CharacterInfo(Guid characterID, Guid authID, string characterName, string accountName, int characterClass, int characterJob)
{
    public Guid CharacterID { get; set; } = characterID;
    public Guid AuthID { get; set; } = authID;
    public string? CharacterName { get; set; } = characterName;
    public string? AccountName { get; set; } = accountName;
    public int Class { get; set; } = characterClass;
    public int Job { get; set; } = characterJob;
}
