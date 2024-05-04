using Microsoft.Win32;
using System.Security.Cryptography;
using Wpf.Ui.Appearance;

namespace RHToolkit.Models.UISettings
{
    public static class RegistrySettingsHelper
    {
        private const string RegistryKey = "SOFTWARE\\RustyHeartsToolkit";
        private const string ThemeValueName = "AppTheme";
        private const string LanguageValueName = "AppLanguage";
        private const string SQLServerValueName = "SQLServer";
        private const string SQLUserValueName = "SQLUser";
        private const string SQLPwdValueName = "SQLPwd";

        private static byte[] ProtectData(byte[] data)
        {
            return ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser);
        }

        private static byte[] UnprotectData(byte[] data)
        {
            return ProtectedData.Unprotect(data, null, DataProtectionScope.CurrentUser);
        }

        private static string EncryptString(string data)
        {
            byte[] encryptedData = ProtectData(Encoding.UTF8.GetBytes(data));
            return Convert.ToBase64String(encryptedData);
        }

        private static string DecryptString(string encryptedData)
        {
            byte[] decryptedData = UnprotectData(Convert.FromBase64String(encryptedData));
            return Encoding.UTF8.GetString(decryptedData);
        }

        public static ApplicationTheme GetAppTheme()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(RegistryKey))
            {
                if (key != null)
                {
                    return Enum.TryParse<ApplicationTheme>(key.GetValue(ThemeValueName)?.ToString(), out var theme)
                        ? theme
                        : ApplicationTheme.Dark;
                }
                return ApplicationTheme.Dark;
            }
        }

        public static void SetAppTheme(ApplicationTheme theme)
        {
            using (var key = Registry.CurrentUser.CreateSubKey(RegistryKey))
            {
                key.SetValue(ThemeValueName, theme.ToString());
            }
        }

        public static string GetAppLanguage()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(RegistryKey))
            {
                if (key != null)
                {
                    return key.GetValue(LanguageValueName)?.ToString() ?? "English";
                }
                return "English";
            }
        }

        public static void SetAppLanguage(string language)
        {
            using (var key = Registry.CurrentUser.CreateSubKey(RegistryKey))
            {
                key.SetValue(LanguageValueName, language);
            }
        }

        public static string GetSQLServer()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(RegistryKey))
            {
                if (key != null)
                {
                    return key.GetValue(SQLServerValueName)?.ToString() ?? "localhost";
                }
                return "localhost";
            }
        }

        public static void SetSQLServer(string server)
        {
            using (var key = Registry.CurrentUser.CreateSubKey(RegistryKey))
            {
                key.SetValue(SQLServerValueName, server);
            }
        }

        public static string GetSQLUser()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(RegistryKey))
            {
                if (key != null)
                {
                    return key.GetValue(SQLUserValueName)?.ToString() ?? "sa";
                }
                return "sa";
            }
        }

        public static void SetSQLUser(string user)
        {
            using (var key = Registry.CurrentUser.CreateSubKey(RegistryKey))
            {
                key.SetValue(SQLUserValueName, user);
            }
        }

        public static string GetSQLPassword()
    {
        using (var key = Registry.CurrentUser.OpenSubKey(RegistryKey))
        {
            if (key != null)
            {
                return key.GetValue(SQLPwdValueName)?.ToString() ?? string.Empty;
            }
            return string.Empty;
        }
    }

    public static void SetSQLPassword(string password)
    {
        using (var key = Registry.CurrentUser.CreateSubKey(RegistryKey))
        {
            key.SetValue(SQLPwdValueName, password);
        }
    }
    }
}
