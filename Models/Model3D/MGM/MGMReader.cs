using System.Numerics;
using static RHToolkit.Models.Model3D.ModelExtensions;

namespace RHToolkit.Models.Model3D.MGM;

// ---------- Root ----------
/// <summary>
/// Header information for MGM models.
/// </summary>
public sealed class MgmHeader
{
    public int Version { get; set; }
    public int NumObjects { get; set; }
    public int[] Index { get; set; } = [];
    public int[] Length { get; set; } = [];
    public int[] TypeId { get; set; } = [];
}

public sealed class MgmModel
{
    public int Version { get; set; }
    public MgmHeader Header { get; set; } = new();
    public string BaseDirectory { get; set; } = string.Empty;
    public List<ModelMaterial> Materials { get; set; } = []; // type 1
    public List<MgmBonePose> BonesPoses { get; set; } = []; // type 2
    public List<MgmBone> Bones { get; set; } = [];         // type 3
    public List<MgmDummy> Dummies { get; set; } = [];      // type 4
    public List<MgmMesh> Meshes { get; set; } = [];        // type 5,6
    public List<MgmMeshMeta> MeshMetadata { get; set; } = []; // type 7
    public List<MgmCollider> Colliders { get; set; } = []; // type 17
    public List<MgmEdgeDef> Edges { get; set; } = [];      // type 18
    public List<MgmHitbox> Hitboxes { get; set; } = []; // type 24
}

// ---------- Bone Pose (type 2) ----------
public sealed class MgmBonePose
{
    public string Name { get; set; } = string.Empty;
    public string ParentName { get; set; } = string.Empty;
    public uint NameHash { get; set; }
    public uint ParentHash { get; set; }
    public Vector3 Translation { get; set; }
    public Vector4 RotAnglesDeg { get; set; }
    public Vector3 AuxAnglesDeg { get; set; }

    public bool HasParent => !string.IsNullOrEmpty(ParentName);
}

// ---------- Bones (type 3) ----------
public sealed class MgmBone
{
    public string Name { get; set; } = string.Empty;
    public string ParentName { get; set; } = string.Empty;
    public string NameAlias { get; set; } = string.Empty;
    public uint NameHash { get; set; }
    public uint ParentNameHash { get; set; }
    public uint NameAliasHash { get; set; }
    public MgmBoneType BoneType { get; set; }
    public int BoneFlag { get; set; }
    public int[] Reserved { get; set; } = [4];
    public ushort U0 { get; set; }

    // Matrices
    public Matrix4x4 GlobalRestPoseMatrix { get; set; }
    public Matrix4x4 InverseGlobalRestPoseMatrix { get; set; }
    public Matrix4x4 LocalRestPoseMatrix { get; set; }

    // Decomposed Local TRS
    public Vector3 LocalTranslation { get; set; }
    public Quaternion LocalRotation { get; set; }
    public Vector3 LocalScale { get; set; }
}

public enum MgmBoneType : int
{
    Bone = 2,
    Dummy = 4,
    WeaponSocket = 5,
    Collider = 17
}

// ---------- Dummies (type 4) ----------
public sealed class MgmDummy
{
    public string Name { get; set; } = string.Empty;
    public uint NameHash { get; set; }
    public string Target { get; set; } = string.Empty;   // bone/node this attaches to
    public uint TargetHash { get; set; }
    // Local pose relative to Target
    public Vector3 Translation { get; set; }   // meters
    public Vector4 RotAnglesDeg { get; set; }  // degrees: R0,R1,R2,R3
    public Vector3 AuxAnglesDeg { get; set; }  // degrees: P0,P1,P2
    public bool HasTarget => !string.IsNullOrEmpty(Target);
}

public struct MgmVertex
{
    public Vector3 Position;      // P
    public Vector3 Normal;        // N
    public Vector2 UV0;           // UV0
    public Vector2? UV1;          // UV1 (optional)
    public Vector4? Tangent;      // Tangent (optional, handedness in W if present)
    public Vector4? Weights;      // Skin weights
    public (ushort X, ushort Y, ushort Z, ushort W)? BoneIdxU16;   // Skin indices as 4xU16
}

