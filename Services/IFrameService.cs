namespace RHToolkit.Services
{
    public interface IFrameService
    {
        string GetBranchColor(int branch);
        string GetRankText(int rank);
        string GetSocketText(int colorId);
        string GetSocketColor(int colorId);
        string GetColorFromOption(int option);
        string GetOptionName(int option, int optionValue);
        string FormatMainStat(int itemType, int physicalStat, int magicStat, int jobClass, int weaponId);
        string FormatSellValue(int sellPrice);
        string FormatRequiredLevel(int levelLimit);
        string FormatItemTrade(int itemTrade);
        string FormatDurability(int durability, int maxDurability);
        string FormatWeight(int weight);
        string FormatReconstruction(int reconstruction, int reconstructionMax, int itemTrade);
        string FormatPetFood(int petFood);
        string FormatPetFoodColor(int petFood);
        string FormatNameID(string option, string replacement01, string replacement02, string replacement03, int maxValue);
        
    }
}
