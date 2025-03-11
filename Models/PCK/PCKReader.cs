using System.Diagnostics;
using static RHToolkit.Models.MIP.MIPCoder;

namespace RHToolkit.Models.PCK;

public class PCKReader
{
    public static bool IsGameDirectory(string gameDirectory)
    {
        string fileF00XDAT = Path.Combine(gameDirectory, "f00X.dat");
        return File.Exists(fileF00XDAT);
    }

    /// <summary>
    /// Read PCK file list
    /// </summary>
    /// <returns></returns>
    public static List<PCKFile> ReadPCKFileList(string gameDirectory, CancellationToken cancellationToken)
    {
        string pckFileList = Path.Combine(gameDirectory, "f00X.dat");

        byte[] compressedBytes = File.ReadAllBytes(pckFileList);

        if (compressedBytes.Length == 0) return [];

        byte[] decompressedBytes = DecompressFileZlibAsync(compressedBytes);
        if (decompressedBytes.Length == 0) throw new Exception(Resources.PCKTool_InvalidFileList);

        List<PCKFile> listPck = new(100000);
        using (MemoryStream ms = new(decompressedBytes, 0, decompressedBytes.Length, false))
        {
            using BinaryReader br = new(ms);
            while (br.BaseStream.Position < br.BaseStream.Length)
            {
                cancellationToken.ThrowIfCancellationRequested();
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
                        if (name.Contains('/')
                            || name.Contains(':')
                            || name.Contains('*')
                            || name.Contains('?')
                            || name.Contains('<')
                            || name.Contains('>'))
                        {
                            Debug.WriteLine("Invalid name:" + name);
                            continue;
                        }

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
    /// unpack file list
    /// </summary>
    /// <param name="listPckFile"></param>
    public static void UnpackPCK(List<PCKFile> listPckFile, bool replaceUnpack, string gameDirectory, Action<string> reportProgress, Action<int, int> unpackProgress, CancellationToken cancellationToken)
    {
        Dictionary<int, FileStream> archives = [];
        Dictionary<int, BinaryReader> readers = [];

        try
        {
            string[] pckFiles = Directory.GetFiles(gameDirectory, "*.pck");

            foreach (string pckFile in pckFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();

                FileStream ofs = new(pckFile, FileMode.Open, FileAccess.Read);
                BinaryReader obr = new(ofs);
                int key = int.Parse(Path.GetFileNameWithoutExtension(pckFile));
                archives.Add(key, ofs);
                readers.Add(key, obr);
            }

            int count = listPckFile.Count;
            for (int i = 0; i < count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                PCKFile pckFile = listPckFile[i];
                unpackProgress(i + 1, count);

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

                        cancellationToken.ThrowIfCancellationRequested();

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
            foreach (var kvp in archives)
            {
                kvp.Value.Close();
                kvp.Value.Dispose();
            }
        }
    }

    /// <summary>
    /// organize PCKFile list into a tree structure
    /// </summary>
    /// <param name="listPckFile"></param>
    /// <returns></returns>
    public static SortedDictionary<string, PCKFileNode> ConvertListToNode(List<PCKFile> listPckFile)
    {
        TrieNode rootNode = new("PCK");
        SortedDictionary<string, PCKFileNode> result = new(new SortstringComparer());

        foreach (PCKFile pckFile in listPckFile)
        {
            string[] elements = pckFile.PathElements;
            TrieNode curNode = rootNode;

            // Traverse the Trie to find the node corresponding to the path elements
            foreach (string element in elements)
            {
                if (!curNode.Children.TryGetValue(element, out TrieNode? childNode))
                {
                    // Create a new node if it doesn't already exist
                    childNode = new TrieNode(element);
                    curNode.Children.Add(element, childNode);
                }
                curNode = childNode;
            }

            // Assign the PCKFile to the leaf node
            curNode.PCKFile = pckFile;
        }

        // Traverse the Trie to create the PCKFileNode tree structure
        Stack<(TrieNode node, SortedDictionary<string, PCKFileNode> parentNodes)> stack = new();
        stack.Push((rootNode, result));
        while (stack.Count > 0)
        {
            (TrieNode node, SortedDictionary<string, PCKFileNode> parentNodes) = stack.Pop();

            PCKFileNode newNode = new(node.Name, node.PCKFile!);
            parentNodes.Add(node.Name, newNode);

            if (node.Children.Count > 0)
            {
                SortedDictionary<string, PCKFileNode> childNodes = new(new SortstringComparer());
                newNode.Nodes = childNodes;

                foreach (TrieNode childNode in node.Children.Values)
                {
                    stack.Push((childNode, childNodes));
                }
            }
        }

        return result;
    }

}
