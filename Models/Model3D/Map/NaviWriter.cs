using SharpAssimp;
using System.Numerics;

namespace RHToolkit.Models.Model3D.Map;

/// <summary>
/// Writes a navigation mesh file (.navi) from a scene and a specified navigation node.
/// </summary>
public static class NaviWriter
{
    private const string FILE_HEADER = "DoBal";
    private static readonly Encoding ASCII = Encoding.ASCII;

    public static void WriteFromNode(Scene scene, Node navNode, string outPath)
    {
        // 1) Extract geometry from navNode
        ExtractMesh(scene, navNode, out var positions, out var triangles);

        if (positions.Count == 0 || triangles.Count == 0)
            throw new InvalidDataException("Navigation Mesh has no triangulated geometry.");

        // 2) Build header
        string navName = navNode.Name;
        uint navNameKey = ModelExtensions.HashName(navName);
        int version = 8; // export as latest version

        // 3) Write file
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms, ASCII);

        bw.Write(ASCII.GetBytes(FILE_HEADER));    // header
        bw.Write(version);                        // version
        bw.Write(3);                              // object count

        long tableStart = ms.Position;
        for (int i = 0; i < 3; i++) bw.Write(0);  // offsets
        for (int i = 0; i < 3; i++) bw.Write(0);  // sizes
        for (int i = 0; i < 3; i++) bw.Write(0);  // typeIds

        var offsets = new int[3];
        var sizes = new int[3];
        var types = new int[] { 16, 3, 8 };

        // Write Type 16 (Mesh)
        offsets[0] = (int)ms.Position;
        WriteType16Mesh(bw, version, navName, navNameKey, positions, triangles);
        sizes[0] = (int)ms.Position - offsets[0];

        // Write Type 3 (Transform)
        offsets[1] = (int)ms.Position;
        WriteType3Transform(bw, navNode, navName, navNameKey, version);
        sizes[1] = (int)ms.Position - offsets[1];

        // Write Type 8 (Octree)
        offsets[2] = (int)ms.Position;
        WriteType8Octree(bw, positions, triangles);
        sizes[2] = (int)ms.Position - offsets[2];

