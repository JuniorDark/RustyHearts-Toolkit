using HelixToolkit.Maths;
using SharpAssimp;
using System.Numerics;
using static RHToolkit.Models.Model3D.Map.MMP;
using static RHToolkit.Models.Model3D.ModelMaterial;

namespace RHToolkit.Models.Model3D.Map;

/// <summary>
/// Exports an MMP model to FBX (via Assimp).
/// </summary>
public class MMPExporter
{
    public static async Task ExportMmpToFbx(MmpModel mmp, string outFilePath, bool embedTextures = false, bool copyTextures = false,
    CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(mmp);
        if (string.IsNullOrWhiteSpace(outFilePath))
            throw new ArgumentException("Output path is empty.", nameof(outFilePath));

        Scene? scene = null;
        try
        {
            ct.ThrowIfCancellationRequested();

            var outDir = Path.GetDirectoryName(outFilePath)!;
            Directory.CreateDirectory(outDir);

            var fileName = Path.GetFileNameWithoutExtension(outFilePath);

            // Base scene + root node
            scene = CreateScene(fileName, mmp, out var mmpNode);

            // Populate MMP meshes/materials
            BuildMmpObjects(scene, mmp, mmpNode, embedTextures, copyTextures, outDir);

            // Attach NAVI (navigation mesh) if present
            await AttachNaviAsync(scene, mmpNode, outFilePath, ct);

            SaveScene(scene, outFilePath);
        }
        finally
        {
            scene?.Clear();
        }
    }

    #region Save
    private static void SaveScene(Scene scene, string outFilePath)
    {
        var steps =
                 PostProcessSteps.MakeLeftHanded |
                 PostProcessSteps.FlipUVs |
                 PostProcessSteps.FlipWindingOrder;

        using var assimpContext = new AssimpContext();
        assimpContext.ExportFile(scene, outFilePath, "fbx", steps);
    }

    #endregion

    private static Scene CreateScene(string fileName, MmpModel mmp, out Node mmpNode)
    {
        var scene = new Scene
        {
            RootNode = new Node("Root")
        };

        mmpNode = new Node(fileName);
        scene.RootNode.Children.Add(mmpNode);
        SetNodeMeta(mmpNode, "mmp:version", mmp.Version);

        return scene;
    }

    private static void BuildMmpObjects(Scene scene, MmpModel mmp, Node mmpNode, bool embedTextures, bool copyTextures, string outDir)
    {
        // --- Material cache (Phong-ish) ---
        var matCache = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        // Map Type-3 transforms by hash (required for world→local conversion)
        var xformByHash = new Dictionary<uint, ModelNodeXform>();
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
                Transform = Matrix4x4.Transpose(nx.MWorld)
            };
            mmpNode.Children.Add(objNode);
            SetNodeMeta(objNode, "mmp:nodeGroupName", obj.AltNodeName ?? "");
            SetNodeMeta(objNode, "mmp:nodeKind", nx.Kind);
            SetNodeMeta(objNode, "mmp:nodeFlag", nx.Flag);

            // Precompute world scale magnitude (billboard sizing, kept for parity)
            Matrix4x4.Decompose(nx.MWorld, out var worldScale, out _, out _);
            var sx = worldScale.X == 0 ? 1f : MathF.Abs(worldScale.X);
            var sy = worldScale.Y == 0 ? 1f : MathF.Abs(worldScale.Y);

            // --- Mesh parts ---
            if (obj.Meshes == null || obj.Meshes.Count == 0) continue;

            // Prepare world→local for geometry bake (once per object)
            Matrix4x4.Invert(nx.MWorld, out var invWorld);
            var invWorldT = Matrix4x4.Transpose(invWorld);

