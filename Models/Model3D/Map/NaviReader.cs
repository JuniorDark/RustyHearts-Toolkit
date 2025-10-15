using System.Numerics;

namespace RHToolkit.Models.Model3D.Map;

/// <summary>
/// Reader for NAVI (navigation mesh) files.
/// </summary>
public static class NaviReader
{
    private const string FILE_HEADER = "DoBal";

    public static async Task<NaviMeshFile> ReadAsync(string path, CancellationToken ct = default)
    {
        byte[] bytes = await File.ReadAllBytesAsync(path, ct).ConfigureAwait(false);
        using var ms = new MemoryStream(bytes, writable: false);
        using var br = new BinaryReader(ms, Encoding.ASCII, leaveOpen: false);
        return ReadNavi(br);
    }

    /// <summary>
    /// Reads a NAVI file from a binary reader.
    /// </summary>
    /// <param name="br"></param>
    /// <returns></returns>
    /// <exception cref="InvalidDataException"></exception>
    private static NaviMeshFile ReadNavi(BinaryReader br)
    {
        string header = Encoding.ASCII.GetString(br.ReadBytes(5));
        if (header != FILE_HEADER)
            throw new InvalidDataException("Not a NAVI file.");

        var model = new NaviMeshFile();

        model.Header.Version = br.ReadInt32();
        int objCount = br.ReadInt32();
        model.Header.NumObjects = objCount;

        // Object tables: offsets, sizes, class IDs
        model.Header.Index = new int[objCount];
        model.Header.Length = new int[objCount];
        model.Header.ClassId = new int[objCount];

        for (int i = 0; i < objCount; i++) model.Header.Index[i] = br.ReadInt32();
        for (int i = 0; i < objCount; i++) model.Header.Length[i] = br.ReadInt32();
        for (int i = 0; i < objCount; i++) model.Header.ClassId[i] = br.ReadInt32();

        for (int i = 0; i < objCount; i++)
        {
            int off = model.Header.Index[i];
            int size = model.Header.Length[i];
            int type = model.Header.ClassId[i];

            br.BaseStream.Seek(off, SeekOrigin.Begin);
            long start = br.BaseStream.Position;

            switch (type)
            {
                case 3: ReadNodeTransform(br, model, model.Header.Version, size); break;
                case 8:
                    ReadOctree(br, model, size);
                    break;
                case 16:
                    ReadMeshEntry(br, model, size);
                    break;
                default:
                    _ = br.ReadBytes(size);
                    break;
            }

            long read = br.BaseStream.Position - start;
            if (read != size)
                throw new InvalidDataException($"ClassID {type}: expected {size} bytes, read {read}.");
        }

        return model;
    }

    // ---------- Readers ----------

