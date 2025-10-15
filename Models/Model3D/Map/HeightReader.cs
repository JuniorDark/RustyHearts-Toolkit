using System.Numerics;

namespace RHToolkit.Models.Model3D.Map;

/// <summary>
/// Reader for .height files used by the game engine for quick height lookups.
/// </summary>
public static class HeightReader
{
    public static async Task<NaviHeightFile> ReadAsync(string path, CancellationToken ct = default)
    {
        byte[] bytes = await File.ReadAllBytesAsync(path, ct).ConfigureAwait(false);
        using var ms = new MemoryStream(bytes, writable: false);
        using var br = new BinaryReader(ms, Encoding.ASCII, leaveOpen: false);
        return Read(br);
    }

    public static NaviHeightFile Read(BinaryReader br)
    {
        long start = br.BaseStream.Position;

        int version = br.ReadInt32();

        int stepXZ = 0, stepY = 0;
        if (version >= 2)
        {
            stepXZ = br.ReadInt32();   // nHeightStep (X/Z)
            stepY = br.ReadInt32();   // nHeightStepY
        }

        int minX = br.ReadInt32();
        int minY = br.ReadInt32();
        int minZ = br.ReadInt32();

        int nNum = br.ReadInt32(); // MaxY entries
        int nNum2 = br.ReadInt32(); // Full entries

        var file = new NaviHeightFile
        {
            Version = version,
            HeightStepXZ = stepXZ,
            HeightStepY = stepY,
            Min = new Vector3(minX, minY, minZ),
            MaxY = new Dictionary<ulong, NaviHeightEntry>(nNum),
            Full = new Dictionary<ulong, NaviHeightEntry>(nNum2)
        };

        // Read MaxY table (nNum entries)
        for (int i = 0; i < nNum; i++)
        {
            ulong key = br.ReadUInt64();
            ushort octIdx = br.ReadUInt16();
            ushort navIdx = br.ReadUInt16();
            float height = br.ReadSingle();
            file.MaxY[key] = new NaviHeightEntry(octIdx, navIdx, height);
        }

        // Read Full table (nNum2 entries)
        for (int i = 0; i < nNum2; i++)
        {
            ulong key = br.ReadUInt64();
            ushort octIdx = br.ReadUInt16();
            ushort navIdx = br.ReadUInt16();
            float height = br.ReadSingle();
            file.Full[key] = new NaviHeightEntry(octIdx, navIdx, height);
        }

        return file;
    }
}

// ===== Models + helpers =====

public sealed class NaviHeightFile
{
    public int Version { get; set; }
    public int HeightStepXZ { get; set; }   // nHeightStep
    public int HeightStepY { get; set; }   // nHeightStepY
    public Vector3 Min { get; set; }        // nNaviHeightTableMinX/Y/Z

    /// <summary>Quick lookup by (ix, iz): keyMaxY = ((ulong)ix << 32) | (uint)iz</summary>
    public Dictionary<ulong, NaviHeightEntry> MaxY { get; set; } = [];

    /// <summary>Full lookup by (ix, iy, iz): keyFull = ((ulong)ix << 40) | ((ulong)iy << 20) | (ulong)iz</summary>
    public Dictionary<ulong, NaviHeightEntry> Full { get; set; } = [];


    public (int ix, int iy, int iz) ToGrid(Vector3 world)
    {
        int ix = HeightStepXZ != 0 ? (int)((world.X - Min.X) / HeightStepXZ) : 0;
        int iy = HeightStepY != 0 ? (int)((world.Y - Min.Y) / HeightStepY) : 0;
        int iz = HeightStepXZ != 0 ? (int)((world.Z - Min.Z) / HeightStepXZ) : 0;
        return (ix, iy, iz);
    }

    public static ulong MakeKeyMaxY(int ix, int iz)
        => ((ulong)(uint)ix << 32) | (uint)iz;

    public static ulong MakeKeyFull(int ix, int iy, int iz)
        => (((ulong)(uint)ix << 40) | ((ulong)(uint)iy << 20) | (uint)iz);

    /// <summary>
    /// try MaxY; if (y+50 >= height) accept; otherwise try Full.
    /// Returns true when a height is found; out args carry the details.
    /// </summary>
    public bool TryGetHeight(Vector3 world, out float height, out int octreeIndex, out int naviIndex)
    {
        var (ix, iy, iz) = ToGrid(world);

        // 1) MaxY quick path
        ulong kMax = MakeKeyMaxY(ix, iz);
        if (MaxY.TryGetValue(kMax, out var eMax))
        {
            if (world.Y + 50.0f >= eMax.Height)
            {
                height = eMax.Height; octreeIndex = eMax.OctreeIndex; naviIndex = eMax.NaviIndex;
                return true;
            }
        }

        // 2) Full table
        ulong kFull = MakeKeyFull(ix, iy, iz);
        if (Full.TryGetValue(kFull, out var eFull))
        {
            height = eFull.Height; octreeIndex = eFull.OctreeIndex; naviIndex = eFull.NaviIndex;
            return true;
        }

        height = default; octreeIndex = -1; naviIndex = -1;
        return false;
    }
}

public readonly struct NaviHeightEntry(ushort oct, ushort nav, float h)
{
    public readonly ushort OctreeIndex = oct;
    public readonly ushort NaviIndex = nav;
    public readonly float Height = h;

    public void Deconstruct(out ushort oct, out ushort nav, out float h)
    { oct = OctreeIndex; nav = NaviIndex; h = Height; }
}
