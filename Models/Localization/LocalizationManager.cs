using RHToolkit.Models.UISettings;
using System.Resources;

namespace RHToolkit.Models.Localization;

/// <summary>
/// Manages localization settings and resources for the application.
/// </summary>
public static class LocalizationManager
{
    /// <summary>
    /// Loads localized strings based on the specified language code.
    /// </summary>
    /// <param name="lang">The language code to load.</param>
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

    /// <summary>
    /// Gets the current application language from the registry.
    /// </summary>
    /// <returns>The current language code.</returns>
    public static string GetCurrentLanguage()
    {
        var languageCode = RegistrySettingsHelper.GetAppLanguage();

        return languageCode ?? "en-US";
    }

    /// <summary>
    /// Sets the current application language by loading localized strings.
    /// </summary>
    public static void SetCurrentLanguage()
    {
        var currentLanguage = GetCurrentLanguage();

        LoadLocalizedStrings(currentLanguage);
    }
}