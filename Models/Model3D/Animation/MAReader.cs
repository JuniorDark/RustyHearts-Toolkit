using RHToolkit.Models.Model3D.MGM;
using System.Numerics;

namespace RHToolkit.Models.Model3D.Animation;

#region Data Models
public sealed class MaHeader
{
    public int Version { get; set; }
    public int NumObjects { get; set; }
    public int[] Index { get; set; } = [];
    public int[] Length { get; set; } = [];
    public int[] TypeId { get; set; } = [];
}

public sealed class MaAnimation
{
    public int Version { get; set; }
    public MaHeader Header { get; set; } = new();
    public string BaseDirectory { get; set; } = string.Empty;
    public float ClipLength { get; set; } // type 0
    public List<MaBoundingVolume> BoundingVolumes { get; set; } = [];  // type 7
    public List<AnimTrack> Tracks { get; set; } = [];    // type 3
}

// ----- Type 3
public sealed class AnimTrack
{
    public string Name { get; set; } = string.Empty;
    public string ParentName { get; set; } = string.Empty;
    public string Name2 { get; set; } = string.Empty;
    public uint NameHash { get; set; }
    public uint ParentHash { get; set; }
    public uint Name2Hash { get; set; }
    public MgmBoneType TargetBoneType { get; set; } = MgmBoneType.Bone;

    // Flags / extras
    public int Unknown1 { get; set; } // usually 1
    public bool HasMoveWeight { get; set; }
    public bool HasBoundingVolumeAnim { get; set; }

    // Channels
    public MaTrackData TrackData { get; set; } = new();
    public AnimChannel<Vector3> Position { get; } = new();
    public bool IsRootBone { get; set; }
    public AnimChannel<Quaternion> Rotation { get; } = new();
    public AnimChannel<Vector3> Scale { get; } = new();
    public MoveWeightSection? MoveWeight { get; set; }
    public Vector4 BoundingVolumeAnim { get; set; }
    public List<AuxWindow> AuxWindows { get; set; } = [];

    public override string ToString() => $"Track[{Name}] P:{Position.Count} R:{Rotation.Count} S:{Scale.Count}";
}

public sealed class AnimChannel<T>
{
    private readonly List<(ushort t, short[] v)> _rawKeys = [];
    private readonly List<(float t, T v)> _keys = [];
    public int Count => _keys.Count;
    public IReadOnlyList<(ushort t, short[] v)> RawKeys => _rawKeys;
    public IReadOnlyList<(float t, T v)> Keys => _keys;
    public void AddRaw(ushort time, short[] values) => _rawKeys.Add((time, values));
    public void Add(float timeSec, T value) => _keys.Add((timeSec, value));
}

public struct M3dMoveWeight
{
    public float StartTime;
    public float EndTime;
    public Vector3 StartPos;
    public Vector3 EndPos;
}

// v>=8 “-1.0f tagged” short record: time + vec3 (16 bytes)
public struct M3dMoveWeightDelta
{
    public float Time;
    public Vector3 Pos;
}

public sealed class MoveWeightSection
{
    public int Count { get; internal set; }
    public float HeaderValue { get; internal set; }
    public List<M3dMoveWeight> FullRecords { get; } = [];
    public List<M3dMoveWeightDelta> DeltaRecords { get; } = [];
}

public sealed class AuxWindow
{
    public ushort StartTick { get; init; }
    public ushort EndTick { get; init; }
    public float StartSec { get; init; }
    public float EndSec { get; init; }
    public bool Wraps { get; init; } // Start > End => spans clip end -> start
}

// ----- Type 7
public sealed class MaBoundingVolume
{
    public string Name { get; set; } = string.Empty;   // e.g., "Main"
    public uint NameHash { get; set; }
    public bool IsMain { get; set; }

    // Bounding sphere center
    public Vector3 Center { get; set; }

    // Bounding sphere radius
    public float Radius { get; set; }

    // Axis-aligned bounding box
    public Vector3 AabbMin { get; set; }
    public Vector3 AabbMax { get; set; }

}

/// <summary>
/// Track decompression data.
/// </summary>
public sealed class MaTrackData
{
    public Matrix4x4 Matrix1 { get; private set; }
    public Matrix4x4 Matrix2 { get; private set; }
    public Matrix4x4 Matrix3 { get; private set; }

