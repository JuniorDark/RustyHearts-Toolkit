using Assimp;
using System.Diagnostics;
using Num = System.Numerics;

namespace RHToolkit.Models.Model3D.MMP;

/// <summary>
/// MMP file writer (currently rebuild type 3/19 from FBX + original MMP for type 1 material)
/// </summary>
public static class MMPWriter
{
    private const string Header = "DoBal";
    private static readonly Encoding ASCII = Encoding.ASCII;
    private readonly record struct Chunk(int Type, byte[] Data);

    public static void RebuildFromFbx(string originalMmpPath, string fbxPath, string outMmpPath)
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
            var scene = ctx.ImportFile(fbxPath, PostProcessSteps.FlipWindingOrder | PostProcessSteps.FlipUVs | PostProcessSteps.ImproveCacheLocality);

            var ai = new AssimpContext();
            foreach (var desc in ai.GetSupportedExportFormats())
                Debug.Write($"\n{desc.FormatId} -> {desc.Description}");

            var fileStem = Path.GetFileNameWithoutExtension(fbxPath);
            var container = scene.RootNode.Children.FirstOrDefault(n => n.Name == fileStem) ?? scene.RootNode;

            foreach (var objNode in container.Children)
            {
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
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to rebuild MMP from FBX: {fbxPath}: {ex.Message}", ex);
        }
    }

    // ---------------- mesh & node helpers (Assimp) ----------------

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

    private static int GetRequiredIntMeta(Assimp.Node node, string key)
    {
        if (node.Metadata != null && node.Metadata.TryGetValue(key, out Assimp.Metadata.Entry entry))
        {
            var data = GetMetaDataObject(entry);
            if (data != null)
            {
                switch (data)
                {
                    case int i: return i;
                    case long l: return checked((int)l);
                    case uint ui: return checked((int)ui);
                    case ulong ul: return checked((int)ul);
                    case short s: return s;
                    case ushort us: return us;
                    case byte b: return b;
                    case sbyte sb: return sb;
                    case float f: return checked((int)f);
                    case double d: return checked((int)d);
                    case bool bo: return bo ? 1 : 0;
                    case string str:
                        if (int.TryParse(str, out var v)) return v;
                        break;
                }
            }
            throw new InvalidDataException($"Metadata '{key}' on node '{node.Name}' exists but is not a valid integer.");
        }

        throw new InvalidDataException($"Required metadata '{key}' missing on node '{node.Name}'.");
    }

    private static string GetRequiredStringMeta(Assimp.Node node, string key)
    {
        if (node.Metadata != null && node.Metadata.TryGetValue(key, out Assimp.Metadata.Entry entry))
        {
            var data = GetMetaDataObject(entry);
            if (data is null) return string.Empty;
            if (data is string s) return s;
            throw new InvalidDataException(
                $"Metadata '{key}' on node '{node.Name}' exists but is not a string (got {data.GetType().Name}).");
        }

        throw new InvalidDataException($"Required metadata '{key}' missing on node '{node.Name}'.");
    }

    private static object? GetMetaDataObject(Assimp.Metadata.Entry entry)
    {
        var t = entry.GetType();
        var prop = t.GetProperty("Data") ?? t.GetProperty("Value");
        return prop?.GetValue(entry);
    }

    #region Type-19 Mesh Builder
    // ---------------- Type-19: geometry chunk (world-space, deterministic reindex) ----------------

    /// <summary>
    /// Per-part data accumulator
    /// </summary>
    /// <param name="SubName"></param>
    /// <param name="MaterialId"></param>
    /// <param name="LayoutTag"></param>
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
        int LayoutTag,
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
        WriteUtf16Len(bw, objectName);
        WriteUtf16Len(bw, objectName2);
        bw.Write(HashName(objectName));
        bw.Write(HashName(objectName2));
        WriteUtf16Body(bw, objectName);
        WriteUtf16Body(bw, objectName2);
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
        var m = System.Text.RegularExpressions.Regex.Match(subName, @"^(.*?)(\.\d{3,})$");
        if (m.Success) subName = m.Groups[1].Value;

        int materialId = GetRequiredIntMeta(partNode, "mmp:materialId");
        int layoutTag = GetRequiredIntMeta(partNode, "mmp:vertexLayoutTag");
        byte fl0 = (byte)GetRequiredIntMeta(partNode, "mmp:isEmissiveAdditive");
        byte fl1 = (byte)GetRequiredIntMeta(partNode, "mmp:isAlphaBlend");
        byte fl2 = (byte)GetRequiredIntMeta(partNode, "mmp:isEnabled");
        int uvSets = GetRequiredIntMeta(partNode, "mmp:uvSetCount");
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

        if (layoutTag == 2)
        {
            // Billboard/corona
            forcedBounds = BuildBillboard(partNode, mesh, Pw, Nw, UV0, indices,
                                          ref objMinX, ref objMinY, ref objMinZ, ref objMaxX, ref objMaxY, ref objMaxZ);
        }
        else
        {
            // Regular mesh
            BuildRegularMesh(partNode, mesh, uvSets, Pw, Nw, UV0, UV1, indices,
                             ref objMinX, ref objMinY, ref objMinZ, ref objMaxX, ref objMaxY, ref objMaxZ);
        }

        return new PartData(subName, materialId, layoutTag, fl0, fl1, fl2, uvSets, Pw, Nw, UV0, UV1, indices, forcedBounds);
    }

    // ---------- Step 2a: regular mesh path ----------
    /// <summary>
    /// Build a regular mesh (layoutTag 0 or 1) with world-space positions and normals, deterministic reindexing
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

        var M = GetGlobalTransform(partNode);
        Num.Matrix4x4.Invert(M, out var invM);
        var invMT = Num.Matrix4x4.Transpose(invM);

        // Deterministic vertex deduplication key (quantized position, normal, UVs)
        static uint Q(float f, float scale = 1e6f) => (uint)MathF.Round(f * scale);

        var vmap = new Dictionary<(uint, uint, uint, uint, uint, uint, uint, uint, uint, uint), int>(capacity: vpos.Count);

        int AddVertex(int srcIndex)
        {
            var pL = vpos[srcIndex];
            var pw4 = Num.Vector4.Transform(new Num.Vector4(pL.X, pL.Y, pL.Z, 1), M);
            var pw = new Num.Vector3(pw4.X, pw4.Y, pw4.Z);

            var n0 = (vnrms != null && srcIndex < vnrms.Count) ? vnrms[srcIndex] : new Assimp.Vector3D(0, 1, 0);
            var nw4 = Num.Vector4.Transform(new Num.Vector4(n0.X, n0.Y, n0.Z, 0), invMT);
            var nw = new Num.Vector3(nw4.X, nw4.Y, nw4.Z);

            var t0 = (uv0Src != null && srcIndex < uv0Src.Count) ? uv0Src[srcIndex] : default;
            var t1 = (uv1Src != null && srcIndex < uv1Src.Count) ? uv1Src[srcIndex] : default;

            var uv0 = new Num.Vector2(t0.X, t0.Y);
            var uv1 = uvSets == 2 ? new Num.Vector2(t1.X, t1.Y) : default;

            var key = (
            Q(pw.X), Q(pw.Y), Q(pw.Z),
            Q(nw.X), Q(nw.Y), Q(nw.Z),
            Q(uv0.X), Q(uv0.Y),
            uvSets == 2 ? Q(uv1.X) : 0u,
            uvSets == 2 ? Q(uv1.Y) : 0u
            );

            // Check for existing vertex
            if (vmap.TryGetValue(key, out int idx))
                return idx;

            idx = Pw.Count;
            Pw.Add(pw);
            Nw.Add(nw);
            UV0.Add(uv0);
            if (uvSets == 2) UV1.Add(uv1);
            vmap[key] = idx;
            return idx;
        }

        // Build indices with deduplicated vertices
        foreach (var f in mesh.Faces)
        {
            if (f.IndexCount < 3) continue;
            int a = AddVertex(f.Indices[0]);
            int b = AddVertex(f.Indices[1]);
            int c = AddVertex(f.Indices[2]);
            indices.Add((ushort)a); indices.Add((ushort)b); indices.Add((ushort)c);
        }

        // Contribute to OBJECT bounds with all emitted vertices
        for (int i = 0; i < Pw.Count; i++)
            ExpandAabb(Pw[i], ref objMinX, ref objMinY, ref objMinZ, ref objMaxX, ref objMaxY, ref objMaxZ);
    }

    // ---------- Step 2b: billboard/corona path (layoutTag == 2) ----------
    /// <summary>
    /// Build a billboard/corona (layoutTag 2) with world-space positions and normals, deterministic reindexing
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
        var M = GetGlobalTransform(partNode);

        // 1) Find true world extents based on source geometry
        float pMinX = float.PositiveInfinity, pMinY = float.PositiveInfinity, pMinZ = float.PositiveInfinity;
        float pMaxX = float.NegativeInfinity, pMaxY = float.NegativeInfinity, pMaxZ = float.NegativeInfinity;

        foreach (var f in mesh.Faces)
        {
            if (f.IndexCount < 3) continue;
            for (int k = 0; k < 3; k++)
            {
                int src = f.Indices[k];
                var pL = mesh.Vertices[src];
                var pw4 = Num.Vector4.Transform(new Num.Vector4(pL.X, pL.Y, pL.Z, 1), M);
                var pw = new Num.Vector3(pw4.X, pw4.Y, pw4.Z);
                if (pw.X < pMinX) pMinX = pw.X; if (pw.Y < pMinY) pMinY = pw.Y; if (pw.Z < pMinZ) pMinZ = pw.Z;
                if (pw.X > pMaxX) pMaxX = pw.X; if (pw.Y > pMaxY) pMaxY = pw.Y; if (pw.Z > pMaxZ) pMaxZ = pw.Z;
            }
        }

        // 2) Contribute to OBJECT bounds with true extents
        objMinX = MathF.Min(objMinX, pMinX); objMinY = MathF.Min(objMinY, pMinY); objMinZ = MathF.Min(objMinZ, pMinZ);
        objMaxX = MathF.Max(objMaxX, pMaxX); objMaxY = MathF.Max(objMaxY, pMaxY); objMaxZ = MathF.Max(objMaxZ, pMaxZ);

        // 3) Billboard center and scalar S (stored in Normal.Z)
        var center = new Num.Vector3(0.5f * (pMinX + pMaxX), 0.5f * (pMinY + pMaxY), 0.5f * (pMinZ + pMaxZ));
        float halfX = 0.5f * (pMaxX - pMinX);
        float S = halfX; // originals use single scalar; X half matches

        // 4) Emit vertices in original expected order; all positions = center; UV ignored
        Pw.Add(center); Nw.Add(new Num.Vector3(1, 1, -S)); UV0.Add(default); // v0
        Pw.Add(center); Nw.Add(new Num.Vector3(0, 1, -S)); UV0.Add(default); // v1
        Pw.Add(center); Nw.Add(new Num.Vector3(1, 0, +S)); UV0.Add(default); // v2
        Pw.Add(center); Nw.Add(new Num.Vector3(0, 0, +S)); UV0.Add(default); // v3

        // 5) Indices as in originals
        indices.Add(2); indices.Add(3); indices.Add(1);
        indices.Add(1); indices.Add(0); indices.Add(2);

        // 6) Per-part bounds to replicate original billboard style
        float minZ = pMinZ;
        float maxZ = float.Epsilon; // not zero
        return (center.X, center.Y, minZ, center.X, center.Y, maxZ, false);
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
        bw.Write(min.X); bw.Write(min.Y); bw.Write(min.Z);
        bw.Write(max.X); bw.Write(max.Y); bw.Write(max.Z);
    }

    // ---------- Step 5: write each mesh (header → bounds → vertex stream → indices) ----------
    /// <summary>
    /// Write a single mesh block (header, bounds, vertex stream, indices)
    /// </summary>
    /// <param name="bw"></param>
    /// <param name="part"></param>
    private static void WriteMeshBlock(BinaryWriter bw, PartData part)
    {
        var (subName, materialId, layoutTag, fl0, fl1, fl2, uvSets, Pw, Nw, UV0, UV1, indices, forcedBounds) = part;

        // Mesh header (name)
        WriteUtf16Len(bw, subName);
        WriteUtf16Body(bw, subName);

        // Counts & attributes
        bw.Write(Pw.Count);
        bw.Write(indices.Count / 3);
        bw.Write(layoutTag);
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
        WriteVertexStream(bw, layoutTag, uvSets, Pw, Nw, UV0, UV1);

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
        BinaryWriter bw, int layoutTag, int uvSets,
        List<Num.Vector3> Pw, List<Num.Vector3> Nw,
        List<Num.Vector2> UV0, List<Num.Vector2> UV1)
    {
        int stride = ComputeStride(layoutTag, uvSets);
        using var vms = new MemoryStream(Pw.Count * stride);
        using var vbw = new BinaryWriter(vms, Encoding.ASCII, leaveOpen: true);

        for (int i = 0; i < Pw.Count; i++)
        {
            var p = Pw[i];
            var n = Nw[i];
            vbw.Write(p.X); vbw.Write(p.Y); vbw.Write(p.Z);
            vbw.Write(n.X); vbw.Write(n.Y); vbw.Write(n.Z);

            if (layoutTag == 2)
            {
                vbw.Write(0f); // pad to 28 bytes/vertex
            }
            else
            {
                var t0 = UV0[i];
                vbw.Write(t0.X); vbw.Write(t0.Y);
                if (uvSets == 2)
                {
                    var t1 = (i < UV1.Count) ? UV1[i] : default;
                    vbw.Write(t1.X); vbw.Write(t1.Y);
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
        string objectName2 = GetRequiredStringMeta(objNode, "mmp:nodeGroupName");

        // ---- names + hashes (object, group, object) ----
        WriteUtf16Len(bw, objectName);
        WriteUtf16Len(bw, objectName2);
        WriteUtf16Len(bw, objectName);
        bw.Write(HashName(objectName));
        bw.Write(HashName(objectName2));
        bw.Write(HashName(objectName));
        WriteUtf16Body(bw, objectName);
        WriteUtf16Body(bw, objectName2);
        WriteUtf16Body(bw, objectName);

        // flags
        int kind = GetRequiredIntMeta(objNode, "mmp:nodeKind");
        int flag = GetRequiredIntMeta(objNode, "mmp:nodeFlag");
        bw.Write(kind);
        bw.Write(flag);

        bw.Write(0); bw.Write(0); bw.Write(0); bw.Write(0);   // unk1..unk4
        bw.Write((byte)0);                                    // b1
        if (version >= 7) bw.Write((byte)0);                  // b2

        // ---- Matrices: world, bind (inverse world), world dup ----
        var world = GetGlobalTransform(objNode);
        Num.Matrix4x4.Invert(world, out var bind);
        var worldDup = world;

        // Write matrices in row-major order
        WriteMatrix(bw, world);
        WriteMatrix(bw, bind);
        WriteMatrix(bw, worldDup);

        // ---- Decompose world to TRS ----
        Num.Matrix4x4.Decompose(world, out var sc, out var rot, out var tr);
        bw.Write(tr.X); bw.Write(tr.Y); bw.Write(tr.Z);
        bw.Write(rot.X); bw.Write(rot.Y); bw.Write(rot.Z); bw.Write(rot.W);
        bw.Write(sc.X); bw.Write(sc.Y); bw.Write(sc.Z);

        return ms.ToArray();
    }

    #endregion

    #region Matrices Helpers

    /// <summary>
    /// Write a 4x4 matrix in row-major order
    /// </summary>
    /// <param name="bw"></param>
    /// <param name="m"></param>
    private static void WriteMatrix(BinaryWriter bw, Num.Matrix4x4 m)
    {
        bw.Write(m.M11); bw.Write(m.M12); bw.Write(m.M13); bw.Write(m.M14);
        bw.Write(m.M21); bw.Write(m.M22); bw.Write(m.M23); bw.Write(m.M24);
        bw.Write(m.M31); bw.Write(m.M32); bw.Write(m.M33); bw.Write(m.M34);
        bw.Write(m.M41); bw.Write(m.M42); bw.Write(m.M43); bw.Write(m.M44);
    }

    /// <summary>
    /// convert Assimp matrix to System.Numerics matrix
    /// </summary>
    /// <param name="a"></param>
    /// <returns></returns>
    private static Num.Matrix4x4 FromAssimp(Assimp.Matrix4x4 a)
    {
        // Assimp uses row-major order, so direct mapping
        return new Num.Matrix4x4(
            a.A1, a.B1, a.C1, a.D1,
            a.A2, a.B2, a.C2, a.D2,
            a.A3, a.B3, a.C3, a.D3,
            a.A4, a.B4, a.C4, a.D4
        );
    }

    /// <summary>
    /// Get global transform of a node by accumulating local transforms up the hierarchy
    /// </summary>
    /// <param name="node"></param>
    /// <returns></returns>
    private static Num.Matrix4x4 GetGlobalTransform(Node node)
    {
        var global = Num.Matrix4x4.Identity;
        for (var current = node; current != null; current = current.Parent)
        {
            var local = FromAssimp(current.Transform);
            global = Num.Matrix4x4.Multiply(local, global);
        }
        return global;
    }

    #endregion

    #region Helpers
    /// <summary>
    /// Compute vertex stride from layout tag and UV set count
    /// </summary>
    /// <param name="vertexLayoutTag"></param>
    /// <param name="uvSetCount"></param>
    /// <returns></returns>
    /// <exception cref="InvalidDataException"></exception>
    public static int ComputeStride(int vertexLayoutTag, int uvSetCount)
    {
        if (vertexLayoutTag == 2) return 28;
        if (vertexLayoutTag == 0 && uvSetCount == 1) return 32;
        if (vertexLayoutTag == 0 && uvSetCount == 2) return 40;
        throw new InvalidDataException($"Unknown vertex layout: tag={vertexLayoutTag}, uv={uvSetCount}");
    }

    // ---- string helpers ----
    static void WriteUtf16Len(BinaryWriter bw, string s) => bw.Write(s?.Length ?? 0);
    static void WriteUtf16Body(BinaryWriter bw, string s) { if (!string.IsNullOrEmpty(s)) bw.Write(Encoding.Unicode.GetBytes(s)); }

    /// <summary>
    /// Calculate the hash of a name string (ASCII, case-sensitive)
    /// </summary>
    /// <param name="s"></param>
    /// <returns></returns>
    public static uint HashName(string s)
    {
        uint h = 0;
        foreach (byte b in Encoding.ASCII.GetBytes(s))
            h = unchecked(h * 31 + b);
        return h;
    }
    #endregion
}
