using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using static RHToolkit.Models.Model3D.ModelMaterial;

namespace RHToolkit.Models.Model3D;

/// <summary>
/// Represents the material sidecar JSON structure for FBX export/import.
/// Contains all material data needed to reconstruct type-1 chunks.
/// </summary>
public class ModelMaterialSidecar
{
    public const string FileExtension = ".materials.json";

    /// <summary>
    /// Version of the original MMP/MGM file.
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// Material libraries (used in type-1 chunks).
    /// </summary>
    public List<MaterialLibraryDto> Libraries { get; set; } = [];

    /// <summary>
    /// Materials with shaders, textures, and lights.
    /// </summary>
    public List<ModelMaterialDto> Materials { get; set; } = [];

    #region DTOs for JSON Serialization

    public class MaterialLibraryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        public float[] V0 { get; set; } = [0, 0, 0];
        public uint V0Tag { get; set; }
        public float[] V1 { get; set; } = [0, 0, 0];
        public uint V1Tag { get; set; }
        public float[] V2 { get; set; } = [0, 0, 0];
        public uint V2Tag { get; set; }

        public int ScalarRawI32 { get; set; }

        public int Unk2 { get; set; }
        public byte Unk3 { get; set; }

        public int TailMode { get; set; }
        public int TailCount { get; set; }

        public List<int[]>? Pairs { get; set; }

