using Microsoft.Win32;
using System.Security.Cryptography;
using Wpf.Ui.Appearance;

namespace RHToolkit.Models.UISettings
{
    /// <summary>
    /// class for managing application settings stored in the Windows Registry.
    /// </summary>
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
        private const string DefaultTableEncryptKey = "gkw3iurpamv;kj20984;asdkfjat1af";
        private const string TableEncryptKeyValueName = "TableEncryptKey";
        private const string DefaultVFSKey = "MCJBqFumakm/UzXlng7suF4VH8FP7Hfot06H5vU8s0PMUzasWne43TB0jEqam7wKpK27E0uM1IDOZR0IWmpvJfk/7xukchTtlyJKLriWS46Wk/Eosgs8+F2qqYITbsGpIFeyWxbPnl/UzC71yUwc7uM/KbMGcEM99ZCiQgKYUP1dTpKtrX+rYCy4Q3aPX+anGeC5tWJr1EdpNA5tpFLjZEplR/U/U16LG/0h97po+d9oqJYPiwGXWIwe77NBRCHa4PTgLc0L8FxZ1pnnARVnMuASL82i3lLO7O93Drw4ZI2022f/yGYMimDhLgBDqTecEaq5mO0hNdTD3mVUnRywqQ==";
        private const string VFSKeyValueName = "VFSKey";
        private const string TableFolderValueName = "TableFolder";
        private const string ClientFolderValueName = "ClientFolder";
        private const string FilesToPackFolderValueName = "FilesToPackFolder";
        private const string InputFolderValueName = "InputFolder";
        private const string OutputFolderValueName = "OutputFolder";
        private const string ClientAssetsFolderValueName = "ClientAssetsFolder";

        /// <summary>
        /// Encrypts a plain text string using the current user's data protection scope.
        /// </summary>
        /// <param name="plainText">The plain text to encrypt.</param>
        /// <returns>The encrypted text as a base64 string.</returns>
        private static string Encrypt(string plainText)
        {
            byte[] plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] encryptedBytes = ProtectedData.Protect(plainTextBytes, null, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(encryptedBytes);
        }

        /// <summary>
        /// Decrypts an encrypted base64 string using the current user's data protection scope.
        /// </summary>
        /// <param name="encryptedText">The encrypted text as a base64 string.</param>
        /// <returns>The decrypted plain text.</returns>
        private static string Decrypt(string encryptedText)
        {
            byte[] encryptedBytes = Convert.FromBase64String(encryptedText);
            byte[] plainTextBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(plainTextBytes);
        }

        /// <summary>
        /// Gets the application theme from the registry.
        /// </summary>
        /// <returns>The application theme.</returns>
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

        /// <summary>
        /// Sets the application theme in the registry.
        /// </summary>
        /// <param name="theme">The application theme to set.</param>
        public static void SetAppTheme(ApplicationTheme theme)
        {
            using var key = Registry.CurrentUser.CreateSubKey(RegistryKey);
            key.SetValue(ThemeValueName, theme.ToString());
        }

