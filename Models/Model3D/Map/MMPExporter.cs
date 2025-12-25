using HelixToolkit.Maths;
using SharpAssimp;
using System.Numerics;
using static RHToolkit.Models.Model3D.Map.MMP;
using static RHToolkit.Models.Model3D.ModelMaterial;

namespace RHToolkit.Models.Model3D.Map;

/// <summary>
/// Exports MMP models to FBX format.
/// </summary>
public class MMPExporter
{
    public static async Task ExportMmpToFbx(MmpModel mmp, string outFilePath, bool embedTextures = false, bool copyTextures = false,
    bool exportSeparateObjects = false, CancellationToken ct = default)
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

            // Export materials as sidecar JSON
            await ExportMaterialSidecarAsync(mmp, outFilePath, ct);

            // Export separate objects if requested
            if (exportSeparateObjects)
            {
                await ExportSeparateObjectsAsync(mmp, outFilePath, embedTextures, copyTextures, ct);
            }
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

    /// <summary>
    /// Exports material data as a sidecar JSON file.
    /// </summary>
    private static async Task ExportMaterialSidecarAsync(MmpModel mmp, string outFilePath, CancellationToken ct)
    {
        // Collect material libraries from all materials
        var libraries = new List<MaterialLibrary>();
        foreach (var mat in mmp.Materials)
        {
            libraries.AddRange(mat.Library);
        }

        var sidecar = ModelMaterialSidecar.FromMaterials(mmp.Version, libraries, mmp.Materials);
        var sidecarPath = ModelMaterialSidecar.GetSidecarPath(outFilePath);
        await sidecar.SaveAsync(sidecarPath, ct);
    }

