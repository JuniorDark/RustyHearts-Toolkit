//using System.Runtime.InteropServices;

//namespace RHGMTool.Utilities
//{
//    public partial class IniFile
//    {
//        private readonly string _iniFilePath;

//        [LibraryImport("kernel32", EntryPoint = "WritePrivateProfileStringW", StringMarshalling = StringMarshalling.Utf16)]
//        private static partial long WritePrivateProfileString(string section, string key, string val, string filePath);

//        [LibraryImport("kernel32", EntryPoint = "GetPrivateProfileStringW", StringMarshalling = StringMarshalling.Utf16)]
//        private static partial int GetPrivateProfileString(string section, string key, string def, [Out] char[] retVal, int size, string filePath);

//        public IniFile(string iniFileName)
//        {
//            string appDirectory = AppDomain.CurrentDomain.BaseDirectory;
//            _iniFilePath = Path.Combine(appDirectory, iniFileName);

//            if (!File.Exists(_iniFilePath))
//            {
//                WritePrivateProfileString("Option", "SqlServer", "", _iniFilePath);
//                WritePrivateProfileString("Option", "SqlUser", "", _iniFilePath);
//                WritePrivateProfileString("Option", "SqlPasswd", "", _iniFilePath);
//            }
//        }

//        public string ReadValue(string section, string key)
//        {
//            char[] buffer = new char[255];
//            int length = GetPrivateProfileString(section, key, "", buffer, buffer.Length, _iniFilePath);
//            return new string(buffer, 0, length);
//        }

//        public void WriteValue(string section, string key, string value)
//        {
//            WritePrivateProfileString(section, key, value, _iniFilePath);
//        }

//    }
//}