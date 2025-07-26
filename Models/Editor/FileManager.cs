using RHToolkit.Models.RH;
using System.Data;
using static RHToolkit.Models.Crypto.ZLibHelper;

namespace RHToolkit.Models.Editor
{
    /// <summary>
    /// Manages file operations including reading, writing, and converting rh data to files.
    /// </summary>
    public class FileManager
    {
        private static readonly string _appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        private static readonly string _backupDirectory = Path.Combine(_appDataPath, "RHToolkit", "RHEditor", "backup");
        private readonly DataTableCryptor _dataTableCryptor = new();

        /// <summary>
        /// Converts an RH file to a DataTable.
        /// </summary>
        /// <param name="sourceFile">The path to the source RH file.</param>
        /// <returns>A DataTable containing the data from the RH file.</returns>
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

        /// <summary>
        /// Converts a DataTable to an RH file.
        /// </summary>
        /// <param name="file">The path to the destination RH file.</param>
        /// <param name="fileData">The DataTable containing the data to write.</param>
        public async Task DataTableToRHFileAsync(string file, DataTable fileData)
        {
            byte[] encryptedData = _dataTableCryptor.DataTableToRh(fileData);
            await File.WriteAllBytesAsync(file, encryptedData);
        }

        /// <summary>
        /// Converts a byte array representing an RH file to a DataTable.
        /// </summary>
        /// <param name="sourceBytes"></param>
        /// <returns>A DataTable containing the data from the RH file represented by the byte array.</returns>
        public DataTable RHFileDataToDataTableAsync(byte[] sourceBytes)
        {
            return _dataTableCryptor.RhToDataTable(sourceBytes);
        }

        /// <summary>
        /// Converts a DataTable to a byte array representing an RH file.
        /// </summary>
        /// <param name="fileData"></param>
        /// <returns>A byte array containing the data from the DataTable in RH file format.</returns>
        public async Task<byte[]> DataTableDataToRHFileAsync(DataTable fileData)
        {
            byte[] encryptedData = _dataTableCryptor.DataTableToRh(fileData);
            return await Task.FromResult(encryptedData);
        }

        /// <summary>
        /// Saves a temporary backup of a DataTable to a file.
        /// </summary>
        /// <param name="fileName">The name of the file to save.</param>
        /// <param name="fileData">The DataTable containing the data to save.</param>
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

        /// <summary>
        /// Clears temporary backup files for a specified file name.
        /// </summary>
        /// <param name="fileName">The name of the file whose backups should be cleared.</param>
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

        /// <summary>
        /// Compresses a DataTable to a MIP file.
        /// </summary>
        /// <param name="fileData">The DataTable containing the data to compress.</param>
        /// <param name="file">The path to the destination MIP file.</param>
        /// <param name="compressionMode">The compression mode to use.</param>
        public async Task CompressToMipAsync(DataTable fileData, string file, ZLibOperationMode compressionMode)
        {
            if (compressionMode == ZLibOperationMode.Compress)
            {
                byte[] encryptedData = _dataTableCryptor.DataTableToRh(fileData);
                // Compress file
                byte[] compressedData = CompressFileZlibAsync(encryptedData);

                await File.WriteAllBytesAsync(file, compressedData);
            }
        }

        /// <summary>
        /// Exports a DataTable to an XML file.
        /// </summary>
        /// <param name="fileData">The DataTable containing the data to export.</param>
        /// <param name="file">The path to the destination XML file.</param>
        public static async Task ExportToXMLAsync(DataTable fileData, string file)
        {
            byte[] xmlData = DataTableCryptor.DataTableToXML(fileData);

            await File.WriteAllBytesAsync(file, xmlData);
        }

        /// <summary>
        /// Exports a DataTable to an XLSX file.
        /// </summary>
        /// <param name="fileData">The DataTable containing the data to export.</param>
        /// <param name="file">The path to the destination XLSX file.</param>
        public static async Task ExportToXLSXAsync(DataTable fileData, string file)
        {
            byte[] xlsxData = DataTableCryptor.DataTableToXLSX(fileData);

            await File.WriteAllBytesAsync(file, xlsxData);
        }

        /// <summary>
        /// Compresses a byte array to a MIP file.
        /// </summary>
        /// <param name="fileData">The Wdata containing the data to compress.</param>
        /// <param name="file">The path to the destination MIP file.</param>
        /// <param name="compressionMode">The compression mode to use.</param>
        public static async Task CompressFileToMipAsync(byte[] fileData, string file)
        {
            // Compress file
            byte[] compressedData = CompressFileZlibAsync(fileData);

            await File.WriteAllBytesAsync(file, compressedData);
        }
    }
}