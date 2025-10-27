using Assimp;
using System.Text.RegularExpressions;
using Num = System.Numerics;

namespace RHToolkit.Models.Model3D.Map;

/// <summary>
/// MMP file writer (currently rebuild type 3/19 from FBX + original MMP for type 1 material)
/// </summary>
public static class MMPWriter
{
    private const string Header = "DoBal";
    private static readonly Encoding ASCII = Encoding.ASCII;
    private readonly record struct Chunk(int Type, byte[] Data);

    public static async Task RebuildFromFbx(string originalMmpPath, string fbxPath, string outMmpPath)
    {
        try
        {
            // --- read original MMP file table ---
            var originalBytes = File.ReadAllBytes(originalMmpPath);
            using var ms = new MemoryStream(originalBytes, writable: false);
            using var br = new BinaryReader(ms, ASCII, leaveOpen: true);

            var header = ASCII.GetString(br.ReadBytes(5));
            if (header != Header) throw new InvalidDataException("Not an MMP file.");
            int version = br.ReadInt32();
            int objectCount = br.ReadInt32();

            var offsets = new int[objectCount];
            var sizes = new int[objectCount];
            var types = new int[objectCount];
            for (int i = 0; i < objectCount; i++) offsets[i] = br.ReadInt32();
            for (int i = 0; i < objectCount; i++) sizes[i] = br.ReadInt32();
            for (int i = 0; i < objectCount; i++) types[i] = br.ReadInt32();

            var outChunks = new List<Chunk>();

            // --- copy all type-1 chunks (material data) as-is ---
            for (int i = 0; i < objectCount; i++)
            {
                if (types[i] != 1) continue;
                ms.Position = offsets[i];
                var data = br.ReadBytes(sizes[i]);
                outChunks.Add(new Chunk(1, data));
            }

            // -- import FBX and build type-3 (node) + type-19 (mesh) chunks ---
            var ctx = new AssimpContext();
            var scene = ctx.ImportFile(fbxPath, PostProcessSteps.FlipWindingOrder 
            | PostProcessSteps.FlipUVs | PostProcessSteps.JoinIdenticalVertices);

            var fileStem = Path.GetFileNameWithoutExtension(fbxPath);
            var container = scene.RootNode.Children.FirstOrDefault(n => n.Name == fileStem) ?? scene.RootNode;

            var flipX = new Assimp.Matrix4x4(
                -1, 0, 0, 0,
                 0, 1, 0, 0,
                 0, 0, 1, 0,
                 0, 0, 0, 1);

            // Pre-multiply so all children inherit the reflection
            container.Transform = flipX * container.Transform;

            // --- Find navi node
            var naviNodes = new List<Assimp.Node>();
            void Walk(Assimp.Node n)
            {
                bool isNavi = n.Metadata != null && n.Metadata.TryGetValue("navi:isNavMesh", out var _);
                if (isNavi) naviNodes.Add(n);
                foreach (var c in n.Children) Walk(c);
            }
            Walk(container);

            // If present, build a .navi next to output MMP
            if (naviNodes.Count > 0)
            {
                var outNaviPath = Path.ChangeExtension(outMmpPath, ".navi");
                NaviWriter.WriteFromFbxNode(scene, naviNodes[0], outNaviPath);
            }

            foreach (var objNode in container.Children)
            {
                // Skip navi nodes
                if (objNode.Metadata != null && objNode.Metadata.TryGetValue("navi:isNavMesh", out var _))
                {
                    continue;
                }

                var partPairs = CollectParts(scene, objNode);
                outChunks.Add(new Chunk(3, BuildType3FromNode(objNode, version)));
                outChunks.Add(new Chunk(19, BuildType19FromNode(objNode, partPairs)));
            }

            // --- write new MMP file ---
            using var outMs = new MemoryStream();
            using var bw = new BinaryWriter(outMs, ASCII, leaveOpen: true);

            bw.Write(ASCII.GetBytes(Header));
            bw.Write(version);
            bw.Write(outChunks.Count);

            int tablePos = (int)outMs.Position;
            for (int i = 0; i < outChunks.Count; i++) bw.Write(0); // offsets
            for (int i = 0; i < outChunks.Count; i++) bw.Write(0); // sizes
            for (int i = 0; i < outChunks.Count; i++) bw.Write(0); // types

            var newOffsets = new int[outChunks.Count];
            var newSizes = new int[outChunks.Count];
            var newTypes = new int[outChunks.Count];

            for (int i = 0; i < outChunks.Count; i++)
            {
                newOffsets[i] = (int)outMs.Position;
                bw.Write(outChunks[i].Data);
                newSizes[i] = outChunks[i].Data.Length;
                newTypes[i] = outChunks[i].Type;
            }

            outMs.Position = tablePos;
            for (int i = 0; i < outChunks.Count; i++) bw.Write(newOffsets[i]);
            for (int i = 0; i < outChunks.Count; i++) bw.Write(newSizes[i]);
            for (int i = 0; i < outChunks.Count; i++) bw.Write(newTypes[i]);

            File.WriteAllBytes(outMmpPath, outMs.ToArray());

            //  Write .height file
            var naviPath = Path.ChangeExtension(outMmpPath, ".navi");
            var heightPath = Path.ChangeExtension(outMmpPath, ".height");
            await HeightWriter.BuildFromNaviFileAsync(naviPath, heightPath, 20, 50, 2);


        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to rebuild MMP from FBX: {fbxPath}: {ex.Message}", ex);
        }
    }

