using System.IO.Compression;

namespace RHToolkit.Models.MIP;

public class MIPCoder
{
    public enum MIPCompressionMode
    {
        Compress,
        Decompress
    }

    public static byte[] CompressFileZlibAsync(byte[] file)
    {
        byte[] buffer = CompressBytesZlib(file);
        MIP.BytesWithCodeMip(buffer);
        return buffer;
    }

    public static byte[] DecompressFileZlibAsync(byte[] file)
    {
        MIP.BytesWithCodeMip(file);
        byte[] buffer = DecompressBytesZlib(file);
        return buffer;
    }

    private static byte[] CompressBytesZlib(byte[] toBytes)
    {
        using MemoryStream outputStream = new();
        using (ZLibStream zlibStream = new(outputStream, CompressionLevel.Optimal))
        {
            zlibStream.Write(toBytes, 0, toBytes.Length);
        }

        return outputStream.ToArray();
    }

    private static byte[] DecompressBytesZlib(byte[] toBytes)
    {
        using MemoryStream inputStream = new(toBytes);
        using ZLibStream zlibStream = new(inputStream, CompressionMode.Decompress);
        using MemoryStream outputStream = new();
        zlibStream.CopyTo(outputStream);
        return outputStream.ToArray();
    }
}