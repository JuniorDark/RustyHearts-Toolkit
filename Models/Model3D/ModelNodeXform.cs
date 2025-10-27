using System.Numerics;

namespace RHToolkit.Models.Model3D;

/// <summary>
/// A node transform entry in a 3D model.
/// </summary>
public sealed class ModelNodeXform
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
