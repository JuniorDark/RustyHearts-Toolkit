using RHToolkit.Models.RH;
using System.Data;

namespace RHToolkit.Models.Editor
{
    public class FileManager
    {
        private static readonly string _appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        private static readonly string _backupDirectory = Path.Combine(_appDataPath, "RHToolkit", "RHEditor", "backup");

        public static async Task<DataTable?> FileToDataTableAsync(string sourceFile)
        {
            using FileStream sourceFileStream = File.OpenRead(sourceFile);
            byte[] buffer = new byte[4096];
            int bytesRead;

            using MemoryStream memoryStream = new();
            while ((bytesRead = await sourceFileStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await memoryStream.WriteAsync(buffer, 0, bytesRead);
            }

            byte[] sourceBytes = memoryStream.ToArray();

            return DataTableCryptor.RhToDataTable(sourceBytes);
        }

        public static async Task DataTableToFileAsync(string file, DataTable fileData)
        {
            byte[] encryptedData = DataTableCryptor.DataTableToRh(fileData);
            await File.WriteAllBytesAsync(file, encryptedData);
        }

        public static async Task SaveTempFile(string fileName, DataTable fileData)
        {
            if (fileData == null || fileName == null) return;

            try
            {
                if (!Directory.Exists(_backupDirectory))
                {
                    Directory.CreateDirectory(_backupDirectory);
                }

                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
                string fileExtension = Path.GetExtension(fileName);
                string newBackupFileName = $"{fileNameWithoutExtension}@{DateTime.Now:yyyy-MM-dd_HHmmss}{fileExtension}";
                string newBackupFilePath = Path.Combine(_backupDirectory, newBackupFileName);

                string? existingBackupFile = Directory.GetFiles(_backupDirectory, $"{fileNameWithoutExtension}@*{fileExtension}")
                                                     .FirstOrDefault();

                if (existingBackupFile != null)
                {
                    try
                    {
                        File.Move(existingBackupFile, newBackupFilePath);
                    }
                    catch (IOException)
                    {
                        await Task.Delay(500);
                        File.Move(existingBackupFile, newBackupFilePath);
                    }
                }

                await DataTableToFileAsync(newBackupFilePath, fileData);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error saving backup file: {ex.Message}");
            }
        }

        public static void ClearTempFile(string? fileName)
        {
            if (fileName == null) return;

            try
            {
                if (!Directory.Exists(_backupDirectory))
                {
                    return;
                }

                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
                string fileExtension = Path.GetExtension(fileName);

                foreach (var file in Directory.GetFiles(_backupDirectory, $"{fileNameWithoutExtension}@*{fileExtension}"))
                {
                    File.Delete(file);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error clearing backup file: {ex.Message}");
            }
        }
    }
}
