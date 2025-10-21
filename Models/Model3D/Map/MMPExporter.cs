using Assimp;
using static RHToolkit.Models.Model3D.Map.MMP;
using Matrix4x4 = Assimp.Matrix4x4;
using MDEntry = Assimp.Metadata.Entry;
using MDType = Assimp.MetaDataType;
using Num = System.Numerics;

namespace RHToolkit.Models.Model3D.Map;

/// <summary>
/// Exports an MMP model to FBX (via Assimp)
/// </summary>
public static class MMPExporter
{
    /// <summary>
    /// Exports the given MMP model to an FBX file.
    /// </summary>
    public static void ExportMmpToFbx(MmpModel mmp, string outFilePath, bool generateTangents, bool embedTextures)
    {
        ArgumentNullException.ThrowIfNull(mmp);
        if (string.IsNullOrWhiteSpace(outFilePath))
            throw new ArgumentException("Output path is empty.", nameof(outFilePath));

        var outDir = Path.GetDirectoryName(outFilePath)!;
        Directory.CreateDirectory(outDir);

        var fileName = Path.GetFileNameWithoutExtension(outFilePath);

        // --- Assimp scene root ---
        var scene = new Scene
        {
            RootNode = new Node("Root")
        };

        var mmpNode = new Node(fileName);
        scene.RootNode.Children.Add(mmpNode);
        SetNodeMeta(mmpNode, "mmp:version", mmp.Version);


        // --- Material cache (Phong-ish) ---
        var matCache = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase); // MaterialIndex in scene.Materials
        int GetOrCreateMaterial(MmpMaterial? m, byte additive, byte alpha)
        {
            var key = m?.MaterialName ?? "_Fallback";
            if (matCache.TryGetValue(key, out var found)) return found;

            var mat = new Material
            {
                Name = key,
                ColorDiffuse = new Color4D(1, 1, 1, 1)
            };

            // Textures (best-effort)
            var diffuseRel = ResolveTexture(outDir, Texture(m, "DiffuseMap")?.TexturePath, m?.MaterialName);
            if (!string.IsNullOrWhiteSpace(diffuseRel))
                mat.TextureDiffuse = new TextureSlot(
                    diffuseRel, TextureType.Diffuse, 0,
                    TextureMapping.FromUV, 0, 1.0f, TextureOperation.Add,
                    TextureWrapMode.Wrap, TextureWrapMode.Wrap, 0);

            var normalRel = ResolveTexture(outDir, Texture(m, "BumpMap")?.TexturePath, null);
            if (!string.IsNullOrWhiteSpace(normalRel))
                mat.TextureNormal = new TextureSlot(
                    normalRel, TextureType.Normals, 0,
                    TextureMapping.FromUV, 0, 1.0f, TextureOperation.Add,
                    TextureWrapMode.Wrap, TextureWrapMode.Wrap, 0);


            scene.Materials.Add(mat);
            var idx = scene.Materials.Count - 1;
            matCache[key] = idx;
            return idx;
        }

        // Map Type-3 transforms by hash (required for world→local conversion)
        var xformByHash = new Dictionary<uint, MmpNodeXform>();
        foreach (var nx in mmp.Nodes)
            if (nx.NameHash != 0) xformByHash[nx.NameHash] = nx;

        // Collect meshes as we build them
        var meshes = scene.Meshes;