        public int TailHeadI32 { get; set; }
        public int TailBlobSize { get; set; }
        public string? TailBlobBase64 { get; set; }
    }

    public class ModelMaterialDto
    {
        public int MaterialIndex { get; set; }
        public string MaterialName { get; set; } = string.Empty;
        public string ShaderName { get; set; } = string.Empty;
        public int MaterialFlags { get; set; }
        public byte MaterialVariant { get; set; }

        public List<MaterialShaderDto> Shaders { get; set; } = [];
        public List<MaterialTextureDto> Textures { get; set; } = [];
        public List<MaterialLightDto> Lights { get; set; } = [];
    }

    public class MaterialShaderDto
    {
        public string Slot { get; set; } = string.Empty;
        public uint Key0 { get; set; }
        public uint Key1 { get; set; }
        public uint Key2 { get; set; }
        public uint Key3 { get; set; }
        public uint ValueType { get; set; }
        public float[] Value { get; set; } = [0, 0, 0, 0];
    }

    public class MaterialTextureDto
    {
        public string Slot { get; set; } = string.Empty;
        public uint Key0 { get; set; }
        public uint Key1 { get; set; }
        public uint Key2 { get; set; }
        public ushort Key3Lo { get; set; }
        public ushort Key3Hi { get; set; }
        public string TexturePath { get; set; } = string.Empty;
        public string RawPayloadBase64 { get; set; } = string.Empty;
    }

    public class MaterialLightDto
    {
        public string Semantic { get; set; } = string.Empty;
        public uint Key0 { get; set; }
        public uint Key1 { get; set; }
        public uint Key2 { get; set; }
        public ushort Key3Lo { get; set; }
        public ushort Key3Hi { get; set; }
        public float[] Basis18 { get; set; } = new float[18];
    }

    #endregion

    #region Conversion Methods

    /// <summary>
    /// Creates a sidecar from MMP/MGM material data.
    /// </summary>
    public static ModelMaterialSidecar FromMaterials(int version, List<MaterialLibrary> libraries, List<ModelMaterial> materials)
    {
        var sidecar = new ModelMaterialSidecar
        {
            Version = version
        };

        // Convert libraries
        foreach (var lib in libraries)
        {
            var libDto = new MaterialLibraryDto
            {
                Id = lib.Id,
                Name = lib.Name,
                V0 = [lib.V0.X, lib.V0.Y, lib.V0.Z],
                V0Tag = lib.V0Tag,
                V1 = [lib.V1.X, lib.V1.Y, lib.V1.Z],
                V1Tag = lib.V1Tag,
                V2 = [lib.V2.X, lib.V2.Y, lib.V2.Z],
                V2Tag = lib.V2Tag,
                ScalarRawI32 = lib.ScalarRawI32,
                Unk2 = lib.Unk2,
                Unk3 = lib.Unk3,
                TailMode = lib.TailMode,
                TailCount = lib.TailCount,
                TailHeadI32 = lib.TailHeadI32,
                TailBlobSize = lib.TailBlobSize
            };

            if (lib.TailMode <= 0 && lib.Pairs.Count > 0)
            {
                libDto.Pairs = lib.Pairs.Select(p => new[] { p.A, p.B }).ToList();
            }

            if (lib.TailBlob != null && lib.TailBlob.Length > 0)
            {
                libDto.TailBlobBase64 = Convert.ToBase64String(lib.TailBlob);
            }

            sidecar.Libraries.Add(libDto);
        }

        // Convert materials (sorted by MaterialIndex)
        foreach (var mat in materials.OrderBy(m => m.MaterialIndex))
        {
            var matDto = new ModelMaterialDto
            {
                MaterialIndex = mat.MaterialIndex,
                MaterialName = mat.MaterialName,
                ShaderName = mat.ShaderName,
                MaterialFlags = mat.MaterialFlags,
                MaterialVariant = mat.MaterialVariant
            };

            // Shaders
            foreach (var sh in mat.Shaders)
            {
                matDto.Shaders.Add(new MaterialShaderDto
                {
                    Slot = sh.Slot,
                    Key0 = sh.Key0,
                    Key1 = sh.Key1,
                    Key2 = sh.Key2,
                    Key3 = sh.Key3,
                    ValueType = (uint)sh.ValueType,
                    Value = [sh.Value.X, sh.Value.Y, sh.Value.Z, sh.Value.W]
                });
            }

            // Textures
            foreach (var tex in mat.Textures)
            {
                matDto.Textures.Add(new MaterialTextureDto
                {
                    Slot = tex.Slot,
                    Key0 = tex.Key0,
                    Key1 = tex.Key1,
                    Key2 = tex.Key2,
                    Key3Lo = tex.Key3Lo,
                    Key3Hi = tex.Key3Hi,
                    RawPayloadBase64 = Convert.ToBase64String(tex.Payload),
                    TexturePath = tex.TexturePath
                });
            }

            // Lights
            foreach (var light in mat.Lights)
            {
                matDto.Lights.Add(new MaterialLightDto
                {
                    Semantic = light.Semantic,
                    Key0 = light.Key0,
                    Key1 = light.Key1,
                    Key2 = light.Key2,
                    Key3Lo = light.Key3Lo,
                    Key3Hi = light.Key3Hi,
                    Basis18 = light.Basis18
                });
            }

            sidecar.Materials.Add(matDto);
        }

        return sidecar;
    }

    /// <summary>
    /// Converts the sidecar back to library and material lists.
    /// </summary>
    public (List<MaterialLibrary> libraries, List<ModelMaterial> materials) ToMaterials()
    {
        var libraries = new List<MaterialLibrary>();
        var materials = new List<ModelMaterial>();

        // Convert libraries
        foreach (var libDto in Libraries)
        {
            var lib = new MaterialLibrary
            {
                Id = libDto.Id,
                Name = libDto.Name,
                V0 = new Vector3(libDto.V0[0], libDto.V0[1], libDto.V0[2]),
                V0Tag = libDto.V0Tag,
                V1 = new Vector3(libDto.V1[0], libDto.V1[1], libDto.V1[2]),
                V1Tag = libDto.V1Tag,
                V2 = new Vector3(libDto.V2[0], libDto.V2[1], libDto.V2[2]),
                V2Tag = libDto.V2Tag,
                ScalarRawI32 = libDto.ScalarRawI32,
                ScalarF32 = BitConverter.Int32BitsToSingle(libDto.ScalarRawI32),
                Unk2 = libDto.Unk2,
                Unk3 = libDto.Unk3,
                TailMode = libDto.TailMode,
                TailCount = libDto.TailCount,
                TailHeadI32 = libDto.TailHeadI32,
                TailBlobSize = libDto.TailBlobSize
            };

            if (libDto.Pairs != null)
            {
                lib.Pairs = libDto.Pairs.Select(p => (p[0], p[1])).ToList();
            }

            if (!string.IsNullOrEmpty(libDto.TailBlobBase64))
            {
                lib.TailBlob = Convert.FromBase64String(libDto.TailBlobBase64);
            }

            libraries.Add(lib);
        }

        // Convert materials
        foreach (var matDto in Materials)
        {
            var mat = new ModelMaterial
            {
                MaterialIndex = matDto.MaterialIndex,
                MaterialName = matDto.MaterialName,
                ShaderName = matDto.ShaderName,
                MaterialFlags = matDto.MaterialFlags,
                MaterialVariant = matDto.MaterialVariant
            };

            // Shaders
            foreach (var shDto in matDto.Shaders)
            {
                mat.Shaders.Add(new MaterialShader
                {
                    Slot = shDto.Slot,
                    Key0 = shDto.Key0,
                    Key1 = shDto.Key1,
                    Key2 = shDto.Key2,
                    Key3 = shDto.Key3,
                    ValueType = (ShaderValueType)shDto.ValueType,
                    Value = new Quaternion(shDto.Value[0], shDto.Value[1], shDto.Value[2], shDto.Value[3])
                });
            }

            // Textures
            foreach (var texDto in matDto.Textures)
            {
                mat.Textures.Add(new MaterialTexture
                {
                    Slot = texDto.Slot,
                    Key0 = texDto.Key0,
                    Key1 = texDto.Key1,
                    Key2 = texDto.Key2,
                    Key3Lo = texDto.Key3Lo,
                    Key3Hi = texDto.Key3Hi,
                    Payload = Convert.FromBase64String(texDto.RawPayloadBase64),
                    TexturePath = texDto.TexturePath,
                });
            }

            // Lights
            foreach (var lightDto in matDto.Lights)
            {
                mat.Lights.Add(new MaterialLight
                {
                    Semantic = lightDto.Semantic,
                    Key0 = lightDto.Key0,
                    Key1 = lightDto.Key1,
                    Key2 = lightDto.Key2,
                    Key3Lo = lightDto.Key3Lo,
                    Key3Hi = lightDto.Key3Hi,
                    Basis18 = lightDto.Basis18
                });
            }

            materials.Add(mat);
        }

        return (libraries, materials);
    }

    #endregion

    #region File I/O

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals
    };

    /// <summary>
    /// Saves the sidecar asynchronously to a JSON file.
    /// </summary>
    public async Task SaveAsync(string filePath, CancellationToken ct = default)
    {
        await using var stream = File.Create(filePath);
        await JsonSerializer.SerializeAsync(stream, this, JsonOptions, ct);
    }

    /// <summary>
    /// Loads a sidecar asynchronously from a JSON file.
    /// </summary>
    public static async Task<ModelMaterialSidecar> LoadAsync(string filePath, CancellationToken ct = default)
    {
        await using var stream = File.OpenRead(filePath);
        return await JsonSerializer.DeserializeAsync<ModelMaterialSidecar>(stream, JsonOptions, ct) ?? new ModelMaterialSidecar();
    }

    /// <summary>
    /// Gets the sidecar file path for a given FBX file path.
    /// </summary>
    public static string GetSidecarPath(string fbxPath)
    {
        return Path.ChangeExtension(fbxPath, null) + FileExtension;
    }

    #endregion
}
