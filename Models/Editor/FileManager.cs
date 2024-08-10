using RHToolkit.Models.RH;
using System.Data;
using static RHToolkit.Models.MIP.MIPCoder;

namespace RHToolkit.Models.Editor
{
    public class FileManager
    {
        private static readonly string _appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        private static readonly string _backupDirectory = Path.Combine(_appDataPath, "RHToolkit", "RHEditor", "backup");
        private readonly DataTableCryptor _dataTableCryptor = new();

        public async Task<DataTable?> RHFileToDataTableAsync(string sourceFile)
        {
            using FileStream sourceFileStream = File.OpenRead(sourceFile);
            byte[] buffer = new byte[4096];
            int bytesRead;

            using MemoryStream memoryStream = new();
            while ((bytesRead = await sourceFileStream.ReadAsync(buffer)) > 0)
            {
                await memoryStream.WriteAsync(buffer.AsMemory(0, bytesRead));
            }

            byte[] sourceBytes = memoryStream.ToArray();

            return _dataTableCryptor.RhToDataTable(sourceBytes);
        }

        public async Task DataTableToRHFileAsync(string file, DataTable fileData)
        {
            byte[] encryptedData = _dataTableCryptor.DataTableToRh(fileData);
            await File.WriteAllBytesAsync(file, encryptedData);
        }

        public async Task SaveTempFile(string fileName, DataTable fileData)
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
                string newBackupFileName = $"{fileNameWithoutExtension}@{DateTime.Now:yyyy-MM-dd_HHmm}{fileExtension}";
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

                await DataTableToRHFileAsync(newBackupFilePath, fileData);
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}");
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
                throw new Exception($"{ex.Message}");
            }
        }

        public async Task CompressToMipAsync(DataTable fileData, string file, MIPCompressionMode compressionMode)
        {
            if (compressionMode == MIPCompressionMode.Compress)
            {
                byte[] encryptedData = _dataTableCryptor.DataTableToRh(fileData);
                // Compress file
                byte[] compressedData = CompressFileZlibAsync(encryptedData);

                await File.WriteAllBytesAsync(file, compressedData);
            }
        }

        public static async Task ExportToXMLAsync(DataTable fileData, string file)
        {
            byte[] xmlData = DataTableCryptor.DataTableToXML(fileData);

            await File.WriteAllBytesAsync(file, xmlData);
        }

        public static async Task ExportToXLSXAsync(DataTable fileData, string file)
        {
            byte[] xlsxData = DataTableCryptor.DataTableToXLSX(fileData);

            await File.WriteAllBytesAsync(file, xlsxData);
        }
    }
}