            foreach (var part in obj.Meshes)
            {
                if (part.Vertices == null || part.Vertices.Length == 0) continue;
                if (part.Indices == null || part.Indices.Length < 3) continue;

                var meshName = string.IsNullOrWhiteSpace(part.MeshName) ? "Mesh" : part.MeshName;
                var verts = part.Vertices;

                // --- MeshType 2 = billboard quad  ---
                if (part.MeshType == 2)
                {
                    if (verts.Length == 0)
                        continue;

                    var meshBillboard = new Mesh(PrimitiveType.Triangle) { Name = meshName };

                    // Use first vertex position, baked to local, replicated for quad
                    var p0Local = Vector3.Transform(verts[0].Position, invWorld);

                    for (int i = 0; i < 4; i++)
                    {
                        var src = verts[Math.Min(i, verts.Length - 1)];

                        // position
                        meshBillboard.Vertices.Add(p0Local);

                        // normal
                        var n4 = Vector4.Transform(new Vector4(src.Normal, 0), invWorldT);
                        var n3 = new Vector3(n4.X, n4.Y, n4.Z);
                        if (n3.LengthSquared() > 1e-20f) n3 = Vector3.Normalize(n3);
                        meshBillboard.Normals.Add(n3);

                        // UV0
                        var t0 = src.UV0;
                        meshBillboard.TextureCoordinateChannels[0].Add(new Vector3(t0.X, t0.Y, 0));
                    }
                    meshBillboard.UVComponentCount[0] = 2;

                    // Indices
                    for (int i = 0; i + 2 < part.Indices.Length; i += 3)
                    {
                        var face = new Face();
                        face.Indices.Add(part.Indices[i]);
                        face.Indices.Add(part.Indices[i + 1]);
                        face.Indices.Add(part.Indices[i + 2]);
                        meshBillboard.Faces.Add(face);
                    }

                    meshBillboard.MaterialIndex = GetOrCreateMaterial(
                    scene, mmp, matCache,
                    part.Material, embedTextures, copyTextures, outDir);


                    meshes.Add(meshBillboard);
                    var meshIdx = meshes.Count - 1;

                    var billboardNode = new Node(meshName) { Transform = Matrix4x4.Identity };
                    billboardNode.MeshIndices.Add(meshIdx);
                    objNode.Children.Add(billboardNode);

                    SetNodeMeta(billboardNode, "mmp:meshType", part.MeshType);
                    SetNodeMeta(billboardNode, "mmp:vertexLayoutTag", part.MeshType);
                    SetNodeMeta(billboardNode, "mmp:isEmissiveAdditive", (int)part.AdditiveEmissive);
                    SetNodeMeta(billboardNode, "mmp:isAlphaBlend", (int)part.AlphaBlend);
                    SetNodeMeta(billboardNode, "mmp:isEnabled", (int)part.Enabled);
                    SetNodeMeta(billboardNode, "mmp:uvSetCount", part.UVSetCount);
                    SetNodeMeta(billboardNode, "mmp:materialId", part.MaterialIdx);

                    continue;
                }

                // --- Regular mesh path ---
                var meshR = new Mesh(PrimitiveType.Triangle) { Name = meshName };

                // Locals for tangent build
                var localPos = new Vector3[verts.Length];
                var localNrm = new Vector3[verts.Length];
                var localUV0 = new Vector2[verts.Length];

                for (int i = 0; i < verts.Length; i++)
                {
                    // position
                    var pL = Vector3.Transform(verts[i].Position, invWorld);
                    localPos[i] = pL;
                    meshR.Vertices.Add(new Vector3(pL.X, pL.Y, pL.Z));

                    // normal
                    var n4 = Vector4.Transform(new Vector4(verts[i].Normal, 0), invWorldT);
                    var n3 = new Vector3(n4.X, n4.Y, n4.Z);
                    if (n3.LengthSquared() > 1e-20f) n3 = Vector3.Normalize(n3);
                    localNrm[i] = n3;
                    meshR.Normals.Add(new Vector3(n3.X, n3.Y, n3.Z));

                    // UV0
                    var t0 = verts[i].UV0;
                    var uv = new Vector2(t0.X, t0.Y);
                    localUV0[i] = uv;
                    meshR.TextureCoordinateChannels[0].Add(new Vector3(uv.X, uv.Y, 0));
                }
                meshR.UVComponentCount[0] = 2;

                // UV1 if present
                if (verts.Any(v => v.UV1.HasValue))
                {
                    for (int i = 0; i < verts.Length; i++)
                    {
                        var uv1 = verts[i].UV1 ?? default;
                        meshR.TextureCoordinateChannels[1].Add(new Vector3(uv1.X, uv1.Y, 0));
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

                meshR.MaterialIndex = GetOrCreateMaterial(
                scene, mmp, matCache,
                part.Material, embedTextures, copyTextures, outDir);

                meshes.Add(meshR);
                var meshIndex = meshes.Count - 1;

                var partNode = new Node(meshName) { Transform = Matrix4x4.Identity };
                partNode.MeshIndices.Add(meshIndex);
                objNode.Children.Add(partNode);

                SetNodeMeta(partNode, "mmp:meshType", part.MeshType);
                SetNodeMeta(partNode, "mmp:vertexLayoutTag", part.MeshType);
                SetNodeMeta(partNode, "mmp:isEmissiveAdditive", (int)part.AdditiveEmissive);
                SetNodeMeta(partNode, "mmp:isAlphaBlend", (int)part.AlphaBlend);
                SetNodeMeta(partNode, "mmp:isEnabled", (int)part.Enabled);
                SetNodeMeta(partNode, "mmp:uvSetCount", part.UVSetCount);
                SetNodeMeta(partNode, "mmp:materialId", part.MaterialIdx);
            }
        }
    }