        foreach (var obj in mmp.Objects)
        {
            var objName = string.IsNullOrWhiteSpace(obj.NodeName) ? "Object" : obj.NodeName;

            if (!xformByHash.TryGetValue(obj.NodeNameHash, out var nx))
                throw new InvalidOperationException($"No matching Type-3 node found for object '{obj.NodeName}' (hash: {obj.NodeNameHash}).");

            // Node carries WORLD transform; geometry baked to local (invWorld)
            var objNode = new Node(objName)
            {
                Transform = ToAssimp(nx.MWorld)
            };
            mmpNode.Children.Add(objNode);
            SetNodeMeta(objNode, "mmp:nodeGroupName", obj.AltNodeName ?? "");
            SetNodeMeta(objNode, "mmp:nodeKind", nx.Kind);
            SetNodeMeta(objNode, "mmp:nodeFlag", nx.Flag);

            // Precompute world scale magnitude (billboard sizing)
            Num.Matrix4x4.Decompose(nx.MWorld, out var worldScale, out _, out _);
            var sx = worldScale.X == 0 ? 1f : MathF.Abs(worldScale.X);
            var sy = worldScale.Y == 0 ? 1f : MathF.Abs(worldScale.Y);

            // --- Mesh parts ---
            if (obj.Meshes == null || obj.Meshes.Count == 0) continue;

            foreach (var part in obj.Meshes)
            {
                if (part.Vertices == null || part.Vertices.Length == 0) continue;
                if (part.Indices == null || part.Indices.Length < 3) continue;

                var meshName = string.IsNullOrWhiteSpace(part.MeshName) ? "Mesh" : part.MeshName;

                // --- Special case: VertexLayoutTag 2 = billboard quad ---
                if (part.MeshType == 2)
                {
                    // Use OBJECT bounds in world; localize by dividing scale
                    var worldW = obj.GeometryBounds.Size.X;
                    var worldH = obj.GeometryBounds.Size.Y;

                    if (!(worldW > 0)) worldW = 1f;
                    if (!(worldH > 0)) worldH = 1f;

                    var halfW_local = 0.5f * worldW / (sx == 0 ? 1f : sx);
                    var halfH_local = 0.5f * worldH / (sy == 0 ? 1f : sy);

                    var mesh = new Mesh(PrimitiveType.Triangle)
                    {
                        Name = meshName
                    };

                    // positions
                    mesh.Vertices.Add(new Vector3D(-halfW_local, -halfH_local, 0));
                    mesh.Vertices.Add(new Vector3D(halfW_local, -halfH_local, 0));
                    mesh.Vertices.Add(new Vector3D(halfW_local, halfH_local, 0));
                    mesh.Vertices.Add(new Vector3D(-halfW_local, halfH_local, 0));

                    // normals (+Z)
                    mesh.Normals.Add(new Vector3D(0, 0, 1));
                    mesh.Normals.Add(new Vector3D(0, 0, 1));
                    mesh.Normals.Add(new Vector3D(0, 0, 1));
                    mesh.Normals.Add(new Vector3D(0, 0, 1));

                    // UV0
                    mesh.TextureCoordinateChannels[0].Add(new Vector3D(0, 0, 0));
                    mesh.TextureCoordinateChannels[0].Add(new Vector3D(1, 0, 0));
                    mesh.TextureCoordinateChannels[0].Add(new Vector3D(1, 1, 0));
                    mesh.TextureCoordinateChannels[0].Add(new Vector3D(0, 1, 0));
                    mesh.UVComponentCount[0] = 2;

                    // triangles (0,1,2) (2,3,0)
                    mesh.Faces.Add(new Face([0, 1, 2]));
                    mesh.Faces.Add(new Face([2, 3, 0]));

                    // material
                    mesh.MaterialIndex = GetOrCreateMaterial(part.Material, part.AdditiveEmissive, part.AlphaBlend);

                    // track mesh & bind to node
                    meshes.Add(mesh);
                    var bmeshIdx = meshes.Count - 1;

                    // create a child node for this mesh
                    var bpartNode = new Node(meshName)
                    {
                        Transform = Matrix4x4.Identity   // geometry already baked to local
                    };
                    bpartNode.MeshIndices.Add(bmeshIdx);

                    // hang it under the object node
                    objNode.Children.Add(bpartNode);

                    continue;
                }

                // --- Regular mesh path ---
                var verts = part.Vertices;

                // Prepare world→local for geometry bake
                Num.Matrix4x4.Invert(nx.MWorld, out var invWorld);
                var invWorldT = Num.Matrix4x4.Transpose(invWorld);

                // Locals for tangent build
                var localPos = new Num.Vector3[verts.Length];
                var localNrm = new Num.Vector3[verts.Length];
                var localUV0 = new Num.Vector2[verts.Length];

                var meshR = new Mesh(PrimitiveType.Triangle) { Name = meshName };

                for (int i = 0; i < verts.Length; i++)
                {
                    // position
                    var pL = Num.Vector3.Transform(verts[i].Position, invWorld);
                    localPos[i] = pL;
                    meshR.Vertices.Add(new Vector3D(pL.X, pL.Y, pL.Z));

                    // normal
                    var n4 = Num.Vector4.Transform(new Num.Vector4(verts[i].Normal, 0), invWorldT);
                    var n3 = new Num.Vector3(n4.X, n4.Y, n4.Z);
                    if (n3.LengthSquared() > 1e-20f) n3 = Num.Vector3.Normalize(n3);
                    localNrm[i] = n3;
                    meshR.Normals.Add(new Vector3D(n3.X, n3.Y, n3.Z));

                    // UV0
                    var t0 = verts[i].UV0;
                    var uv = new Num.Vector2(t0.X, t0.Y);
                    localUV0[i] = uv;
                    meshR.TextureCoordinateChannels[0].Add(new Vector3D(uv.X, uv.Y, 0));
                }
                meshR.UVComponentCount[0] = 2;

                // UV1 if present
                if (verts.Any(v => v.UV1.HasValue))
                {
                    for (int i = 0; i < verts.Length; i++)
                    {
                        var uv1 = verts[i].UV1 ?? default;
                        meshR.TextureCoordinateChannels[1].Add(new Vector3D(uv1.X, uv1.Y, 0));
                    }
                    meshR.UVComponentCount[1] = 2;
                }

                // Indices
                for (int i = 0; i + 2 < part.Indices.Length; i += 3)
                {
                    var face = new Face();
                    face.Indices.Add(part.Indices[i]);
                    face.Indices.Add(part.Indices[i + 1]);
                    face.Indices.Add(part.Indices[i + 2]);
                    meshR.Faces.Add(face);
                }

                meshR.MaterialIndex = GetOrCreateMaterial(part.Material, part.AdditiveEmissive, part.AlphaBlend);

                meshes.Add(meshR);
                var meshIdx = meshes.Count - 1;

                var partNode = new Node(meshName) { Transform = Matrix4x4.Identity };
                partNode.MeshIndices.Add(meshIdx);
                objNode.Children.Add(partNode);
                SetNodeMeta(partNode, "mmp:vertexLayoutTag", part.MeshType);
                SetNodeMeta(partNode, "mmp:isEmissiveAdditive", (int)part.AdditiveEmissive);
                SetNodeMeta(partNode, "mmp:isAlphaBlend", (int)part.AlphaBlend);
                SetNodeMeta(partNode, "mmp:isEnabled", (int)part.Enabled);
                SetNodeMeta(partNode, "mmp:uvSetCount", 1);
                SetNodeMeta(partNode, "mmp:materialId", part.MaterialIdx);
            }
        }

