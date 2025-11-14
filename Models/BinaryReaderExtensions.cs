using System.Diagnostics;
using System.Numerics;

namespace RHToolkit.Models;

public class BinaryReaderExtensions
{
    #region Strings

    /// <summary>
    /// Read a 16-bit length-prefixed Unicode string (UTF-16LE) from the binary stream.
    /// </summary>
    /// <param name="br"></param>
    /// <param name="emptyIfDot"></param>
    /// <returns> Returns the string read from the stream, or an empty string if the read value is ".\".</returns>
    public static string ReadRHString(BinaryReader br, bool emptyIfDot = false)
    {
        int charCount = br.ReadUInt16();
        if (charCount == 0) return string.Empty;

        var bytes = br.ReadBytes(charCount * 2);
        var s = Encoding.Unicode.GetString(bytes).TrimEnd('\0');

        return emptyIfDot && s == ".\\" ? string.Empty : s;
    }

    /// <summary>Reads a UTF-16LE string of the specified character count.</summary>
    public static string ReadUtf16String(BinaryReader br, int charCount)
        => charCount <= 0 ? string.Empty : Encoding.Unicode.GetString(br.ReadBytes(charCount * 2));

    /// <summary> Reads a fixed-length ASCII string, trimming any trailing nulls.
    public static string ReadAsciiFixed(BinaryReader br, int length)
    {
        var bytes = br.ReadBytes(length);
        // Trim any trailing zeros; keep ASCII
        int end = Array.FindLastIndex(bytes, b => b != 0) + 1;
        if (end <= 0) return string.Empty;
        return Encoding.ASCII.GetString(bytes, 0, end);
    }

    /// <summary>
    /// Reads a null-terminated ASCII string from the given byte array.
    /// </summary>
    /// <param name="bytes"></param>
    /// <returns>The read string, or the entire byte array as a string if no null terminator is found. </returns>
    public static string ReadAsciiZ(byte[] bytes)
    {
        int len = Array.IndexOf<byte>(bytes, 0);
        return Encoding.ASCII.GetString(bytes, 0, len >= 0 ? len : bytes.Length);
    }

    public static string ReadUnicode256Count(BinaryReader br, int size = 512)
    {
        byte[] data = br.ReadBytes(size);
        int len = 0;
        while (len + 1 < data.Length)
        {
            if (data[len] == 0 && data[len + 1] == 0) break;
            len += 2;
        }
        return Encoding.Unicode.GetString(data, 0, len);
    }

    public static string? ReadUtf16ZFromBuffer(byte[] buffer)
    {
        for (int i = 0; i + 1 < buffer.Length; i += 2)
        {
            if (buffer[i] == 0 && buffer[i + 1] == 0)
                return Encoding.Unicode.GetString(buffer, 0, i);
        }
        return string.Empty;
    }
    #endregion

    #region Vectors
    public static Vector3 ReadVector3(BinaryReader reader)
    {
        Vector3 v = new()
        {
            X = reader.ReadSingle(),
            Y = reader.ReadSingle(),
            Z = reader.ReadSingle()
        };
        return v;
    }

    public static Vector4 ReadVector4(BinaryReader reader)
    {
        Vector4 v = new()
        {
            X = reader.ReadSingle(),
            Y = reader.ReadSingle(),
            Z = reader.ReadSingle(),
            W = reader.ReadSingle()
        };
        return v;
    }

    public static Quaternion ReadQuaternion(BinaryReader reader)
    {
        Quaternion q = new()
        {
            X = reader.ReadSingle(),
            Y = reader.ReadSingle(),
            Z = reader.ReadSingle(),
            W = reader.ReadSingle()
        };
        return q;
    }
    #endregion

    #region Matrices
    /// <summary>
    /// Reads 16 floats and returns them as a System.Numerics.Matrix4x4.
    /// Assumes the 16 floats in the stream are in row-major order:
    /// m11, m12, m13, m14,
    /// m21, m22, m23, m24,
    /// m31, m32, m33, m34,
    /// m41, m42, m43, m44
    /// If file uses column-major (OpenGL style), use ReadMatrix4x4(columnMajor: true).
    /// </summary>
    public static Matrix4x4 ReadMatrix4x4(BinaryReader br, bool columnMajor = false)
    {
        ArgumentNullException.ThrowIfNull(br);

        // Read 16 floats in natural stream order
        float f0 = br.ReadSingle();
        float f1 = br.ReadSingle();
        float f2 = br.ReadSingle();
        float f3 = br.ReadSingle();
        float f4 = br.ReadSingle();
        float f5 = br.ReadSingle();
        float f6 = br.ReadSingle();
        float f7 = br.ReadSingle();
        float f8 = br.ReadSingle();
        float f9 = br.ReadSingle();
        float f10 = br.ReadSingle();
        float f11 = br.ReadSingle();
        float f12 = br.ReadSingle();
        float f13 = br.ReadSingle();
        float f14 = br.ReadSingle();
        float f15 = br.ReadSingle();

        if (!columnMajor)
        {
            // Treat the sequence as row-major:
            // [ f0  f1  f2  f3 ]
            // [ f4  f5  f6  f7 ]
            // [ f8  f9  f10 f11]
            // [ f12 f13 f14 f15]
            return new Matrix4x4(
                f0, f1, f2, f3,
                f4, f5, f6, f7,
                f8, f9, f10, f11,
                f12, f13, f14, f15
            );
        }
        else
        {
            // Treat the sequence as column-major (typical for GL),
            // convert into Matrix4x4 which stores elements by named fields M11..M44
            // column-major stream order:
            // [ f0 f4  f8  f12 ]
            // [ f1 f5  f9  f13 ]
            // [ f2 f6  f10 f14 ]
            // [ f3 f7  f11 f15 ]
            return new Matrix4x4(
                f0, f4, f8, f12,
                f1, f5, f9, f13,
                f2, f6, f10, f14,
                f3, f7, f11, f15
            );
        }
    }

    #endregion

    #region Debugging
    public static int ReadSmartInt32(BinaryReader br)
    {
        long pos = br.BaseStream.Position;
        byte[] bytes = br.ReadBytes(4);

        int asInt = BitConverter.ToInt32(bytes, 0);
        float asFloat = BitConverter.ToSingle(bytes, 0);

        if (float.IsNaN(asFloat) || float.IsInfinity(asFloat))
            return asInt;

        if (asInt == 0x00000000)
            return asInt;

        if (!(asFloat == float.MinValue) && !(asFloat == float.MaxValue))
        {
            float abs = Math.Abs(asFloat);
            if (abs < float.MinValue || abs > float.MaxValue)
                return asInt;
        }

        Debug.Write($"[{pos - 4:X8}] float? 0x{asInt:X8} = {asFloat}");

        return asInt;
    }

    #endregion
}
