using System.Numerics;
using static RHToolkit.Models.Model3D.ModelExtensions;

namespace RHToolkit.Models.Model3D.Map;

#region Data Models
public sealed class NaviMeshFile
{
    public NaviHeader Header { get; set; } = new();
    public List<NaviMeshOctree> Octrees { get; } = [];
    public List<ModelNodeXform> Nodes { get; set; } = [];
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

#endregion

/// <summary>
/// Reader for NAVI (navigation mesh) files.
/// </summary>
public static class NaviReader
{
    private const string FILE_HEADER = "DoBal";

    public static async Task<NaviMeshFile> ReadAsync(string path, CancellationToken ct = default)
    {
        await using var fs = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 64 * 1024,
            useAsync: true);

        using var br = new BinaryReader(fs, Encoding.ASCII, leaveOpen: false);
        return await Task.Run(() => ReadNavi(br), ct);
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

            try
            {
                switch (type)
                {
                    case 3: model.Nodes.Add(ModelSharedTypeReader.ReadNodeTransformData(br, size, model.Header.Version)); break;
                    case 8:
                        ReadOctree(br, model, size);
                        break;
                    case 16:
                        ReadMeshEntry(br, model, size);
                        break;
                    default:
                        throw new NotSupportedException($"Unknown/Unsupported NAVI object type: {type} (at index {i})");
                }

                int consumed = (int)(br.BaseStream.Position - start);
                int remain = Math.Max(0, size - consumed);

                long read = br.BaseStream.Position - start;
                if (remain != 0)
                    throw new InvalidDataException($"Type {type}: expected {size} bytes, read {read}.");
            }
            catch (Exception ex)
            {
                throw new Exception($"Type {type}: Error on Position: 0x{br.BaseStream.Position:X8}: {ex}");
            }
        }

        return model;
    }

    #region Readers

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
        var objCenter = BinaryReaderExtensions.ReadVector3(br);
        var objRadius = br.ReadSingle();
        var objMin = BinaryReaderExtensions.ReadVector3(br);
        var objMax = BinaryReaderExtensions.ReadVector3(br);
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
            szName = BinaryReaderExtensions.ReadUnicode256Count(br);
            szParentName = BinaryReaderExtensions.ReadUnicode256Count(br);
        }
        else
        {
            int nNameLen = br.ReadInt32();
            int nParentNameLen = br.ReadInt32();
            nNameKey = br.ReadUInt32();
            nParentNameKey = br.ReadUInt32();
            szName = BinaryReaderExtensions.ReadUtf16String(br, nNameLen);
            szParentName = BinaryReaderExtensions.ReadUtf16String(br, nParentNameLen);
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
            n.Vertices[i] = BinaryReaderExtensions.ReadVector3(br);
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
#endregion