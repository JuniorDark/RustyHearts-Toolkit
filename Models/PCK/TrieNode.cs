namespace RHToolkit.Models.PCK;

public class TrieNode(string name)
{
    public string Name { get; private set; } = name;
    public PCKFile? PCKFile { get; set; }
    public Dictionary<string, TrieNode> Children { get; private set; } = [];
}
