using Assimp;
using Num = System.Numerics;

namespace RHToolkit.Models.Model3D.Map;

/// <summary>
/// Writes NAVI mesh files from Assimp nodes.
/// </summary>
public static class NaviWriter
{
    private const string FILE_HEADER = "DoBal";
    private static readonly Encoding ASCII = Encoding.ASCII;

    public static void WriteFromFbxNode(Scene scene, Node navNode, string outPath)
    {
        // 1) Extract geometry from navNode
        ExtractMesh(scene, navNode, out var positions, out var triangles);

        if (positions.Count == 0 || triangles.Count == 0)
            throw new InvalidDataException("Mesh has no triangulated geometry.");

        // 2) Build blobs
        string navName = ModelHelpers.GetStringMeta(navNode, "navi:name") ?? navNode.Name;
        uint navNameKey = ModelHelpers.HashName(navName);
        int version = ModelHelpers.GetIntMeta(navNode, "navi:version");

        var class16 = WriteType16Mesh(version, navName, navNameKey, positions, triangles);
        var class3 = WriteType3Transform(navNode, navName, navNameKey, version);
        var class8 = WriteType8Octree(positions, triangles);

        // 3) Write file
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms, ASCII, leaveOpen: true);

        bw.Write(ASCII.GetBytes(FILE_HEADER));    // header
        bw.Write(version);                   // version
        bw.Write(3);                              // object count

        long tableStart = ms.Position;
        for (int i = 0; i < 3; i++) bw.Write(0);  // offsets
        for (int i = 0; i < 3; i++) bw.Write(0);  // sizes
        for (int i = 0; i < 3; i++) bw.Write(0);  // classIds

        var blobs = new (int classId, byte[] data)[] {
            (16,  class16),
            (3, class3),
            (8,  class8)
        };

        var offsets = new int[3];
        var sizes = new int[3];
        var types = new int[3];

        for (int i = 0; i < blobs.Length; i++)
        {
            offsets[i] = (int)ms.Position;
            bw.Write(blobs[i].data);
            sizes[i] = blobs[i].data.Length;
            types[i] = blobs[i].classId;
        }

        ms.Position = tableStart;
        for (int i = 0; i < 3; i++) bw.Write(offsets[i]);
        for (int i = 0; i < 3; i++) bw.Write(sizes[i]);
        for (int i = 0; i < 3; i++) bw.Write(types[i]);