    /// <summary>
    /// Exports each MMP object as a separate FBX file with its own material sidecar.
    /// </summary>
    private static async Task ExportSeparateObjectsAsync(MmpModel mmp, string outFilePath, bool embedTextures, bool copyTextures, CancellationToken ct)
    {
        var outDir = Path.GetDirectoryName(outFilePath)!;
        var fileName = Path.GetFileNameWithoutExtension(outFilePath);

        // Create a subfolder named after the MMP file
        var objectsDir = Path.Combine(outDir, fileName);
        Directory.CreateDirectory(objectsDir);

        // Map Type-3 transforms by hash
        var xformByHash = new Dictionary<uint, ModelNodeXform>();
        foreach (var nx in mmp.Nodes)
            if (nx.NameHash != 0) xformByHash[nx.NameHash] = nx;

        foreach (var obj in mmp.Objects)
        {
            ct.ThrowIfCancellationRequested();

            var objName = string.IsNullOrWhiteSpace(obj.NodeName) ? "Object" : obj.NodeName;

            if (!xformByHash.TryGetValue(obj.NodeNameHash, out var nx))
                continue; // Skip objects without matching transform

            if (obj.Meshes == null || obj.Meshes.Count == 0)
                continue; // Skip objects without meshes

            // Collect materials used by this object
            var usedMaterialIds = new HashSet<int>();
            foreach (var part in obj.Meshes)
            {
                if (part.Material != null)
                    usedMaterialIds.Add(part.Material.MaterialIndex);
                else
                    usedMaterialIds.Add(part.MaterialIdx);
            }

            var usedMaterials = mmp.Materials
                .Where(m => usedMaterialIds.Contains(m.MaterialIndex))
                .ToList();

            // Create a scene for this single object
            Scene? objScene = null;
            try
            {
                objScene = new Scene
                {
                    RootNode = new Node("Root")
                };

                var objRootNode = new Node(objName);
                objScene.RootNode.Children.Add(objRootNode);

                // Build material cache for this object
                var matCache = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                var matIdRemap = new Dictionary<int, int>(); // Original material ID -> new index in this scene

                // Add materials used by this object
                foreach (var mat in usedMaterials)
                {
                    var newIdx = GetOrCreateMaterial(objScene, mmp, matCache, mat, embedTextures, copyTextures, objectsDir);
                    matIdRemap[mat.MaterialIndex] = newIdx;
                }

                // Build the object node
                var objNodeNameEncoded = $"{objName}__K{nx.Kind}";
                var objNode = new Node(objNodeNameEncoded)
                {
                    Transform = Matrix4x4.Transpose(nx.MWorld)
                };
                objRootNode.Children.Add(objNode);

                // Prepare world→local for geometry bake
                Matrix4x4.Invert(nx.MWorld, out var invWorld);

                foreach (var part in obj.Meshes)
                {
                    if (part.Vertices == null || part.Vertices.Length == 0) continue;
                    if (part.Indices == null || part.Indices.Length < 3) continue;

                    var meshName = string.IsNullOrWhiteSpace(part.MeshName) ? "Mesh" : part.MeshName;
                    var verts = part.Vertices;

                    // Get remapped material index
                    var origMatId = part.Material?.MaterialIndex ?? part.MaterialIdx;
                    var matIdx = matIdRemap.TryGetValue(origMatId, out var remapped) ? remapped : 0;

                    // --- MeshType 2 = billboard quad ---
                    if (part.MeshType == 2)
                    {
                        if (verts.Length == 0) continue;

                        var meshBillboard = new Mesh(PrimitiveType.Triangle) { Name = meshName };
                        var p0Local = Vector3.Transform(verts[0].Position, invWorld);

                        for (int i = 0; i < 4; i++)
                        {
                            var src = verts[Math.Min(i, verts.Length - 1)];
                            meshBillboard.Vertices.Add(p0Local);
                            meshBillboard.Normals.Add(new Vector3(0, 0, 1));

                            var payload = src.Normal;
                            var t0 = src.UV0;
                            meshBillboard.TextureCoordinateChannels[0].Add(new Vector3(t0.X, payload.X, 0));
                            meshBillboard.TextureCoordinateChannels[1].Add(new Vector3(payload.Y, payload.Z, 0));
                        }

                        meshBillboard.UVComponentCount[0] = 2;
                        meshBillboard.UVComponentCount[1] = 2;

                        for (int i = 0; i + 2 < part.Indices.Length; i += 3)
                        {
                            var face = new Face();
                            face.Indices.Add(part.Indices[i]);
                            face.Indices.Add(part.Indices[i + 1]);
                            face.Indices.Add(part.Indices[i + 2]);
                            meshBillboard.Faces.Add(face);
                        }

                        meshBillboard.MaterialIndex = matIdx;
                        objScene.Meshes.Add(meshBillboard);
                        var meshIdx = objScene.Meshes.Count - 1;

                        var suffix = $"__T{part.MeshType}_A{(int)part.AdditiveEmissive}_B{(int)part.AlphaBlend}_E{(int)part.Enabled}_M{part.MaterialIdx}";
                        var billboardNode = new Node(meshName + suffix) { Transform = Matrix4x4.Identity };
                        billboardNode.MeshIndices.Add(meshIdx);
                        objNode.Children.Add(billboardNode);

                        continue;
                    }

                    // --- Regular mesh path ---
                    var meshR = new Mesh(PrimitiveType.Triangle) { Name = meshName };

                    for (int i = 0; i < verts.Length; i++)
                    {
                        var pL = Vector3.Transform(verts[i].Position, invWorld);
                        meshR.Vertices.Add(new Vector3(pL.X, pL.Y, pL.Z));

                        var n3 = verts[i].Normal;
                        meshR.Normals.Add(new Vector3(n3.X, n3.Y, n3.Z));

                        var t0 = verts[i].UV0;
                        meshR.TextureCoordinateChannels[0].Add(new Vector3(t0.X, t0.Y, 0));
                    }
                    meshR.UVComponentCount[0] = 2;

                    if (verts.Any(v => v.UV1.HasValue))
                    {
                        for (int i = 0; i < verts.Length; i++)
                        {
                            var uv1 = verts[i].UV1 ?? default;
                            meshR.TextureCoordinateChannels[1].Add(new Vector3(uv1.X, uv1.Y, 0));
                        }
                        meshR.UVComponentCount[1] = 2;
                    }

                    for (int i = 0; i + 2 < part.Indices.Length; i += 3)
                    {
                        var face = new Face();
                        face.Indices.Add(part.Indices[i]);
                        face.Indices.Add(part.Indices[i + 1]);
                        face.Indices.Add(part.Indices[i + 2]);
                        meshR.Faces.Add(face);
                    }

                    meshR.MaterialIndex = matIdx;
                    objScene.Meshes.Add(meshR);
                    var meshIndex = objScene.Meshes.Count - 1;

                    var suffix2 = $"__T{part.MeshType}_A{(int)part.AdditiveEmissive}_B{(int)part.AlphaBlend}_E{(int)part.Enabled}_M{part.MaterialIdx}";
                    var partNode = new Node(meshName + suffix2) { Transform = Matrix4x4.Identity };
                    partNode.MeshIndices.Add(meshIndex);
                    objNode.Children.Add(partNode);
                }

                // Save the object FBX
                var objFbxPath = Path.Combine(objectsDir, $"{objName}.fbx");
                SaveScene(objScene, objFbxPath);

                // Export material sidecar for this object
                var objLibraries = new List<MaterialLibrary>();
                foreach (var mat in usedMaterials)
                {
                    objLibraries.AddRange(mat.Library);
                }

                var objSidecar = ModelMaterialSidecar.FromMaterials(mmp.Version, objLibraries, usedMaterials);
                var objSidecarPath = ModelMaterialSidecar.GetSidecarPath(objFbxPath);
                await objSidecar.SaveAsync(objSidecarPath, ct);
            }
            finally
            {
                objScene?.Clear();
            }
        }
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
                throw new InvalidOperationException($"No matching Type-3 node found for object '{obj.NodeName}'.");

            // Node carries WORLD transform; geometry baked to local (invWorld)
            var objNodeNameEncoded = $"{objName}__K{nx.Kind}";
            var objNode = new Node(objNodeNameEncoded)
            {
                Transform = Matrix4x4.Transpose(nx.MWorld)
            };
            mmpNode.Children.Add(objNode);

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

                        // position (center), replicated for quad
                        meshBillboard.Vertices.Add(p0Local);

                        // IMPORTANT: don't store billboard payload in normals; Blender will normalize them.
                        // Write a unit-length normal for compatibility/shading.
                        meshBillboard.Normals.Add(new Vector3(0, 0, 1));

                        // Billboard payload we must preserve losslessly:
                        //   payload = src.Normal (can be non-unit / large magnitude)
                        var payload = src.Normal;

                        // UV0:
                        //   X = original scalar
                        //   Y = payload.X
                        var t0 = src.UV0;
                        meshBillboard.TextureCoordinateChannels[0].Add(new Vector3(t0.X, payload.X, 0));

                        // UV1:
                        //   X = payload.Y
                        //   Y = payload.Z
                        meshBillboard.TextureCoordinateChannels[1].Add(new Vector3(payload.Y, payload.Z, 0));
                    }

