using static RHToolkit.Models.Crypto.ZLibHelper;

namespace RHToolkit.Models.PCK;

/// <summary>
/// Provides methods to read and unpack PCK files.
/// </summary>
public sealed class PCKReader(string gameDirectory) : IDisposable
{
    private readonly Dictionary<int, FileStream> _pckArchives = [];
    private readonly Dictionary<int, BinaryReader> _readers = [];
    private readonly string _gameDirectory = gameDirectory ?? throw new ArgumentNullException(nameof(gameDirectory));
    private bool _disposed;

    /// <summary>
    /// Loads and opens all PCK archives in the given game directory.
    /// </summary>
    public void OpenArchives(CancellationToken ct = default)
    {
        foreach (string pckFile in Directory.GetFiles(_gameDirectory, "*.pck"))
        {
            ct.ThrowIfCancellationRequested();

            int key = int.Parse(Path.GetFileNameWithoutExtension(pckFile));
            if (_pckArchives.ContainsKey(key)) continue;

            var fs = new FileStream(pckFile, FileMode.Open, FileAccess.Read, FileShare.Read);
            var br = new BinaryReader(fs);

            _pckArchives[key] = fs;
            _readers[key] = br;
        }
    }

    /// <summary>
    /// Extracts files from the PCK list into the PckOutput folder.
    /// </summary>
    public void UnpackPCK(List<PCKFile> listPckFile, bool replaceUnpack, IProgress<(int pos, int count)> progress, CancellationToken ct)
    {
        OpenArchives(ct);

        int count = listPckFile.Count;
        for (int i = 0; i < count; i++)
        {
            ct.ThrowIfCancellationRequested();

            var pckFile = listPckFile[i];
            progress.Report((i + 1, count));

            try
            {
                string filePath = Path.Combine(_gameDirectory, "PckOutput", pckFile.Name);
                string? dirPath = Path.GetDirectoryName(filePath);

                if (string.IsNullOrWhiteSpace(dirPath))
                    throw new InvalidOperationException("Output directory path is null.");

                Directory.CreateDirectory(dirPath);

                bool canWrite = !File.Exists(filePath) || replaceUnpack;
                if (!canWrite) continue;

                BinaryReader br = _readers[pckFile.Archive];
                br.BaseStream.Seek(pckFile.Offset, SeekOrigin.Begin);

                byte[] fileBytes = br.ReadBytes(pckFile.FileSize);
                ct.ThrowIfCancellationRequested();

                using FileStream outFs = new(filePath, FileMode.Create, FileAccess.Write);
                outFs.Write(fileBytes, 0, fileBytes.Length);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"{Resources.Error}: {pckFile.Name}\n{ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// Releases all file resources used by the reader.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        foreach (var br in _readers.Values)
            br.Dispose();
        foreach (var fs in _pckArchives.Values)
            fs.Dispose();

        _readers.Clear();
        _pckArchives.Clear();

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Reads the PCK file list from the f00X.dat file and returns a list of PCKFile objects.
    /// </summary>
    /// <param name="gameDirectory"></param>
    /// <param name="ct"></param>
    /// <returns>List of PCKFile objects.</returns>
    /// <exception cref="InvalidDataException"></exception>
    /// <exception cref="Exception"></exception>
    public static async Task<List<PCKFile>> ReadPCKFileListAsync(string gameDirectory, CancellationToken ct = default)
    {
        string pckFileList = Path.Combine(gameDirectory, "f00X.dat");

        if (!File.Exists(pckFileList))
            return [];

        byte[] compressedBytes = await File.ReadAllBytesAsync(pckFileList, ct);
        if (compressedBytes.Length == 0) return [];

        byte[] decompressedBytes = DecompressFileZlibAsync(compressedBytes);
        if (decompressedBytes.Length == 0)
            throw new InvalidDataException($"{Resources.PCKTool_InvalidFileList}");

        var listPck = new List<PCKFile>(100000);
        using MemoryStream ms = new(decompressedBytes);
        using BinaryReader br = new(ms);

        while (br.BaseStream.Position < br.BaseStream.Length)
        {
            string? name = null;
            try
            {
                ct.ThrowIfCancellationRequested();

                ushort len = br.ReadUInt16();
                byte[] byteName = br.ReadBytes(len * 2);
                name = Encoding.Unicode.GetString(byteName); // File name

                byte archive = br.ReadByte(); // Which pck archive is it in
                int size = br.ReadInt32(); // Size of the file
                uint hash = br.ReadUInt32(); // crc32 hash
                long offset = br.ReadInt64(); // Offset in the pck file

                byte correctArchive = PCKWriter.GetArchiveNumberFromName(name);

                if (!string.IsNullOrWhiteSpace(name))
                {
                    listPck.Add(new PCKFile(name, correctArchive, size, hash, offset));
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"{Resources.Error} {name}\n{ex.Message}", ex);
            }
        }

        return listPck;
    }

    /// <summary>
    /// Reads the PCK file list and returns a sorted dictionary of PCKFile objects.
    /// </summary>
    /// <param name="gameDirectory"></param>
    /// <returns>SortedDictionary with file names as keys and PCKFile objects as values.</returns>
    public static async Task<SortedDictionary<string, PCKFile>> GetPCKSortedDictionaryAsync(string gameDirectory)
    {
        var pckFilesList = await ReadPCKFileListAsync(gameDirectory);
        var pckDict = new SortedDictionary<string, PCKFile>();
        foreach (var pf in pckFilesList)
            pckDict[pf.Name] = pf;
        return pckDict;
    }

    /// <summary>
    /// Loads a file from the PCK archive.
    /// </summary>
    /// <param name="gameDirectory"></param>
    /// <param name="fileNameInPck"></param>
    /// <returns></returns>
    /// <exception cref="FileNotFoundException"></exception>
    /// <exception cref="InvalidDataException"></exception>
    public static async Task<byte[]> LoadFileFromPCKAsync(string gameDirectory, string fileNameInPck)
    {
        var pckFiles = await ReadPCKFileListAsync(gameDirectory);
        var fileEntry = pckFiles.FirstOrDefault(f =>
            string.Equals(Path.GetFileName(f.Name), fileNameInPck, StringComparison.OrdinalIgnoreCase)) ??
            throw new FileNotFoundException(string.Format(Resources.PCKTool_FileNotFoundInList, fileNameInPck));
        string archivePath = Path.Combine(gameDirectory, $"{fileEntry.Archive:D3}.pck");

        if (!File.Exists(archivePath))
            throw new FileNotFoundException(string.Format(Resources.PCKTool_ArchiveNotFound, archivePath));

        byte[] buffer = new byte[fileEntry.FileSize];

        using FileStream fs = new(archivePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        fs.Seek(fileEntry.Offset, SeekOrigin.Begin);
        int read = await fs.ReadAsync(buffer.AsMemory(0, fileEntry.FileSize));

        if (read != fileEntry.FileSize)
            throw new InvalidDataException(Resources.PCKTool_CantReadFile);

        return buffer;
    }
}
