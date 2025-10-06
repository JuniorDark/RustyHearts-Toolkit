using RHToolkit.Models.Model3D.MMP;
using SharpDX.DXGI;
using System;
using System.Diagnostics;
using System.Numerics;

namespace RHToolkit.Models.Model3D.MGM;

// ---------- math / common ----------
public struct Vec3 { public float X, Y, Z; }
public struct BoundingBox { public Vec3 Min, Max; }
public struct BoundingSphere { public Vec3 Center; public float Radius; }

// ---------- Root ----------
public sealed class MgmModel
{
    public int Version { get; set; }
    public string BaseDirectory { get; set; } = string.Empty;
    public List<MgmMaterial> Materials { get; } = []; // type 1
    public List<MgmBonePose> BonesPoses { get; } = []; // type 2
    public List<MgmBone> Bones { get; } = [];         // type 3
    public List<MgmDummy> Dummies { get; } = [];      // type 4
    public List<MgmMesh> Meshes { get; } = [];        // type 5,6
    public List<MgmMeshMeta> MeshMetadata { get; } = []; // type 7
    public List<MgmCollider> Colliders { get; } = []; // type 17
    public List<MgmEdgeDef> Edges { get; } = [];      // type 18
    public List<MgmRawSection> Unknown { get; } = [];
}

// ---------- Materials (type 1) ----------
public sealed class MgmMaterial
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Shader { get; set; } = string.Empty;
    public int Flags { get; set; }
    public byte Variant { get; set; }
    public List<MgmShader> Shaders { get; set; } = [];
    public List<MgmTexture> Textures { get; } = [];
    public List<MgmLightRef> Lights { get; } = [];
}

public sealed class MgmShader
{
    public string Slot { get; set; } = string.Empty;

    public Quaternion Base;    // Values[0..3]
    public float Scalar; // Values[4]
    public Quaternion Payload; // Values[5..8]
}

public sealed class MgmTexture
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

public sealed class MgmLightRef
{
    public string Semantic { get; set; } = string.Empty; // 16B ASCII
    public uint I0, I1, I2; // indices
    public ushort OffsetBytes, SizeBytes;
    /// <summary>First 14 floats are global basis; last 4 behave like a tint (RGBA).</summary>
    public float[] Basis18 { get; set; } = new float[18];
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
    public MgmNodeClass Class { get; set; }
    public int ClassFlag { get; set; }
    public int[] Reserved { get; set; } = [4];
    public ushort U0 { get; set; }

    // Matrices
    public Matrix4x4 WorldRestMatrix { get; set; }
    public Matrix4x4 BindPoseGlobalMatrix { get; set; }
    public Matrix4x4 WorldRestMatrixDuplicate { get; set; }
    public Matrix4x4 InverseBindMatrix => Matrix4x4.Invert(BindPoseGlobalMatrix, out var inv) ? inv : Matrix4x4.Identity;

    // Local TRS
    public Vector3 LocalTranslation { get; set; }
    public Quaternion LocalRotation { get; set; }
    public Vector3 LocalScale { get; set; }
}

public enum MgmNodeClass : int
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
    public Vector3? Bitangent;    // Optional if stored explicitly
    public Vector4? Weights;      // Skin weights (optional)
    public Byte4? BoneIdxU8;     // Skin indices as 4xU8 (optional)
    public UShort4? BoneIdxU16;   // Skin indices as 4xU16 (optional, rare)
}

[Flags]
public enum MgmVertexFeatures : uint
{
    UV1 = 1 << 0,  // has a secondary UV set
    Tangent = 1 << 1,  // has tangent (and maybe bitangent)
    Skinning = 1 << 2,  // has weights+indices
    BoneIdxU16 = 1 << 3,  // skin indices are 16-bit (else 8-bit)
    // add more as you learn (colors, extra UVs, etc.)
}