        File.WriteAllBytes(outPath, ms.ToArray());
    }

    /// <summary>
    /// Extracts all geometry from the first child mesh of navRoot
    /// </summary>
    /// <param name="scene"></param>
    /// <param name="navRoot"></param>
    /// <param name="outPositions"></param>
    /// <param name="outTris"></param>
    private static void ExtractMesh(
        Scene scene, Node navRoot,
        out List<Num.Vector3> outPositions,
        out List<(int A, int B, int C)> outTris)
    {
        var positions = new List<Num.Vector3>();
        var triangles = new List<(int A, int B, int C)>();

        var meshNode = navRoot.Children.First(); // "NM_Plane01"
        var meshWorld = ModelHelpers.GetGlobalTransform(meshNode);

        foreach (int mi in meshNode.MeshIndices)
        {
            var mesh = scene.Meshes[mi];
            int baseIndex = positions.Count;

            foreach (var v in mesh.Vertices)
            {
                var p = Num.Vector3.Transform(
                            new Num.Vector3(v.X, v.Y, v.Z), meshWorld);
                positions.Add(p);
            }

            foreach (var f in mesh.Faces)
                if (f.IndexCount == 3)
                    triangles.Add((baseIndex + f.Indices[0],
                                   baseIndex + f.Indices[1],
                                   baseIndex + f.Indices[2]));
        }

        outPositions = positions;
        outTris = triangles;
    }

    /// <summary>
    /// Class 3: names/hashes + flags + matrices + TRS
    /// </summary>
    /// <param name="node"></param>
    /// <param name="name"></param>
    /// <param name="nameKey"></param>
    /// <param name="version"></param>
    /// <returns></returns>
    private static byte[] WriteType3Transform(Node node, string name, uint nameKey, int version)
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms, ASCII, leaveOpen: true);

        // name lengths
        bw.Write(name.Length); // nameLen
        bw.Write(0);           // parentNameLen
        bw.Write(name.Length); // name2Len

        // keys
        bw.Write(nameKey);
        bw.Write(0u);
        bw.Write(nameKey);

        // strings
        ModelHelpers.WriteUtf16String(bw, name);
        ModelHelpers.WriteUtf16String(bw, string.Empty);
        ModelHelpers.WriteUtf16String(bw, name);

        // flags
        int kind = ModelHelpers.GetIntMeta(node, "navi:nodeKind");
        int flag = ModelHelpers.GetIntMeta(node, "navi:nodeFlag");
        bw.Write(kind);
        bw.Write(flag);

        bw.Write(0); bw.Write(0); bw.Write(0); bw.Write(0);   // unk1..unk4
        bw.Write((byte)0);                                    // b1
        if (version >= 7) bw.Write((byte)0);                  // b2

        // ---- Matrices: world, bind (inverse world), world dup ----
        var world = ModelHelpers.GetGlobalTransform(node);
        Num.Matrix4x4.Invert(world, out var bind);
        var worldDup = world;

        // Write matrices in row-major order
        ModelHelpers.WriteMatrix(bw, world);
        ModelHelpers.WriteMatrix(bw, bind);
        ModelHelpers.WriteMatrix(bw, worldDup);

        // ---- Decompose world to TRS ----
        Num.Matrix4x4.Decompose(world, out var sc, out var rot, out var tr);
        bw.Write(tr.X); bw.Write(tr.Y); bw.Write(tr.Z);
        bw.Write(rot.X); bw.Write(rot.Y); bw.Write(rot.Z); bw.Write(rot.W);
        bw.Write(sc.X); bw.Write(sc.Y); bw.Write(sc.Z);

        return ms.ToArray();
    }

    /// <summary>
    /// Class 16: names/hashes + verts/tris
    /// </summary>
    /// <param name="name"></param>
    /// <param name="nodeKey"></param>
    /// <param name="verts"></param>
    /// <param name="tris"></param>
    /// <returns></returns>
    private static byte[] WriteType16Mesh(int version,
        string name,
        uint nodeKey,
        List<Num.Vector3> verts,
        List<(int A, int B, int C)> tris)
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms, ASCII, leaveOpen: true);

        if (version < 6)
        {
            bw.Write(nodeKey);
            bw.Write(0);
            ModelHelpers.WriteAsciiFixed(bw, name ?? string.Empty, 512);
            ModelHelpers.WriteAsciiFixed(bw, string.Empty, 512);
        }
        else
        {
            int nameLen = (name ?? string.Empty).Length;
            int parentLen = (string.Empty).Length;

            bw.Write(nameLen);
            bw.Write(parentLen);
            bw.Write(nodeKey);
            bw.Write(0);

            ModelHelpers.WriteUtf16String(bw, name ?? string.Empty);
            ModelHelpers.WriteUtf16String(bw, string.Empty);
        }

        // Counts
        bw.Write(verts.Count);
        bw.Write(tris.Count);

        for (int i = 0; i < verts.Count; i++)
        {
            bw.Write(verts[i].X);
            bw.Write(verts[i].Y);
            bw.Write(verts[i].Z);
        }

        for (int i = 0; i < tris.Count; i++)
        {
            bw.Write(tris[i].A);
            bw.Write(tris[i].B);
            bw.Write(tris[i].C);
        }

        return ms.ToArray();
    }

    /// <summary>
    /// Class 8: octree of AABBs, pre-order, 3 levels deep
    /// </summary>
    /// <param name="verts"></param>
    /// <param name="tris"></param>
    /// <returns></returns>
    private static byte[] WriteType8Octree(
    List<Num.Vector3> verts,
    List<(int A, int B, int C)> tris)
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms, ASCII, leaveOpen: true);

        // ---- 1) Global triangle AABBs (for fast overlap) ----
        var triAabbs = new (Num.Vector3 min, Num.Vector3 max)[tris.Count];
        for (int i = 0; i < tris.Count; i++)
        {
            var (a, b, c) = tris[i];
            var va = verts[a]; var vb = verts[b]; var vc = verts[c];
            var min = Num.Vector3.Min(Num.Vector3.Min(va, vb), vc);
            var max = Num.Vector3.Max(Num.Vector3.Max(va, vb), vc);
            triAabbs[i] = (min, max);
        }

        // ---- 2) Root cube ----
        var globalMin = new Num.Vector3(float.PositiveInfinity);
        var globalMax = new Num.Vector3(float.NegativeInfinity);
        foreach (var (min, max) in triAabbs) { globalMin = Num.Vector3.Min(globalMin, min); globalMax = Num.Vector3.Max(globalMax, max); }
        var size = globalMax - globalMin;
        float w = MathF.Max(size.X, MathF.Max(size.Y, size.Z));
        var center = (globalMin + globalMax) * 0.5f;
        var half = w * 0.5f;
        var rootMin = center - new Num.Vector3(half);
        var rootMax = center + new Num.Vector3(half);

        int nextIndex = 0;

        // ---- 3) Emit nodes (pre-order) ----
        void WriteNode(Num.Vector3 nmin, Num.Vector3 nmax, int depth)
        {
            int myIndex = nextIndex++;
            bool subdivided = depth < 3;
            float width = nmax.X - nmin.X;
            var c = (nmin + nmax) * 0.5f;
            float radius = width * 0.8660254037844386f; // sqrt(3)/2

            // header
            bw.Write(myIndex);
            bw.Write(subdivided);
            bw.Write(width);

            // bounds: center, radius, min, max
            bw.Write(c.X); bw.Write(c.Y); bw.Write(c.Z);
            bw.Write(radius);
            bw.Write(nmin.X); bw.Write(nmin.Y); bw.Write(nmin.Z);
            bw.Write(nmax.X); bw.Write(nmax.Y); bw.Write(nmax.Z);

            // 6 planes in the observed order
            // +X (minX), -X (maxX), -Y (maxY), +Y (minY), +Z (minZ), -Z (maxZ)
            bw.Write(1f); bw.Write(0f); bw.Write(0f); bw.Write(-nmin.X);
            bw.Write(-1f); bw.Write(0f); bw.Write(0f); bw.Write(nmax.X);
            bw.Write(0f); bw.Write(-1f); bw.Write(0f); bw.Write(nmax.Y);
            bw.Write(0f); bw.Write(1f); bw.Write(0f); bw.Write(-nmin.Y);
            bw.Write(0f); bw.Write(0f); bw.Write(1f); bw.Write(-nmin.Z);
            bw.Write(0f); bw.Write(0f); bw.Write(-1f); bw.Write(nmax.Z);

            if (!subdivided)
            {
                // leaf: gather triangle ids whose AABB overlaps this node cube
                var ids = new List<int>();
                for (int t = 0; t < triAabbs.Length; t++)
                {
                    var (tmin, tmax) = triAabbs[t];
                    bool overlap =
                        !(tmax.X < nmin.X || tmin.X > nmax.X ||
                          tmax.Y < nmin.Y || tmin.Y > nmax.Y ||
                          tmax.Z < nmin.Z || tmin.Z > nmax.Z);
                    if (overlap) ids.Add(t);
                }

                bw.Write(ids.Count);
                foreach (var id in ids) bw.Write(id);
                return;
            }

            // interior: no triangles, then 8 children
            bw.Write(0); // naviIndexCount
                         // child order: (x,y,z) bits from min→max
            var h = (nmax - nmin) * 0.5f;
            for (int zi = 0; zi < 2; zi++)
                for (int yi = 0; yi < 2; yi++)
                    for (int xi = 0; xi < 2; xi++)
                    {
                        var cmn = new Num.Vector3(
                            nmin.X + xi * h.X,
                            nmin.Y + yi * h.Y,
                            nmin.Z + zi * h.Z);
                        var cmx = cmn + h;
                        WriteNode(cmn, cmx, depth + 1);
                    }
        }

        WriteNode(rootMin, rootMax, 0);
        return ms.ToArray();
    }

}