        // Update table
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
        out List<Vector3> outPositions,
        out List<(int A, int B, int C)> outTris)
    {
        var positions = new List<Vector3>();
        var triangles = new List<(int A, int B, int C)>();

        var meshNode = navRoot.Children.FirstOrDefault() ?? throw new InvalidDataException("navRoot.Children is empty; cannot extract mesh."); // "NM_Plane01"

        var meshWorld = Matrix4x4.Transpose(meshNode.Transform);
        
        // If meshNode has a parent (navRoot), accumulate the parent transform
        if (meshNode.Parent != null)
        {
            var parentWorld = Matrix4x4.Transpose(meshNode.Parent.Transform);
            meshWorld *= parentWorld;
        }

        foreach (int mi in meshNode.MeshIndices)
        {
            var mesh = scene.Meshes[mi];
            int baseIndex = positions.Count;

            foreach (var v in mesh.Vertices)
            {
                var p = Vector3.Transform(
                            new Vector3(v.X, v.Y, v.Z), meshWorld);
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
    /// Type 3: names/hashes + flags + matrices + TRS
    /// </summary>
    private static void WriteType3Transform(BinaryWriter bw, Node node, string name, uint nameKey, int version)
    {
        // name lengths
        bw.Write(name.Length); // nameLen
        bw.Write(0);           // parentNameLen
        bw.Write(name.Length); // name2Len

        // name keys
        bw.Write(nameKey); // nameKey
        bw.Write(0u); // parentNameKey
        bw.Write(nameKey); // name2Key

        // strings
        BinaryWriterExtensions.WriteUtf16String(bw, name);
        BinaryWriterExtensions.WriteUtf16String(bw, string.Empty);
        BinaryWriterExtensions.WriteUtf16String(bw, name);

        // flags
        bw.Write(16); // node type (16 navi)
        bw.Write(1); // isActive?

        bw.Write(0); bw.Write(0); bw.Write(0); bw.Write(0);   // unk1..unk4
        bw.Write((byte)0);                                    // b1
        if (version >= 7) bw.Write((byte)0);                  // b2

        // ---- Matrices ----
        var world = Matrix4x4.Transpose(node.Transform);
        Matrix4x4.Invert(world, out var bind);
        var worldDup = world;

        // Write matrices in row-major order
        BinaryWriterExtensions.WriteMatrix(bw, world);
        BinaryWriterExtensions.WriteMatrix(bw, bind);
        BinaryWriterExtensions.WriteMatrix(bw, worldDup);

        // ---- Decompose world to TRS ----
        Matrix4x4.Decompose(world, out var sc, out var rot, out var tr);
        BinaryWriterExtensions.WriteVector3(bw, tr);
        BinaryWriterExtensions.WriteQuaternion(bw, rot);
        BinaryWriterExtensions.WriteVector3(bw, sc);
    }

    /// <summary>
    /// Type 8: Octree
    /// </summary>
    private static void WriteType8Octree(
    BinaryWriter bw,
    List<Vector3> verts,
    List<(int A, int B, int C)> tris)
    {
        if (verts == null || verts.Count == 0) throw new InvalidDataException("No vertices.");
        if (tris == null || tris.Count == 0) throw new InvalidDataException("No triangles.");

        // ---- Root cube from mesh AABB ----
        var meshMin = verts[0];
        var meshMax = verts[0];
        for (int i = 1; i < verts.Count; i++)
        {
            var v = verts[i];
            meshMin = Vector3.Min(meshMin, v);
            meshMax = Vector3.Max(meshMax, v);
        }

        var center = (meshMin + meshMax) * 0.5f;
        var size = meshMax - meshMin;
        float width = MathF.Max(size.X, MathF.Max(size.Y, size.Z));
        float half = width * 0.5f;

        var rootMin = center - new Vector3(half, half, half);
        var rootMax = center + new Vector3(half, half, half);

        // Precompute triangle AABBs for cheap rejection before SAT.
        var triMin = new Vector3[tris.Count];
        var triMax = new Vector3[tris.Count];
        for (int t = 0; t < tris.Count; t++)
        {
            var (A, B, C) = tris[t];
            var v0 = verts[A];
            var v1 = verts[B];
            var v2 = verts[C];
            triMin[t] = Vector3.Min(v0, Vector3.Min(v1, v2));
            triMax[t] = Vector3.Max(v0, Vector3.Max(v1, v2));
        }

        int nextOctreeIndex = 0;
        const int LeafLevel = 3; // full tree: levels 0..3, leaves at level 3

        void WriteNode(Vector3 nodeMin, Vector3 nodeMax, int level)
        {
            int myIndex = nextOctreeIndex++;
            bool subdivided = level < LeafLevel;

            float nodeWidth = nodeMax.X - nodeMin.X; // cube
            var nodeCenter = (nodeMin + nodeMax) * 0.5f;
            float nodeRadius = MathF.Sqrt(3.0f) * (nodeWidth * 0.5f);

            // ---- Header ----
            bw.Write(myIndex);            // OctreeIndex
            bw.Write(subdivided);         // Subdivided
            bw.Write(nodeWidth);          // Width

            // ---- Bounds block: center, radius, min, max ----
            BinaryWriterExtensions.WriteVector3(bw, nodeCenter);
            bw.Write(nodeRadius);
            BinaryWriterExtensions.WriteVector3(bw, nodeMin);
            BinaryWriterExtensions.WriteVector3(bw, nodeMax);

            // ---- 6 AABB planes in fixed order ----
            WritePlane(bw, +1f, 0f, 0f, -nodeMin.X);  // X-min
            WritePlane(bw, -1f, 0f, 0f, nodeMax.X);  // X-max
            WritePlane(bw, 0f, -1f, 0f, nodeMax.Y);  // Y-max
            WritePlane(bw, 0f, +1f, 0f, -nodeMin.Y);  // Y-min
            WritePlane(bw, 0f, 0f, +1f, -nodeMin.Z);  // Z-min
            WritePlane(bw, 0f, 0f, -1f, nodeMax.Z);  // Z-max

            if (!subdivided)
            {
                // Leaf: emit triangle indices that overlap this cube using SAT tri-box test.
                var halfSize = (nodeMax - nodeMin) * 0.5f;

                // Reserve conservatively; avoids many reallocations.
                var hits = new List<int>(32);

                for (int ti = 0; ti < tris.Count; ti++)
                {
                    // AABB reject first
                    var tMin = triMin[ti];
                    var tMax = triMax[ti];
                    if (tMax.X < nodeMin.X || tMin.X > nodeMax.X) continue;
                    if (tMax.Y < nodeMin.Y || tMin.Y > nodeMax.Y) continue;
                    if (tMax.Z < nodeMin.Z || tMin.Z > nodeMax.Z) continue;

                    var (A, B, C) = tris[ti];
                    var v0 = verts[A];
                    var v1 = verts[B];
                    var v2 = verts[C];

                    if (TriBoxOverlap(nodeCenter, halfSize, v0, v1, v2))
                        hits.Add(ti);
                }

                bw.Write(hits.Count);
                for (int i = 0; i < hits.Count; i++)
                    bw.Write(hits[i]);

                return;
            }

            // Internal node: always 0 indices in the original format
            bw.Write(0);

            // Recurse children in the required order
            float cx = nodeCenter.X, cy = nodeCenter.Y, cz = nodeCenter.Z;
            float minx = nodeMin.X, miny = nodeMin.Y, minz = nodeMin.Z;
            float maxx = nodeMax.X, maxy = nodeMax.Y, maxz = nodeMax.Z;

            // zLow
            WriteNode(new Vector3(minx, miny, minz), new Vector3(cx, cy, cz), level + 1); // 0
            WriteNode(new Vector3(minx, cy, minz), new Vector3(cx, maxy, cz), level + 1); // 1
            WriteNode(new Vector3(cx, cy, minz), new Vector3(maxx, maxy, cz), level + 1); // 2
            WriteNode(new Vector3(cx, miny, minz), new Vector3(maxx, cy, cz), level + 1); // 3

            // zHigh
            WriteNode(new Vector3(minx, miny, cz), new Vector3(cx, cy, maxz), level + 1); // 4
            WriteNode(new Vector3(minx, cy, cz), new Vector3(cx, maxy, maxz), level + 1); // 5
            WriteNode(new Vector3(cx, cy, cz), new Vector3(maxx, maxy, maxz), level + 1); // 6
            WriteNode(new Vector3(cx, miny, cz), new Vector3(maxx, cy, maxz), level + 1); // 7
        }

        WriteNode(rootMin, rootMax, level: 0);
    }

    private static void WritePlane(BinaryWriter bw, float a, float b, float c, float d)
    {
        bw.Write(a); bw.Write(b); bw.Write(c); bw.Write(d);
    }

    /// <summary>
    /// SAT triangle-AABB overlap test (Akenine-Möller style).
    /// Returns true if the triangle overlaps or touches the box.
    /// </summary>
    private static bool TriBoxOverlap(Vector3 boxCenter, Vector3 boxHalfSize, Vector3 tv0, Vector3 tv1, Vector3 tv2)
    {
        // Move triangle to box space
        Vector3 v0 = tv0 - boxCenter;
        Vector3 v1 = tv1 - boxCenter;
        Vector3 v2 = tv2 - boxCenter;

        Vector3 e0 = v1 - v0;
        Vector3 e1 = v2 - v1;
        Vector3 e2 = v0 - v2;

        // 9 axis tests: cross(edge, axis)
        if (!AxisTest(Vector3.Cross(e0, Vector3.UnitX), v0, v1, v2, boxHalfSize)) return false;
        if (!AxisTest(Vector3.Cross(e0, Vector3.UnitY), v0, v1, v2, boxHalfSize)) return false;
        if (!AxisTest(Vector3.Cross(e0, Vector3.UnitZ), v0, v1, v2, boxHalfSize)) return false;

        if (!AxisTest(Vector3.Cross(e1, Vector3.UnitX), v0, v1, v2, boxHalfSize)) return false;
        if (!AxisTest(Vector3.Cross(e1, Vector3.UnitY), v0, v1, v2, boxHalfSize)) return false;
        if (!AxisTest(Vector3.Cross(e1, Vector3.UnitZ), v0, v1, v2, boxHalfSize)) return false;

        if (!AxisTest(Vector3.Cross(e2, Vector3.UnitX), v0, v1, v2, boxHalfSize)) return false;
        if (!AxisTest(Vector3.Cross(e2, Vector3.UnitY), v0, v1, v2, boxHalfSize)) return false;
        if (!AxisTest(Vector3.Cross(e2, Vector3.UnitZ), v0, v1, v2, boxHalfSize)) return false;

        // Test overlap in the AABB’s local axes (X, Y, Z)
        if (!MinMaxOverlap(v0.X, v1.X, v2.X, boxHalfSize.X)) return false;
        if (!MinMaxOverlap(v0.Y, v1.Y, v2.Y, boxHalfSize.Y)) return false;
        if (!MinMaxOverlap(v0.Z, v1.Z, v2.Z, boxHalfSize.Z)) return false;

        // Test the triangle normal axis
        Vector3 normal = Vector3.Cross(e0, e1);
        if (!PlaneBoxOverlap(normal, v0, boxHalfSize)) return false;

        return true;
    }

    private static bool AxisTest(Vector3 axis, Vector3 v0, Vector3 v1, Vector3 v2, Vector3 half)
    {
        // Degenerate axis => skip
        if (axis.X == 0f && axis.Y == 0f && axis.Z == 0f)
            return true;

        float p0 = v0.X * axis.X + v0.Y * axis.Y + v0.Z * axis.Z;
        float p1 = v1.X * axis.X + v1.Y * axis.Y + v1.Z * axis.Z;
        float p2 = v2.X * axis.X + v2.Y * axis.Y + v2.Z * axis.Z;

        float min = MathF.Min(p0, MathF.Min(p1, p2));
        float max = MathF.Max(p0, MathF.Max(p1, p2));

        float r = half.X * MathF.Abs(axis.X) + half.Y * MathF.Abs(axis.Y) + half.Z * MathF.Abs(axis.Z);

        // Strictly outside => separating axis exists
        return !(min > r || max < -r);
    }

    private static bool MinMaxOverlap(float a, float b, float c, float half)
    {
        float min = MathF.Min(a, MathF.Min(b, c));
        float max = MathF.Max(a, MathF.Max(b, c));
        return !(min > half || max < -half);
    }

    private static bool PlaneBoxOverlap(Vector3 normal, Vector3 vert, Vector3 maxBox)
    {
        // Build vmin/vmax along normal
        Vector3 vmin = new(
            normal.X > 0f ? -maxBox.X : maxBox.X,
            normal.Y > 0f ? -maxBox.Y : maxBox.Y,
            normal.Z > 0f ? -maxBox.Z : maxBox.Z);

        Vector3 vmax = new(
            normal.X > 0f ? maxBox.X : -maxBox.X,
            normal.Y > 0f ? maxBox.Y : -maxBox.Y,
            normal.Z > 0f ? maxBox.Z : -maxBox.Z);

        float dotVmin = vmin.X * normal.X + vmin.Y * normal.Y + vmin.Z * normal.Z;
        float dotVmax = vmax.X * normal.X + vmax.Y * normal.Y + vmax.Z * normal.Z;
        float dotVert = vert.X * normal.X + vert.Y * normal.Y + vert.Z * normal.Z;

        if (dotVmin + dotVert > 0f) return false;
        if (dotVmax + dotVert >= 0f) return true;

        return false;
    }

    /// <summary>
    /// Type 16: names/hashes + verts/tris
    /// </summary>
    private static void WriteType16Mesh(
        BinaryWriter bw,
        int version,
        string name,
        uint nodeKey,
        List<Vector3> verts,
        List<(int A, int B, int C)> tris)
    {
        // exported version will always be 8
        if (version < 6)
        {
            bw.Write(nodeKey);
            bw.Write(0);
            BinaryWriterExtensions.WriteAsciiFixed(bw, name ?? string.Empty, 512);
            BinaryWriterExtensions.WriteAsciiFixed(bw, string.Empty, 512);
        }
        else
        {
            int nameLen = (name ?? string.Empty).Length;
            int parentLen = string.Empty.Length;

            bw.Write(nameLen);
            bw.Write(parentLen);
            bw.Write(nodeKey);
            bw.Write(0); // parentKey

            BinaryWriterExtensions.WriteUtf16String(bw, name ?? string.Empty);
            BinaryWriterExtensions.WriteUtf16String(bw, string.Empty);
        }

        // Counts
        bw.Write(verts.Count);
        bw.Write(tris.Count);

        // Write vertex positions
        for (int i = 0; i < verts.Count; i++)
        {
            bw.Write(verts[i].X);
            bw.Write(verts[i].Y);
            bw.Write(verts[i].Z);
        }

        // Write triangle indices
        for (int i = 0; i < tris.Count; i++)
        {
            bw.Write(tris[i].A);
            bw.Write(tris[i].B);
            bw.Write(tris[i].C);
        }
    }
}
