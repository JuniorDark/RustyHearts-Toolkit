using System.IO.Compression;

namespace RHToolkit.Models.MIP;

public class MIPCoder
{
    public enum MIPCompressionMode
    {
        Compress,
        Decompress
    }

    /// <summary>
    /// Compresses a file using Zlib compression asynchronously.
    /// </summary>
    /// <param name="file">The byte array representing the file to compress.</param>
    /// <returns>The compressed byte array.</returns>
    public static byte[] CompressFileZlibAsync(byte[] file)
    {
        byte[] buffer = CompressBytesZlib(file);
        MIP.BytesWithCodeMip(buffer);
        return buffer;
    }

    /// <summary>
    /// Decompresses a file using Zlib compression asynchronously.
    /// </summary>
    /// <param name="file">The byte array representing the file to decompress.</param>
    /// <returns>The decompressed byte array.</returns>
    public static byte[] DecompressFileZlibAsync(byte[] file)
    {
        MIP.BytesWithCodeMip(file);
        byte[] buffer = DecompressBytesZlib(file);
        return buffer;
    }

    /// <summary>
    /// Compresses a byte array using Zlib compression.
    /// </summary>
    /// <param name="toBytes">The byte array to compress.</param>
    /// <returns>The compressed byte array.</returns>
    private static byte[] CompressBytesZlib(byte[] toBytes)
    {
        using MemoryStream outputStream = new();
        using (ZLibStream zlibStream = new(outputStream, CompressionLevel.Optimal))
        {
            zlibStream.Write(toBytes, 0, toBytes.Length);
        }

        return outputStream.ToArray();
    }

    /// <summary>
    /// Decompresses a byte array using Zlib compression.
    /// </summary>
    /// <param name="toBytes">The byte array to decompress.</param>
    /// <returns>The decompressed byte array.</returns>
    private static byte[] DecompressBytesZlib(byte[] toBytes)
    {
        using MemoryStream inputStream = new(toBytes);
        using ZLibStream zlibStream = new(inputStream, CompressionMode.Decompress);
        using MemoryStream outputStream = new();
        zlibStream.CopyTo(outputStream);
        return outputStream.ToArray();
    }
}