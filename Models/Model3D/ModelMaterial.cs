// ModelMaterial.cs
using System.Numerics;

namespace RHToolkit.Models.Model3D;

public class ModelMaterial
{
    public int MaterialIndex { get; set; }
    public string MaterialName { get; set; } = string.Empty;
    public string ShaderName { get; set; } = string.Empty;
    public int MaterialFlags { get; set; }
    public byte MaterialVariant { get; set; }

    public List<MaterialShader> Shaders { get; set; } = [];
    public List<MaterialTexture> Textures { get; set; } = [];
    public List<MaterialLight> Lights { get; set; } = [];
    public List<MaterialLibrary> Library { get; set; } = [];

    public sealed class MaterialLibrary
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        // Three 16-byte blocks, each = float3 + uint32
        public Vector3 V0 { get; set; }
        public uint V0Tag { get; set; }
        public Vector3 V1 { get; set; }
        public uint V1Tag { get; set; }
        public Vector3 V2 { get; set; }
        public uint V2Tag { get; set; }

        public int ScalarRawI32 { get; set; }
        public float ScalarF32 { get; set; }

        public int Unk2 { get; set; }
        public byte Unk3 { get; set; }

        public int TailMode { get; set; } 
        public int TailCount { get; set; } 

        // TailMode <= 0: list of pairs (int32,int32)
        public List<(int A, int B)> Pairs { get; set; } = [];

        // TailMode > 0: (int32 head, int32 size, byte[size])
        public int TailHeadI32 { get; set; }
        public int TailBlobSize { get; set; }
        public byte[] TailBlob { get; set; } = [];
    }

    // -------- Shader parameters --------
    // first 5 floats are uint32 keys/type via float-bit-patterns; last 4 are float.
    public sealed class MaterialShader
    {
        /// <summary>
        /// Shader parameter slot name (16B ASCII-Z)
        /// </summary>
        public string Slot { get; set; } = string.Empty;

        // 4 x uint32 binding keys (stored as float bits in file)
        public uint Key0 { get; set; }
        public uint Key1 { get; set; }
        public uint Key2 { get; set; }
        public uint Key3 { get; set; }

        // Stored as float bits; small values (0..3)

        //From the slot names and the valueType distribution:

        //AlphaValue / Outline_Width / SunFactor / SunPower / TexScale → valueType=1 (scalar float in X)

        //Diffuse / Ambient / Outline_Color / WaterColor / WaveMapVelocity → valueType=2 (float4)

        //Twoside / AlphaBlending / StarEffect / BILLBOARD → valueType=3 (boolean in X)

        //AlphaType → valueType=0 (enum in X; observed values 1 or 2)

        //DiffuseTexturema and LightmapTexturem (both truncated to 16 bytes) → valueType=0 and values are always 1 and 2 respectively in LIGHTMAP materials, i.e. they act like texture-unit selectors / bind indices for the shader.
        public ShaderValueType ValueType { get; set; }

        // (v[5..8])
        public Quaternion Value { get; set; }

        public int AsInt => (int)Value.X;
        public bool AsBool => Value.X != 0;
        public Vector3 AsVector3 => new(Value.X, Value.Y, Value.Z);
    }

    /// <summary>
    /// Shader parameter value type
    /// </summary>
    public enum ShaderValueType : uint
    {
        Enum = 0,
        Float = 1,
        Float4 = 2,
        Bool = 3,
    }

    // -------- Texture bindings --------
    // On disk: slot(16) + 3*u32 + 2*u16 + payload[512]
    // binding keys: Key0/Key1/Key2 and a split Key3 (low16/high16).
    public sealed class MaterialTexture
    {
        /// <summary>
        /// Texture slot name (16B ASCII-Z)
        /// </summary>
        public string Slot { get; set; } = string.Empty;

        // 4 x uint32 binding keys (Key3 is split into two u16)
        public uint Key0 { get; set; }
        public uint Key1 { get; set; }
        public uint Key2 { get; set; }
        public ushort Key3Lo { get; set; }
        public ushort Key3Hi { get; set; }
        public uint Key3 => (uint)(Key3Lo | (Key3Hi << 16));
        public string TexturePath { get; set; } = string.Empty;
        public byte[] Payload { get; set; } = [];
    }

    // -------- Light bindings --------
    // On disk: semantic(16) + 3*u32 + 2*u16 + 18 floats
    // Same binding key scheme as textures: Key0/Key1/Key2 and split Key3.
    public sealed class MaterialLight
    {
        /// <summary>
        /// Light semantic name (16B ASCII-Z)
        /// </summary>
        public string Semantic { get; set; } = string.Empty;

        // 4 x uint32 binding keys (Key3 is split into two u16)
        public uint Key0 { get; set; }
        public uint Key1 { get; set; }
        public uint Key2 { get; set; }
        public ushort Key3Lo { get; set; }
        public ushort Key3Hi { get; set; }
        public uint Key3 => (uint)(Key3Lo | (Key3Hi << 16));

        // 18 float basis values
        public float[] Basis18 { get; set; } = new float[18];
    }

    public static MaterialTexture? Texture(ModelMaterial? m, string slotExact)
        => m?.Textures.FirstOrDefault(t => t.Slot.Equals(slotExact, StringComparison.OrdinalIgnoreCase));

    public static MaterialShader? Shader(ModelMaterial? m, string slotExact)
    => m?.Shaders.FirstOrDefault(s => s.Slot.Equals(slotExact, StringComparison.OrdinalIgnoreCase));

    /// <summary>Returns UV scale from TexScale shader param. Defaults to (1,1).</summary>
    public static Vector2 GetTexScale(ModelMaterial? m)
    {
        var s = Shader(m, "TexScale");
        if (s == null) return new Vector2(1, 1);

        var x = s.Value.X == 0 ? 1f : s.Value.X;
        var y = s.Value.Y == 0 ? 1f : s.Value.Y;
        return new Vector2(x, y);
    }

    /// <summary>
    /// Resolves a texture path to an absolute file path, checking for existence.
    /// </summary>
    /// <param name="baseDir"></param>
    /// <param name="texturePath"></param>
    /// <returns> Absolute file path if found; null if not found or input is null/empty.</returns>
    public static string? ResolveTextureAbsolute(string baseDir, string? texturePath)
    {
        if (string.IsNullOrWhiteSpace(texturePath)) return null;

        var raw = texturePath.Trim().Trim('"', '\'')
            .Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar);

        var candidate = Path.IsPathRooted(raw) ? raw : Path.GetFullPath(Path.Combine(baseDir, raw));

        if (File.Exists(candidate)) return candidate;

        var ext = Path.GetExtension(candidate);
        if (!ext.Equals(".dds", StringComparison.OrdinalIgnoreCase))
        {
            var ddsCandidate = Path.ChangeExtension(candidate, ".dds");
            if (File.Exists(ddsCandidate)) return ddsCandidate;
        }

        return null;
    }
}
