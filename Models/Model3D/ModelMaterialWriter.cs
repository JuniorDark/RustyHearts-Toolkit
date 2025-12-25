using System.Numerics;
using static RHToolkit.Models.Model3D.ModelMaterial;

namespace RHToolkit.Models.Model3D;

/// <summary>
/// Writes material data (type-1 chunks) to binary format.
/// </summary>
public static class ModelMaterialWriter
{
    /// <summary>
    /// Writes material data (libraries and materials) to a binary chunk.
    /// </summary>
    /// <param name="libraries">Material libraries to write.</param>
    /// <param name="materials">Materials to write.</param>
    /// <param name="version">File version for format selection.</param>
    /// <returns>Binary data representing the type-1 chunk.</returns>
    public static byte[] WriteMaterialChunk(List<MaterialLibrary> libraries, List<ModelMaterial> materials)
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms, Encoding.ASCII, leaveOpen: true);

        // Library count and material count
        bw.Write(libraries.Count);
        bw.Write(materials.Count);

        // ---- Material Libraries ----
        foreach (var lib in libraries)
        {
            bw.Write(lib.Id);

            bw.Write(lib.Name.Length);
            BinaryWriterExtensions.WriteUtf16String(bw, lib.Name);

            // 3 x 16 bytes: float3 + u32 tag
            WriteVec3Tag(bw, lib.V0, lib.V0Tag);
            WriteVec3Tag(bw, lib.V1, lib.V1Tag);
            WriteVec3Tag(bw, lib.V2, lib.V2Tag);

            bw.Write(lib.ScalarRawI32);
            bw.Write(lib.Unk2);
            bw.Write(lib.Unk3);

            bw.Write(lib.TailMode);
            bw.Write(lib.TailCount);

            if (lib.TailMode <= 0)
            {
                foreach (var (a, b) in lib.Pairs)
                {
                    bw.Write(a);
                    bw.Write(b);
                }
            }
            else
            {
                bw.Write(lib.TailHeadI32);
                bw.Write(lib.TailBlobSize);
                if (lib.TailBlob != null && lib.TailBlob.Length > 0)
                {
                    bw.Write(lib.TailBlob);
                }
            }
        }

        // ---- Materials ----
        foreach (var mat in materials)
        {
            bw.Write(mat.MaterialIndex);

            bw.Write(mat.MaterialName.Length);
            bw.Write(mat.ShaderName.Length);
            BinaryWriterExtensions.WriteUtf16String(bw, mat.MaterialName);
            BinaryWriterExtensions.WriteUtf16String(bw, mat.ShaderName);

            bw.Write(mat.MaterialFlags);
            bw.Write(mat.MaterialVariant);

            bw.Write(mat.Shaders.Count);
            bw.Write(mat.Textures.Count);
            bw.Write(mat.Lights.Count);

            // ---- Shader params ----
            foreach (var sh in mat.Shaders)
            {
                WriteAsciiZ16(bw, sh.Slot);

                // Write 5 uint32 values as floats
                bw.Write(BitConverter.UInt32BitsToSingle(sh.Key0));
                bw.Write(BitConverter.UInt32BitsToSingle(sh.Key1));
                bw.Write(BitConverter.UInt32BitsToSingle(sh.Key2));
                bw.Write(BitConverter.UInt32BitsToSingle(sh.Key3));
                bw.Write(BitConverter.UInt32BitsToSingle((uint)sh.ValueType));

                // Write 4 float values
                bw.Write(sh.Value.X);
                bw.Write(sh.Value.Y);
                bw.Write(sh.Value.Z);
                bw.Write(sh.Value.W);
            }

            // ---- Texture bindings ----
            foreach (var tex in mat.Textures)
            {
                WriteAsciiZ16(bw, tex.Slot);
                bw.Write(tex.Key0);
                bw.Write(tex.Key1);
                bw.Write(tex.Key2);
                bw.Write(tex.Key3Lo);
                bw.Write(tex.Key3Hi);
                var texturePathLength = string.IsNullOrEmpty(tex.TexturePath) ? 0 : tex.TexturePath.Length;
                if (texturePathLength > 127)
                    throw new InvalidDataException($"Texture path ({tex.TexturePath}) is too long ({texturePathLength} characters). Max is 127 characters.");
                WriteUnicodeFixedString(bw, tex.TexturePath, 256);
                if (tex.Payload.Length != 256)
                    throw new InvalidDataException("Texture payload must be exactly 256 bytes.");
                bw.Write(tex.Payload);
            }

            // ---- Light bindings ----
            foreach (var light in mat.Lights)
            {
                WriteAsciiZ16(bw, light.Semantic);
                bw.Write(light.Key0);
                bw.Write(light.Key1);
                bw.Write(light.Key2);
                bw.Write(light.Key3Lo);
                bw.Write(light.Key3Hi);

                for (int k = 0; k < 18; k++)
                {
                    bw.Write(k < light.Basis18.Length ? light.Basis18[k] : 0f);
                }
            }
        }

        return ms.ToArray();
    }

    #region Helpers

    private static void WriteVec3Tag(BinaryWriter bw, Vector3 v, uint tag)
    {
        bw.Write(v.X);
        bw.Write(v.Y);
        bw.Write(v.Z);
        bw.Write(tag);
    }

    private static void WriteAsciiZ16(BinaryWriter bw, string value)
    {
        var bytes = new byte[16];
        if (!string.IsNullOrEmpty(value))
        {
            var asciiBytes = Encoding.ASCII.GetBytes(value);
            var copyLen = Math.Min(asciiBytes.Length, 15); // Leave room for null terminator
            Buffer.BlockCopy(asciiBytes, 0, bytes, 0, copyLen);
        }
        bw.Write(bytes);
    }

    private static void WriteUnicodeZ(BinaryWriter bw, string value)
    {
        if (value == null)
            value = string.Empty;

        // Write UTF-16LE bytes (no BOM)
        var bytes = Encoding.Unicode.GetBytes(value);
        bw.Write(bytes);

        // Write UTF-16 null terminator (0x00 0x00)
        bw.Write((ushort)0);
    }

    public static void WriteUnicodeFixedString(BinaryWriter bw, string value, int size)
    {
        if (size <= 0 || (size & 1) != 0)
            throw new ArgumentException("Size must be a positive even number.", nameof(size));

        // Null or empty is allowed
        if (string.IsNullOrEmpty(value))
        {
            bw.Write(new byte[size]);
            return;
        }

        // Encode as UTF-16LE (no BOM)
        byte[] stringBytes = Encoding.Unicode.GetBytes(value);

        // +2 for UTF-16 null terminator
        if (stringBytes.Length + 2 > size)
        {
            int maxChars = (size / 2) - 1;
            throw new InvalidDataException(
                $"Unicode string too long. Max {maxChars} UTF-16 characters.");
        }

        // Write string bytes
        bw.Write(stringBytes);

        // Write UTF-16 null terminator
        bw.Write((ushort)0);

        // Pad remaining space with zeros
        int bytesWritten = stringBytes.Length + 2;
        int padding = size - bytesWritten;

        if (padding > 0)
            bw.Write(new byte[padding]);
    }

    #endregion
}
