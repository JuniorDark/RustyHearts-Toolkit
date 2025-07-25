using System.IO.Compression;

namespace RHToolkit.Models.Crypto;

public class ZLibHelper
{
    public enum ZLibOperationMode
    {
        Compress,
        Decompress
    }

    /// <summary>
    /// Compresses a file using Zlib compression asynchronously and applies an XOR cipher.
    /// </summary>
    /// <param name="file">The byte array representing the file to compress.</param>
    /// <returns>The compressed byte array.</returns>
    public static byte[] CompressFileZlibAsync(byte[] file)
    {
        byte[] buffer = CompressBytesZlib(file);
        byte[] numArray = XorCipher.Xor(buffer);
        return numArray;
    }

    /// <summary>
    /// Decompresses a file using Zlib compression asynchronously and applies an XOR cipher.
    /// </summary>
    /// <param name="file">The byte array representing the file to decompress.</param>
    /// <returns>The decompressed byte array.</returns>
    public static byte[] DecompressFileZlibAsync(byte[] file)
    {
        byte[] numArray = XorCipher.Xor(file);
        byte[] buffer = DecompressBytesZlib(numArray);
        return buffer;
    }

    /// <summary>
    /// Compresses a byte array using Zlib compression.
    /// </summary>
    /// <param name="uncompressedBytes">The byte array to compress.</param>
    /// <returns>The compressed byte array.</returns>
    private static byte[] CompressBytesZlib(byte[] uncompressedBytes)
    {
        using MemoryStream outputStream = new();
        using (ZLibStream zlibStream = new(outputStream, CompressionLevel.Optimal))
        {
            zlibStream.Write(uncompressedBytes, 0, uncompressedBytes.Length);
        }

        return outputStream.ToArray();
    }

    /// <summary>
    /// Decompresses a byte array using Zlib compression.
    /// </summary>
    /// <param name="toBytes">The byte array to decompress.</param>
    /// <returns>The decompressed byte array.</returns>
    private static byte[] DecompressBytesZlib(byte[] compressedBytes)
    {
        using MemoryStream inputStream = new(compressedBytes);
        using ZLibStream zlibStream = new(inputStream, CompressionMode.Decompress);
        using MemoryStream outputStream = new();
        zlibStream.CopyTo(outputStream);
        return outputStream.ToArray();
    }
}