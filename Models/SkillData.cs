using static RHToolkit.Models.EnumService;

namespace RHToolkit.Models;

public class SkillData
{
    public int ID { get; set; }
    public SkillType CharacterSkillType { get; set; }
    public int SlotIndex { get; set; }
    public int SkillID { get; set; }
    public string? SkillName { get; set; }
    public string? Description1 { get; set; }
    public string? Description2 { get; set; }
    public string? Description3 { get; set; }
    public string? Description4 { get; set; }
    public string? Description5 { get; set; }
    public string? IconName { get; set; }
    public string? SkillType { get; set; }
    public string? CharacterType { get; set; }
    public int CharacterTypeValue { get; set; }
    public int SkillLevel { get; set; }
    public int RequiredLevel { get; set; }
    public int SPCost { get; set; }
    public double MPCost { get; set; }
    public double Cooltime { get; set; }

}