    private static async Task AttachNaviAsync(Scene scene, Node mmpNode, string outFilePath, CancellationToken ct)
    {
        try
        {
            var mmpDir = Path.GetDirectoryName(outFilePath)!;
            var mmpStem = Path.GetFileNameWithoutExtension(outFilePath);
            var naviPath = Path.Combine(mmpDir, mmpStem + ".navi");
            if (!File.Exists(naviPath))
                return;

            var navi = await NaviReader.ReadAsync(naviPath, ct);

            var naviXform = new Dictionary<uint, ModelNodeXform>();
            foreach (var nx in navi.Nodes)
                if (nx.NameHash != 0) naviXform[nx.NameHash] = nx;

            // Ensure we have at least one material to bind the nav mesh to
            if (scene.Materials.Count == 0)
            {
                var navMat = new Material
                {
                    Name = "_NavigationMesh",
                    ColorDiffuse = new Color4(0.2f, 0.8f, 0.2f, 1.0f)
                };
                scene.Materials.Add(navMat);
            }
            var navMaterialIndex = scene.Materials.Count - 1;

            var meshes = scene.Meshes;

            foreach (var e in navi.Entries)
            {
                if (!naviXform.TryGetValue(e.NameKey, out var nx))
                    throw new InvalidOperationException($"No matching Type-3 node found for navi entry '{e.Name}' (hash: {e.NameKey}).");

                // Nav root node carries WORLD transform, geometry baked to local
                var navRoot = new Node("_NavigationMesh_")
                {
                    Transform = Matrix4x4.Transpose(nx.MWorld)
                };
                mmpNode.Children.Add(navRoot);

                Matrix4x4.Invert(nx.MWorld, out var invWorld);

                var mesh = new Mesh(PrimitiveType.Triangle)
                {
                    Name = e.Name ?? "NavMesh"
                };

                foreach (var v in e.Vertices)
                {
                    var pLocal = Vector3.Transform(new Vector3(v.X, v.Y, v.Z), invWorld);
                    mesh.Vertices.Add(pLocal);
                }

                foreach (var tri in e.Indices)
                {
                    var face = new Face();
                    face.Indices.Add(tri.A);
                    face.Indices.Add(tri.B);
                    face.Indices.Add(tri.C);
                    mesh.Faces.Add(face);
                }

                mesh.MaterialIndex = navMaterialIndex;

                meshes.Add(mesh);
                var meshIdx = meshes.Count - 1;

                var navChild = new Node(e.Name ?? "NavMesh") { Transform = Matrix4x4.Identity };
                navChild.MeshIndices.Add(meshIdx);
                navRoot.Children.Add(navChild);

                SetNodeMeta(navRoot, "navi:isNavMesh", 1);
                SetNodeMeta(navRoot, "navi:version", navi.Header.Version);
                SetNodeMeta(navRoot, "navi:name", e.Name ?? string.Empty);
                SetNodeMeta(navRoot, "navi:nodeKind", nx.Kind);
                SetNodeMeta(navRoot, "navi:nodeFlag", nx.Flag);
            }
        }
        catch
        {
            // NAVI support is best-effort; failures should not break mesh export
        }
    }

    #region Helpers

    private static void SetNodeMeta(Node node, string key, object? value)
    {
        node.Metadata[key] = ToEntry(value);
    }

    private static Metadata.Entry ToEntry(object? value)
    {
        return value switch
        {
            bool b => new Metadata.Entry(MetaDataType.Bool, b),
            int i => new Metadata.Entry(MetaDataType.Int32, i),
            uint ui => new Metadata.Entry(MetaDataType.UInt64, (ulong)ui),
            float f => new Metadata.Entry(MetaDataType.Float, f),
            double d => new Metadata.Entry(MetaDataType.Double, d),
            string s => new Metadata.Entry(MetaDataType.String, s),
            null => new Metadata.Entry(MetaDataType.String, ""),
            _ => new Metadata.Entry(MetaDataType.String, value.ToString() ?? ""),
        };
    }