        // --- Export FBX ---
        using var ctx = new AssimpContext();

        var steps = PostProcessSteps.FlipUVs | PostProcessSteps.MakeLeftHanded;
        ctx.ExportFile(scene, outFilePath, "fbx", steps);
    }

    #region Helpers

    static void SetNodeMeta(Assimp.Node node, string key, object? value)
    {
        node.Metadata[key] = ToEntry(value);
    }

    static MDEntry ToEntry(object? value)
    {
        return value switch
        {
            bool b => new MDEntry(MDType.Bool, b),
            int i => new MDEntry(MDType.Int32, i),
            uint ui => new MDEntry(MDType.UInt64, (ulong)ui),
            float f => new MDEntry(MDType.Float, f),
            double d => new MDEntry(MDType.Double, d),
            string s => new MDEntry(MDType.String, s),
            null => new MDEntry(MDType.String, ""),
            _ => new MDEntry(MDType.String, value.ToString() ?? ""),
        };
    }

    /// <summary>
    /// Converts a System.Numerics.Matrix4x4 to an Assimp.Matrix4x4
    /// </summary>
    /// <param name="m"></param>
    /// <returns></returns>
    private static Matrix4x4 ToAssimp(Num.Matrix4x4 m)
    {
        // Assimp uses row-major; translation is the 4th *column* (A4,B4,C4)
        return new Matrix4x4
        {
            A1 = m.M11,
            A2 = m.M12,
            A3 = m.M13,
            A4 = m.M41,
            B1 = m.M21,
            B2 = m.M22,
            B3 = m.M23,
            B4 = m.M42,
            C1 = m.M31,
            C2 = m.M32,
            C3 = m.M33,
            C4 = m.M43,
            D1 = m.M14,
            D2 = m.M24,
            D3 = m.M34,
            D4 = m.M44
        };
    }

    private static MmpTexture? Texture(MmpMaterial? m, string slotExact)
        => m?.Textures.FirstOrDefault(t =>
            t.Slot.Equals(slotExact, StringComparison.OrdinalIgnoreCase));

    private static string? ResolveTexture(string outDir, string? refPath, string? materialNameFallback)
    {
        // Normalize separators to the platform first
        static string Norm(string s) => s
            .Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar);

        // Try the referenced path first
        if (!string.IsNullOrWhiteSpace(refPath))
        {
            var norm = Norm(refPath.Trim());

            if (norm.StartsWith("." + Path.DirectorySeparatorChar))
            {
                var rel = norm.Substring(2);
                var abs = Path.GetFullPath(Path.Combine(outDir, rel));
                return File.Exists(abs) ? rel.Replace(Path.DirectorySeparatorChar, '/') : null;
            }

            if (norm.StartsWith(".." + Path.DirectorySeparatorChar))
            {
                var abs = Path.GetFullPath(Path.Combine(outDir, norm));
                return File.Exists(abs) ? norm.Replace(Path.DirectorySeparatorChar, '/') : null;
            }

            if (Path.IsPathRooted(norm))
            {
                var abs = Path.GetFullPath(norm);
                if (File.Exists(abs))
                {
                    var rel = Path.GetRelativePath(outDir, abs);
                    return rel.Replace(Path.DirectorySeparatorChar, '/');
                }
            }
            else
            {
                var abs = Path.GetFullPath(Path.Combine(outDir, norm));
                if (File.Exists(abs))
                    return norm.Replace(Path.DirectorySeparatorChar, '/');
            }

            var fo = Path.GetFileName(norm);
            if (!string.IsNullOrEmpty(fo))
            {
                var alt = Path.GetFullPath(Path.Combine(outDir, "texture", fo));
                if (File.Exists(alt))
                    return ("texture/" + fo).Replace('\\', '/');
            }
        }

        // Fallback by material name → texture/<material>.dds next to FBX
        if (!string.IsNullOrWhiteSpace(materialNameFallback))
        {
            var file = materialNameFallback.EndsWith(".dds", StringComparison.OrdinalIgnoreCase)
                ? materialNameFallback
                : materialNameFallback + ".dds";

            var abs1 = Path.GetFullPath(Path.Combine(outDir, "texture", file));
            if (File.Exists(abs1))
                return ("texture/" + file).Replace('\\', '/');

            var abs2 = Path.GetFullPath(Path.Combine(outDir, file));
            if (File.Exists(abs2))
                return file.Replace('\\', '/');
        }

        return null;
    }

    #endregion
}