        /// <summary>
        /// Gets the application language from the registry.
        /// </summary>
        /// <returns>The application language.</returns>
        public static string GetAppLanguage()
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKey);
            if (key != null)
            {
                return key.GetValue(LanguageValueName)?.ToString() ?? DefaultLanguage;
            }
            return DefaultLanguage;
        }

        /// <summary>
        /// Sets the application language in the registry.
        /// </summary>
        /// <param name="language">The application language to set.</param>
        public static void SetAppLanguage(string language)
        {
            using var key = Registry.CurrentUser.CreateSubKey(RegistryKey);
            key.SetValue(LanguageValueName, language);
        }

        /// <summary>
        /// Gets the SQL server address from the registry.
        /// </summary>
        /// <returns>The SQL server address.</returns>
        public static string GetSQLServer()
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKey);
            if (key != null)
            {
                return key.GetValue(SQLServerValueName)?.ToString() ?? DefaultSQLServer;
            }
            return DefaultSQLServer;
        }

        /// <summary>
        /// Sets the SQL server address in the registry.
        /// </summary>
        /// <param name="server">The SQL server address to set.</param>
        public static void SetSQLServer(string server)
        {
            using var key = Registry.CurrentUser.CreateSubKey(RegistryKey);
            key.SetValue(SQLServerValueName, server);
        }

        /// <summary>
        /// Gets the SQL user name from the registry.
        /// </summary>
        /// <returns>The SQL user name.</returns>
        public static string GetSQLUser()
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKey);
            if (key != null)
            {
                return key.GetValue(SQLUserValueName)?.ToString() ?? DefaultSQLUser;
            }
            return DefaultSQLUser;
        }

        /// <summary>
        /// Sets the SQL user name in the registry.
        /// </summary>
        /// <param name="user">The SQL user name to set.</param>
        public static void SetSQLUser(string user)
        {
            using var key = Registry.CurrentUser.CreateSubKey(RegistryKey);
            key.SetValue(SQLUserValueName, user);
        }

        /// <summary>
        /// Gets the SQL password from the registry.
        /// </summary>
        /// <returns>The SQL password.</returns>
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

        /// <summary>
        /// Checks if a string is a valid base64 string.
        /// </summary>
        /// <param name="s">The string to check.</param>
        /// <returns>True if the string is a valid base64 string, otherwise false.</returns>
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

        /// <summary>
        /// Sets the SQL password in the registry.
        /// </summary>
        /// <param name="password">The SQL password to set.</param>
        public static void SetSQLPassword(string password)
        {
            using var key = Registry.CurrentUser.CreateSubKey(RegistryKey);

            key.SetValue(SQLPwdValueName, password != null ? Encrypt(password) : string.Empty);
        }

        /// <summary>
        /// Gets the table encryption key from the registry.
        /// </summary>
        /// <returns>The table encryption key.</returns>
        public static string GetTableEncryptKey()
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKey);
            if (key != null)
            {
                return key.GetValue(TableEncryptKeyValueName)?.ToString() ?? DefaultTableEncryptKey;
            }
            return DefaultTableEncryptKey;
        }


        /// <summary>
        /// Sets the table encryption key in the registry.
        /// </summary>
        /// <param name="encryptKey">The table encryption key to set.</param>
        public static void SetTableEncryptKey(string encryptKey)
        {
            using var key = Registry.CurrentUser.CreateSubKey(RegistryKey);
            key.SetValue(TableEncryptKeyValueName, encryptKey);
        }

        /// <summary>
        /// Sets the virtual file system (VFS) key (for f00X.dat) in the registry.
        /// </summary>
        /// <param name="vfsKey">The VFS key to set.</param>
        public static void SetVFSKey(string vfsKey)
        {
            using var key = Registry.CurrentUser.CreateSubKey(RegistryKey);
            key.SetValue(VFSKeyValueName, vfsKey);
        }

        /// <summary>
        /// Gets the virtual file system (VFS) key (for f00X.dat) from the registry.
        /// </summary>
        /// <returns>The VFS key.</returns>
        public static string GetVFSKey()
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKey);
            if (key != null)
            {
                return key.GetValue(VFSKeyValueName)?.ToString() ?? DefaultVFSKey;
            }
            return DefaultVFSKey;
        }

        /// <summary>
        /// Gets the table folder path from the registry.
        /// </summary>
        /// <returns>The table folder path.</returns>
        public static string GetTableFolder()
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKey);
            if (key != null)
            {
                return key.GetValue(TableFolderValueName)?.ToString() ?? string.Empty;
            }
            return string.Empty;
        }

        /// <summary>
        /// Sets the table folder path in the registry.
        /// </summary>
        /// <param name="folderPath">The table folder path to set.</param>
        public static void SetTableFolder(string folderPath)
        {
            using var key = Registry.CurrentUser.CreateSubKey(RegistryKey);
            key.SetValue(TableFolderValueName, folderPath);
        }

        /// <summary>
        /// Gets the client folder path from the registry.
        /// </summary>
        /// <returns>The client folder path.</returns>
        public static string GetClientFolder()
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKey);
            if (key != null)
            {
                return key.GetValue(ClientFolderValueName)?.ToString() ?? string.Empty;
            }
            return string.Empty;
        }

        /// <summary>
        /// Sets the client folder path in the registry.
        /// </summary>
        /// <param name="folderPath">The client folder path to set.</param>
        public static void SetClientFolder(string folderPath)
        {
            using var key = Registry.CurrentUser.CreateSubKey(RegistryKey);
            key.SetValue(ClientFolderValueName, folderPath);
        }

        /// <summary>
        /// Gets the files folder path from the registry.
        /// </summary>
        /// <returns>The files folder path.</returns>
        public static string GetFilesToPackFolder()
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKey);
            if (key != null)
            {
                return key.GetValue(FilesToPackFolderValueName)?.ToString() ?? string.Empty;
            }
            return string.Empty;
        }

        /// <summary>
        /// Sets the files folder path in the registry.
        /// </summary>
        /// <param name="folderPath">The files folder path to set.</param>
        public static void SetFilesToPackFolder(string folderPath)
        {
            using var key = Registry.CurrentUser.CreateSubKey(RegistryKey);
            key.SetValue(FilesToPackFolderValueName, folderPath);
        }

        /// <summary>
        /// Gets the input folder path from the registry.
        /// </summary>
        /// <returns>The files folder path.</returns>
        public static string GetInputFolder()
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKey);
            if (key != null)
            {
                return key.GetValue(InputFolderValueName)?.ToString() ?? string.Empty;
            }
            return string.Empty;
        }

        /// <summary>
        /// Sets the input folder path in the registry.
        /// </summary>
        /// <param name="folderPath">The files folder path to set.</param>
        public static void SetInputFolder(string folderPath)
        {
            using var key = Registry.CurrentUser.CreateSubKey(RegistryKey);
            key.SetValue(InputFolderValueName, folderPath);
        }

        /// <summary>
        /// Gets the output folder path from the registry.
        /// </summary>
        /// <returns>The files folder path.</returns>
        public static string GetOutputFolder()
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKey);
            if (key != null)
            {
                return key.GetValue(OutputFolderValueName)?.ToString() ?? string.Empty;
            }
            return string.Empty;
        }

        /// <summary>
        /// Sets the output folder path in the registry.
        /// </summary>
        /// <param name="folderPath">The files folder path to set.</param>
        public static void SetOutputFolder(string folderPath)
        {
            using var key = Registry.CurrentUser.CreateSubKey(RegistryKey);
            key.SetValue(OutputFolderValueName, folderPath);
        }

        /// <summary>
        /// Gets the client assets folder path from the registry.
        /// </summary>
        /// <returns>The client assets folder path.</returns>
        public static string GetClientAssetsFolder()
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKey);
            if (key != null)
            {
                return key.GetValue(ClientAssetsFolderValueName)?.ToString() ?? string.Empty;
            }
            return string.Empty;
        }

        /// <summary>
        /// Sets the client assets folder path in the registry.
        /// </summary>
        /// <param name="folderPath">The client assets folder path to set.</param>
        public static void SetClientAssetsFolder(string folderPath)
        {
            using var key = Registry.CurrentUser.CreateSubKey(RegistryKey);
            key.SetValue(ClientAssetsFolderValueName, folderPath);
        }
    }
}