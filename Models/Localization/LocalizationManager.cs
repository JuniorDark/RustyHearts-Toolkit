using RHToolkit.Properties;
using System.Resources;

namespace RHToolkit.Models.Localization;

public static class LocalizationManager
{
    public static void LoadLocalizedStrings(string lang)
    {
        CultureInfo cultureInfo;

        if (!string.IsNullOrEmpty(lang))
        {
            try
            {
                cultureInfo = new CultureInfo(lang);
            }
            catch (CultureNotFoundException)
            {
                cultureInfo = new CultureInfo("en-US");
            }
        }
        else
        {
            cultureInfo = new CultureInfo("en-US");
        }

        Thread.CurrentThread.CurrentUICulture = cultureInfo;

        _ = new ResourceManager(typeof(Resources));
        Resources.Culture = cultureInfo;
    }
}

