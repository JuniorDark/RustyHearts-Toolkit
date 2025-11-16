using System.Numerics;
using static RHToolkit.Models.Model3D.ModelExtensions;

namespace RHToolkit.Models.Model3D.Map;

/// <summary>
/// Core classes for MMP Models.
/// </summary>
public static class MMP
{
    /// <summary>
    /// Header information for MMP models.
    /// </summary>
    public sealed class MmpHeader
    {
        public int Version { get; set; }
        public int NumObjects { get; set; }
        public int[] Index { get; set; } = [];
        public int[] Length { get; set; } = [];
        public int[] TypeId { get; set; } = [];
    }

    /// <summary>
    /// The root MMP model, containing version, materials, nodes, and objects.
    /// </summary>
    public sealed class MmpModel
    {
        public int Version { get; set; }
        public string BaseDirectory { get; set; } = string.Empty;
        public MmpHeader Header { get; set; } = new();
        public List<ModelMaterial> Materials { get; set; } = [];
        public List<ModelNodeXform> Nodes { get; set; } = [];
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
        /// Index of the material applied to the mesh.
        /// </summary>
        public int MaterialIdx { get; set; }
        public ModelMaterial? Material { get; set; }
        /// <summary>
        /// The type of the mesh, which may indicate different rendering or processing methods.
        /// </summary>
        public int MeshType { get; set; }
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
}