public readonly struct Byte4(byte x, byte y, byte z, byte w)
{ public readonly byte X = x, Y = y, Z = z, W = w;
}
public readonly struct UShort4(ushort x, ushort y, ushort z, ushort w)
{ public readonly ushort X = x, Y = y, Z = z, W = w;
}


public sealed class MgmMesh
{
    public string Name { get; set; } = string.Empty;
    public string AltName { get; set; } = string.Empty;
    public uint NameHash { get; set; }
    public uint AltNameHash { get; set; }
    public string MaterialName { get; set; } = string.Empty;
    public int MaterialId { get; set; }
    public MgmMaterial? Material { get; set; }
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


public sealed class MgmRawSection { public int Type; public byte[] Data = Array.Empty<byte>(); }

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
        int version = br.ReadInt32();
        int secCount = br.ReadInt32();

        int[] offs = new int[secCount];
        int[] sizes = new int[secCount];
        int[] types = new int[secCount];
        for (int i = 0; i < secCount; i++) offs[i] = br.ReadInt32();
        for (int i = 0; i < secCount; i++) sizes[i] = br.ReadInt32();
        for (int i = 0; i < secCount; i++) types[i] = br.ReadInt32();

        var model = new MgmModel { Version = version, BaseDirectory = baseDir };

        for (int i = 0; i < secCount; i++)
        {
            int type = types[i];
            int off = offs[i];
            int size = sizes[i];
            br.BaseStream.Seek(off, SeekOrigin.Begin);
            long start = br.BaseStream.Position;

            switch (type)
            {
                case 1: ReadType1_Materials(br, model, version, size); break;
                case 2: model.BonesPoses.Add(ReadType2_BonePose(br, size)); break;
                case 3: model.Bones.Add(ReadType3_Bones(br, size)); break;
                case 4: model.Dummies.Add(ReadType4_Dummy(br, size)); break;
                case 5: model.Meshes.Add(ReadType5_Mesh(br, size, model)); break;
                case 6: model.Meshes.Add(ReadType6_Mesh(br, size, version)); break;
                case 7: model.MeshMetadata.Add(ReadType7_MeshMeta(br, size)); break;
                case 17: model.Colliders.Add(ReadType17_Collider(br, size)); break;
                case 18: model.Edges.Add(ReadType18_Edge(br, size)); break;
                default:
                    model.Unknown.Add(new MgmRawSection { Type = type, Data = br.ReadBytes(size) });
                    break;
            }

            //long read = br.BaseStream.Position - start;
            //if (read != size)
            //    throw new InvalidDataException($"Type {type}: expected {size} bytes, read {read}.");
        }

