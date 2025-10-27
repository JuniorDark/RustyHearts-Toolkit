using System.Numerics;
using A3DS = Aspose.ThreeD.Shading;

namespace RHToolkit.Models.Model3D;

public class ModelMaterial
{
    public int Id { get; set; }
    public string MaterialName { get; set; } = string.Empty;
    public string ShaderName { get; set; } = string.Empty;
    public int MaterialFlags { get; set; }
    public byte MaterialVariant { get; set; }
    public List<ModelShader> Shaders { get; set; } = [];
    public List<ModelTexture> Textures { get; set; } = [];
    public List<ModelLight> Lights { get; set; } = [];

    /// <summary>
    /// A shader parameter set.
    /// </summary>
    public sealed class ModelShader
    {
        /// <summary>
        /// The name of the shader parameter slot.
        /// </summary>
        public string Slot { get; set; } = string.Empty;

        public Quaternion Base { get; set; }    // Values[0..3]
        public float Scalar { get; set; } // Values[4]
        public Quaternion Payload { get; set; } // Values[5..8]
    }

    /// <summary>
    /// A texture reference, including texture ID, sampler state, UV source/transform, and shader parameters.
    /// </summary>
    public sealed class ModelTexture
    {
        public string Slot { get; set; } = string.Empty;
        public uint TextureId { get; set; }
        public uint SamplerStateId { get; set; }
        public uint UVSourceOrTransformId { get; set; }
        public ushort ShaderParamOffsetBytes { get; set; }
        public ushort ShaderParamSizeBytes { get; set; }
        public byte[] RawPayload { get; set; } = [];
        public string? TexturePath { get; set; }
    }

    /// <summary>
    /// A light reference, including semantic name, light block indices, offsets, sizes, and basis functions.
    /// </summary>
    public sealed class ModelLight
    {
        public string Semantic { get; set; } = string.Empty; // 16B ASCII
        public uint I0, I1, I2; // indices
        public ushort OffsetBytes, SizeBytes;
        /// <summary>First 14 floats are global basis; last 4 behave like a tint (RGBA).</summary>
        public float[] Basis18 { get; set; } = new float[18];
    }

    public struct RgbaColor
    {
        public float R, G, B, A;
    }

    public struct LightBasis
    {
        public float G0, G1, G2, G3, G4, G5, G6, G7, G8, G9, G10, G11, G12, G13;
        public RgbaColor Tint;
    }

    public static ModelTexture? Texture(ModelMaterial? m, string slotExact)
        => m?.Textures.FirstOrDefault(t => t.Slot.Equals(slotExact, StringComparison.OrdinalIgnoreCase));

    public static ModelShader? Shader(ModelMaterial? m, string slotPrefix)
        => m?.Shaders.FirstOrDefault(s => s.Slot.StartsWith(slotPrefix, StringComparison.OrdinalIgnoreCase));

    public static Vector3 ToRGB(Quaternion q) => new(q.X, q.Y, q.Z);
    public static Vector3 Clamp01(Vector3 v)
        => new(Math.Clamp(v.X, 0, 1), Math.Clamp(v.Y, 0, 1), Math.Clamp(v.Z, 0, 1));

    /// <summary>
    /// Returns UV scale from TexScale shader (X,Y in Base.XY). Defaults to (1,1).
    /// </summary>
    /// <param name="m"></param>
    /// <returns></returns>
    public static Vector2 GetTexScale(ModelMaterial? m)
    {
        var s = Shader(m, "TexScale");
        if (s == null) return new Vector2(1, 1);
        return new Vector2(
            s.Base.X == 0 ? 1f : s.Base.X,
            s.Base.Y == 0 ? 1f : s.Base.Y
        );
    }

    /// <summary>
    /// Clamps a float value to the range [0,1].
    /// </summary>
    /// <param name="v"></param>
    /// <returns></returns>
    public static float Clamp01(float v) => v < 0 ? 0 : (v > 1 ? 1 : v);

    /// <summary>
    /// Resolves a texture path to an absolute file path, based on the model's base directory.
    /// </summary>
    /// <param name="baseDir"></param>
    /// <param name="texturePath"></param>
    /// <returns>The absolute file path if the texture exists; otherwise, null.</returns>
    public static string? ResolveTextureAbsolute(string baseDir, string? texturePath)
    {
        if (string.IsNullOrWhiteSpace(texturePath)) return null;

        // Normalize separators and trim quotes/whitespace
        var p = texturePath.Trim().Trim('"', '\'')
            .Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar);

        // If already absolute, use as-is; otherwise resolve against the *model's* base directory
        var candidate = Path.IsPathRooted(p) ? p : Path.GetFullPath(Path.Combine(baseDir, p));

        return File.Exists(candidate) ? candidate : null;
    }

    /// <summary>
    /// Creates an Aspose.ThreeD texture, either embedded or as a file reference.
    /// </summary>
    /// <param name="outDir"></param>
    /// <param name="pathRelOrAbs"></param>
    /// <param name="embed"></param>
    /// <returns> The created texture.</returns>
    public static A3DS.Texture MakeTexture(string outDir, string pathRelOrAbs, bool embed)
    {
        var tex = new A3DS.Texture();
        if (embed)
        {
            var abs = Path.IsPathRooted(pathRelOrAbs)
                ? Path.GetFullPath(pathRelOrAbs)
                : Path.GetFullPath(Path.Combine(outDir, pathRelOrAbs));

            tex.FileName = Path.GetFileName(abs);
            if (File.Exists(abs))
                tex.Content = File.ReadAllBytes(abs);
            else
                tex.FileName = pathRelOrAbs;
        }
        else
        {
            tex.FileName = pathRelOrAbs;
        }
        return tex;
    }

}
