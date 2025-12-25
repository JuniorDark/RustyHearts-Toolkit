using System.Numerics;

namespace RHToolkit.Models.Model3D.Map;

/// <summary>
/// Builds a .HEIGHT file from a .NAVI mesh.
/// </summary>
public static class HeightWriter
{
    private const int DefaultRayStartPadding = 100; // start above geometry
    private const int HeightStepXZ = 20; // grid cell size in XZ
    private const int HeightStepY = 50; // vertical band size

    public static async Task BuildFromNaviFileAsync(
        string naviPath,
        string outHeightPath,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(naviPath)) throw new ArgumentNullException(nameof(naviPath));
        if (string.IsNullOrWhiteSpace(outHeightPath)) throw new ArgumentNullException(nameof(outHeightPath));

        var navi = await NaviReader.ReadAsync(naviPath, ct).ConfigureAwait(false);
        var (verts, tris) = FlattenMesh(navi);

        if (verts.Length == 0 || tris.Length == 0)
            throw new InvalidDataException("NAVI contains no vertices or triangles.");

        var minV = ComputeMin(verts);
        var maxV = ComputeMax(verts);

        int minX = (int)minV.X;
        int minY = (int)minV.Y;
        int minZ = (int)minV.Z;

        float rayStartY = maxV.Y + DefaultRayStartPadding;

        // Map tri -> octree index (approx): first octree leaf that references this tri.
        // (If a tri is referenced multiple times, we keep the first.)
        var triToOct = BuildTriangleToOctreeMap(navi, tris.Length);

        // Build candidate triangle lists per (ix,iz) key by rasterizing triangle XZ AABBs into grid bins.
        var cellTris = BuildCellTriangleMap(verts, tris, minX, minZ, HeightStepXZ);

        // Compute MaxY and Full tables.
        var maxY = new Dictionary<ulong, (ushort oct, ushort tri, float h)>(cellTris.Count);
        var full = new Dictionary<ulong, (ushort oct, ushort tri, float h)>();

        foreach (var kv in cellTris)
        {
            ct.ThrowIfCancellationRequested();

            int ix = kv.Key.ix;
            int iz = kv.Key.iz;
            var candidates = kv.Value;

            float x = minX + ix * HeightStepXZ;
            float z = minZ + iz * HeightStepXZ;

            // MaxY: cast from above all geometry, choose nearest hit downward => topmost surface.
            if (!TryRaycastDown(
                    origin: new Vector3(x, rayStartY, z),
                    verts: verts,
                    tris: tris,
                    triCandidates: candidates,
                    out int hitTri,
                    out float hitY))
            {
                continue;
            }

            ushort oct = triToOct[hitTri] != ushort.MaxValue ? triToOct[hitTri] : (ushort)0;

            ulong kMax = MakeKeyMaxY(ix, iz);
            maxY[kMax] = (oct, (ushort)hitTri, hitY);


            // Full table approximation:
            // For each Y band below the MaxY surface, shoot a ray from just below (bandTop + 50)
            // and store the first surface below that height.
            //
            // The runtime uses Full when (queryY + 50) < MaxYHeight; queryY is bucketed by stepY.
            // We emulate "bandTop + 50 < MaxYHeight" as the inclusion condition.
            int maxIy = (int)MathF.Ceiling((hitY - minY) / HeightStepY);
            for (int iy = 0; iy <= maxIy; iy++)
            {
                float bandTop = (minY + iy * HeightStepY) + (HeightStepY - 1);
                if (bandTop + 50.0f >= hitY)
                    continue;

                // Start slightly below the "MaxY acceptance line" for that band
                // so we don't just re-hit the top surface.
                float y0 = (bandTop + 50.0f) - 0.01f;

                if (!TryRaycastDown(
                        origin: new Vector3(x, y0, z),
                        verts: verts,
                        tris: tris,
                        triCandidates: candidates,
                        out int hitTri2,
                        out float hitY2))
                {
                    continue;
                }

                ushort oct2 = triToOct[hitTri2] != ushort.MaxValue ? triToOct[hitTri2] : (ushort)0;

                ulong kFull = MakeKeyFull(ix, iy, iz);
                // If duplicates happen, keep the higher surface (closer to y0) – typically the intended one.
                if (full.TryGetValue(kFull, out var existing))
                {
                    if (hitY2 > existing.h)
                        full[kFull] = (oct2, (ushort)hitTri2, hitY2);
                }
                else
                {
                    full[kFull] = (oct2, (ushort)hitTri2, hitY2);
                }
            }
        }

