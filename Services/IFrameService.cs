namespace RHToolkit.Services
{
    public interface IFrameService
    {
        string FormatAugmentStone(int value);
        string FormatDurability(int durability);
        string FormatItemTrade(int itemTrade);
        string FormatMainStat(int itemType, int physicalStat, int magicStat, int jobClass, int weaponId);
        string FormatNameID(string option, string replacement01, string replacement02, string replacement03, int maxValue, bool isFixedOption = false);
        string FormatPetFood(int petFood);
        string FormatPetFoodColor(int petFood);
        string FormatReconstruction(int reconstruction, int reconstructionMax, int itemTrade);
        string FormatRequiredLevel(int levelLimit);
        string FormatSellValue(int sellPrice);
        string FormatSetEffect(int setId);
        string FormatWeight(int weight);
        string GetBranchColor(int branch);
        string GetColorFromOption(int option);
        string GetOptionName(int option, int optionValue, bool isFixedOption = false);
        string GetRankText(int rank);
        string GetSocketColor(int colorId);
        string GetSocketText(int colorId);
    }
}