        return model;
    }

    // ---- Type 1 (Materials) ----
    private static void ReadType1_Materials(BinaryReader br, MgmModel model, int version, int size)
    {
        long start = br.BaseStream.Position;

        int id = br.ReadInt32();
        int matCount = br.ReadInt32();

        for (int j = 0; j < matCount; j++)
        {
            var mat = new MgmMaterial { Id = br.ReadInt32() };
            int nameLen = br.ReadInt32();
            int shLen = br.ReadInt32();
            mat.Name = ReadUtf16String(br, nameLen);
            mat.Shader = ReadUtf16String(br, shLen);
            mat.Flags = br.ReadInt32();
            mat.Variant = br.ReadByte();

            int shaderCount = br.ReadInt32();
            int textureCount = br.ReadInt32();
            int lightCount = br.ReadInt32();

            for (int p = 0; p < shaderCount; p++)
            {
                string name = ReadAsciiZ(br.ReadBytes(16));
                float[] v = new float[9];
                for (int fidx = 0; fidx < 9; fidx++) v[fidx] = br.ReadSingle();

                mat.Shaders.Add(new MgmShader
                {
                    Slot = name,
                    Base = new Quaternion { X = v[0], Y = v[1], Z = v[2], W = v[3] },
                    Scalar = v[4],
                    Payload = new Quaternion { X = v[5], Y = v[6], Z = v[7], W = v[8] }
                });
            }

            for (int t = 0; t < textureCount; t++)
            {
                string slot = ReadAsciiZ(br.ReadBytes(16));
                uint texId = br.ReadUInt32();
                uint samp = br.ReadUInt32();
                uint uv = br.ReadUInt32();
                ushort off = br.ReadUInt16();
                ushort sz = br.ReadUInt16();
                byte[] payload = br.ReadBytes(512);

                mat.Textures.Add(new MgmTexture
                {
                    Slot = slot,
                    TextureId = texId,
                    SamplerStateId = samp,
                    UVSourceOrTransformId = uv,
                    ShaderParamOffsetBytes = off,
                    ShaderParamSizeBytes = sz,
                    RawPayload = payload,
                    TexturePath = ReadUtf16ZFromBuffer(payload)
                });
            }

            for (int t = 0; t < lightCount; t++)
            {
                var lr = new MgmLightRef
                {
                    Semantic = ReadAsciiZ(br.ReadBytes(16)),
                    I0 = br.ReadUInt32(),
                    I1 = br.ReadUInt32(),
                    I2 = br.ReadUInt32(),
                    OffsetBytes = br.ReadUInt16(),
                    SizeBytes = br.ReadUInt16()
                };
                for (int k = 0; k < 18; k++) lr.Basis18[k] = br.ReadSingle();
                mat.Lights.Add(lr);
            }

            model.Materials.Add(mat);
        }

        int consumed = (int)(br.BaseStream.Position - start);
        int remain = Math.Max(0, size - consumed);
    }

    private static MgmBonePose ReadType2_BonePose(BinaryReader br, int size)
    {
        long start = br.BaseStream.Position;

        int nLen = br.ReadInt32();
        int pLen = br.ReadInt32();
        uint nameHash = br.ReadUInt32();
        uint parentHash = br.ReadUInt32();
        string name = ReadUtf16String(br, nLen);
        string parentName = ReadUtf16String(br, pLen);

        // Pose block: T(3) → R(4) → P(3)
        Vector3 t = new(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
        Vector4 r = new(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
        Vector3 p = new(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());

        int consumed = (int)(br.BaseStream.Position - start);
        int remain = Math.Max(0, size - consumed);
        

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
    private static MgmBone ReadType3_Bones(BinaryReader br, int size)
    {
        long start = br.BaseStream.Position;

        int nameLen = br.ReadInt32();
        int pNameLen = br.ReadInt32();
        int nameALen = br.ReadInt32();
        uint hName = br.ReadUInt32();
        uint hParent = br.ReadUInt32();
        uint hNameAlias = br.ReadUInt32();

        string name = ReadUtf16String(br, nameLen);
        string parentName = ReadUtf16String(br, pNameLen);
        string nameAlias = ReadUtf16String(br, nameALen);

        int classValue = br.ReadInt32();
        int ClassFlag = br.ReadInt32();
        int value3 = br.ReadInt32();
        int value4 = br.ReadInt32();
        int value5 = br.ReadInt32();
        int value6 = br.ReadInt32();

        ushort u1 = br.ReadUInt16();

        float[] worldRestMatrix = ReadFloats(br, 16);
        float[] bindPoseGlobalMatrix = ReadFloats(br, 16);
        float[] worldRestMatrixDuplicate = ReadFloats(br, 16);

        Vector3 pos = new(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
        Quaternion rot = new(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
        Vector3 scl = new(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());

        int consumed = (int)(br.BaseStream.Position - start);
        int remain = Math.Max(0, size - consumed);
        

        return new MgmBone
        {
            Name = name,
            ParentName = parentName,
            NameAlias = nameAlias,
            NameHash = hName,
            ParentNameHash = hParent,
            NameAliasHash = hNameAlias,
            Class = (MgmNodeClass)classValue,
            ClassFlag = ClassFlag,
            Reserved = [value3, value4, value5, value6],
            U0 = u1,
            WorldRestMatrix = new Matrix4x4(
                worldRestMatrix[0], worldRestMatrix[1], worldRestMatrix[2], worldRestMatrix[3],
                worldRestMatrix[4], worldRestMatrix[5], worldRestMatrix[6], worldRestMatrix[7],
                worldRestMatrix[8], worldRestMatrix[9], worldRestMatrix[10], worldRestMatrix[11],
                worldRestMatrix[12], worldRestMatrix[13], worldRestMatrix[14], worldRestMatrix[15]
            ),
            BindPoseGlobalMatrix = new Matrix4x4(
                bindPoseGlobalMatrix[0], bindPoseGlobalMatrix[1], bindPoseGlobalMatrix[2], bindPoseGlobalMatrix[3],
                bindPoseGlobalMatrix[4], bindPoseGlobalMatrix[5], bindPoseGlobalMatrix[6], bindPoseGlobalMatrix[7],
                bindPoseGlobalMatrix[8], bindPoseGlobalMatrix[9], bindPoseGlobalMatrix[10], bindPoseGlobalMatrix[11],
                bindPoseGlobalMatrix[12], bindPoseGlobalMatrix[13], bindPoseGlobalMatrix[14], bindPoseGlobalMatrix[15]
            ),
            WorldRestMatrixDuplicate = new Matrix4x4(
                worldRestMatrixDuplicate[0], worldRestMatrixDuplicate[1], worldRestMatrixDuplicate[2], worldRestMatrixDuplicate[3],
                worldRestMatrixDuplicate[4], worldRestMatrixDuplicate[5], worldRestMatrixDuplicate[6], worldRestMatrixDuplicate[7],
                worldRestMatrixDuplicate[8], worldRestMatrixDuplicate[9], worldRestMatrixDuplicate[10], worldRestMatrixDuplicate[11],
                worldRestMatrixDuplicate[12], worldRestMatrixDuplicate[13], worldRestMatrixDuplicate[14], worldRestMatrixDuplicate[15]
            ),
            LocalTranslation = pos,
            LocalRotation = rot,
            LocalScale = scl

        };
    }

    // ---- Type 4 (Dummy / attachment) ----
    private static MgmDummy ReadType4_Dummy(BinaryReader br, int size)
    {
        long start = br.BaseStream.Position;

        int nameLen = br.ReadInt32();
        int targetLen = br.ReadInt32();
        uint nameHash = br.ReadUInt32();
        uint targetHash = br.ReadUInt32();

        string name = ReadUtf16String(br, nameLen);
        string target = ReadUtf16String(br, targetLen);

        // Pose block: T(3) → R(4) → P(3)
        Vector3 t = new(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
        Vector4 r = new(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
        Vector3 p = new(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());

        int consumed = (int)(br.BaseStream.Position - start);
        int remain = Math.Max(0, size - consumed);
        

        return new MgmDummy
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

    private static MgmMesh ReadType5_Mesh(BinaryReader br, int size, MgmModel model)
    {
        long start = br.BaseStream.Position;

        int nameLen = br.ReadInt32();
        int altLen = br.ReadInt32();
        uint nameHash = br.ReadUInt32();
        uint altHash = br.ReadUInt32();
        string name = ReadUtf16String(br, nameLen);
        string altName = ReadUtf16String(br, altLen);

        int layoutTag = br.ReadInt32();
        int matLen = br.ReadInt32();
        string matName = ReadUtf16String(br, matLen);

        int verticesCount = br.ReadInt32();
        int trianglesCount = br.ReadInt32();

        int flag = br.ReadInt32();

        byte[] flags = br.ReadBytes(4);
        int materialId = br.ReadInt32();

        var mesh = new MgmMesh
        {
            Name = name,
            AltName = altName,
            NameHash = nameHash,
            AltNameHash = altHash,
            MaterialName = matName,
            VertexCount = verticesCount,
            TriangleCount = trianglesCount,
            Flags = flags,
            Flag = flag,
            MaterialId = materialId,
            Material = model.Materials.FirstOrDefault(m => m.Id == materialId),
            Vertices = new MgmVertex[verticesCount],
            Indices = new ushort[trianglesCount * 3],
        };

        // Continue

        int consumed = (int)(br.BaseStream.Position - start);
        int remain = Math.Max(0, size - consumed);


        return mesh;
    }

    static int ComputeStrideForMGM(int layoutTag, int flag, byte[] flags, out MgmVertexFeatures features)
    {
        // Baseline assumptions by layout "family"
        // layoutTag == 0 : P,N,UV0 only (static-like)
        // layoutTag == 1 : P,N,UV0 + maybe UV1/tangent (weapons/props)
        // layoutTag == 2 : billboards/particles (small stride)
        // layoutTag == 3 : character/skinned (weights/indices)

        features = 0;

        // Fold the small flag fields into a simple bitmask we can pattern-match on.
        // Example convention (adjust once you confirm):
        // flags[2] bit0 => UV1, bit1 => Tangent, bit2 => Skinning, bit3 => BoneIdxU16
        if (flags?.Length >= 3)
        {
            byte f = flags[2];
            if ((f & 0x01) != 0) features |= MgmVertexFeatures.UV1;
            if ((f & 0x02) != 0) features |= MgmVertexFeatures.Tangent;
            if ((f & 0x04) != 0) features |= MgmVertexFeatures.Skinning;
            if ((f & 0x08) != 0) features |= MgmVertexFeatures.BoneIdxU16;
        }

        // Some assets also duplicate a bit in flags[3] or in "flag".
        // You can OR in extra bits here if you discover them:
        // if ((flag & 0x100) != 0) features |= MgmVertexFeatures.Tangent;

        // Base strides by family
        int stride;
        switch (layoutTag)
        {
            default:
            case 0: // P,N,UV0 (MMP-like static)
                stride = 12 + 12 + 8; // 32
                break;

            case 1: // P,N,UV0 + optional UV1/tangent
                stride = 12 + 12 + 8; // start at 32
                if (features.HasFlag(MgmVertexFeatures.UV1)) stride += 8;   // 40
                if (features.HasFlag(MgmVertexFeatures.Tangent)) stride += 16;  // +tangent(vec4) => 56
                break;

            case 2: // small/billboard/point-light like (seen as 28 in MMP)
                    // You might see P(12) + UV0(8) + packed normal(4) + extra(4) => 28
                    // Keep as 28 unless you find otherwise.
                stride = 28;
                break;

            case 3: // skinned
                    // Start with P,N,UV0
                stride = 12 + 12 + 8; // 32
                if (features.HasFlag(MgmVertexFeatures.UV1)) stride += 8;   // 40
                if (features.HasFlag(MgmVertexFeatures.Tangent)) stride += 16;  // 56

                // skin block: prefer 4x weights (float) + 4x indices (U8 or U16)
                // (Some games use half/byte weights—adjust if you find it.)
                stride += 16; // weights as 4*float
                if (features.HasFlag(MgmVertexFeatures.BoneIdxU16)) stride += 8;  // 4*ushort
                else stride += 4;  // 4*byte
                break;
        }

        return stride;
    }


    // ---- Type 6 (Skinned Meshes) ----
    private static MgmMesh ReadType6_Mesh(BinaryReader br, int size, int version)
    {
        long start = br.BaseStream.Position;

        int nameLen = br.ReadInt32();
        int altLen = br.ReadInt32();
        uint nameHash = br.ReadUInt32();
        uint altHash = br.ReadUInt32();
        string name = ReadUtf16String(br, nameLen);
        string altName = ReadUtf16String(br, altLen);

        int layoutTag = br.ReadInt32();
        int matLen = br.ReadInt32();
        string matName = ReadUtf16String(br, matLen);

        int verticesCount = br.ReadInt32();
        int trianglesCount = br.ReadInt32();

        int flag = br.ReadInt32();

        byte[] flags = br.ReadBytes(4);
        int materialId = br.ReadInt32();

        var mesh = new MgmMesh
        {
            Name = name,
            AltName = altName,
            NameHash = nameHash,
            AltNameHash = altHash,
            MaterialName = matName,

            VertexCount = verticesCount,
            TriangleCount = trianglesCount,
        };

        //more data

        int consumed = (int)(br.BaseStream.Position - start);
        int remain = Math.Max(0, size - consumed);

        return mesh;
    }

    // ---- Type 7 (Mesh metadata) ----
    private static MgmMeshMeta ReadType7_MeshMeta(BinaryReader br, int size)
    {
        long start = br.BaseStream.Position;

        int nameLen = br.ReadInt32();
        uint nameHash = br.ReadUInt32();
        string name = ReadUtf16String(br, nameLen);

        byte flag = br.ReadByte();

        Vector3 t = new(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
        Vector4 r = new(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
        Vector3 p = new(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());

        int consumed = (int)(br.BaseStream.Position - start);
        int remain = Math.Max(0, size - consumed);
        

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


    // ---- Type 17 (Collider) ----
    private static MgmCollider ReadType17_Collider(BinaryReader br, int size)
    {
        long start = br.BaseStream.Position;

        int nameLen = br.ReadInt32();
        int targetLen = br.ReadInt32();
        uint nameHash = br.ReadUInt32();
        uint targetHash = br.ReadUInt32();
        string name = ReadUtf16String(br, nameLen);
        string target = ReadUtf16String(br, targetLen);

        // Pose block: T(3) → R(4) → P(3)
        Vector3 t = new(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
        Vector4 r = new(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
        Vector3 p = new(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());

        int consumed = (int)(br.BaseStream.Position - start);
        int remain = Math.Max(0, size - consumed);

        return new MgmCollider
        {   Name = name,
            Target = target,
            NameHash = nameHash,
            TargetHash = targetHash,
            Translation = t,
            RotAnglesDeg = r,
            AuxAnglesDeg = p
        };
    }

    // ---- Type 18 (Edge definition) ----
    private static MgmEdgeDef ReadType18_Edge(BinaryReader br, int size)
    {
        long start = br.BaseStream.Position;
        int nameLen = br.ReadInt32();
        uint nameHash = br.ReadUInt32();
        string name = ReadUtf16String(br, nameLen);
        var flags = (MgmEdgeFlags)br.ReadUInt32();
        float angleDeg = br.ReadSingle();
        float bias = br.ReadSingle();

        int consumed = (int)(br.BaseStream.Position - start);
        int remain = Math.Max(0, size - consumed);
        
        return new MgmEdgeDef { Name = name, NameHash = nameHash, Flags = flags, AngleDeg = angleDeg, Bias = bias };
    }

    private static string ReadUtf16String(BinaryReader br, int charCount)
        => charCount <= 0 ? string.Empty : Encoding.Unicode.GetString(br.ReadBytes(charCount * 2));

    private static string ReadAsciiZ(byte[] bytes)
    {
        int len = Array.IndexOf<byte>(bytes, 0);
        return Encoding.ASCII.GetString(bytes, 0, len >= 0 ? len : bytes.Length);
    }

    private static string? ReadUtf16ZFromBuffer(byte[] buffer)
    {
        for (int i = 0; i + 1 < buffer.Length; i += 2)
        {
            if (buffer[i] == 0 && buffer[i + 1] == 0)
                return Encoding.Unicode.GetString(buffer, 0, i);
        }
        return string.Empty;
    }

    static float[] ReadFloats(BinaryReader br, int n)
    {
        float[] a = new float[n];
        for (int i = 0; i < n; i++) a[i] = br.ReadSingle();
        return a;
    }

}
