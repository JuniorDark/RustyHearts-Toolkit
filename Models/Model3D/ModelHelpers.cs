using Assimp;
using Num = System.Numerics;

namespace RHToolkit.Models.Model3D;

/// <summary>
/// Helper functions for working with 3D models
/// </summary>
public class ModelHelpers
{
    #region Matrix helpers
    /// <summary>
    /// Get global transform of a node by accumulating local transforms up the hierarchy
    /// </summary>
    /// <param name="node"></param>
    /// <returns></returns>
    public static Num.Matrix4x4 GetGlobalTransform(Node node)
    {
        var global = Num.Matrix4x4.Identity;
        for (var current = node; current != null; current = current.Parent)
        {
            var local = FromAssimp(current.Transform);
            global = Num.Matrix4x4.Multiply(local, global);
        }
        return global;
    }

    /// <summary>
    /// convert Assimp matrix to System.Numerics matrix
    /// </summary>
    /// <param name="a"></param>
    /// <returns></returns>
    public static Num.Matrix4x4 FromAssimp(Assimp.Matrix4x4 a)
    {
        // Assimp uses row-major order, so direct mapping
        return new Num.Matrix4x4(
            a.A1, a.B1, a.C1, a.D1,
            a.A2, a.B2, a.C2, a.D2,
            a.A3, a.B3, a.C3, a.D3,
            a.A4, a.B4, a.C4, a.D4
        );
    }

    /// <summary>
    /// Write a 4x4 matrix in row-major order
    /// </summary>
    /// <param name="bw"></param>
    /// <param name="m"></param>
    public static void WriteMatrix(BinaryWriter bw, Num.Matrix4x4 m)
    {
        bw.Write(m.M11); bw.Write(m.M12); bw.Write(m.M13); bw.Write(m.M14);
        bw.Write(m.M21); bw.Write(m.M22); bw.Write(m.M23); bw.Write(m.M24);
        bw.Write(m.M31); bw.Write(m.M32); bw.Write(m.M33); bw.Write(m.M34);
        bw.Write(m.M41); bw.Write(m.M42); bw.Write(m.M43); bw.Write(m.M44);
    }
    #endregion

    #region Read arrays

    /// <summary>Reads a UTF-16LE string of the specified character count.</summary>
    public static string ReadUtf16String(BinaryReader br, int charCount)
        => charCount <= 0 ? string.Empty : Encoding.Unicode.GetString(br.ReadBytes(charCount * 2));

    /// <summary> Reads an array of <see cref="float"/> values.</summary>
    public static float[] ReadFloats(BinaryReader br, int n)
    {
        float[] a = new float[n];
        for (int i = 0; i < n; i++) a[i] = br.ReadSingle();
        return a;
    }

    /// <summary> Reads a fixed-length ASCII string, trimming any trailing nulls.
    public static string ReadAsciiFixed(BinaryReader br, int length)
    {
        var bytes = br.ReadBytes(length);
        // Trim any trailing zeros; keep ASCII
        int end = Array.FindLastIndex(bytes, b => b != 0) + 1;
        if (end <= 0) return string.Empty;
        return Encoding.ASCII.GetString(bytes, 0, end);
    }

    #endregion

    #region Write arrays
    /// <summary> Compute a simple hash of a name string, suitable for use as an identifier.
    public static uint HashName(string s)
    {
        uint h = 0;
        foreach (byte b in Encoding.ASCII.GetBytes(s))
            h = unchecked(h * 31 + b);
        return h;
    }

    /// <summary> Write the length of a UTF-16LE string in characters.
    public static void WriteUtf16Len(BinaryWriter bw, string s) => bw.Write(s?.Length ?? 0);
    public static void WriteUtf16Body(BinaryWriter bw, string s) { if (!string.IsNullOrEmpty(s)) bw.Write(Encoding.Unicode.GetBytes(s)); }

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

    #region Metadata helpers
    public static int GetIntMeta(Assimp.Node node, string key)
    {
        if (node.Metadata != null && node.Metadata.TryGetValue(key, out Assimp.Metadata.Entry entry))
        {
            var data = GetMetaDataObject(entry);
            if (data != null)
            {
                switch (data)
                {
                    case int i: return i;
                    case long l: return checked((int)l);
                    case uint ui: return checked((int)ui);
                    case ulong ul: return checked((int)ul);
                    case short s: return s;
                    case ushort us: return us;
                    case byte b: return b;
                    case sbyte sb: return sb;
                    case float f: return checked((int)f);
                    case double d: return checked((int)d);
                    case bool bo: return bo ? 1 : 0;
                    case string str:
                        if (int.TryParse(str, out var v)) return v;
                        break;
                }
            }
            throw new InvalidDataException($"Metadata '{key}' on node '{node.Name}' exists but is not a valid integer.");
        }

        throw new InvalidDataException($"Required metadata '{key}' missing on node '{node.Name}'.");
    }

    public static string GetStringMeta(Assimp.Node node, string key)
    {
        if (node.Metadata != null && node.Metadata.TryGetValue(key, out Assimp.Metadata.Entry entry))
        {
            var data = GetMetaDataObject(entry);
            if (data is null) return string.Empty;
            if (data is string s) return s;
            throw new InvalidDataException(
                $"Metadata '{key}' on node '{node.Name}' exists but is not a string (got {data.GetType().Name}).");
        }

        throw new InvalidDataException($"Required metadata '{key}' missing on node '{node.Name}'.");
    }

    public static object? GetMetaDataObject(Assimp.Metadata.Entry entry)
    {
        var t = entry.GetType();
        var prop = t.GetProperty("Data") ?? t.GetProperty("Value");
        return prop?.GetValue(entry);
    }
    #endregion
}
