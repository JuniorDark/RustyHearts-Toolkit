using System.Numerics;

namespace RHToolkit.Models;

public class BinaryWriterExtensions
{
    #region Vectors and Matrices
    /// <summary>
    /// Write a 4x4 matrix in row-major order
    /// </summary>
    /// <param name="bw"></param>
    /// <param name="m"></param>
    public static void WriteMatrix(BinaryWriter bw, Matrix4x4 m)
    {
        bw.Write(m.M11); bw.Write(m.M12); bw.Write(m.M13); bw.Write(m.M14);
        bw.Write(m.M21); bw.Write(m.M22); bw.Write(m.M23); bw.Write(m.M24);
        bw.Write(m.M31); bw.Write(m.M32); bw.Write(m.M33); bw.Write(m.M34);
        bw.Write(m.M41); bw.Write(m.M42); bw.Write(m.M43); bw.Write(m.M44);
    }

    /// <summary>
    /// Write a 4x4 matrix in column-major order
    /// </summary>
    /// <param name="bw"></param>
    /// <param name="m"></param>
    public static void WriteMatrixCM(BinaryWriter bw, Matrix4x4 m)
    {
        bw.Write(m.M11); bw.Write(m.M21); bw.Write(m.M31); bw.Write(m.M41);
        bw.Write(m.M12); bw.Write(m.M22); bw.Write(m.M32); bw.Write(m.M42);
        bw.Write(m.M13); bw.Write(m.M23); bw.Write(m.M33); bw.Write(m.M43);
        bw.Write(m.M14); bw.Write(m.M24); bw.Write(m.M34); bw.Write(m.M44);
    }

    /// <summary> Write a Vector2 </summary>
    public static void WriteVector2(BinaryWriter bw, Vector2 v)
    {
        bw.Write(v.X);
        bw.Write(v.Y);
    }

    /// <summary> Write a Vector3 </summary>
    public static void WriteVector3(BinaryWriter bw, Vector3 v)
    {
        bw.Write(v.X);
        bw.Write(v.Y);
        bw.Write(v.Z);
    }

    /// <summary> Write a Vector4 </summary>
    public static void WriteVector4(BinaryWriter bw, Vector4 v)
    {
        bw.Write(v.X);
        bw.Write(v.Y);
        bw.Write(v.Z);
        bw.Write(v.W);
    }

    /// <summary> Write a Quaternion </summary>
    public static void WriteQuaternion(BinaryWriter bw, Quaternion q)
    {
        bw.Write(q.X);
        bw.Write(q.Y);
        bw.Write(q.Z);
        bw.Write(q.W);
    }
    #endregion

    #region Strings
    /// <summary> Write a UTF-16LE string with length prefix characters.</summary>
    public static void WriteUtf16String(BinaryWriter bw, string s)
    {
        if (s.Length == 0) return;
        bw.Write(Encoding.Unicode.GetBytes(s));
    }

    /// <summary> Write a fixed-length ASCII string, padding with zeros or truncating as needed.</summary>
    public static void WriteAsciiFixed(BinaryWriter bw, string s, int width)
    {
        var bytes = Encoding.ASCII.GetBytes(s ?? string.Empty);
        if (bytes.Length >= width)
        {
            bw.Write(bytes, 0, width);
            return;
        }

        bw.Write(bytes);
        // pad with zeros
        Span<byte> pad = stackalloc byte[Math.Min(1024, width - bytes.Length)];
        pad.Clear();
        int remain = width - bytes.Length;
        while (remain > 0)
        {
            int chunk = Math.Min(pad.Length, remain);
            bw.Write(pad.Slice(0, chunk));
            remain -= chunk;
        }
    }
    #endregion
}