    private static int GetOrCreateMaterial(
    Scene scene,
    MmpModel mmp,
    Dictionary<string, int> matCache,
    ModelMaterial? m,
    bool embedTextures,
    bool copyTextures,
    string outDir)
    {
        var key = m?.MaterialName ?? "_Fallback";
        if (matCache.TryGetValue(key, out var found)) return found;

        var mat = new Material
        {
            Name = key,
            ColorDiffuse = new Color4(1, 1, 1, 1)
        };

        // Textures
        var diffuseAbs = ResolveTextureAbsolute(mmp.BaseDirectory, Texture(m, "DiffuseMap")?.TexturePath);
        if (!string.IsNullOrWhiteSpace(diffuseAbs) && File.Exists(diffuseAbs))
        {
            mat.TextureDiffuse = MakeTextureSlot(
                scene, outDir, diffuseAbs, embedTextures, copyTextures, TextureType.Diffuse);
        }

        var normalAbs = ResolveTextureAbsolute(mmp.BaseDirectory, Texture(m, "BumpMap")?.TexturePath);
        if (!string.IsNullOrWhiteSpace(normalAbs) && File.Exists(normalAbs))
        {
            mat.TextureNormal = MakeTextureSlot(
                scene, outDir, normalAbs, embedTextures, copyTextures, TextureType.Normals);
        }

        scene.Materials.Add(mat);
        var idx = scene.Materials.Count - 1;
        matCache[key] = idx;
        return idx;
    }

    private static TextureSlot MakeTextureSlot(
    Scene scene,
    string outDir,
    string absPath,
    bool embedTextures,
    bool copyTextures,
    TextureType texType)
    {
        var slot = new TextureSlot
        {
            TextureType = texType,
            Mapping = TextureMapping.FromUV,
            UVIndex = 0,
            BlendFactor = 1.0f,
            WrapModeU = TextureWrapMode.Wrap,
            WrapModeV = TextureWrapMode.Wrap,
            Flags = 0
        };

        // Check if the file is a DDS file
        bool isDds = Path.GetExtension(absPath).Equals(".dds", StringComparison.OrdinalIgnoreCase);

        if (embedTextures)
        {
            byte[] data;
            string ext;

            if (isDds)
            {
                // Convert DDS to PNG
                var ddsData = File.ReadAllBytes(absPath);
                var pngData = TextureConverter.ConvertDdsToPngData(ddsData);
                
                if (pngData != null)
                {
                    data = pngData;
                    ext = "png";
                }
                else
                {
                    // Fallback to original DDS if conversion fails
                    data = ddsData;
                    ext = Path.GetExtension(absPath).ToLowerInvariant().TrimStart('.');
                }
            }
            else
            {
                ext = Path.GetExtension(absPath).ToLowerInvariant().TrimStart('.');
                data = File.ReadAllBytes(absPath);
            }

            var index = scene.Textures.Count;
            var embName = $"*{index}.{ext}";

            var emb = new EmbeddedTexture(ext, data)
            {
                Filename = embName
            };

            scene.Textures.Add(emb);
            slot.FilePath = embName;
        }
        else if (copyTextures)
        {
            const string textureDir = "texture";
            var destDir = Path.Combine(outDir, textureDir);

            if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                Directory.CreateDirectory(destDir);

            string dest;
            
            if (isDds)
            {
                // Convert DDS to PNG
                var texNameWithoutExt = Path.GetFileNameWithoutExtension(absPath);
                dest = Path.Combine(destDir, texNameWithoutExt + ".png");
                
                if (!TextureConverter.ConvertDdsToPng(absPath, dest))
                {
                    // Fallback to copying the original DDS if conversion fails
                    var texName = Path.GetFileName(absPath);
                    dest = Path.Combine(destDir, texName);
                    try { File.Copy(absPath, dest, overwrite: false); }
                    catch (IOException) { }
                }
            }
            else
            {
                var texName = Path.GetFileName(absPath);
                dest = Path.Combine(destDir, texName);
                try { File.Copy(absPath, dest, overwrite: false); }
                catch (IOException) { }
            }

            slot.FilePath = dest;
        }
        else
        {
            slot.FilePath = absPath;
        }

        return slot;
    }


    #endregion
}
