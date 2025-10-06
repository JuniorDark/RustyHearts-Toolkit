using System.Numerics;

namespace RHToolkit.Models.Model3D.MMP;

/// <summary>
/// Core classes for MMP Models.
/// </summary>
public class MMP
{
    // ---------- Common Structs ----------
    /// <summary>
    /// AABB (axis-aligned bounding box) information for geometry.
    /// </summary>
    public struct GeometryBounds
    {
        public Vector3 Min;
        public Vector3 Max;
        public Vector3 Size;       // Max - Min
        public Vector3 Center;     // (Min + Max) / 2
        public float SphereRadius;
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

    /// <summary>
    /// The root MMP model, containing version, materials, nodes, and objects.
    /// </summary>
    public sealed class MmpModel
    {
        public int Version { get; set; }
        public string BaseDirectory { get; set; } = string.Empty;
        public List<MmpMaterial> Materials { get; set; } = [];
        public List<MmpNodeXform> Nodes { get; set; } = [];
        public List<MmpObjectGroup> Objects { get; set; } = [];

    }

    // ---------- Geometry ----------
    /// <summary>
    /// A named object node containing one or more meshes.
    /// </summary>
    public sealed class MmpObjectGroup
    {
        public string NodeName { get; set; } = string.Empty;
        public string AltNodeName { get; set; } = string.Empty;
        public uint NodeNameHash { get; set; }
        public uint AltNodeNameHash { get; set; }
        public List<MmpMesh> Meshes { get; set; } = [];
        public GeometryBounds GeometryBounds { get; set; }
    }

    /// <summary>
    /// A single mesh within an object node.
    /// </summary>
    public sealed class MmpMesh
    {
        /// <summary>
        /// Rhe name of the mesh.
        /// </summary>
        public string MeshName { get; set; } = string.Empty;
        /// <summary>
        /// The material ID used by this mesh.
        /// </summary>
        public int MaterialId { get; set; }
        public MmpMaterial? Material { get; set; }
        /// <summary>
        /// Tag associated with the vertex layout.
        /// </summary>
        public int VertexLayoutTag { get; set; }
        /// <summary>
        /// Additive emissive value, which determines the intensity of the emissive effect.
        /// </summary>
        public byte AdditiveEmissive { get; set; }
        /// <summary>
        /// Alpha blending value, which determines the transparency level of the object.
        /// </summary>
        public byte AlphaBlend { get; set; }
        /// <summary>
        /// Always 1 in known files; possibly a visibility/enabled flag.
        /// </summary>
        public byte Enabled { get; set; }
        /// <summary>
        /// Number of UV sets associated with the object.
        /// </summary>
        public int UVSetCount { get; set; }
        /// <summary>
        /// Computed stride, which represents the number of bytes per row of the data.
        /// </summary>
        public int Stride { get; set; }
        /// <summary>
        /// Collection of vertices that define the structure of the object.
        /// </summary>
        public MmpVertex[] Vertices { get; set; } = [];
        /// <summary>
        /// Collection of indices that define how vertices are connected to form triangles.
        /// </summary>
        public ushort[] Indices { get; set; } = [];
        /// <summary>
        /// Bounding information for the mesh.
        /// </summary>
        public GeometryBounds GeometryBounds { get; set; }
    }

    /// <summary>
    /// A single vertex within a mesh.
    /// </summary>
    public sealed class MmpVertex
    {
        public Vector3 Position { get; set; }
        public Vector3 Normal { get; set; }
        public Vector2 UV0 { get; set; }
        public Vector2? UV1 { get; set; }
    }

    // ---------- Materials ----------
    /// <summary>
    /// WIP
    /// A material definition, including shader parameters, texture references, and light references.
    /// </summary>
    public sealed class MmpMaterial
    {
        public int Id { get; set; }
        public string MaterialName { get; set; } = string.Empty;
        public string ShaderName { get; set; } = string.Empty;
        public int MaterialFlags { get; set; }
        public byte MaterialVariant { get; set; }
        public List<MmpShader> Shaders { get; set; } = [];
        public List<MmpTexture> Textures { get; set; } = [];
        public List<MmpLight> Lights { get; set; } = [];
    }

    /// <summary>
    /// WIP
    /// A shader parameter set.
    /// </summary>
    public sealed class MmpShader
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
    public sealed class MmpTexture
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
    public sealed class MmpLight
    {
        public string Semantic { get; set; } = string.Empty;   // usually "Light_Direction"

        public uint LightBlockIndex0 { get; set; }
        public uint LightBlockIndex1 { get; set; }
        public uint LightBlockIndex2 { get; set; }

        public ushort LightBlockOffsetBytes { get; set; }
        public ushort LightBlockSizeBytes { get; set; }

        public LightBasis Basis { get; set; }
    }

    // ---------- NodeXform ----------
    /// <summary>
    /// A transform node, including name, group, kind, flags, and transformation matrices.
    /// </summary>
    public sealed class MmpNodeXform
    {
        public string Name { get; set; } = string.Empty;
        public uint NameHash { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public uint GroupHash { get; set; }
        public string SubName { get; set; } = string.Empty;
        public uint SubNameHash { get; set; }
        public int Kind { get; set; } // 5,4
        public int Flag { get; set; } // 1
        /// <summary>
        /// World rest matrix (4x4).
        /// </summary>
        public Matrix4x4 MWorld;
        /// <summary>
        /// Bind pose global matrix (4x4). Often the inverse of MWorld.
        /// </summary>
        public Matrix4x4 MBind;
        /// <summary>
        /// Duplicate of world rest matrix (4x4).
        /// </summary>
        public Matrix4x4 MWorldDup;
        /// <summary>
        /// Pose block: Translation (3), Rotation (4), Scale (3).
        /// </summary>
        public Vector3 Translation, Scale;
        public Quaternion Rotation;
    }
}
