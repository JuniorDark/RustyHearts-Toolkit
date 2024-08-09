namespace RHToolkit.Services
{
    public interface IFrameService
    {
        string FormatMainStat(int itemType, int physicalStat, int magicStat, int jobClass, int weaponId);
        string FormatSetEffect(int setId);
        string GetColorFromOption(int option);
        string GetOptionName(int option, int optionValue, bool isFixedOption = false);
        string GetString(int stringId);
    }
}