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
        private const string DefaultLanguage = "English";
        private const string DefaultSQLServer = "localhost";
        private const string DefaultSQLUser = "sa";
        private const string TableFolderValueName = "TableFolder";

        private static string Encrypt(string plainText)
        {
            byte[] plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] encryptedBytes = ProtectedData.Protect(plainTextBytes, null, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(encryptedBytes);
        }

        private static string Decrypt(string encryptedText)
        {
            byte[] encryptedBytes = Convert.FromBase64String(encryptedText);
            byte[] plainTextBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(plainTextBytes);
        }

        public static ApplicationTheme GetAppTheme()
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKey);
            if (key != null)
            {
                return Enum.TryParse<ApplicationTheme>(key.GetValue(ThemeValueName)?.ToString(), out var theme)
                    ? theme
                    : ApplicationTheme.Dark;
            }
            return ApplicationTheme.Dark;
        }

        public static void SetAppTheme(ApplicationTheme theme)
        {
            using var key = Registry.CurrentUser.CreateSubKey(RegistryKey);
            key.SetValue(ThemeValueName, theme.ToString());
        }

        public static string GetAppLanguage()
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKey);
            if (key != null)
            {
                return key.GetValue(LanguageValueName)?.ToString() ?? DefaultLanguage;
            }
            return DefaultLanguage;
        }

        public static void SetAppLanguage(string language)
        {
            using var key = Registry.CurrentUser.CreateSubKey(RegistryKey);
            key.SetValue(LanguageValueName, language);
        }

        public static string GetSQLServer()
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKey);
            if (key != null)
            {
                return key.GetValue(SQLServerValueName)?.ToString() ?? DefaultSQLServer;
            }
            return DefaultSQLServer;
        }

        public static void SetSQLServer(string server)
        {
            using var key = Registry.CurrentUser.CreateSubKey(RegistryKey);
            key.SetValue(SQLServerValueName, server);
        }

        public static string GetSQLUser()
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKey);
            if (key != null)
            {
                return key.GetValue(SQLUserValueName)?.ToString() ?? DefaultSQLUser;
            }
            return DefaultSQLUser;
        }

        public static void SetSQLUser(string user)
        {
            using var key = Registry.CurrentUser.CreateSubKey(RegistryKey);
            key.SetValue(SQLUserValueName, user);
        }

        public static string GetSQLPassword()
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKey);
            if (key != null)
            {
                var password = key.GetValue(SQLPwdValueName)?.ToString();

                if (!string.IsNullOrEmpty(password))
                {
                    if (IsBase64String(password))
                    {
                        return Decrypt(password);
                    }
                    else
                    {
                        return password;
                    }
                }
            }
            return string.Empty;
        }

        private static bool IsBase64String(string s)
        {
            try
            {
                Convert.FromBase64String(s);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        public static void SetSQLPassword(string password)
        {
            using var key = Registry.CurrentUser.CreateSubKey(RegistryKey);

            key.SetValue(SQLPwdValueName, password != null ? Encrypt(password) : string.Empty);
        }

        public static string GetTableFolder()
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKey);
            if (key != null)
            {
                return key.GetValue(TableFolderValueName)?.ToString() ?? string.Empty;
            }
            return string.Empty;
        }

        public static void SetTableFolder(string folderPath)
        {
            using var key = Registry.CurrentUser.CreateSubKey(RegistryKey);
            key.SetValue(TableFolderValueName, folderPath);
        }
    }
}