    // ---------------- mesh & node helpers (Assimp) ----------------
    /// <summary>
    /// Collect all parts (meshes) under the given object node, recursively.
    /// </summary>
    /// <param name="scene"></param>
    /// <param name="objNode"></param>
    /// <returns> List of (Node, Mesh) pairs </returns>
    private static List<(Node part, Mesh mesh)> CollectParts(Scene scene, Node objNode)
    {
        var parts = new List<(Node, Mesh)>();
        void Walk(Node n)
        {
            if (n.MeshCount > 0)
            {
                foreach (var mi in n.MeshIndices)
                    parts.Add((n, scene.Meshes[mi]));
            }
            foreach (var c in n.Children) Walk(c);
        }
        Walk(objNode);
        return parts;
    }

    #region Type-19 Mesh Builder
    // ---------------- Type-19: geometry chunk (world-space, deterministic reindex) ----------------

    /// <summary>
    /// Per-part data accumulator
    /// </summary>
    /// <param name="SubName"></param>
    /// <param name="MaterialId"></param>
    /// <param name="meshType"></param>
    /// <param name="FlagAdditive"></param>
    /// <param name="FlagAlpha"></param>
    /// <param name="FlagEnabled"></param>
    /// <param name="UvSets"></param>
    /// <param name="Pw"></param>
    /// <param name="Nw"></param>
    /// <param name="UV0"></param>
    /// <param name="UV1"></param>
    /// <param name="Indices"></param>
    /// <param name="ForcedBounds"></param>
    private readonly record struct PartData(
        string SubName,
        int MaterialId,
        int meshType,
        byte FlagAdditive,
        byte FlagAlpha,
        byte FlagEnabled,
        int UvSets,
        List<Num.Vector3> Pw,
        List<Num.Vector3> Nw,
        List<Num.Vector2> UV0,
        List<Num.Vector2> UV1,
        List<ushort> Indices,
        (float minX, float minY, float minZ, float maxX, float maxY, float maxZ, bool useZero)? ForcedBounds
    );

