namespace RHToolkit.Services
{
    public interface IFrameService
    {
        string FormatAugmentStone(int value);
        string FormatDurability(int durability);
        string FormatItemTrade(int itemTrade);
        string FormatMainStat(int itemType, int physicalStat, int magicStat, int jobClass, int weaponId);
        string FormatNameID(string option, string replacement01, string replacement02, string replacement03, int maxValue);
        string FormatPetFood(int petFood);
        string FormatPetFoodColor(int petFood);
        string FormatReconstruction(int reconstruction, int reconstructionMax, int itemTrade);
        string FormatRequiredLevel(int levelLimit);
        string FormatSellValue(int sellPrice);
        string FormatWeight(int weight);
        string GetBranchColor(int branch);
        string GetColorFromOption(int option);
        string GetOptionName(int option, int optionValue);
        string GetRankText(int rank);
        string GetSocketColor(int colorId);
        string GetSocketText(int colorId);
    }
}