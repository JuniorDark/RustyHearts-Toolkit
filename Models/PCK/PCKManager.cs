using RHToolkit.Utilities;
using System.Collections.Concurrent;
using static RHToolkit.Models.Crypto.CRC32;

namespace RHToolkit.Models.PCK;

public class PCKManager
{
    /// <summary>
    /// Checks if the specified directory is a valid game directory by looking for the f00X.dat file.
    /// </summary>
    /// <param name="gameDirectory"></param>
    /// <returns>True if the directory is a game directory, false otherwise.</returns>
    public static bool IsGameDirectory(string gameDirectory)
        => File.Exists(Path.Combine(gameDirectory, "f00X.dat"));

    /// <summary>
    /// Retrieves a list of PCK files that are checked in the provided nodes.
    /// </summary>
    /// <param name="nodes"></param>
    /// <returns>List of PCKFile objects that are checked.</returns>
    public static List<PCKFile> GetCheckedFiles(IEnumerable<PCKFileNodeViewModel> nodes)
    {
        var list = new List<PCKFile>();
        foreach (var n in nodes)
        {
            if (n.IsChecked == true && !n.IsDir) list.Add(n.PckFile!);
            if (n.Children.Count > 0) list.AddRange(GetCheckedFiles(n.Children));
        }
        return list;
    }

    /// <summary>
    /// Enumerates files in the specified directory and filters them based on the provided PCK map.
    /// </summary>
    /// <param name="baseDir"></param>
    /// <param name="pckMap"></param>
    /// <param name="ct"></param>
    /// <returns>A sorted dictionary where the key is the relative file path and the value is the full file path.</returns>
    public static async Task<SortedDictionary<string, string>> EnumerateAndFilterFilesToPackAsync(
    string baseDir,
    IReadOnlyDictionary<string, PCKFile> pckMap,
    CancellationToken ct)
    {
        var dictionary = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var files = await Task.Run(() =>
        {
            return Directory.EnumerateFiles(baseDir, "*", SearchOption.AllDirectories)
                .Where(file =>
                {
                    var ext = Path.GetExtension(file);
                    return !file.EndsWith(".mip", StringComparison.OrdinalIgnoreCase) &&
                           !file.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) &&
                           !file.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) &&
                           !file.EndsWith(".usm", StringComparison.OrdinalIgnoreCase);
                })
                .ToList();
        }, ct).ConfigureAwait(false);

        await Parallel.ForEachAsync(files, new ParallelOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount,
            CancellationToken = ct
        }, async (file, token) =>
        {
            try
            {
                var rel = Path.GetRelativePath(baseDir, file);
                var info = new FileInfo(file);

                if (info.Length == 0)
                    return;

                if (!pckMap.TryGetValue(rel, out var existing))
                {
                    dictionary[rel] = file;
                    return;
                }

                if (existing.FileSize != info.Length)
                {
                    dictionary[rel] = file;
                    return;
                }

                uint hash = await ComputeCrc32HashAsync(file, token);
                if (existing.Hash != hash)
                {
                    dictionary[rel] = file;
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
        });

        return new SortedDictionary<string, string>(dictionary, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Builds a tree structure from a list of PCK files.
    /// </summary>
    /// <param name="files"></param>
    /// <param name="progress"></param>
    /// <param name="ct"></param>
    /// <returns>ObservableCollection of PCKFileNodeViewModel representing the tree structure.</returns>
    public static async Task<ObservableCollection<PCKFileNodeViewModel>> BuildTreeAsync(
    List<PCKFile> files,
    IProgress<int>? progress,
    CancellationToken ct)
    {
        var tree = new ObservableCollection<PCKFileNodeViewModel>();
        if (files.Count == 0) return tree;

        await Task.Run(async () =>
        {
            ct.ThrowIfCancellationRequested();

            var rootDict = ConvertListToNode(files);

            // Make AddNodes async to await Dispatcher.InvokeAsync
            async Task AddNodesAsync(SortedDictionary<string, PCKFileNode> listNode, ObservableCollection<PCKFileNodeViewModel> treeNodes)
            {
                foreach (var kv in listNode)
                {
                    ct.ThrowIfCancellationRequested();

                    var node = new PCKFileNodeViewModel
                    {
                        Name = kv.Value.IsDir
                        ? kv.Key
                        : kv.Value.PCKFile != null
                            ? $"{kv.Key} [{FileSizeFormatter.FormatFileSize(kv.Value.PCKFile.FileSize)}] {kv.Value.PCKFile.Archive}"
                            : kv.Key,
                        IsDir = kv.Value.IsDir,
                        PckFile = kv.Value.PCKFile,
                    };

                    if (kv.Value.Nodes is { Count: > 0 })
                        await AddNodesAsync(kv.Value.Nodes, node.Children);

                    if (!kv.Value.IsDir)
                        progress?.Report(1);

                    ct.ThrowIfCancellationRequested();

                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        treeNodes.Add(node);
                    });
                }
            }

            await AddNodesAsync(rootDict, tree);
        }, ct).ConfigureAwait(false);

        return tree;
    }

    /// <summary>
    /// Organize PCKFile list into a tree structure
    /// </summary>
    /// <param name="listPckFile"></param>
    /// <returns>SortedDictionary of PCKFileNode</returns>
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

    /// <summary>
    /// Filters the PCK file tree based on the search text.
    /// </summary>
    /// <param name="originalTree"></param>
    /// <param name="searchText"></param>
    /// <returns>Filtered ObservableCollection of PCKFileNodeViewModel</returns>
    public static ObservableCollection<PCKFileNodeViewModel> FilterTree(
    ObservableCollection<PCKFileNodeViewModel> originalTree,
    string searchText)
    {
        ObservableCollection<PCKFileNodeViewModel> filteredTree = [];

        foreach (var node in originalTree)
        {
            var filteredNode = FilterNode(node, searchText);
            if (filteredNode != null)
                filteredTree.Add(filteredNode);
        }

        return filteredTree;
    }

    private static PCKFileNodeViewModel? FilterNode(PCKFileNodeViewModel node, string searchText)
    {
        var matchedChildren = new ObservableCollection<PCKFileNodeViewModel>();

        foreach (var child in node.Children)
        {
            var filteredChild = FilterNode(child, searchText);
            if (filteredChild != null)
                matchedChildren.Add(filteredChild);
        }

        bool isMatch = node.Name?.Contains(searchText, StringComparison.OrdinalIgnoreCase) == true;

        if (isMatch || matchedChildren.Count > 0)
        {
            return new PCKFileNodeViewModel
            {
                Name = node.Name,
                IsDir = node.IsDir,
                PckFile = node.PckFile,
                Children = matchedChildren
            };
        }

        return null;
    }

}