    static byte[] BuildType19FromNode(Node objNode, List<(Node part, Mesh mesh)> parts)
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms, Encoding.ASCII, leaveOpen: true);

        // 1) Names & hashes
        string objectName = objNode.Name ?? "Object";
        string objectName2 = "";
        WriteType19Header(bw, objectName, objectName2);

        // 2) Build per-part data (including vertex transforms, reindexing, flags) and accumulate object bounds
        var builtParts = new List<PartData>(parts.Count);
        (var objMin, var objMax) = BuildAllPartsAndObjectBounds(parts, builtParts);

        // 3) Write object/world bounds block
        WriteObjectBounds(bw, objMin, objMax);

        // 4) Mesh count
        bw.Write(builtParts.Count);

        // 5) Write each mesh payload (header, bounds, vertex stream, indices)
        foreach (var part in builtParts)
            WriteMeshBlock(bw, part);

        return ms.ToArray();
    }

    // ---------- Step 1: write the header (names + hashes) ----------
    private static void WriteType19Header(BinaryWriter bw, string objectName, string objectName2)
    {
        bw.Write(objectName.Length);
        bw.Write(objectName2.Length);
        bw.Write(ModelExtensions.HashName(objectName));
        bw.Write(ModelExtensions.HashName(objectName2));
        BinaryWriterExtensions.WriteUtf16String(bw, objectName);
        BinaryWriterExtensions.WriteUtf16String(bw, objectName2);
    }

    // ---------- Step 2: build all parts and compute the object AABB ----------
    /// <summary>
    /// Build all parts: transform vertices to world space, reindex, extract flags and metadata
    /// </summary>
    /// <param name="parts"></param>
    /// <param name="builtParts"></param>
    /// <returns></returns>
    private static (Num.Vector3 min, Num.Vector3 max) BuildAllPartsAndObjectBounds(
        List<(Node part, Mesh mesh)> parts,
        List<PartData> builtParts)
    {
        float objMinX = float.PositiveInfinity, objMinY = float.PositiveInfinity, objMinZ = float.PositiveInfinity;
        float objMaxX = float.NegativeInfinity, objMaxY = float.NegativeInfinity, objMaxZ = float.NegativeInfinity;

        foreach (var (partNode, mesh) in parts)
        {
            var part = BuildSinglePart(partNode, mesh, ref objMinX, ref objMinY, ref objMinZ, ref objMaxX, ref objMaxY, ref objMaxZ);
            builtParts.Add(part);
        }

        return (new Num.Vector3(objMinX, objMinY, objMinZ), new Num.Vector3(objMaxX, objMaxY, objMaxZ));
    }

    /// <summary>
    /// Build a single part: transform vertices to world space, reindex, extract flags and metadata
    /// </summary>
    /// <param name="partNode"></param>
    /// <param name="mesh"></param>
    /// <param name="objMinX"></param>
    /// <param name="objMinY"></param>
    /// <param name="objMinZ"></param>
    /// <param name="objMaxX"></param>
    /// <param name="objMaxY"></param>
    /// <param name="objMaxZ"></param>
    /// <returns></returns>
    private static PartData BuildSinglePart(
        Node partNode,
        Mesh mesh,
        ref float objMinX, ref float objMinY, ref float objMinZ,
        ref float objMaxX, ref float objMaxY, ref float objMaxZ)
    {
        string subName = partNode.Name ?? "Mesh";
        // strip FBX auto-suffixes like ".001"
        subName = Regex.Replace(subName, @"^(.*?\d)(0+\d{2,})$", "$1");

        int materialId = ModelExtensions.GetIntMeta(partNode, "mmp:materialIdx");
        int meshType = ModelExtensions.GetIntMeta(partNode, "mmp:meshType");
        byte fl0 = (byte)ModelExtensions.GetIntMeta(partNode, "mmp:isEmissiveAdditive");
        byte fl1 = (byte)ModelExtensions.GetIntMeta(partNode, "mmp:isAlphaBlend");
        byte fl2 = (byte)ModelExtensions.GetIntMeta(partNode, "mmp:isEnabled");
        int uvSets = ModelExtensions.GetIntMeta(partNode, "mmp:uvSetCount");
        var maxSets = Math.Min(2, mesh.TextureCoordinateChannelCount);
        if (uvSets > maxSets)
            throw new InvalidDataException(
                $"Node '{partNode.Name}' uvSetCount={uvSets} exceeds available channels ({maxSets}).");


        // Shared accumulators
        var Pw = new List<Num.Vector3>();
        var Nw = new List<Num.Vector3>();
        var UV0 = new List<Num.Vector2>();
        var UV1 = new List<Num.Vector2>(uvSets == 2 ? mesh.VertexCount : 0);
        var indices = new List<ushort>(mesh.FaceCount * 3);

        (float minX, float minY, float minZ, float maxX, float maxY, float maxZ, bool useZero)? forcedBounds = null;

        if (meshType == 2)
        {
            // Billboard
            forcedBounds = BuildBillboard(partNode, mesh, Pw, Nw, UV0, indices,
                                          ref objMinX, ref objMinY, ref objMinZ, ref objMaxX, ref objMaxY, ref objMaxZ);
        }
        else
        {
            // Regular mesh
            BuildRegularMesh(partNode, mesh, uvSets, Pw, Nw, UV0, UV1, indices,
                             ref objMinX, ref objMinY, ref objMinZ, ref objMaxX, ref objMaxY, ref objMaxZ);
        }

        return new PartData(subName, materialId, meshType, fl0, fl1, fl2, uvSets, Pw, Nw, UV0, UV1, indices, forcedBounds);
    }

    // ---------- Step 2a: regular mesh path ----------
    /// <summary>
    /// Build a regular mesh (meshType 0) with world-space positions and normals, deterministic reindexing
    /// </summary>
    /// <param name="partNode"></param>
    /// <param name="mesh"></param>
    /// <param name="uvSets"></param>
    /// <param name="Pw"></param>
    /// <param name="Nw"></param>
    /// <param name="UV0"></param>
    /// <param name="UV1"></param>
    /// <param name="indices"></param>
    /// <param name="objMinX"></param>
    /// <param name="objMinY"></param>
    /// <param name="objMinZ"></param>
    /// <param name="objMaxX"></param>
    /// <param name="objMaxY"></param>
    /// <param name="objMaxZ"></param>
    private static void BuildRegularMesh(
    Node partNode, Mesh mesh, int uvSets,
    List<Num.Vector3> Pw, List<Num.Vector3> Nw,
    List<Num.Vector2> UV0, List<Num.Vector2> UV1, List<ushort> indices,
    ref float objMinX, ref float objMinY, ref float objMinZ,
    ref float objMaxX, ref float objMaxY, ref float objMaxZ)
    {
        var vpos = mesh.Vertices;
        var vnrms = mesh.HasNormals ? mesh.Normals : null;
        var uv0Src = mesh.TextureCoordinateChannelCount >= 1 ? mesh.TextureCoordinateChannels[0] : null;
        var uv1Src = mesh.TextureCoordinateChannelCount >= 2 ? mesh.TextureCoordinateChannels[1] : null;

        // World transform and proper normal transform
        var M = ModelExtensions.GetGlobalTransform(partNode);
        Num.Matrix4x4.Invert(M, out var invM);
        var invMT = Num.Matrix4x4.Transpose(invM);

        int baseIndex = Pw.Count;

        // 1) Emit ALL vertices
        for (int i = 0; i < vpos.Count; i++)
        {
            // Position → world
            var pL = vpos[i];
            var pw4 = Num.Vector4.Transform(new Num.Vector4(pL.X, pL.Y, pL.Z, 1), M);
            var pw = new Num.Vector3(pw4.X, pw4.Y, pw4.Z);
            Pw.Add(pw);

            // Normal → world (inverse-transpose of M)
            Assimp.Vector3D n0;
            if (vnrms != null && i < vnrms.Count)
                n0 = vnrms[i];
            else
                n0 = new Assimp.Vector3D(0, 1, 0);

            var nw4 = Num.Vector4.Transform(new Num.Vector4(n0.X, n0.Y, n0.Z, 0), invMT);
            var nw = new Num.Vector3(nw4.X, nw4.Y, nw4.Z);
            if (nw != default)
            {
                var len = MathF.Sqrt(nw.X * nw.X + nw.Y * nw.Y + nw.Z * nw.Z);
                if (len > 1e-20f) nw /= len;
            }
            Nw.Add(nw);

            // UV0
            Num.Vector2 uv0 = default;
            if (uv0Src != null && i < uv0Src.Count)
            {
                var t0 = uv0Src[i];
                uv0 = new Num.Vector2(t0.X, t0.Y);
            }
            UV0.Add(uv0);

            // UV1
            if (uvSets == 2)
            {
                Num.Vector2 uv1 = default;
                if (uv1Src != null && i < uv1Src.Count)
                {
                    var t1 = uv1Src[i];
                    uv1 = new Num.Vector2(t1.X, t1.Y);
                }
                UV1.Add(uv1);
            }

            // Expand object AABB with this emitted world-space vertex
            if (pw.X < objMinX) objMinX = pw.X; if (pw.Y < objMinY) objMinY = pw.Y; if (pw.Z < objMinZ) objMinZ = pw.Z;
            if (pw.X > objMaxX) objMaxX = pw.X; if (pw.Y > objMaxY) objMaxY = pw.Y; if (pw.Z > objMaxZ) objMaxZ = pw.Z;
        }

        // 2) Emit indices as-is, offset by baseIndex
        foreach (var f in mesh.Faces)
        {
            if (f.IndexCount < 3) continue; // skip lines/points

            // Triangulate n-gons
            for (int k = 2; k < f.Indices.Count; k++)
            {
                int a = f.Indices[0];
                int b = f.Indices[k - 1];
                int c = f.Indices[k];

                indices.Add((ushort)(baseIndex + a));
                indices.Add((ushort)(baseIndex + b));
                indices.Add((ushort)(baseIndex + c));
            }
        }
    }

    // ---------- Step 2b: billboard path (meshType == 2) ----------
    /// <summary>
    /// Build a billboard/corona (meshType 2) with world-space positions and normals, deterministic reindexing
    /// </summary>
    /// <param name="partNode"></param>
    /// <param name="mesh"></param>
    /// <param name="Pw"></param>
    /// <param name="Nw"></param>
    /// <param name="UV0"></param>
    /// <param name="indices"></param>
    /// <param name="objMinX"></param>
    /// <param name="objMinY"></param>
    /// <param name="objMinZ"></param>
    /// <param name="objMaxX"></param>
    /// <param name="objMaxY"></param>
    /// <param name="objMaxZ"></param>
    /// <returns></returns>
    private static (float, float, float, float, float, float, bool) BuildBillboard(
    Node partNode, Mesh mesh,
    List<Num.Vector3> Pw, List<Num.Vector3> Nw, List<Num.Vector2> UV0, List<ushort> indices,
    ref float objMinX, ref float objMinY, ref float objMinZ,
    ref float objMaxX, ref float objMaxY, ref float objMaxZ)
    {
        var M = ModelExtensions.GetGlobalTransform(partNode);

        // All billboard vertices share the same center position
        var pL = mesh.Vertices[0];
        var center4 = Num.Vector4.Transform(new Num.Vector4(pL.X, pL.Y, pL.Z, 1), M);
        var center = new Num.Vector3(center4.X, center4.Y, center4.Z);

        // Determine bounding box
        float pMinX = center.X, pMaxX = center.X;
        float pMinY = center.Y, pMaxY = center.Y;
        float pMinZ = center.Z, pMaxZ = center.Z;

        // Push vertex data (positions are all center, but normals and UVs differ)
        for (int i = 0; i < mesh.Vertices.Count; i++)
        {
            Pw.Add(center);

            var n = mesh.Normals[i];
            Nw.Add(new Num.Vector3(n.X, n.Y, n.Z));

            var uv = mesh.TextureCoordinateChannels[0][i];
            UV0.Add(new Num.Vector2(uv.X, uv.Y));
        }

        // Push all 6 original indices (two triangles)
        foreach (var f in mesh.Faces)
        {
            for (int i = 0; i < f.IndexCount; i++)
                indices.Add((ushort)f.Indices[i]);
        }

        // Update global object bounds
        objMinX = MathF.Min(objMinX, pMinX); objMinY = MathF.Min(objMinY, pMinY); objMinZ = MathF.Min(objMinZ, pMinZ);
        objMaxX = MathF.Max(objMaxX, pMaxX); objMaxY = MathF.Max(objMaxY, pMaxY); objMaxZ = MathF.Max(objMaxZ, pMaxZ);


        // Z bounds only needed for billboard height tracking
        float minZ = pMinZ;
        float maxZ = pMaxZ;
        return (center.X, center.Y, minZ, center.X, center.Y, maxZ, true);
    }

    // ---------- Step 3: object/world bounds ----------
    /// <summary>
    /// Write object/world bounds block
    /// </summary>
    /// <param name="bw"></param>
    /// <param name="min"></param>
    /// <param name="max"></param>
    private static void WriteObjectBounds(BinaryWriter bw, Num.Vector3 min, Num.Vector3 max)
    {
        float sizeX = max.X - min.X, sizeY = max.Y - min.Y, sizeZ = max.Z - min.Z;
        float ctrX = 0.5f * (min.X + max.X), ctrY = 0.5f * (min.Y + max.Y), ctrZ = 0.5f * (min.Z + max.Z);
        float rad = 0.5f * MathF.Sqrt(sizeX * sizeX + sizeY * sizeY + sizeZ * sizeZ);

        bw.Write(ctrX); bw.Write(ctrY); bw.Write(ctrZ);
        bw.Write(rad);
        BinaryWriterExtensions.WriteVector3(bw, min);
        BinaryWriterExtensions.WriteVector3(bw, max);
    }

    // ---------- Step 5: write each mesh (header → bounds → vertex stream → indices) ----------
    /// <summary>
    /// Write a single mesh block (header, bounds, vertex stream, indices)
    /// </summary>
    /// <param name="bw"></param>
    /// <param name="part"></param>
    private static void WriteMeshBlock(BinaryWriter bw, PartData part)
    {
        var (subName, materialId, meshType, fl0, fl1, fl2, uvSets, Pw, Nw, UV0, UV1, indices, forcedBounds) = part;

        // Mesh header (name)
        bw.Write(subName.Length);
        BinaryWriterExtensions.WriteUtf16String(bw, subName);

        // Counts & attributes
        bw.Write(Pw.Count);
        bw.Write(indices.Count / 3);
        bw.Write(meshType);
        bw.Write(fl0); bw.Write(fl1); bw.Write(fl2);
        bw.Write(uvSets);
        bw.Write(materialId);

        // Per-part bounds
        ComputePartBounds(Pw, forcedBounds, out var pCtr, out float pRad, out var pMin, out var pMax);
        bw.Write(pCtr.X); bw.Write(pCtr.Y); bw.Write(pCtr.Z);
        bw.Write(pRad);
        bw.Write(pMin.X); bw.Write(pMin.Y); bw.Write(pMin.Z);
        bw.Write(pMax.X); bw.Write(pMax.Y); bw.Write(pMax.Z);

        // Vertex stream
        WriteVertexStream(bw, meshType, uvSets, Pw, Nw, UV0, UV1);

        // Indices (ushort)
        var ib = new byte[indices.Count * 2];
        Buffer.BlockCopy(indices.ToArray(), 0, ib, 0, ib.Length);
        bw.Write(ib);
    }

    private static void ComputePartBounds(
        List<Num.Vector3> Pw,
        (float minX, float minY, float minZ, float maxX, float maxY, float maxZ, bool useZero)? forced,
        out Num.Vector3 center, out float radius, out Num.Vector3 min, out Num.Vector3 max)
    {
        if (forced.HasValue)
        {
            var (minX, minY, minZ, maxX, maxY, maxZ, useZero) = forced.Value;
            min = new Num.Vector3(minX, minY, minZ);
            max = new Num.Vector3(maxX, maxY, maxZ);
        }
        else
        {
            float minX = float.PositiveInfinity, minY = float.PositiveInfinity, minZ = float.PositiveInfinity;
            float maxX = float.NegativeInfinity, maxY = float.NegativeInfinity, maxZ = float.NegativeInfinity;
            for (int i = 0; i < Pw.Count; i++)
                ExpandAabb(Pw[i], ref minX, ref minY, ref minZ, ref maxX, ref maxY, ref maxZ);

            min = new Num.Vector3(minX, minY, minZ);
            max = new Num.Vector3(maxX, maxY, maxZ);
        }

        float sx = max.X - min.X, sy = max.Y - min.Y, sz = max.Z - min.Z;
        center = new Num.Vector3(0.5f * (min.X + max.X), 0.5f * (min.Y + max.Y), 0.5f * (min.Z + max.Z));
        radius = 0.5f * MathF.Sqrt(sx * sx + sy * sy + sz * sz);
    }

    private static void WriteVertexStream(
        BinaryWriter bw, int meshType, int uvSets,
        List<Num.Vector3> Pw, List<Num.Vector3> Nw,
        List<Num.Vector2> UV0, List<Num.Vector2> UV1)
    {
        int stride = ModelExtensions.ComputeMMPStride(meshType, uvSets);
        using var vms = new MemoryStream(Pw.Count * stride);
        using var vbw = new BinaryWriter(vms, Encoding.ASCII, leaveOpen: true);

        for (int i = 0; i < Pw.Count; i++)
        {
            var p = Pw[i];
            var n = Nw[i];
            BinaryWriterExtensions.WriteVector3(bw, p);
            BinaryWriterExtensions.WriteVector3(bw, n);

            if (meshType == 2)
            {
                vbw.Write(UV0[i].X);
            }
            else
            {
                BinaryWriterExtensions.WriteVector2(bw, UV0[i]);
                if (uvSets == 2)
                {
                    BinaryWriterExtensions.WriteVector2(bw, UV1[i]);
                }
            }
        }

        bw.Write(vms.ToArray());
    }

    // ---------- Small helpers ----------
    private static void ExpandAabb(Num.Vector3 p,
        ref float minX, ref float minY, ref float minZ,
        ref float maxX, ref float maxY, ref float maxZ)
    {
        if (p.X < minX) minX = p.X; if (p.Y < minY) minY = p.Y; if (p.Z < minZ) minZ = p.Z;
        if (p.X > maxX) maxX = p.X; if (p.Y > maxY) maxY = p.Y; if (p.Z > maxZ) maxZ = p.Z;
    }

    
    #endregion

    #region Type 3 Node Builder
    // ---------------- Type-3: transform chunk ----------------
    private static byte[] BuildType3FromNode(Node objNode, int version)
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms, Encoding.ASCII, leaveOpen: true);

        string objectName = objNode.Name;
        string objectName2 = ModelExtensions.GetStringMeta(objNode, "mmp:nodeGroupName");

        // ---- names + hashes (object, group, object) ----
        bw.Write(objectName.Length);
        bw.Write(objectName2.Length);
        bw.Write(objectName.Length);
        bw.Write(ModelExtensions.HashName(objectName));
        bw.Write(ModelExtensions.HashName(objectName2));
        bw.Write(ModelExtensions.HashName(objectName));
        BinaryWriterExtensions.WriteUtf16String(bw, objectName);
        BinaryWriterExtensions.WriteUtf16String(bw, objectName2);
        BinaryWriterExtensions.WriteUtf16String(bw, objectName);

        // flags
        int kind = ModelExtensions.GetIntMeta(objNode, "mmp:nodeKind");
        int flag = ModelExtensions.GetIntMeta(objNode, "mmp:nodeFlag");
        bw.Write(kind);
        bw.Write(flag);

        bw.Write(0); bw.Write(0); bw.Write(0); bw.Write(0);   // unk1..unk4
        bw.Write((byte)0);                                    // b1
        if (version >= 7) bw.Write((byte)0);                  // b2

        // ---- Matrices: world, bind (inverse world), world dup ----
        var world = ModelExtensions.GetGlobalTransform(objNode);
        Num.Matrix4x4.Invert(world, out var bind);
        var worldDup = world;

        // Write matrices in row-major order
        BinaryWriterExtensions.WriteMatrix(bw, world);
        BinaryWriterExtensions.WriteMatrix(bw, bind);
        BinaryWriterExtensions.WriteMatrix(bw, worldDup);

        // ---- Decompose world to TRS ----
        Num.Matrix4x4.Decompose(world, out var sc, out var rot, out var tr);
        BinaryWriterExtensions.WriteVector3(bw, tr);
        BinaryWriterExtensions.WriteQuaternion(bw, rot);
        BinaryWriterExtensions.WriteVector3(bw, sc);

        return ms.ToArray();
    }

    #endregion
}
