namespace RHToolkit.Models.Model3D.Model;

public static class MDataModelPathReader
{
    private const string FILE_HEADER = "stairwaygames.";

    public static async Task<MDataModel> ReadAsync(string path, CancellationToken ct = default)
    {
        byte[] bytes = await File.ReadAllBytesAsync(path, ct).ConfigureAwait(false);
        using var ms = new MemoryStream(bytes, writable: false);
        using var br = new BinaryReader(ms, Encoding.Unicode, leaveOpen: false);
        return ReadMData(br);
    }

    private static MDataModel ReadMData(BinaryReader br)
    {
        string header = BinaryReaderExtensions.ReadRHString(br);

        if (header != FILE_HEADER)
        {
            throw new InvalidDataException($"{string.Format(Resources.InvalidFileDesc, "MData")}");
        }

        int nVersion = br.ReadInt32();

        MDataModel mdata = new()
        {
            Version = nVersion,
            MgmPath = BinaryReaderExtensions.ReadRHString(br)
        };

        return mdata;
    }

    public sealed class MDataModel
    {
        public int Version { get; set; }
        public string Path { get; set; } = string.Empty;
        public string MgmPath { get; set; } = string.Empty;
    }
}