public sealed class MgmMesh
{
    public string Name { get; set; } = string.Empty;
    public string AltName { get; set; } = string.Empty;
    public uint NameHash { get; set; }
    public uint AltNameHash { get; set; }
    public string MaterialName { get; set; } = string.Empty;
    public int MaterialId { get; set; }
    public ModelMaterial? Material { get; set; }
    public int VertexCount { get; set; }
    public int TriangleCount { get; set; }
    public byte[] Flags { get; set; } = [];
    public int Flag { get; set; }
    public MgmVertex[] Vertices { get; set; } = [];
    public ushort[] Indices { get; set; } = [];
}

public sealed class MgmMeshMeta
{
    public string Name { get; set; } = string.Empty;   // e.g., "Main"
    public uint NameHash { get; set; }
    public byte VariantFlag { get; set; }
    // Local pose (T(3) → R(4) → P(3), angles in degrees)
    public Vector3 Translation { get; set; }     // T_x, T_y, T_z
    public Vector4 RotAnglesDeg { get; set; }    // R0, R1, R2, R3
    public Vector3 AuxAnglesDeg { get; set; }    // P0, P1, P2
}

public sealed class MgmCollider // type 17
{
    public string Name { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public uint NameHash { get; set; }
    public uint TargetHash { get; set; }
    public Vector3 Translation { get; set; }      // Tx,Ty,Tz
    public Vector4 RotAnglesDeg { get; set; }     // R0,R1,R2,R3  (degrees)
    public Vector3 AuxAnglesDeg { get; set; }     // P0,P1,P2     (degrees)

    public bool HasTarget => !string.IsNullOrEmpty(Target);
}

public sealed class MgmHitbox // type 24
{
    public string Name { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public uint NameHash { get; set; }
    public uint TargetHash { get; set; }
    public GeometryBounds GeometryBounds { get; set; }

    public bool HasTarget => !string.IsNullOrEmpty(Target);
}

// ---------- Edges (type 18) ----------
[Flags]
public enum MgmEdgeFlags : uint
{
    None = 0,
    Bit18 = 1u << 18, Bit20 = 1u << 20, Bit21 = 1u << 21,
    Bit23 = 1u << 23, Bit24 = 1u << 24, Bit25 = 1u << 25,
    Bit26 = 1u << 26, Bit28 = 1u << 28, Bit29 = 1u << 29, Bit31 = 1u << 31,
}

public sealed class MgmEdgeDef
{
    public string Name { get; set; } = string.Empty;
    public uint NameHash { get; set; } = 0;
    public MgmEdgeFlags Flags { get; set; } = MgmEdgeFlags.None;
    public float AngleDeg { get; set; }
    public float Bias { get; set; }
}


// ---------- Reader ----------
public static class MGMReader
{
    private const string HEADER = "DoBal";

    public static async Task<MgmModel> ReadAsync(string path, CancellationToken ct = default)
    {
        using var fs = File.OpenRead(path);
        return await ReadAsync(fs, Path.GetDirectoryName(path)!, ct).ConfigureAwait(false);
    }

    public static async Task<MgmModel> ReadAsync(Stream stream, string baseDir, CancellationToken ct = default)
    {
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, ct).ConfigureAwait(false);
        ms.Position = 0;
        using var br = new BinaryReader(ms, Encoding.ASCII, leaveOpen: true);
        return ReadMGM(br, baseDir);
    }

