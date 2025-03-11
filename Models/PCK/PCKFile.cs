namespace RHToolkit.Models.PCK;

public class PCKFile(string name, byte archive, int size, uint hash, long offset)
{
    public string Name { get; private set; } = name;

    public byte Archive { get; private set; } = archive;

    public int FileSize { get; private set; } = size;

    public uint Hash { get; private set; } = hash;

    public long Offset { get; private set; } = offset;

    public bool IsChecked { get; set; }

    public string[] PathElements { get { return Name.Split(['\\']); } }
}