                    // Declare UV set sizes
                    meshBillboard.UVComponentCount[0] = 2;
                    meshBillboard.UVComponentCount[1] = 2;

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

                    var suffix = $"__T{part.MeshType}_A{(int)part.AdditiveEmissive}_B{(int)part.AlphaBlend}_E{(int)part.Enabled}_M{part.MaterialIdx}";
                    var billboardNode = new Node(meshName + suffix) { Transform = Matrix4x4.Identity };
                    billboardNode.MeshIndices.Add(meshIdx);
                    objNode.Children.Add(billboardNode);

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
                    var n3 = verts[i].Normal;
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

                var suffix2 = $"__T{part.MeshType}_A{(int)part.AdditiveEmissive}_B{(int)part.AlphaBlend}_E{(int)part.Enabled}_M{part.MaterialIdx}";
                var partNode = new Node(meshName + suffix2) { Transform = Matrix4x4.Identity };
                partNode.MeshIndices.Add(meshIndex);
                objNode.Children.Add(partNode);
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

            var navMat = new Material
            {
                Name = "_NavigationMesh",
                ColorDiffuse = new Color4(1, 1, 1, 1)
            };
            scene.Materials.Add(navMat);
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
                    Name = e.Name ?? "NM_Plane01"
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

                var navChild = new Node(e.Name ?? "NM_Plane01") { Transform = Matrix4x4.Identity };
                navChild.MeshIndices.Add(meshIdx);
                navRoot.Children.Add(navChild);
            }
        }
        catch
        {
            // NAVI support is best-effort; failures should not break mesh export
        }
    }

    #region Helpers

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
            ShadingMode = ShadingMode.Phong,
            ColorDiffuse = new Color4(1, 1, 1, 1),
            ColorAmbient = new Color4(0, 0, 0, 1),
            ColorSpecular = new Color4(0.04f, 0.04f, 0.04f, 1),
            ColorEmissive = new Color4(0, 0, 0, 1),
            Shininess = 1,
            Opacity = 1
        };

        // Textures
        var diffuseAbs = ResolveTextureAbsolute(mmp.BaseDirectory, Texture(m, "DiffuseMap")?.TexturePath);
        if (!string.IsNullOrWhiteSpace(diffuseAbs) && File.Exists(diffuseAbs))
        {
            mat.TextureDiffuse = MakeTextureSlot(
                scene, outDir, diffuseAbs, embedTextures, copyTextures, TextureType.Diffuse);
        }

        var specularAbs = ResolveTextureAbsolute(mmp.BaseDirectory, Texture(m, "SpecularMap")?.TexturePath);
        if (!string.IsNullOrWhiteSpace(specularAbs) && File.Exists(specularAbs))
        {
            mat.TextureSpecular = MakeTextureSlot(
                scene, outDir, specularAbs, embedTextures, copyTextures, TextureType.Specular);
        }

        var normalAbs = ResolveTextureAbsolute(mmp.BaseDirectory, Texture(m, "BumpMap")?.TexturePath);
        if (!string.IsNullOrWhiteSpace(normalAbs) && File.Exists(normalAbs))
        {
            mat.TextureNormal = MakeTextureSlot(
                scene, outDir, normalAbs, embedTextures, copyTextures, TextureType.Normals);
        }

        // ---------- Shader-driven values ----------
        // Colors
        var shDiffuse = Shader(m, "Diffuse");
        if (shDiffuse != null)
        {
            var c = shDiffuse.Value;
            mat.ColorDiffuse = new Color4(c.X, c.Y, c.Z, c.W);
        }

        var shAmbient = Shader(m, "Ambient");
        if (shAmbient != null)
        {
            var c = shAmbient.Value;
            mat.ColorAmbient = new Color4(c.X, c.Y, c.Z, c.W);
        }

        // SunFactor/SunPower heuristics → shininess/spec strength
        var shSunFactor = Shader(m, "SunFactor");
        var shSunPower = Shader(m, "SunPower");
        if (shSunFactor != null || shSunPower != null)
        {
            var pow = shSunPower?.Value.X ?? 0f;
            var fac = Math.Clamp(shSunFactor?.Value.X ?? 0f, 0f, 1.5f);
            mat.Shininess = Math.Clamp(8f + pow * 16f, 2f, 128f);
            var spec = 0.04f + 0.6f * fac;
            mat.ColorSpecular = new Color4(spec, spec, spec, 1);
        }

        // Alpha: use AlphaValue as transparency factor (0=opaque, 1=fully transparent)
        var shAlphaValue = Shader(m, "AlphaValue");
        if (shAlphaValue != null)
            mat.Opacity = 1f - Math.Clamp(shAlphaValue.Value.X, 0f, 1f);

        // Emissive hint (StarEffect or Additive styles often imply glow)
        var shStar = Shader(m, "StarEffect");
        if (shStar != null)
        {
            var e = Math.Clamp(shStar.Value.X, 0f, 1f);
            mat.ColorEmissive = new Color4(e, e, e, 1);
        }

        // WaterColor → make it slightly emissive/diffuse tinted
        var shWater = Shader(m, "WaterColor");
        if (shWater != null)
        {
            var c = shWater.Value;
            mat.ColorDiffuse = new Color4(c.X, c.Y, c.Z, c.W);
            mat.ColorEmissive = new Color4(c.X * 0.15f, c.Y * 0.15f, c.Z * 0.15f, c.W);
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
