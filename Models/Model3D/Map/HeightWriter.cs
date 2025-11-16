using System.Numerics;

namespace RHToolkit.Models.Model3D.Map
{
    /// <summary>
    /// Writes a .HEIGHT file from a .NAVI mesh.
    /// </summary>
    public static class HeightWriter
    {
        // Möller–Trumbore tolerances 
        private const float EPS_DET_MIN = 1e-9f;   // det guard (engine runs eps≈0)
        private const float EPS_UV = 1e-7f;   // u/v & u+v slack
        private const float T_TIE_EPS = 1e-8f;   // t/y tie break epsilon

        public static async Task BuildFromNaviFileAsync(
            string naviPath,
            string outHeightPath,
            int heightStepXZ = 20,
            int heightStepY = 50,
            int version = 2)
        {
            var navi = await NaviReader.ReadAsync(naviPath).ConfigureAwait(false);
            BuildFromNavi(navi, outHeightPath, heightStepXZ, heightStepY, version);
        }

        public static void BuildFromNavi(
            NaviMeshFile navi,
            string outHeightPath,
            int heightStepXZ = 20,
            int heightStepY = 50,
            int version = 2)
        {
            // --- 1) Collect triangles from ClassID 16
            var verts = new List<Vector3>();
            var tris = new List<(int A, int B, int C)>();

            if (navi.Entries.Count > 0)
            {
                var e = navi.Entries[0];
                int baseIndex = verts.Count;
                verts.AddRange(e.Vertices);
                for (int i = 0; i < e.IndexCount; i++)
                    tris.Add((baseIndex + e.Indices[i].A, baseIndex + e.Indices[i].B, baseIndex + e.Indices[i].C));
            }
            else
            {
                throw new InvalidDataException("No triangles found in NAVI mesh.");
            }

            // --- 2) Integer-trunc mins
            float minXf = float.PositiveInfinity, minYf = float.PositiveInfinity, minZf = float.PositiveInfinity;
            foreach (var v in verts)
            {
                if (v.X < minXf) minXf = v.X;
                if (v.Y < minYf) minYf = v.Y;
                if (v.Z < minZf) minZf = v.Z;
            }
            int minX = (int)minXf;
            int minY = (int)minYf;
            int minZ = (int)minZf;

            // --- 3) Octree leaves for OctreeIndex
            var leaves = navi.Octrees.Where(o => !o.Subdivided).ToArray();
            ushort FindLeafIndex(float x, float y, float z)
            {
                foreach (var node in leaves)
                {
                    var mn = node.GeometryBounds.Min;
                    var mx = node.GeometryBounds.Max;
                    if (x >= mn.X && x <= mx.X &&
                        y >= mn.Y && y <= mx.Y &&
                        z >= mn.Z && z <= mx.Z)
                        return (ushort)node.OctreeIndex;
                }
                return 0;
            }

            // --- 4) Precompute XZ AABBs
            var triAabbs = new (float minX, float maxX, float minZ, float maxZ)[tris.Count];
            for (int t = 0; t < tris.Count; t++)
            {
                var (a, b, c) = tris[t];
                var va = verts[a]; var vb = verts[b]; var vc = verts[c];
                float tminX = MathF.Min(va.X, MathF.Min(vb.X, vc.X));
                float tmaxX = MathF.Max(va.X, MathF.Max(vb.X, vc.X));
                float tminZ = MathF.Min(va.Z, MathF.Min(vb.Z, vc.Z));
                float tmaxZ = MathF.Max(va.Z, MathF.Max(vb.Z, vc.Z));
                triAabbs[t] = (tminX, tmaxX, tminZ, tmaxZ);
            }

            // --- 5) Grid/trace params (0-based ix,iz!)
            float sXZ = heightStepXZ;
            float sY = heightStepY;
            float rayTop = minY + 1000.0f;

            // MaxY/MinY accumulators
            var maxPerCol = new Dictionary<ulong, (ushort oct, ushort tri, float y)>();
            var minPerCol = new Dictionary<(int ix, int iz), (ushort oct, ushort tri, float y)>();

            // --- 6) Sweep triangles and cast straight down at edge points (0-based)
            for (int ti = 0; ti < tris.Count; ti++)
            {
                var (a, b, c) = tris[ti];
                var va = verts[a]; var vb = verts[b]; var vc = verts[c];
                var (tminX, tmaxX, tminZ, tmaxZ) = triAabbs[ti];

                // Convert tri AABB to 0-based grid index range (inclusive), pad ±1
                int ix0 = (int)MathF.Floor((tminX - minX) / sXZ) - 1;
                int ix1 = (int)MathF.Floor((tmaxX - minX) / sXZ) + 1;
                int iz0 = (int)MathF.Floor((tminZ - minZ) / sXZ) - 1;
                int iz1 = (int)MathF.Floor((tmaxZ - minZ) / sXZ) + 1;

                if (ix1 < 0 || iz1 < 0) continue;
                if (ix0 < 0) ix0 = 0;
                if (iz0 < 0) iz0 = 0;

                for (int ix = ix0; ix <= ix1; ix++)
                {
                    float x = minX + ix * sXZ;
                    if (x < tminX - 1e-6f || x > tmaxX + 1e-6f) continue;

                    for (int iz = iz0; iz <= iz1; iz++)
                    {
                        float z = minZ + iz * sXZ;
                        if (z < tminZ - 1e-6f || z > tmaxZ + 1e-6f) continue;

                        if (!RayTriangleIntersect_D3DX(
                                new Vector3(x, rayTop, z),
                                new Vector3(0f, -1f, 0f),
                                va, vb, vc,
                                out float tParam))
                            continue;

                        float yHit = rayTop - tParam;

                        // resolve leaf once and SKIP if not inside any terminal leaf
                        ushort leaf = FindLeafIndex(x, yHit, z);
                        if (leaf == 0)
                            continue;

                        // ---- MaxY: highest y; tie -> lowest tri index
                        ulong kMax = (((ulong)(uint)ix) << 32) | (uint)iz;
                        if (!maxPerCol.TryGetValue(kMax, out var cur) ||
                            (yHit > cur.y + T_TIE_EPS) ||
                            (MathF.Abs(yHit - cur.y) <= T_TIE_EPS && ti < cur.tri))
                        {
                            maxPerCol[kMax] = (leaf, (ushort)ti, yHit);
                        }

                        // ---- MinY: for thickness check later
                        var kMin = (ix, iz);
                        if (!minPerCol.TryGetValue(kMin, out var curMin) ||
                            (yHit < curMin.y - T_TIE_EPS))
                        {
                            minPerCol[kMin] = (leaf, (ushort)ti, yHit);
                        }
                    }
                }
            }

            // --- 7) Build Full entries
            var fullEntries = new Dictionary<ulong, (ushort oct, ushort tri, float h)>(
                capacity: Math.Max(1, minPerCol.Count / 2));

            foreach (var kv in maxPerCol)
            {
                ulong kMax = kv.Key;
                int ix = (int)((kMax >> 32) & 0xffffffff);
                int iz = (int)(kMax & 0xffffffff);

                if (!minPerCol.TryGetValue((ix, iz), out var minEntry))
                    continue;

                float eMax = kv.Value.y;
                float eMin = minEntry.y;
                float thickness = eMax - eMin;

                int topIy = (int)((eMax - minY) / sY) - 2;
                int minIy = (int)(((eMin - minY) - sY) / sY);
                if (minIy < 0) minIy = 0;

                if (thickness < 3f * sY) continue;
                if (topIy > 22) continue;
                if (topIy < minIy) continue;

                for (int iy = minIy; iy <= topIy; iy++)
                {
                    ulong kFull = (((ulong)(uint)ix) << 40)
                                | (((ulong)(uint)iy) << 20)
                                | ((ulong)(uint)iz);
                    fullEntries[kFull] = (minEntry.oct, minEntry.tri, eMin);
                }
            }

            // --- 8) Write file
            using var fs = File.Create(outHeightPath);
            using var bw = new BinaryWriter(fs, Encoding.ASCII, leaveOpen: false);

            bw.Write(version);
            if (version >= 2)
            {
                bw.Write(heightStepXZ);
                bw.Write(heightStepY);
            }
            bw.Write(minX);
            bw.Write(minY);
            bw.Write(minZ);

            // Sorted for determinism
            var maxSorted = maxPerCol.OrderBy(k => k.Key);
            var fullSorted = fullEntries.OrderBy(k => k.Key);

            bw.Write(maxPerCol.Count);
            bw.Write(fullEntries.Count);

            foreach (var kv in maxSorted)
            {
                bw.Write(kv.Key);
                bw.Write(kv.Value.oct);
                bw.Write(kv.Value.tri);
                bw.Write(kv.Value.y);
            }
            foreach (var kv in fullSorted)
            {
                bw.Write(kv.Key);
                bw.Write(kv.Value.oct);
                bw.Write(kv.Value.tri);
                bw.Write(kv.Value.h);
            }
        }

        // D3DX-like Möller–Trumbore with tiny det guard
        private static bool RayTriangleIntersect_D3DX(
            in Vector3 orig,
            in Vector3 dir,
            in Vector3 va,
            in Vector3 vb,
            in Vector3 vc,
            out float t)
        {
            t = 0f;
            var e1 = vb - va;
            var e2 = vc - va;
            var p = Vector3.Cross(dir, e2);
            float det = Vector3.Dot(e1, p);
            if (MathF.Abs(det) < EPS_DET_MIN) return false;

            float inv = 1.0f / det;
            var s = orig - va;
            float u = Vector3.Dot(s, p) * inv;
            if (u < -EPS_UV || u > 1.0f + EPS_UV) return false;

            var q = Vector3.Cross(s, e1);
            float v = Vector3.Dot(dir, q) * inv;
            if (v < -EPS_UV || (u + v) > 1.0f + EPS_UV) return false;

            t = Vector3.Dot(e2, q) * inv;
            return t >= 0.0f;
        }
    }
}