        // Write .height (version 2)
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outHeightPath))!);

        await using var fs = new FileStream(outHeightPath, FileMode.Create, FileAccess.Write, FileShare.None);
        using var bw = new BinaryWriter(fs);

        bw.Write(2);                 // version
        bw.Write(HeightStepXZ);      // nHeightStep
        bw.Write(HeightStepY);       // nHeightStepY

        bw.Write(minX);              // nNaviHeightTableMinX
        bw.Write(minY);              // nNaviHeightTableMinY
        bw.Write(minZ);              // nNaviHeightTableMinZ

        // Counts
        bw.Write(maxY.Count);
        bw.Write(full.Count);

        foreach (var kv in maxY.OrderBy(p => p.Key))
        {
            bw.Write(kv.Key);
            bw.Write(kv.Value.oct);
            bw.Write(kv.Value.tri);
            bw.Write(kv.Value.h);
        }

        foreach (var kv in full.OrderBy(p => p.Key))
        {
            bw.Write(kv.Key);
            bw.Write(kv.Value.oct);
            bw.Write(kv.Value.tri);
            bw.Write(kv.Value.h);
        }
    }

    // --- Key packing ---

    private static ulong MakeKeyMaxY(int ix, int iz)
        => ((ulong)(uint)ix << 32) | (uint)iz;

    private static ulong MakeKeyFull(int ix, int iy, int iz)
        => ((ulong)(uint)ix << 40) | ((ulong)(uint)iy << 20) | (uint)iz;

    // --- Mesh flattening ---

    private static (Vector3[] vertices, (int A, int B, int C)[] triangles) FlattenMesh(NaviMeshFile navi)
    {
        var vList = new List<Vector3>(8192);
        var tList = new List<(int, int, int)>(8192);

        foreach (var entry in navi.Entries)
        {
            int vBase = vList.Count;
            vList.AddRange(entry.Vertices);

            // Each entry's Indices are triangles (A,B,C) into that entry's vertices.
            foreach (var tri in entry.Indices)
            {
                tList.Add((tri.A + vBase, tri.B + vBase, tri.C + vBase));
            }
        }

        return (vList.ToArray(), tList.ToArray());
    }

    private static Vector3 ComputeMin(Vector3[] v)
    {
        var m = v[0];
        for (int i = 1; i < v.Length; i++)
        {
            m.X = MathF.Min(m.X, v[i].X);
            m.Y = MathF.Min(m.Y, v[i].Y);
            m.Z = MathF.Min(m.Z, v[i].Z);
        }
        return m;
    }

    private static Vector3 ComputeMax(Vector3[] v)
    {
        var m = v[0];
        for (int i = 1; i < v.Length; i++)
        {
            m.X = MathF.Max(m.X, v[i].X);
            m.Y = MathF.Max(m.Y, v[i].Y);
            m.Z = MathF.Max(m.Z, v[i].Z);
        }
        return m;
    }

    // --- Triangle->Octree map (best-effort) ---

    private static ushort[] BuildTriangleToOctreeMap(NaviMeshFile navi, int triCount)
    {
        var map = new ushort[triCount];
        Array.Fill(map, ushort.MaxValue);

        // Use the octree's declared index, but clamp to ushort
        foreach (var oct in navi.Octrees)
        {
            if (!oct.Subdivided && oct.Indices != null)
            {
                ushort octId = (ushort)Math.Clamp(oct.OctreeIndex, 0, ushort.MaxValue);

                for (int i = 0; i < oct.Indices.Count; i++)
                {
                    int tri = oct.Indices[i];
                    if ((uint)tri < (uint)triCount && map[tri] == ushort.MaxValue)
                        map[tri] = octId;
                }
            }
        }
        return map;
    }

    // --- Candidate triangle bins per (ix,iz) ---

    private static Dictionary<(int ix, int iz), List<int>> BuildCellTriangleMap(
        Vector3[] verts,
        (int A, int B, int C)[] tris,
        int minX,
        int minZ,
        int stepXZ)
    {
        var dict = new Dictionary<(int ix, int iz), List<int>>(capacity: Math.Max(1024, tris.Length / 2));

        for (int ti = 0; ti < tris.Length; ti++)
        {
            var (A, B, C) = tris[ti];
            var v0 = verts[A];
            var v1 = verts[B];
            var v2 = verts[C];

            float triMinX = MathF.Min(v0.X, MathF.Min(v1.X, v2.X));
            float triMaxX = MathF.Max(v0.X, MathF.Max(v1.X, v2.X));
            float triMinZ = MathF.Min(v0.Z, MathF.Min(v1.Z, v2.Z));
            float triMaxZ = MathF.Max(v0.Z, MathF.Max(v1.Z, v2.Z));

            int ix0 = (int)MathF.Floor((triMinX - minX) / stepXZ);
            int ix1 = (int)MathF.Floor((triMaxX - minX) / stepXZ);
            int iz0 = (int)MathF.Floor((triMinZ - minZ) / stepXZ);
            int iz1 = (int)MathF.Floor((triMaxZ - minZ) / stepXZ);

            // Expand slightly to be safe when triangles lie near grid lines
            ix0 -= 1; iz0 -= 1; ix1 += 1; iz1 += 1;

            for (int ix = ix0; ix <= ix1; ix++)
            {
                for (int iz = iz0; iz <= iz1; iz++)
                {
                    var key = (ix, iz);
                    if (!dict.TryGetValue(key, out var list))
                    {
                        list = new List<int>(8);
                        dict[key] = list;
                    }
                    list.Add(ti);
                }
            }
        }

        return dict;
    }

    // --- Ray casting ---

    private static bool TryRaycastDown(
        Vector3 origin,
        Vector3[] verts,
        (int A, int B, int C)[] tris,
        List<int> triCandidates,
        out int hitTriIndex,
        out float hitY)
    {
        // Downward ray
        var dir = new Vector3(0, -1, 0);

        hitTriIndex = -1;
        hitY = float.NaN;

        float bestT = float.PositiveInfinity;

        for (int i = 0; i < triCandidates.Count; i++)
        {
            int ti = triCandidates[i];
            var (A, B, C) = tris[ti];

            if (RayIntersectsTriangle(origin, dir, verts[A], verts[B], verts[C], out float t))
            {
                if (t >= 0.0f && t < bestT)
                {
                    bestT = t;
                    hitTriIndex = ti;
                }
            }
        }

        if (hitTriIndex < 0 || !float.IsFinite(bestT))
            return false;

        hitY = origin.Y - bestT;
        return true;
    }

    /// <summary>
    /// Standard Möller–Trumbore ray/triangle intersection using float math.
    /// Returns distance t along the ray direction (dir assumed normalized).
    /// </summary>
    private static bool RayIntersectsTriangle(
        Vector3 rayOrigin,
        Vector3 rayDir,
        Vector3 v0,
        Vector3 v1,
        Vector3 v2,
        out float t)
    {
        const float EPS = 1e-6f;

        var e1 = v1 - v0;
        var e2 = v2 - v0;

        var p = Vector3.Cross(rayDir, e2);
        float det = Vector3.Dot(e1, p);

        if (det > -EPS && det < EPS)
        {
            t = 0;
            return false; // parallel or degenerate
        }

        float invDet = 1.0f / det;

        var s = rayOrigin - v0;
        float u = Vector3.Dot(s, p) * invDet;
        if (u < 0.0f || u > 1.0f)
        {
            t = 0;
            return false;
        }

        var q = Vector3.Cross(s, e1);
        float v = Vector3.Dot(rayDir, q) * invDet;
        if (v < 0.0f || u + v > 1.0f)
        {
            t = 0;
            return false;
        }

        float tt = Vector3.Dot(e2, q) * invDet;
        if (tt < 0.0f)
        {
            t = 0;
            return false;
        }

        t = tt;
        return true;
    }
}
