namespace RHToolkit.Models.PCK;

/// <summary>
/// Represents a file in a PCK archive.
/// </summary>
/// <param name="name"></param>
/// <param name="archive"></param>
/// <param name="size"></param>
/// <param name="hash"></param>
/// <param name="offset"></param>
public sealed class PCKFile(string name, byte archive, int size, uint hash, long offset)
{
    public string Name { get; set; } = name;
    public byte Archive { get; set; } = archive;
    public int FileSize { get; set; } = size;
    public uint Hash { get; set; } = hash;
    public long Offset { get; set; } = offset;
    public bool IsChecked { get; set; }
    public string[] PathElements { get { return Name.Split(['\\']); } }
}