    /// <summary>
    /// Reads a transform node (ClassID 3).
    /// </summary>
    /// <param name="br"></param>
    /// <param name="model"></param>
    /// <param name="version"></param>
    /// <param name="size"></param>
    /// <exception cref="InvalidDataException"></exception>
    private static void ReadNodeTransform(BinaryReader br, NaviMeshFile model, int version, int size)
    {
        long start = br.BaseStream.Position;

        // node name lengths & keys
        int nameLen = br.ReadInt32();
        int groupNameLen = br.ReadInt32();
        int name2Len = br.ReadInt32();

        uint nameKey = br.ReadUInt32();
        uint groupNameKey = br.ReadUInt32();
        uint name2Key = br.ReadUInt32();

        string name = ModelHelpers.ReadUtf16String(br, nameLen);
        string groupName = ModelHelpers.ReadUtf16String(br, groupNameLen);
        string name2 = ModelHelpers.ReadUtf16String(br, name2Len);

        // flags
        int kind = br.ReadInt32();
        int flag = br.ReadInt32();

        // unknown counts, usually 0
        int unk1 = br.ReadInt32();
        int unk2 = br.ReadInt32();
        int unk3 = br.ReadInt32();
        int unk4 = br.ReadInt32();

        // byte flags, usually 0
        byte b1 = br.ReadByte();
        byte b2 = 0;

        if (version >= 7)
        {
            b2 = br.ReadByte();
        }

        // transformation matrices (4x4 each, row-major)
        float[] worldRestMatrix = ModelHelpers.ReadFloats(br, 16);
        float[] bindPoseGlobalMatrix = ModelHelpers.ReadFloats(br, 16);
        float[] worldRestMatrixDuplicate = ModelHelpers.ReadFloats(br, 16);

        // Pose block: T(3) → R(4) → S(3)
        Vector3 translation = new(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
        Quaternion rotation = new(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
        Vector3 scale = new(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());

        // unknown data blocks
        if (unk1 > 0)
        {
            if (version >= 8)
            {
                byte unk01 = br.ReadByte();
                byte[] b01 = br.ReadBytes(unk1 * 2);
                if (unk01 > 0)
                {
                    byte[] b02 = br.ReadBytes(12 * unk1);
                }
                else
                {
                    byte[] b03 = br.ReadBytes(6 * unk1);
                }
            }
            else
            {
                byte[] b04 = br.ReadBytes(16 * unk1);

            }
        }

        if (unk2 > 0)
        {
            if (version >= 8)
            {
                byte[] b05 = br.ReadBytes(2 * unk2);
                byte[] b06 = br.ReadBytes(8 * unk2);
            }
            else
            {
                byte[] b07 = br.ReadBytes(20 * unk2);
            }
        }

        if (unk3 > 0)
        {
            if (version >= 8)
            {
                byte[] b08 = br.ReadBytes(2 * unk3);
                byte[] b09 = br.ReadBytes(6 * unk3);
            }
            else
            {
                byte[] b10 = br.ReadBytes(16 * unk3);
            }
        }

        if (unk4 > 0)
        {
            if (version >= 8)
            {
                byte[] b11 = br.ReadBytes(2 * unk4);
                byte[] b12 = br.ReadBytes(2 * unk4);
            }
            else
            {
                byte[] b13 = br.ReadBytes(8 * unk4);
            }
        }

        if (b1 > 0)
        {
            int unk15 = br.ReadInt32();
            if (unk15 > 0)
            {
                int unk01 = br.ReadInt32();
                if (version < 8)
                {
                    byte[] b14 = br.ReadBytes(32 * unk15);
                }
                else
                {
                    for (int j = 0; j < unk15; j++)
                    {
                        float unk16 = br.ReadSingle();
                        if (j > 0 && unk16 == -1.0f)
                        {
                            int unk02 = br.ReadInt32();
                            byte[] b15 = br.ReadBytes(12);
                        }
                        else
                        {
                            byte[] b16 = br.ReadBytes(28);
                        }
                    }
                }
            }
        }
        else
        {
            if (b2 > 0)
            {
                int unk17 = br.ReadInt32();
                if (unk17 > 0)
                {
                    byte[] b17 = br.ReadBytes(16 * unk17);
                }
            }
        }
        model.Nodes.Add(new NaviNodeXform
        {
            Name = name,
            NameKey = nameKey,
            GroupName = groupName,
            GroupKey = groupNameKey,
            SubName = name2,
            SubNameKey = name2Key,
            Kind = kind,
            Flag = flag,
            MWorld = new Matrix4x4(
                worldRestMatrix[0], worldRestMatrix[1], worldRestMatrix[2], worldRestMatrix[3],
                worldRestMatrix[4], worldRestMatrix[5], worldRestMatrix[6], worldRestMatrix[7],
                worldRestMatrix[8], worldRestMatrix[9], worldRestMatrix[10], worldRestMatrix[11],
                worldRestMatrix[12], worldRestMatrix[13], worldRestMatrix[14], worldRestMatrix[15]
            ),
            MBind = new Matrix4x4(
                bindPoseGlobalMatrix[0], bindPoseGlobalMatrix[1], bindPoseGlobalMatrix[2], bindPoseGlobalMatrix[3],
                bindPoseGlobalMatrix[4], bindPoseGlobalMatrix[5], bindPoseGlobalMatrix[6], bindPoseGlobalMatrix[7],
                bindPoseGlobalMatrix[8], bindPoseGlobalMatrix[9], bindPoseGlobalMatrix[10], bindPoseGlobalMatrix[11],
                bindPoseGlobalMatrix[12], bindPoseGlobalMatrix[13], bindPoseGlobalMatrix[14], bindPoseGlobalMatrix[15]
            ),
            MWorldDup = new Matrix4x4(
                worldRestMatrixDuplicate[0], worldRestMatrixDuplicate[1], worldRestMatrixDuplicate[2], worldRestMatrixDuplicate[3],
                worldRestMatrixDuplicate[4], worldRestMatrixDuplicate[5], worldRestMatrixDuplicate[6], worldRestMatrixDuplicate[7],
                worldRestMatrixDuplicate[8], worldRestMatrixDuplicate[9], worldRestMatrixDuplicate[10], worldRestMatrixDuplicate[11],
                worldRestMatrixDuplicate[12], worldRestMatrixDuplicate[13], worldRestMatrixDuplicate[14], worldRestMatrixDuplicate[15]
            ),
            Translation = translation,
            Rotation = rotation,
            Scale = scale
        });

        long read = br.BaseStream.Position - start;
        if (read != size)
            throw new InvalidDataException($"Type 3 {name}: expected {size} bytes, read {read}.");

        //Debug.Write($"\nobject={objectName}, kind={kind}, flag={flag}, unks={unk1}, {unk2}, {unk3}, {unk4}");
    }

    /// <summary>
    /// Reads an octree node and its children (if any).
    /// </summary>
    /// <param name="br"></param>
    /// <param name="model"></param>
    /// <param name="size"></param>
    /// <exception cref="InvalidDataException"></exception>
    private static void ReadOctree(BinaryReader br, NaviMeshFile model, int size)
    {
        long start = br.BaseStream.Position;

        var octreeIndex = br.ReadInt32();
        var subdivided = br.ReadBoolean();
        var width = br.ReadSingle();

        // Bounds block: center(3), radius, min(3), max(3)
        var objCenter = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
        var objRadius = br.ReadSingle();
        var objMin = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
        var objMax = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
        var objSize = objMax - objMin;

        var node = new NaviMeshOctree
        {
            OctreeIndex = octreeIndex,
            Subdivided = subdivided,
            Width = width,
            GeometryBounds = new GeometryBounds { Min = objMin, Max = objMax, Size = objSize, SphereRadius = objRadius, Center = objCenter },
            Plane = new D3DXPLANE[6]
        };

        for (int i = 0; i < 6; i++)
        {
            node.Plane[i] = new D3DXPLANE
            {
                A = br.ReadSingle(),
                B = br.ReadSingle(),
                C = br.ReadSingle(),
                D = br.ReadSingle()
            };
        }

        node.NaviIndexCount = br.ReadInt32();
        node.Indices = new List<int>(node.NaviIndexCount);
        for (int i = 0; i < node.NaviIndexCount; i++)
            node.Indices.Add(br.ReadInt32());

        model.Octrees.Add(node);

        // If subdivided, 8 children follow—each is a full node payload again.
        if (node.Subdivided)
        {
            for (int i = 0; i < 8; i++)
            {
                ReadOctree(br, model, 0);
            }
        }
    }

    /// <summary>
    /// Reads a mesh entry.
    /// </summary>
    /// <param name="br"></param>
    /// <param name="model"></param>
    /// <param name="size"></param>
    /// <exception cref="InvalidDataException"></exception>
    private static void ReadMeshEntry(BinaryReader br, NaviMeshFile model, int size)
    {
        long start = br.BaseStream.Position;

        uint nNameKey, nParentNameKey;
        string szName, szParentName;

        if (model.Header.Version < 6)
        {
            nNameKey = br.ReadUInt32();
            nParentNameKey = br.ReadUInt32();
            szName = ModelHelpers.ReadAsciiFixed(br, 512);
            szParentName = ModelHelpers.ReadAsciiFixed(br, 512);
        }
        else
        {
            int nNameLen = br.ReadInt32();
            int nParentNameLen = br.ReadInt32();
            nNameKey = br.ReadUInt32();
            nParentNameKey = br.ReadUInt32();
            szName = ModelHelpers.ReadUtf16String(br, nNameLen);
            szParentName = ModelHelpers.ReadUtf16String(br, nParentNameLen);
        }

        var n = new NaviMeshEntry
        {
            Name = szName,
            ParentName = szParentName,
            NameKey = nNameKey,
            ParentNameKey = nParentNameKey,
            VertexCount = br.ReadInt32(),
            IndexCount = br.ReadInt32()
        };

        n.Vertices = new Vector3[n.VertexCount];
        for (int i = 0; i < n.VertexCount; i++)
        {
            n.Vertices[i] = new Vector3(
                br.ReadSingle(), br.ReadSingle(), br.ReadSingle()
            );
        }

        n.Indices = new D3DIndexNum[n.IndexCount];
        for (int i = 0; i < n.IndexCount; i++)
        {
            n.Indices[i] = new D3DIndexNum
            {
                A = br.ReadInt32(),
                B = br.ReadInt32(),
                C = br.ReadInt32()
            };
        }

        model.Entries.Add(n);
    }
}

// ==================== Data Models ====================
#region Data Models
public sealed class NaviMeshFile
{
    public NaviHeader Header { get; set; } = new();
    public List<NaviMeshOctree> Octrees { get; } = [];
    public List<NaviNodeXform> Nodes { get; set; } = [];
    public List<NaviMeshEntry> Entries { get; } = [];
}

/// <summary>
/// File header information, including version, object count, and tables of offsets, sizes, and class IDs.
/// </summary>
public sealed class NaviHeader
{
    public int Version { get; set; }
    public int NumObjects { get; set; }
    public int[] Index { get; set; } = [];
    public int[] Length { get; set; } = [];
    public int[] ClassId { get; set; } = [];
}

/// <summary>
/// An octree node, including index, subdivision flag, width, bounds, planes, and triangle indices.
/// </summary>
public sealed class NaviMeshOctree
{
    public int OctreeIndex { get; set; }
    public bool Subdivided { get; set; }
    public float Width { get; set; }
    public GeometryBounds GeometryBounds { get; set; }
    public D3DXPLANE[] Plane { get; set; } = [];
    public int NaviIndexCount { get; set; }
    public List<int> Indices { get; set; } = [];
}

/// <summary>
/// A navigation mesh entry, including name, parent, vertices, and triangle indices.
/// </summary>
public sealed class NaviMeshEntry
{
    public string Name { get; set; } = string.Empty;
    public string ParentName { get; set; } = string.Empty;
    public uint NameKey { get; set; }
    public uint ParentNameKey { get; set; }
    public int VertexCount { get; set; }
    public int IndexCount { get; set; }
    public Vector3[] Vertices { get; set; } = [];
    public D3DIndexNum[] Indices { get; set; } = [];
}

/// <summary>
/// A transform node, including name, group, kind, flags, and transformation matrices.
/// </summary>
public sealed class NaviNodeXform
{
    public string Name { get; set; } = string.Empty;
    public uint NameKey { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public uint GroupKey { get; set; }
    public string SubName { get; set; } = string.Empty;
    public uint SubNameKey { get; set; }
    public int Kind { get; set; } // 16 for navi
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

/// <summary>
/// Triangle by vertex indices.
/// </summary>
public struct D3DIndexNum
{
    public int A;
    public int B;
    public int C;
}

/// <summary>
/// Plane equation: Ax + By + Cz + D = 0
/// </summary>
public struct D3DXPLANE
{
    public float A;
    public float B;
    public float C;
    public float D;
}

/// <summary>
/// Axis-aligned bounding box and derived properties.
/// </summary>
public struct GeometryBounds
{
    public Vector3 Min;
    public Vector3 Max;
    public Vector3 Size;       // Max - Min
    public Vector3 Center;     // (Min + Max) / 2
    public float SphereRadius;
}
#endregion