using static RHToolkit.Models.Crypto.ZLibHelper;

namespace RHToolkit.Models.PCK;

/// <summary>
/// Provides methods to read PCK files.
/// </summary>
public class PCKReader
{
    /// <summary>
    /// Reads the PCK file list 'f00X.dat' from the specified directory.
    /// </summary>
    /// <returns>A list of PCKFile objects representing the files in the PCK archive.</returns>
    public static async Task<List<PCKFile>> ReadPCKFileListAsync(string gameDirectory, CancellationToken ct)
    {
        string pckFileList = Path.Combine(gameDirectory, "f00X.dat");

        if (!File.Exists(pckFileList))
        {
            return [];
        }

        byte[] compressedBytes = await File.ReadAllBytesAsync(pckFileList, ct);

        if (compressedBytes.Length == 0) return [];

        byte[] decompressedBytes = DecompressFileZlibAsync(compressedBytes);
        if (decompressedBytes.Length == 0) throw new InvalidDataException($"{Resources.PCKTool_InvalidFileList}");

        List<PCKFile> listPck = new(100000);
        using (MemoryStream ms = new(decompressedBytes, 0, decompressedBytes.Length, false))
        {
            using BinaryReader br = new(ms);
            while (br.BaseStream.Position < br.BaseStream.Length)
            {
                string? name = null;
                try
                {
                    ushort len = br.ReadUInt16();
                    byte[] byteName = br.ReadBytes(len * 2);

                    name = Encoding.Unicode.GetString(byteName); // File name
                    byte archive = br.ReadByte(); // Which pck archive is it in
                    int size = br.ReadInt32(); // Size of the file
                    uint hash = br.ReadUInt32(); // crc32 hash
                    long offset = br.ReadInt64(); // Offset in the pck file

                    if (name.Length > 0)
                    {
                        PCKFile file = new(name, archive, size, hash, offset);
                        listPck.Add(file);
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    throw new Exception($"{Resources.Error} {name}\n{ex}");
                }
            }
        }

        return listPck;
    }

    /// <summary>
    /// Unpacks the PCK files to the specified directory.
    /// </summary>
    /// <param name="listPckFile"></param>
    public static void UnpackPCK(List<PCKFile> listPckFile, bool replaceUnpack, string gameDirectory, IProgress<(int pos, int count)> progress, CancellationToken ct)
    {
        Dictionary<int, FileStream> pckArchives = [];
        Dictionary<int, BinaryReader> readers = [];

        try
        {
            string[] pckFiles = Directory.GetFiles(gameDirectory, "*.pck");

            foreach (string pckFile in pckFiles)
            {
                ct.ThrowIfCancellationRequested();

                FileStream ofs = new(pckFile, FileMode.Open, FileAccess.Read);
                BinaryReader obr = new(ofs);
                int key = int.Parse(Path.GetFileNameWithoutExtension(pckFile));
                pckArchives.Add(key, ofs);
                readers.Add(key, obr);
            }

            int count = listPckFile.Count;
            for (int i = 0; i < count; i++)
            {
                ct.ThrowIfCancellationRequested();

                PCKFile pckFile = listPckFile[i];
                progress.Report((pos: i + 1, count));

                try
                {
                    string filePath = Path.Combine(gameDirectory, "PckOutput", pckFile.Name);
                    Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? throw new InvalidOperationException("Directory path is null"));

                    bool canWrite = !File.Exists(filePath) || replaceUnpack;

                    if (canWrite)
                    {
                        using FileStream ofs = new(filePath, FileMode.Create, FileAccess.Write);
                        using BinaryWriter bw = new(ofs);

                        BinaryReader br = readers[pckFile.Archive];
                        br.BaseStream.Position = pckFile.Offset;
                        byte[] fileBytes = br.ReadBytes(pckFile.FileSize);

                        ct.ThrowIfCancellationRequested();

                        bw.Write(fileBytes);
                        bw.Flush();
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    throw new Exception($"{Resources.Error}: {pckFile.Name}\n{ex}");
                }
            }
        }
        finally
        {
            foreach (var kvp in pckArchives)
            {
                kvp.Value.Close();
                kvp.Value.Dispose();
            }
        }
    }
}
