using System.Data;
using static RHToolkit.Models.Crypto.CRC32;
using static RHToolkit.Models.Crypto.ZLibHelper;

namespace RHToolkit.Models.PCK;

/// <summary>
/// Handles writing PCK files and the f00X.dat file.
/// </summary>
public sealed class PCKWriter : IDisposable
{
    private static ReaderWriterLockSlim[]? _pckArchiveLocks;
    private bool _disposed;

    /// <summary>
    /// Initializes the PCK archive locks.
    /// </summary>
    public static void InitializePCKArchiveLocks(int count = 10)
    {
        DisposePCKArchiveLocks();

        _pckArchiveLocks = [.. Enumerable.Range(0, count).Select(_ => new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion))];
    }

    /// <summary>
    /// Disposes the archive locks to release unmanaged resources.
    /// </summary>
    public static void DisposePCKArchiveLocks()
    {
        if (_pckArchiveLocks != null)
        {
            foreach (var rwLock in _pckArchiveLocks)
            {
                rwLock?.Dispose();
            }

            _pckArchiveLocks = null;
        }
    }

    /// <summary>
    /// Disposes this writer and its shared resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        DisposePCKArchiveLocks();
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Writes PCK files based on the provided dictionary of files to pack.
    /// </summary>
    /// <param name="gameDirectory"></param>
    /// <param name="filesToPackDict"></param>
    /// <param name="progress"></param>
    /// <param name="createArchivesIfMissing"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public static async Task WritePCKFilesAsync(
    string gameDirectory,
    SortedDictionary<string, string> filesToPackDict,
    IProgress<(string file, int pos, int count)> progress,
    bool createArchivesIfMissing,
    CancellationToken ct)
    {
        var pckDict = await PCKReader.GetPCKSortedDictionaryAsync(gameDirectory);

        if (pckDict.Count == 0 && createArchivesIfMissing)
            await CreateEmptyArchives(gameDirectory, ct);

        try
        {
            await Task.Run(async () =>
            {
                if (_pckArchiveLocks == null || _pckArchiveLocks.Length == 0)
                    InitializePCKArchiveLocks();

                int pos = 0, count = filesToPackDict.Count;

                foreach (var (relPath, fullPath) in filesToPackDict)
                {
                    ct.ThrowIfCancellationRequested();
                    pos++;
                    progress.Report((relPath, pos, count));

                    byte archive = GetArchiveNumberFromName(relPath);

                    using FileStream fs = File.OpenRead(fullPath);
                    using MemoryStream ms = new();
                    await fs.CopyToAsync(ms);
                    byte[] fileData = ms.ToArray();
                    uint hash = await ComputeCrc32HashAsync(fullPath, ct);

                    int offset;
                    // Check if the file already exists in the PCK dictionary
                    if (pckDict.TryGetValue(relPath, out var existing))
                    {
                        offset = fileData.Length > existing.FileSize
                            ? WriteToPCKArchive(gameDirectory, fileData, archive, true, 0)
                            : WriteToPCKArchive(gameDirectory, fileData, archive, false, (int)existing.Offset);

                        existing.FileSize = fileData.Length;
                        existing.Hash = hash;
                        existing.Offset = (uint)offset;
                    }
                    // If it doesn't exist, create a new entry
                    else
                    {
                        offset = WriteToPCKArchive(gameDirectory, fileData, archive, true, 0);
                        pckDict[relPath] = new PCKFile(relPath, archive, fileData.Length, hash, offset);
                    }
                }

                await WriteF00XDAT(gameDirectory, pckDict);
            }, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            await WriteF00XDAT(gameDirectory, pckDict);
            throw;
        }
    }

    /// <summary>
    /// Calculates the archive number based on the file name using the DJB2 hash algorithm.
    /// </summary>
    /// <param name="name"></param>
    /// <returns>The archive number (0-9) derived from the file name.</returns>
    public static byte GetArchiveNumberFromName(string name)
    {
        uint pckNum = 5381;
        foreach (char c in name)
            pckNum = (byte)c + 33 * pckNum;
        return (byte)(pckNum % 10);
    }

    /// <summary>
    /// Creates empty PCK archive files in the specified directory.
    /// </summary>
    /// <param name="dir"></param>
    /// <param name="ct"></param>
    private static async Task CreateEmptyArchives(string dir, CancellationToken ct)
    {
        for (int i = 0; i < 10; i++)
        {
            string pckPath = Path.Combine(dir, $"00{i}.pck");
            if (!File.Exists(pckPath))
            {
                await File.WriteAllBytesAsync(pckPath, [], ct);
            }
        }
    }

    /// <summary>
    /// Writes the f00X.dat file containing the list of PCK files.
    /// </summary>
    /// <param name="directory"></param>
    /// <param name="dicPckFile"></param>
    public static async Task WriteF00XDAT(string directory, SortedDictionary<string, PCKFile> dicPckFile)
    {
        byte[]? bufferF00XDAT = null;
        using (MemoryStream streamF00X = new())
        {
            using BinaryWriter writerF00X = new(streamF00X);

            foreach (KeyValuePair<string, PCKFile> kvFile in dicPckFile)
            {
                var pckFile = kvFile.Value;

                writerF00X.Write((ushort)kvFile.Key.Length);
                writerF00X.Write(Encoding.Unicode.GetBytes(kvFile.Key));
                writerF00X.Write(pckFile.Archive);
                writerF00X.Write((uint)pckFile.FileSize);
                writerF00X.Write(pckFile.Hash);
                writerF00X.Write((ulong)pckFile.Offset);
            }
            writerF00X.Flush();
            bufferF00XDAT = streamF00X.ToArray();
        }

        byte[] compressedData = CompressFileZlibAsync(bufferF00XDAT);

        string pathF00XDAT = Path.Combine(directory, "f00X.dat");
        if (File.Exists(pathF00XDAT + ".old"))
        {
            File.Delete(pathF00XDAT + ".old");
        }
        if (File.Exists(pathF00XDAT))
        {
            File.Move(pathF00XDAT, pathF00XDAT + ".old");
        }

        await File.WriteAllBytesAsync(pathF00XDAT, compressedData);
    }

    /// <summary>
    /// Writes a byte array to a PCK archive file at the specified offset or at the end of the file.
    /// Uses a write lock to ensure thread safety when accessing the archive.
    /// </summary>
    /// <param name="fileBytes"></param>
    /// <param name="archiveNum"></param>
    /// <param name="eof"></param>
    /// <param name="offset"></param>
    /// <returns>The position in the file where the data was written.</returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static int WriteToPCKArchive(string directory, byte[] fileBytes, byte archiveNum, bool eof, long offset)
    {
        if (_pckArchiveLocks == null || archiveNum >= _pckArchiveLocks.Length)
            throw new InvalidOperationException("PCK archive locks not initialized.");

        var rwLock = _pckArchiveLocks[archiveNum];

        rwLock.EnterWriteLock();
        try
        {
            string filePath = Path.Combine(directory, $"00{archiveNum}.pck");

            using FileStream fileStream = new(filePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
            fileStream.Seek(eof ? 0 : offset, eof ? SeekOrigin.End : SeekOrigin.Begin);
            int position = (int)fileStream.Position;

            fileStream.Write(fileBytes, 0, fileBytes.Length);
            fileStream.Flush();
            return position;
        }
        finally
        {
            rwLock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Saves a rh file byte array to the PCK archive.
    /// </summary>
    /// <param name="gameDirectory"></param>
    /// <param name="fileNameInPck"></param>
    /// <param name="fileData"></param>
    /// <exception cref="FileNotFoundException"></exception>
    public static async Task SaveFileToPCKAsync(string gameDirectory, string fileNameInPck, byte[] fileData)
    {
        if (_pckArchiveLocks is null || _pckArchiveLocks.Length == 0)
            InitializePCKArchiveLocks();

        // Get the existing file list
        var pckDict = await PCKReader.GetPCKSortedDictionaryAsync(gameDirectory);

        // Match by exact file name only
        var fileEntryPair = pckDict.FirstOrDefault(f =>
            string.Equals(Path.GetFileName(f.Value.Name), fileNameInPck, StringComparison.Ordinal));

        if (fileEntryPair.Value is null)
            throw new FileNotFoundException(string.Format(Resources.PCKTool_FileNotFoundInList, fileNameInPck));

        var fileEntry = fileEntryPair.Value;
        string archivePath = Path.Combine(gameDirectory, $"{fileEntry.Archive:D3}.pck");

        if (!File.Exists(archivePath))
            throw new FileNotFoundException(string.Format(Resources.PCKTool_ArchiveNotFound, archivePath));

        // Write the new file data to the archive
        int newOffset = fileData.Length > fileEntry.FileSize
            ? WriteToPCKArchive(gameDirectory, fileData, fileEntry.Archive, eof: true, offset: 0)
            : WriteToPCKArchive(gameDirectory, fileData, fileEntry.Archive, eof: false, offset: fileEntry.Offset);

        // Update the metadata
        fileEntry.Offset = newOffset;
        fileEntry.FileSize = fileData.Length;
        fileEntry.Hash = ComputeCrc32Hash(fileData);

        // Save updated f00X.dat
        await WriteF00XDAT(gameDirectory, pckDict);
    }

}
