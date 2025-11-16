using SharpAssimp;
using Num = System.Numerics;

namespace RHToolkit.Models.Model3D;

/// <summary>
/// Helper functions for working with 3D models
/// </summary>
public class ModelExtensions
{
    /// <summary>
    /// AABB (axis-aligned bounding box) information for geometry.
    /// </summary>
    public struct GeometryBounds
    {
        public Num.Vector3 Min;
        public Num.Vector3 Max;
        public Num.Vector3 Size;       // Max - Min
        public Num.Vector3 Center;     // (Min + Max) / 2
        public float SphereRadius;
    }

    #region Model helpers
    public static int ComputeMMPStride(int meshType, int uvSetCount)
    {
        if (meshType == 2) return 28;
        if (meshType == 0 && uvSetCount == 1) return 32;
        if (meshType == 0 && uvSetCount == 2) return 40;
        throw new InvalidDataException($"Unknown layout: meshType={meshType}, uv={uvSetCount}");
    }
    #endregion

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
            var local = current.Transform;
            global = Num.Matrix4x4.Multiply(local, global);
        }
        return global;
    }

    #endregion

    #region String readers

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
    #endregion

    #region String helpers
    /// <summary> Compute a simple hash of a name string, suitable for use as an identifier.
    public static uint HashName(string s)
    {
        uint h = 0;
        foreach (byte b in Encoding.ASCII.GetBytes(s))
            h = unchecked(h * 31 + b);
        return h;
    }
    #endregion

    #region Metadata helpers
    public static int GetIntMeta(Node node, string key)
    {
        if (node.Metadata != null && node.Metadata.TryGetValue(key, out Metadata.Entry entry))
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

    public static string GetStringMeta(Node node, string key)
    {
        if (node.Metadata != null && node.Metadata.TryGetValue(key, out Metadata.Entry entry))
        {
            var data = GetMetaDataObject(entry);
            if (data is null) return string.Empty;
            if (data is string s) return s;
            throw new InvalidDataException(
                $"Metadata '{key}' on node '{node.Name}' exists but is not a string (got {data.GetType().Name}).");
        }

        throw new InvalidDataException($"Required metadata '{key}' missing on node '{node.Name}'.");
    }

    public static object? GetMetaDataObject(Metadata.Entry entry)
    {
        var t = entry.GetType();
        var prop = t.GetProperty("Data") ?? t.GetProperty("Value");
        return prop?.GetValue(entry);
    }
    #endregion
}