    public Vector3 Vector1 { get; private set; }
    public Quaternion Quaternion1 { get; private set; }
    public Vector3 Vector2 { get; private set; }

    public static MaTrackData ReadFrom(BinaryReader br)
    {
        return new MaTrackData
        {
            Matrix1 = BinaryReaderExtensions.ReadMatrix4x4(br),
            Matrix2 = BinaryReaderExtensions.ReadMatrix4x4(br),
            Matrix3 = BinaryReaderExtensions.ReadMatrix4x4(br),

            Vector1 = BinaryReaderExtensions.ReadVector3(br),
            Quaternion1 = BinaryReaderExtensions.ReadQuaternion(br),
            Vector2 = BinaryReaderExtensions.ReadVector3(br),
        };
    }
}

#endregion

public class MAReader
{
    private const string HEADER = "DoBal";

    public async Task<MaAnimation> ReadAsync(string path, CancellationToken ct = default)
    {
        var baseDir = Path.GetDirectoryName(path)!;

        await using var fs = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 64 * 1024,
            useAsync: true);

        using var br = new BinaryReader(fs, Encoding.ASCII, leaveOpen: false);
        return await Task.Run(() => ReadMA(br, baseDir), ct);
    }

    private static MaAnimation ReadMA(BinaryReader br, string baseDir)
    {
        // ---- Header ----
        string header = Encoding.ASCII.GetString(br.ReadBytes(5));
        if (header != HEADER) throw new InvalidDataException("Not an MA file.");
        var version = br.ReadInt32();

        var anim = new MaAnimation { Version = version, BaseDirectory = baseDir };
        anim.Header.Version = version;

        int objCount = br.ReadInt32();
        anim.Header.NumObjects = objCount;

        // Object tables: offsets, sizes, types IDs
        anim.Header.Index = new int[objCount];
        anim.Header.Length = new int[objCount];
        anim.Header.TypeId = new int[objCount];

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
                    case 0:
                        anim.ClipLength = br.ReadSingle();
                        break;
                    case 3:
                        anim.Tracks.Add(ReadType3_Track(br, size, version, anim.ClipLength));
                        break;
                    case 7:
                        anim.BoundingVolumes.Add(ReadType7_BoundingVolume(br, size, version));
                        break;
                    default:
                        throw new NotSupportedException($"Unknown/Unsupported object type: {type} (at index {i})");
                }

                int consumed = (int)(br.BaseStream.Position - start);
                int remain = Math.Max(0, size - consumed);
                if (remain != 0)
                    throw new InvalidDataException($"Type {type}: expected {size} bytes, read {consumed}.");
            }
            catch (Exception ex)
            {
                throw new Exception($"Type {type}: Error on Position: 0x{br.BaseStream.Position:X8}: {ex}");
            }
        }

        return anim;
    }

    private static AnimTrack ReadType3_Track(BinaryReader br, int size, int version, float clipLength)
    {
        long start = br.BaseStream.Position;

        // ---- Names & hashes ----
        string name, parentName, alias;
        uint nameHash, parentHash, aliasHash;

        if (version >= 6)
        {
            int nLen = br.ReadInt32();
            int pLen = br.ReadInt32();
            int aLen = br.ReadInt32();
            nameHash = br.ReadUInt32();
            parentHash = br.ReadUInt32();
            aliasHash = br.ReadUInt32();
            name = BinaryReaderExtensions.ReadUtf16String(br, nLen);
            parentName = BinaryReaderExtensions.ReadUtf16String(br, pLen);
            alias = BinaryReaderExtensions.ReadUtf16String(br, aLen);
        }
        else
        {
            nameHash = br.ReadUInt32();
            parentHash = br.ReadUInt32();
            aliasHash = br.ReadUInt32();
            name = BinaryReaderExtensions.ReadUnicode256Count(br);
            parentName = BinaryReaderExtensions.ReadUnicode256Count(br);
            alias = BinaryReaderExtensions.ReadUnicode256Count(br);
        }

        var track = new AnimTrack
        {
            Name = name,
            ParentName = parentName,
            Name2 = alias,
            NameHash = nameHash,
            ParentHash = parentHash,
            Name2Hash = aliasHash,
            TargetBoneType = (MgmBoneType)br.ReadInt32(),
            Unknown1 = br.ReadInt32()
        };

        // Key counts
        int posCount = br.ReadInt32();
        int rotCount = br.ReadInt32();
        int sclCount = br.ReadInt32();
        int auxCount = br.ReadInt32();

        // Flags
        byte flag = br.ReadByte();
        track.HasMoveWeight = flag != 0;

        if (version >= 7)
        {
            byte boundingVolumeAniData = br.ReadByte();
            track.HasBoundingVolumeAnim = boundingVolumeAniData != 0;
        }

        // track data
        var H = MaTrackData.ReadFrom(br);
        track.TrackData = H;

        float TickToSec(ushort t) => t * (clipLength / 65535f);

        if (posCount > 0)
        {
            if (version >= 8)
            {
                byte isRoot = br.ReadByte(); // 0=int16, 1=float32
                track.IsRootBone = isRoot == 1;
                ushort[] ticks = ReadTimes(br, posCount);

                if (track.IsRootBone)
                {
                    for (int i = 0; i < posCount; i++)
                    {
                        var p = BinaryReaderExtensions.ReadVector3(br);
                        track.Position.Add(TickToSec(ticks[i]), p);
                    }
                }
                else
                {
                    for (int i = 0; i < posCount; i++)
                    {
                        short px = br.ReadInt16(), py = br.ReadInt16(), pz = br.ReadInt16();
                        var n = new Vector3(px, py, pz) / 32767f;           // [-1,1]
                        track.Position.Add(TickToSec(ticks[i]), n);
                        track.Position.AddRaw(ticks[i], [px, py, pz]);
                    }
                }
            }
            else
            {
                float[] times = ReadFTimes(br, posCount);
                for (int i = 0; i < posCount; i++)
                {
                    var p = BinaryReaderExtensions.ReadVector3(br);
                    track.Position.Add(times[i], p);
                }
            }
        }

        // ---- Rotation ----
        if (rotCount > 0)
        {
            if (version >= 8)
            {
                ushort[] ticks = ReadTimes(br, rotCount);
                for (int i = 0; i < rotCount; i++)
                {
                    short qx = br.ReadInt16(), qy = br.ReadInt16(), qz = br.ReadInt16(), qw = br.ReadInt16();
                    var q = new Quaternion(qx / 32767f, qy / 32767f, qz / 32767f, qw / 32767f);
                    track.Rotation.Add(TickToSec(ticks[i]), q);
                    track.Rotation.AddRaw(ticks[i], [qx, qy, qz, qw]);
                }
            }
            // non-quantized float32 scale
            else
            {
                float[] times = ReadFTimes(br, rotCount);
                for (int i = 0; i < rotCount; i++)
                {
                    var q = BinaryReaderExtensions.ReadQuaternion(br);
                    track.Rotation.Add(times[i], q);
                }
            }
        }

        // ---- Scale ----
        if (sclCount > 0)
        {
            if (version >= 8)
            {
                ushort[] ticks = ReadTimes(br, sclCount);
                for (int i = 0; i < sclCount; i++)
                {
                    short sx = br.ReadInt16(), sy = br.ReadInt16(), sz = br.ReadInt16();
                    var scl = new Vector3(sx, sy, sz) / 32767f;     // [-1,1] basis
                    track.Scale.Add(TickToSec(ticks[i]), scl);
                    track.Scale.AddRaw(ticks[i], [sx, sy, sz]);
                }
            }
            // non-quantized float32 scale
            else
            {
                float[] times = ReadFTimes(br, sclCount);
                for (int i = 0; i < sclCount; i++)
                {
                    var scl = BinaryReaderExtensions.ReadVector3(br);
                    track.Scale.Add(times[i], scl);
                }
            }

        }

        if (auxCount != 0)
        {
            if (version >= 8)
            {
                var starts = new ushort[auxCount];
                var ends = new ushort[auxCount];
                for (int i = 0; i < auxCount; i++) starts[i] = br.ReadUInt16();
                for (int i = 0; i < auxCount; i++) ends[i] = br.ReadUInt16();

                float scale = clipLength / 65535.0f;
                for (int i = 0; i < auxCount; i++)
                {
                    ushort s = starts[i], e = ends[i];
                    track.AuxWindows.Add(new AuxWindow
                    {
                        StartTick = s,
                        EndTick = e,
                        StartSec = s * scale,
                        EndSec = e * scale,
                        Wraps = s > e
                    });
                }
            }
            else
            {
                for (int i = 0; i < auxCount; i++)
                {
                    float startSec = br.ReadSingle();
                    float endSec = br.ReadSingle();
                    track.AuxWindows.Add(new AuxWindow
                    {
                        StartTick = 0,
                        EndTick = 0,
                        StartSec = startSec,
                        EndSec = endSec,
                        Wraps = startSec > endSec
                    });
                }
            }
        }

        // M3dMoveWeight
        bool isRootBone = parentHash == 0;

        if (isRootBone && track.HasMoveWeight)
        {
            var section = new MoveWeightSection();

            int count = br.ReadInt32();
            section.Count = count;

            float header = br.ReadSingle();
            section.HeaderValue = header;

            if (version >= 8)
            {
                for (int i = 0; i < count; i++)
                {
                    float tagOrStart = br.ReadSingle();

                    if (i > 0 && tagOrStart == -1.0f)
                    {
                        // Short delta/marker: time + vec3 (16 bytes)
                        float time = br.ReadSingle();
                        Vector3 pos = BinaryReaderExtensions.ReadVector3(br);
                        section.DeltaRecords.Add(new M3dMoveWeightDelta
                        {
                            Time = time,
                            Pos = pos
                        });
                    }
                    else
                    {
                        // Full 28-byte tail: EndTime + StartPos + EndPos
                        float endTime = br.ReadSingle();
                        Vector3 startPos = BinaryReaderExtensions.ReadVector3(br);
                        Vector3 endPos = BinaryReaderExtensions.ReadVector3(br);

                        section.FullRecords.Add(new M3dMoveWeight
                        {
                            StartTime = tagOrStart,
                            EndTime = endTime,
                            StartPos = startPos,
                            EndPos = endPos
                        });
                    }
                }
            }
            else
            {
                // v<8: straight array of 32-byte structs
                for (int i = 0; i < count; i++)
                {
                    var rec = new M3dMoveWeight
                    {
                        StartTime = br.ReadSingle(),
                        EndTime = br.ReadSingle(),
                        StartPos = BinaryReaderExtensions.ReadVector3(br),
                        EndPos = BinaryReaderExtensions.ReadVector3(br)
                    };
                    section.FullRecords.Add(rec);
                }
            }

            track.MoveWeight = section;

            // M3dBoundingVolumeAniData
            if (track.HasBoundingVolumeAnim)
            {
                int boundingVolumeCount = br.ReadInt32();
                for (int i = 0; i < boundingVolumeCount; i++)
                {
                    track.BoundingVolumeAnim = BinaryReaderExtensions.ReadVector4(br);
                }
            }
        }

        int consumed = (int)(br.BaseStream.Position - start);
        int remain = Math.Max(0, size - consumed);

        return track;
    }

    private static ushort[] ReadTimes(BinaryReader br, int count)
    {
        var arr = new ushort[count];
        for (int i = 0; i < count; i++) arr[i] = br.ReadUInt16();
        return arr;
    }

    private static float[] ReadFTimes(BinaryReader br, int count)
    {
        var arr = new float[count];
        for (int i = 0; i < count; i++) arr[i] = br.ReadSingle();
        return arr;
    }

    // ---- Type 7 (Bounding Volume) ----
    private static MaBoundingVolume ReadType7_BoundingVolume(BinaryReader br, int size, int version)
    {
        long start = br.BaseStream.Position;

        string name;
        uint nameHash;
        if (version >= 6)
        {
            int nameLen = br.ReadInt32();
            nameHash = br.ReadUInt32();
            name = BinaryReaderExtensions.ReadUtf16String(br, nameLen);
        }
        else
        {
            nameHash = br.ReadUInt32();
            name = BinaryReaderExtensions.ReadUnicode256Count(br);
        }

        byte isMain = br.ReadByte();

        return new MaBoundingVolume
        {
            Name = name,
            NameHash = nameHash,
            IsMain = isMain == 1,
            Center = BinaryReaderExtensions.ReadVector3(br),
            Radius = br.ReadSingle(),
            AabbMin = BinaryReaderExtensions.ReadVector3(br),
            AabbMax = BinaryReaderExtensions.ReadVector3(br)
        };
    }

}