    private static MgmModel ReadMGM(BinaryReader br, string baseDir)
    {
        // ---- Header ----
        string header = Encoding.ASCII.GetString(br.ReadBytes(5));
        if (header != HEADER) throw new InvalidDataException("Not an MGM file.");
        var version = br.ReadInt32();

        var model = new MgmModel { Version = version, BaseDirectory = baseDir };
        model.Header.Version = version;

        int objCount = br.ReadInt32();
        model.Header.NumObjects = objCount;

        // Object tables: offsets, sizes, types IDs
        model.Header.Index = new int[objCount];
        model.Header.Length = new int[objCount];
        model.Header.TypeId = new int[objCount];

        // object table: offsets, sizes, types
        int[] offsets = new int[objCount];
        int[] sizes = new int[objCount];
        int[] types = new int[objCount];

        // read object table
        for (int i = 0; i < objCount; i++) offsets[i] = br.ReadInt32();
        for (int i = 0; i < objCount; i++) sizes[i] = br.ReadInt32();
        for (int i = 0; i < objCount; i++) types[i] = br.ReadInt32();

        for (int i = 0; i < objCount; i++)
        {
            int type = types[i];
            int off = offsets[i];
            int size = sizes[i];
            br.BaseStream.Seek(off, SeekOrigin.Begin);
            long start = br.BaseStream.Position;

            try
            {
                switch (type)
                {
                    case 1: model.Materials = ModelSharedTypeReader.ReadMaterialData(br, size, version); break;
                    case 2: model.BonesPoses.Add(ReadType2_BonePose(br, size, version)); break;
                    case 3: model.Bones.Add(ReadType3_Bones(br, size, version)); break;
                    case 4: model.Dummies.Add(ReadType4_Dummy(br, size, version)); break;
                    case 5: ReadType5_Meshes(br, size, model); break;
                    case 6: ReadType6_Meshes(br, size, model); break;
                    case 7: model.MeshMetadata.Add(ReadType7_MeshMeta(br, size, version)); break;
                    case 15: ReadType15(br, size, version); break;
                    case 17: model.Colliders.Add(ReadType17_Collider(br, size, version)); break;
                    case 18: model.Edges.Add(ReadType18_Edge(br, size, version)); break;
                    case 24: model.Hitboxes.Add(ReadType24(br, size, version)); break;
                    default:
                        throw new NotSupportedException($"Unknown/Unsupported MGM object type: {type} (at index {i})");
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

    #region Type Readers

    // ---- Type 3 (Bone Pose) ----
    private static MgmBonePose ReadType2_BonePose(BinaryReader br, int size, int version)
    {
        long start = br.BaseStream.Position;

        string name, parentName;
        uint nameHash = 0, parentHash = 0;
        if (version >= 6)
        {
            int nLen = br.ReadInt32();
            int pLen = br.ReadInt32();
            nameHash = br.ReadUInt32();
            parentHash = br.ReadUInt32();
            name = BinaryReaderExtensions.ReadUtf16String(br, nLen);
            parentName = BinaryReaderExtensions.ReadUtf16String(br, pLen);
        }
        else
        {
            nameHash = br.ReadUInt32();
            parentHash = br.ReadUInt32();
            name = BinaryReaderExtensions.ReadUnicode256Count(br);
            parentName = BinaryReaderExtensions.ReadUnicode256Count(br);
        }

        // Pose block: T(3) → R(4) → P(3)
        var t = BinaryReaderExtensions.ReadVector3(br);
        var r = BinaryReaderExtensions.ReadVector4(br);
        var p = BinaryReaderExtensions.ReadVector3(br);

        return new MgmBonePose
        {
            Name = name,
            ParentName = parentName,
            NameHash = nameHash,
            ParentHash = parentHash,
            Translation = t,
            RotAnglesDeg = r,
            AuxAnglesDeg = p
        };

    }

    // ---- Type 3 (Bones) ----
    private static MgmBone ReadType3_Bones(BinaryReader br, int size, int version)
    {
        long start = br.BaseStream.Position;

        string name, parentName, nameAlias;
        uint hName, hParent, hNameAlias;
        if (version >= 6)
        {
            int nameLen = br.ReadInt32();
            int pNameLen = br.ReadInt32();
            int nameALen = br.ReadInt32();
            hName = br.ReadUInt32();
            hParent = br.ReadUInt32();
            hNameAlias = br.ReadUInt32();

            name = BinaryReaderExtensions.ReadUtf16String(br, nameLen);
            parentName = BinaryReaderExtensions.ReadUtf16String(br, pNameLen);
            nameAlias = BinaryReaderExtensions.ReadUtf16String(br, nameALen);
        }
        else
        {
            hName = br.ReadUInt32();
            hParent = br.ReadUInt32();
            hNameAlias = br.ReadUInt32();
            name = BinaryReaderExtensions.ReadUnicode256Count(br);
            parentName = BinaryReaderExtensions.ReadUnicode256Count(br);
            nameAlias = BinaryReaderExtensions.ReadUnicode256Count(br);
        }

        int boneType = br.ReadInt32();
        int boneFlag = br.ReadInt32();
        int value3 = br.ReadInt32();
        int value4 = br.ReadInt32();
        int value5 = br.ReadInt32();
        int value6 = br.ReadInt32();

        ushort u1 = br.ReadUInt16();

        var matrix1 = BinaryReaderExtensions.ReadMatrix4x4(br);
        var matrix2 = BinaryReaderExtensions.ReadMatrix4x4(br);
        var matrix3 = BinaryReaderExtensions.ReadMatrix4x4(br);

        var pos = BinaryReaderExtensions.ReadVector3(br);
        var rot = BinaryReaderExtensions.ReadQuaternion(br);
        var scl = BinaryReaderExtensions.ReadVector3(br);

        return new MgmBone
        {
            Name = name,
            ParentName = parentName,
            NameAlias = nameAlias,
            NameHash = hName,
            ParentNameHash = hParent,
            NameAliasHash = hNameAlias,
            BoneType = (MgmBoneType)boneType,
            BoneFlag = boneFlag,
            Reserved = [value3, value4, value5, value6],
            U0 = u1,
            GlobalRestPoseMatrix = matrix1,
            InverseGlobalRestPoseMatrix = matrix2,
            LocalRestPoseMatrix = matrix3,
            LocalTranslation = pos,
            LocalRotation = rot,
            LocalScale = scl

        };
    }

    // ---- Type 4 (Dummy / attachment) ----
    private static MgmDummy ReadType4_Dummy(BinaryReader br, int size, int version)
    {
        long start = br.BaseStream.Position;

        string name, targetName;
        uint nameHash = 0, targetHash = 0;
        if (version >= 6)
        {
            int nameLen = br.ReadInt32();
            int targetLen = br.ReadInt32();
            nameHash = br.ReadUInt32();
            targetHash = br.ReadUInt32();
            name = BinaryReaderExtensions.ReadUtf16String(br, nameLen);
            targetName = BinaryReaderExtensions.ReadUtf16String(br, targetLen);
        }
        else
        {
            nameHash = br.ReadUInt32();
            targetHash = br.ReadUInt32();
            name = BinaryReaderExtensions.ReadUnicode256Count(br);
            targetName = BinaryReaderExtensions.ReadUnicode256Count(br);
        }

        // Pose block: T(3) → R(4) → P(3)
        var t = BinaryReaderExtensions.ReadVector3(br);
        var r = BinaryReaderExtensions.ReadVector4(br);
        var p = BinaryReaderExtensions.ReadVector3(br);

        return new MgmDummy
        {
            Name = name,
            Target = targetName,
            NameHash = nameHash,
            TargetHash = targetHash,
            Translation = t,
            RotAnglesDeg = r,
            AuxAnglesDeg = p
        };
    }

    // ---- Type 5 (Static meshes) ----
    private static void ReadType5_Meshes(BinaryReader br, int size, MgmModel model)
    {
        long start = br.BaseStream.Position;

        string name, dummyName;

        if (model.Version >= 6)
        {
            int nameLen = br.ReadInt32();
            int dummyLen = br.ReadInt32();
            uint nameHash = br.ReadUInt32();
            uint dummyHash = br.ReadUInt32();
            name = BinaryReaderExtensions.ReadUtf16String(br, nameLen);
            dummyName = BinaryReaderExtensions.ReadUtf16String(br, dummyLen);
        }
        else
        {
            uint nameHash = br.ReadUInt32();
            uint dummyHash = br.ReadUInt32();
            name = BinaryReaderExtensions.ReadUnicode256Count(br);
            dummyName = BinaryReaderExtensions.ReadUnicode256Count(br);
        }

        int subMeshCount = br.ReadInt32();

        for (int i = 0; i < subMeshCount; i++)
        {
            string subName;

            if (model.Version >= 6)
            {
                int subNameLen = br.ReadInt32();
                subName = BinaryReaderExtensions.ReadUtf16String(br, subNameLen);
            }
            else
            {
                subName = BinaryReaderExtensions.ReadUnicode256Count(br);
            }

            int vertexCount = br.ReadInt32();
            int triCount = br.ReadInt32();
            int vertexLayoutTag = br.ReadInt32();

            // flags
            byte isEmissiveAdditive = br.ReadByte();  // 1 = additive/emissive path
            byte isAlphaBlend = br.ReadByte();  // 1 = alpha-blend path
            byte isEnabled = br.ReadByte();  // 1 = visible/enabled?

            // material & UVs
            int uvSetCount = br.ReadInt32();
            int materialIndex = br.ReadInt32();

            int stride = ComputeStrideForMGM(vertexLayoutTag, uvSetCount); // bytes per vertex

            // Bounds block: center(3), radius, min(3), max(3)
            var pCenter = BinaryReaderExtensions.ReadVector3(br);
            var pRadius = br.ReadSingle();
            var pMin = BinaryReaderExtensions.ReadVector3(br);
            var pMax = BinaryReaderExtensions.ReadVector3(br);
            var pSize = pMax - pMin;

            // Read raw buffers
            byte[] vtxBytes = br.ReadBytes(checked(vertexCount * stride));
            byte[] idxBytes = br.ReadBytes(checked(triCount * 3 * 2)); // ushort * (triangles*3)

            // Build mesh
            var mesh = new MgmMesh
            {
                Name = subName,
                AltName = dummyName,
                VertexCount = vertexCount,
                TriangleCount = triCount,
                Flags = [isEmissiveAdditive, isAlphaBlend, isEnabled ],
                MaterialId = materialIndex,
                Material = model.Materials.FirstOrDefault(m => m.Id == materialIndex),
                Vertices = new MgmVertex[vertexCount],
                Indices = new ushort[triCount * 3]
            };

            // Indices
            Buffer.BlockCopy(idxBytes, 0, mesh.Indices, 0, idxBytes.Length);

            // Vertices (P,N,UV0)
            for (int v = 0; v < vertexCount; v++)
            {
                int o = v * stride;

                var pos = new Vector3
                {
                    X = BitConverter.ToSingle(vtxBytes, o + 0),
                    Y = BitConverter.ToSingle(vtxBytes, o + 4),
                    Z = BitConverter.ToSingle(vtxBytes, o + 8)
                };
                var nor = new Vector3
                {
                    X = BitConverter.ToSingle(vtxBytes, o + 12),
                    Y = BitConverter.ToSingle(vtxBytes, o + 16),
                    Z = BitConverter.ToSingle(vtxBytes, o + 20)
                };

                Vector2 uv0, uv1 = default;

                switch (stride)
                {
                    case 32: // single UV set
                        uv0.X = BitConverter.ToSingle(vtxBytes, o + 24);
                        uv0.Y = BitConverter.ToSingle(vtxBytes, o + 28);
                        break;
                    case 40: // two UV sets, UV0 base; UV1
                        uv0.X = BitConverter.ToSingle(vtxBytes, o + 24);
                        uv0.Y = BitConverter.ToSingle(vtxBytes, o + 28);
                        uv1.X = BitConverter.ToSingle(vtxBytes, o + 32);
                        uv1.Y = BitConverter.ToSingle(vtxBytes, o + 36);
                        break;
                    default:
                        uv0 = default;
                        break;
                }

                mesh.Vertices[v] = new MgmVertex
                {
                    Position = pos,
                    Normal = nor,
                    UV0 = uv0,
                    UV1 = (stride == 40) ? uv1 : null
                };
            }

            model.Meshes.Add(mesh);
        }
    }

    // ---- Type 6 (Skinned Meshes) ----
    private static void ReadType6_Meshes(BinaryReader br, int size, MgmModel model)
    {
        long start = br.BaseStream.Position;

        string name, dummyName;

        if (model.Version >= 6)
        {
            int nameLen = br.ReadInt32();
            int dummyLen = br.ReadInt32();
            uint nameHash = br.ReadUInt32();
            uint dummyHash = br.ReadUInt32();
            name = BinaryReaderExtensions.ReadUtf16String(br, nameLen);
            dummyName = BinaryReaderExtensions.ReadUtf16String(br, dummyLen);
        }
        else
        {
            uint nameHash = br.ReadUInt32();
            uint dummyHash = br.ReadUInt32();
            name = BinaryReaderExtensions.ReadUnicode256Count(br);
            dummyName = BinaryReaderExtensions.ReadUnicode256Count(br);
        }

        int meshCount = br.ReadInt32();

        // Build deform-index → full-index map (skip non-deforming bones)
        var deformToFull = new List<int>();
        for (int i = 0; i < model.Bones.Count; i++)
        {
            if (model.Bones[i].BoneType == MgmBoneType.Bone)
                deformToFull.Add(i);
        }

        for (int i = 0; i < meshCount; i++)
        {
            string subName;

            if (model.Version >= 6)
            {
                int subNameLen = br.ReadInt32();
                subName = BinaryReaderExtensions.ReadUtf16String(br, subNameLen);
            }
            else
            {
                subName = BinaryReaderExtensions.ReadUnicode256Count(br);
            }

            int vertexCount = br.ReadInt32();
            int triCount = br.ReadInt32();
            int meshType = br.ReadInt32(); // 0 = regular mesh, 2 = billboard

            byte isEmissiveAdditive = br.ReadByte();
            byte isAlphaBlend = br.ReadByte();
            byte isEnabled = br.ReadByte();

            int uvSetCount = br.ReadInt32();
            int materialIndex = br.ReadInt32();
            int influenceCount = br.ReadInt32();

            int stride = ComputeStrideForMGM(meshType, uvSetCount);

            // read vertex data
            byte[] vtxBytes = br.ReadBytes(checked(vertexCount * stride));
            byte[] idxBytes = br.ReadBytes(checked(triCount * 3 * 2));

            // read bone indices/weights
            int totalInfluences = vertexCount * influenceCount;
            byte[] boneIdxBytes = br.ReadBytes(2 * totalInfluences); // U16 bone indices
            byte[] weightBytes = br.ReadBytes(4 * totalInfluences); // F32 weights

            var weightsList = new Vector4[vertexCount];
            var boneIdxList = new (ushort X, ushort Y, ushort Z, ushort W)[vertexCount];

            int tmpLen = Math.Max(4, influenceCount);
            var rented = System.Buffers.ArrayPool<(float w, ushort idx)>.Shared.Rent(tmpLen);
            try
            {
                var tmp = rented.AsSpan(0, tmpLen);

                for (int v = 0; v < vertexCount; v++)
                {
                    for (int k = 0; k < influenceCount; k++)
                    {
                        int linear = v * influenceCount + k;

                        ushort raw = BitConverter.ToUInt16(boneIdxBytes, linear * 2);
                        float w = BitConverter.ToSingle(weightBytes, linear * 4);

                        // Interpret as deform-space index and map to full bone index
                        ushort idx16;
                        if (raw < deformToFull.Count)
                            idx16 = (ushort)deformToFull[raw];
                        else
                            idx16 = 0;

                        tmp[k] = (w, idx16);
                    }

                    int take = Math.Min(4, influenceCount);
                    tmp[..influenceCount].Sort((a, b) => b.w.CompareTo(a.w));

                    float w0 = take > 0 ? tmp[0].w : 0f;
                    float w1 = take > 1 ? tmp[1].w : 0f;
                    float w2 = take > 2 ? tmp[2].w : 0f;
                    float w3 = take > 3 ? tmp[3].w : 0f;

                    w0 = Math.Clamp(w0, 0f, 1f);
                    w1 = Math.Clamp(w1, 0f, 1f);
                    w2 = Math.Clamp(w2, 0f, 1f);
                    w3 = Math.Clamp(w3, 0f, 1f);
                    float sum = w0 + w1 + w2 + w3;
                    if (sum > 1e-8f) { w0 /= sum; w1 /= sum; w2 /= sum; w3 /= sum; }

                    weightsList[v] = new Vector4(w0, w1, w2, w3);
                    var idx0 = (ushort)(take > 0 ? tmp[0].idx : 0);
                    var idx1 = (ushort)(take > 1 ? tmp[1].idx : 0);
                    var idx2 = (ushort)(take > 2 ? tmp[2].idx : 0);
                    var idx3 = (ushort)(take > 3 ? tmp[3].idx : 0);
                    boneIdxList[v] = (idx0, idx1, idx2, idx3);
                }
            }
            finally
            {
                System.Buffers.ArrayPool<(float w, ushort idx)>.Shared.Return(rented);
            }

            var mesh = new MgmMesh
            {
                Name = subName,
                AltName = dummyName,
                VertexCount = vertexCount,
                TriangleCount = triCount,
                MaterialId = materialIndex,
                Material = model.Materials.FirstOrDefault(m => m.Id == materialIndex),
                Vertices = new MgmVertex[vertexCount],
                Indices = new ushort[triCount * 3],
                Flags = [isEmissiveAdditive, isAlphaBlend, isEnabled]
            };

            Buffer.BlockCopy(idxBytes, 0, mesh.Indices, 0, idxBytes.Length);

            for (int v = 0; v < vertexCount; v++)
            {
                int o = v * stride;

                var pos = new Vector3(
                    BitConverter.ToSingle(vtxBytes, o + 0),
                    BitConverter.ToSingle(vtxBytes, o + 4),
                    BitConverter.ToSingle(vtxBytes, o + 8)
                );
                var nor = new Vector3(
                    BitConverter.ToSingle(vtxBytes, o + 12),
                    BitConverter.ToSingle(vtxBytes, o + 16),
                    BitConverter.ToSingle(vtxBytes, o + 20)
                );

                Vector2 uv0, uv1 = default;
                Vector3 tangent = default;

                switch (stride)
                {
                    case 32:
                        uv0 = new Vector2(BitConverter.ToSingle(vtxBytes, o + 24), BitConverter.ToSingle(vtxBytes, o + 28));
                        break;
                    case 40:
                        uv0 = new Vector2(BitConverter.ToSingle(vtxBytes, o + 24), BitConverter.ToSingle(vtxBytes, o + 28));
                        uv1 = new Vector2(BitConverter.ToSingle(vtxBytes, o + 32), BitConverter.ToSingle(vtxBytes, o + 36));
                        break;
                    case 44:
                        uv0 = new Vector2(BitConverter.ToSingle(vtxBytes, o + 24), BitConverter.ToSingle(vtxBytes, o + 28));
                        tangent = new Vector3(
                            BitConverter.ToSingle(vtxBytes, o + 32),
                            BitConverter.ToSingle(vtxBytes, o + 36),
                            BitConverter.ToSingle(vtxBytes, o + 40)
                        );
                        break;
                    default:
                        uv0 = default;
                        break;
                }

                mesh.Vertices[v] = new MgmVertex
                {
                    Position = pos,
                    Normal = nor,
                    UV0 = uv0,
                    UV1 = (stride == 40) ? uv1 : null,
                    Tangent = new Vector4(tangent, 1.0f),
                    Weights = weightsList[v],
                    BoneIdxU16 = boneIdxList[v]
                };
            }

            model.Meshes.Add(mesh);
        }
    }

    private static int ComputeStrideForMGM(int meshType, int uvSetCount)
    {
        if (uvSetCount <= 1)
        {
            return meshType != 0 ? 44 : 32;
        }
        else
        {
            return meshType == 0 ? 40 : 44;
        }
        throw new InvalidDataException($"Unknown layout: meshType={meshType}, uv={uvSetCount}");
    }

    // ---- Type 7 (Mesh metadata) ----
    private static MgmMeshMeta ReadType7_MeshMeta(BinaryReader br, int size, int version)
    {
        long start = br.BaseStream.Position;

        string name;
        uint nameHash = 0;
        if (version >= 6)
        {
            int nameLen = br.ReadInt32();
            nameHash = br.ReadUInt32();
            name = BinaryReaderExtensions.ReadUtf16String(br, nameLen);
        }
        else
        {
            nameHash = br.ReadUInt32(); ;
            name = BinaryReaderExtensions.ReadUnicode256Count(br);
        }

        byte flag = br.ReadByte();

        var t = BinaryReaderExtensions.ReadVector3(br);
        var r = BinaryReaderExtensions.ReadVector4(br);
        var p = BinaryReaderExtensions.ReadVector3(br);

        return new MgmMeshMeta
        {
            Name = name,
            NameHash = nameHash,
            VariantFlag = flag,
            Translation = t,
            RotAnglesDeg = r,
            AuxAnglesDeg = p
        };
    }

    // ---- Type 15  ??? ----
    private static void ReadType15(BinaryReader br, int size, int version)
    {
        long start = br.BaseStream.Position;

        string name;
        uint nameHash;
        if (version >= 6)
        {
            int nNameLen = br.ReadInt32();
            nameHash = br.ReadUInt32();
            name = BinaryReaderExtensions.ReadUtf16String(br, nNameLen);
        }
        else
        {
            nameHash = br.ReadUInt32();
            name = BinaryReaderExtensions.ReadUnicode256Count(br);
        }

        int nUnk2 = br.ReadInt32();

        for (int i = 0; i < nUnk2; i++)
        {
            if (version >= 6)
            {
                int nUnk3 = br.ReadInt32();
                string strUnk1 = BinaryReaderExtensions.ReadRHString(br);
            }
            else
            {
                string strUnk1 = BinaryReaderExtensions.ReadRHString(br);
            }

            int nUnk4 = br.ReadInt32();

            switch (nUnk4)
            {
                case 0:
                    {
                        if (version >= 6)
                        {
                            int nNameLen = br.ReadInt32();
                            string strUnk2 = BinaryReaderExtensions.ReadUtf16String(br, nNameLen);
                        }
                        else
                        {
                            string strUnk2 = BinaryReaderExtensions.ReadUnicode256Count(br);
                        }
                        break;
                    }
                case 1:
                case 2: // Same parsing logic but something is different here
                    {
                        int nUnk5 = br.ReadInt32();
                        break;
                    }
                default: // Should never be triggered
                    {
                        break;
                    }
            }
        }
    }

    // ---- Type 17 (Collider) ----
    private static MgmCollider ReadType17_Collider(BinaryReader br, int size, int version)
    {
        long start = br.BaseStream.Position;

        string name, target;
        uint nameHash, targetHash;
        if (version >= 6)
        {
            int nameLen = br.ReadInt32();
            int targetLen = br.ReadInt32();
            nameHash = br.ReadUInt32();
            targetHash = br.ReadUInt32();
            name = BinaryReaderExtensions.ReadUtf16String(br, nameLen);
            target = BinaryReaderExtensions.ReadUtf16String(br, targetLen);
        }
        else
        {
            nameHash = br.ReadUInt32();
            targetHash = br.ReadUInt32();
            name = BinaryReaderExtensions.ReadUnicode256Count(br);
            target = BinaryReaderExtensions.ReadUnicode256Count(br);
        }

        // Pose block: T(3) → R(4) → P(3)
        var t = BinaryReaderExtensions.ReadVector3(br);
        var r = BinaryReaderExtensions.ReadVector4(br);
        var p = BinaryReaderExtensions.ReadVector3(br);

        return new MgmCollider
        {
            Name = name,
            Target = target,
            NameHash = nameHash,
            TargetHash = targetHash,
            Translation = t,
            RotAnglesDeg = r,
            AuxAnglesDeg = p
        };
    }

    // ---- Type 18 (Edge definition) ----
    private static MgmEdgeDef ReadType18_Edge(BinaryReader br, int size, int version)
    {
        long start = br.BaseStream.Position;

        string name;
        uint nameHash = 0;
        if (version >= 6)
        {
            int nameLen = br.ReadInt32();
            nameHash = br.ReadUInt32();
            name = BinaryReaderExtensions.ReadUtf16String(br, nameLen);
        }
        else
        {
            nameHash = br.ReadUInt32(); ;
            name = BinaryReaderExtensions.ReadUnicode256Count(br);
        }

        var flags = (MgmEdgeFlags)br.ReadUInt32();
        float angleDeg = br.ReadSingle();
        float bias = br.ReadSingle();

        return new MgmEdgeDef { Name = name, NameHash = nameHash, Flags = flags, AngleDeg = angleDeg, Bias = bias };
    }

    private static MgmHitbox ReadType24(BinaryReader br, int size, int version)
    {
        long start = br.BaseStream.Position;

        string name, target;
        uint nameHash, targetHash;
        if (version >= 6)
        {
            int nameLen = br.ReadInt32();
            int targetLen = br.ReadInt32();
            nameHash = br.ReadUInt32();
            targetHash = br.ReadUInt32();
            name = BinaryReaderExtensions.ReadUtf16String(br, nameLen);
            target = BinaryReaderExtensions.ReadUtf16String(br, targetLen);
        }
        else
        {
            nameHash = br.ReadUInt32();
            targetHash = br.ReadUInt32();
            name = BinaryReaderExtensions.ReadUnicode256Count(br);
            target = BinaryReaderExtensions.ReadUnicode256Count(br);
        }

        // Bounds block: center(3), radius, min(3), max(3)
        var objCenter = BinaryReaderExtensions.ReadVector3(br);
        var objRadius = br.ReadSingle();
        var objMin = BinaryReaderExtensions.ReadVector3(br);
        var objMax = BinaryReaderExtensions.ReadVector3(br);
        var objSize = objMax - objMin;

        return new MgmHitbox
        {
            Name = name,
            Target = target,
            NameHash = nameHash,
            TargetHash = targetHash,
            GeometryBounds = new GeometryBounds { Min = objMin, Max = objMax, Size = objSize, SphereRadius = objRadius, Center = objCenter }
        };
    }

    #endregion
}
