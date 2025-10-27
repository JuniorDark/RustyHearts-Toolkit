using Aspose.ThreeD;
using Aspose.ThreeD.Formats;
using static RHToolkit.Models.Model3D.Map.MMP;
using static RHToolkit.Models.Model3D.ModelMaterial;
using A3D = Aspose.ThreeD;
using A3DE = Aspose.ThreeD.Entities;
using A3DS = Aspose.ThreeD.Shading;
using AU = Aspose.ThreeD.Utilities;
using Num = System.Numerics;

namespace RHToolkit.Models.Model3D.Map;

/// <summary>
/// Exports an MMP model to FBX using Aspose.3D (Trial Version).
/// </summary>
public class MMPExporterAspose
{
    public async Task ExportMmpToFbx(MmpModel mmp, string outFilePath, bool embedTextures = false)
    {
        ArgumentNullException.ThrowIfNull(mmp);
        if (string.IsNullOrWhiteSpace(outFilePath)) throw new ArgumentException("Output path is empty.", nameof(outFilePath));

        // Scene & root container
        var (scene, root, mmpNode, outDir, fileName) = CreateSceneAndRoot(mmp, outFilePath);

        // Materials/Textures factory
        var getOrCreateMaterial = CreateMaterialResolver(mmp, outDir, embedTextures);

        // Map Type-3 transforms by hash (required for world→local conversion)
        var xformByHash = BuildXformLookup(mmp);

        // Objects & meshes
        await BuildObjectsAndMeshesAsync(scene, mmpNode, mmp, xformByHash, getOrCreateMaterial);

        // NAVI attachment
        await AttachNaviAsync(mmp, mmpNode, outFilePath);

        // Save
        await SaveSceneAsync(scene, outFilePath, embedTextures);
    }

    #region Scene / Root
    private static (A3D.Scene scene, A3D.Node root, A3D.Node mmpNode, string outDir, string fileName)
        CreateSceneAndRoot(MmpModel mmp, string outFilePath)
    {
        var outDir = Path.GetDirectoryName(outFilePath)!;
        Directory.CreateDirectory(outDir);
        var fileName = Path.GetFileNameWithoutExtension(outFilePath);

        var scene = new A3D.Scene();
        var root = scene.RootNode;

        var mmpNode = new A3D.Node(fileName);
        root.AddChildNode(mmpNode);
        mmpNode.Transform.Scaling = new AU.Vector3(-1, 1, 1);
        mmpNode.SetProperty("mmp:version", mmp.Version);
        mmpNode.SetProperty("mmp:exporter", "RHToolkit");

        return (scene, root, mmpNode, outDir, fileName);
    }
    #endregion

    #region Materials / Textures
    private Func<ModelMaterial?, A3DS.Material> CreateMaterialResolver(MmpModel mmp, string outDir, bool embedTextures)
    {
        var matCache = new Dictionary<string, A3DS.Material>(StringComparer.OrdinalIgnoreCase);

        A3DS.Material GetOrCreateMaterial(ModelMaterial? m)
        {
            var key = m?.MaterialName ?? "_Fallback";
            if (matCache.TryGetValue(key, out var found)) return found;

            var mat = new A3DS.PhongMaterial
            {
                Name = key,
                DiffuseColor = new AU.Vector3(1, 1, 1),
                AmbientColor = new AU.Vector3(0, 0, 0),
                SpecularColor = new AU.Vector3(0.04f, 0.04f, 0.04f),
                EmissiveColor = new AU.Vector3(0, 0, 0),
                Shininess = 16,
                Transparency = 0
            };

            // ---------- Textures ----------
            var diffuseAbs = ResolveTextureAbsolute(mmp.BaseDirectory, Texture(m, "DiffuseMap")?.TexturePath);
            var specularAbs = ResolveTextureAbsolute(mmp.BaseDirectory, Texture(m, "SpecularMap")?.TexturePath);
            var normalAbs = ResolveTextureAbsolute(mmp.BaseDirectory, Texture(m, "BumpMap")?.TexturePath);

            if (!string.IsNullOrWhiteSpace(diffuseAbs))
                mat.SetTexture("DiffuseColor", MakeTexture(outDir, diffuseAbs, embedTextures));
            if (!string.IsNullOrWhiteSpace(specularAbs))
                mat.SetTexture("SpecularColor", MakeTexture(outDir, specularAbs, embedTextures));
            if (!string.IsNullOrWhiteSpace(normalAbs))
                mat.SetTexture("BumpMap", MakeTexture(outDir, normalAbs, embedTextures));

            // ---------- Shader-driven values ----------
            // Colors
            var shDiffuse = Shader(m, "Diffuse");
            if (shDiffuse != null)
            {
                var c = Clamp01(ToRGB(shDiffuse.Base));
                mat.DiffuseColor = new AU.Vector3(c.X, c.Y, c.Z);
            }

            var shAmbient = Shader(m, "Ambient");
            if (shAmbient != null)
            {
                var c = Clamp01(ToRGB(shAmbient.Base));
                mat.AmbientColor = new AU.Vector3(c.X, c.Y, c.Z);
            }

            // SunFactor/SunPower heuristics → shininess/spec strength
            var shSunFactor = Shader(m, "SunFactor");
            var shSunPower = Shader(m, "SunPower");
            if (shSunFactor != null || shSunPower != null)
            {
                var pow = shSunPower?.Scalar ?? 0f; // exponent-ish
                var fac = Math.Clamp(shSunFactor?.Scalar ?? 0f, 0f, 1.5f);
                mat.Shininess = Math.Clamp(8f + pow * 16f, 2f, 128f);
                var spec = 0.04f + 0.6f * fac;
                mat.SpecularColor = new AU.Vector3(spec, spec, spec);
            }

            // Alpha: use AlphaValue as transparency factor (0=opaque, 1=fully transparent)
            var shAlphaValue = Shader(m, "AlphaValue");
            if (shAlphaValue != null)
                mat.Transparency = Math.Clamp(shAlphaValue.Scalar, 0f, 1f);

            // Emissive hint (StarEffect or Additive styles often imply glow)
            var shStar = Shader(m, "StarEffect");
            if (shStar != null)
            {
                var e = Math.Clamp(shStar.Scalar, 0f, 1f);
                mat.EmissiveColor = new AU.Vector3(e, e, e);
            }

            // Lightmap: if present, attach to AmbientColor channel
            var shLightmap = Shader(m, "LightmapTexturem");
            if (shLightmap != null)
            {
                var lmAbs = ResolveTextureAbsolute(mmp.BaseDirectory, Texture(m, "LightmapTexturem")?.TexturePath);
                if (!string.IsNullOrWhiteSpace(lmAbs))
                    mat.SetTexture("AmbientColor", MakeTexture(outDir, lmAbs, embedTextures));
            }

            // WaterColor → tint diffuse & slight emissive
            var shWater = Shader(m, "WaterColor");
            if (shWater != null)
            {
                var c = Clamp01(ToRGB(shWater.Base));
                mat.DiffuseColor = new AU.Vector3(c.X, c.Y, c.Z);
                mat.EmissiveColor = new AU.Vector3(c.X * 0.15f, c.Y * 0.15f, c.Z * 0.15f);
            }

            matCache[key] = mat;
            return mat;
        }

        return GetOrCreateMaterial;
    }
    #endregion

    #region Xform Lookup
    private static Dictionary<uint, ModelNodeXform> BuildXformLookup(MmpModel mmp)
    {
        var xformByHash = new Dictionary<uint, ModelNodeXform>();
        foreach (var nx in mmp.Nodes)
            if (nx.NameHash != 0) xformByHash[nx.NameHash] = nx;
        return xformByHash;
    }
    #endregion

    #region Objects & Meshes
    private static async Task BuildObjectsAndMeshesAsync(
        A3D.Scene scene,
        A3D.Node mmpNode,
        MmpModel mmp,
        Dictionary<uint, ModelNodeXform> xformByHash,
        Func<ModelMaterial?, A3DS.Material> GetOrCreateMaterial)
    {
        await Task.Run(() =>
        {
            foreach (var obj in mmp.Objects)
            {
                var objName = string.IsNullOrWhiteSpace(obj.NodeName) ? "Object" : obj.NodeName;

                if (!xformByHash.TryGetValue(obj.NodeNameHash, out var nx))
                    throw new InvalidOperationException($"No matching Type-3 node found for object '{obj.NodeName}' (hash: {obj.NodeNameHash}).");

                // FBX node holds WORLD transform; we bake geometry to local space (via invWorld)
                var objNode = new A3D.Node(objName);
                mmpNode.AddChildNode(objNode);
                objNode.Transform.TransformMatrix = ToAspose(nx.MWorld);

                // Metadata
                objNode.SetProperty("mmp:nodeGroupName", obj.AltNodeName ?? string.Empty);
                objNode.SetProperty("mmp:nodeKind", nx.Kind);
                objNode.SetProperty("mmp:nodeFlag", nx.Flag);

                // Precompute decomposed world scale
                Num.Matrix4x4.Decompose(nx.MWorld, out var worldScale, out _, out _);
                var sx = worldScale.X == 0 ? 1f : MathF.Abs(worldScale.X);
                var sy = worldScale.Y == 0 ? 1f : MathF.Abs(worldScale.Y);
                _ = (sx, sy);

                if (obj.Meshes == null || obj.Meshes.Count == 0) continue;

                // Prepare world→local for geometry bake
                Num.Matrix4x4.Invert(nx.MWorld, out var invWorld);
                var invWorldT = Num.Matrix4x4.Transpose(invWorld);

                foreach (var part in obj.Meshes)
                {
                    if (part.Vertices == null || part.Vertices.Length == 0) continue;
                    if (part.Indices == null || part.Indices.Length < 3) continue;

                    var meshName = string.IsNullOrWhiteSpace(part.MeshName) ? "Mesh" : part.MeshName;
                    var uvScale = GetTexScale(part.Material);

                    if (part.MeshType == 2)
                    {
                        var mesh = BuildBillboardMesh(part, invWorld);
                        var node = objNode.CreateChildNode(meshName, mesh);
                        node.Material = GetOrCreateMaterial(part.Material);
                        StampPartMetadata(node, part);
                        continue;
                    }

                    // Regular mesh path
                    var meshR = BuildRegularMesh(part, invWorld, invWorldT, uvScale);
                    var partNode = objNode.CreateChildNode(meshName, meshR);
                    partNode.Material = GetOrCreateMaterial(part.Material);
                    StampPartMetadata(partNode, part);
                }
            }
        });
    }

    private static A3DE.Mesh BuildBillboardMesh(MmpMesh part, Num.Matrix4x4 invWorld)
    {
        var mesh = new A3DE.Mesh { Name = string.IsNullOrWhiteSpace(part.MeshName) ? "Mesh" : part.MeshName };

        var p0 = part.Vertices![0].Position;
        var p0Local4 = Num.Vector4.Transform(new Num.Vector4(p0.X, p0.Y, p0.Z, 1), invWorld);
        var p0Local = new AU.Vector4(p0Local4.X, p0Local4.Y, p0Local4.Z, 1);

        // four control points at the same spot (indices shape the quad)
        mesh.ControlPoints.Add(p0Local);
        mesh.ControlPoints.Add(p0Local);
        mesh.ControlPoints.Add(p0Local);
        mesh.ControlPoints.Add(p0Local);

        for (int i = 0; i < part.Indices!.Length; i += 3)
            mesh.CreatePolygon(part.Indices[i], part.Indices[i + 1], part.Indices[i + 2]);

        var nrm = new A3DE.VertexElementNormal
        {
            MappingMode = A3DE.MappingMode.ControlPoint,
            ReferenceMode = A3DE.ReferenceMode.Direct
        };
        foreach (var v in part.Vertices!)
            nrm.Data.Add(new AU.Vector4(v.Normal.X, v.Normal.Y, v.Normal.Z, 0));
        mesh.VertexElements.Add(nrm);

        var uv0 = new A3DE.VertexElementUV
        {
            MappingMode = A3DE.MappingMode.ControlPoint,
            ReferenceMode = A3DE.ReferenceMode.Direct
        };
        foreach (var v in part.Vertices!)
            uv0.Data.Add(new AU.Vector4(v.UV0.X, v.UV0.Y, 0, 0));
        mesh.VertexElements.Add(uv0);

        return mesh;
    }

    private static A3DE.Mesh BuildRegularMesh(MmpMesh part, Num.Matrix4x4 invWorld, Num.Matrix4x4 invWorldT, Num.Vector2 uvScale)
    {
        var verts = part.Vertices!;

        var cp = new List<AU.Vector4>(verts.Length);
        var nrmLocal = new List<AU.Vector4>(verts.Length);
        var localUV0 = new List<AU.Vector4>(verts.Length);

        for (int i = 0; i < verts.Length; i++)
        {
            var pL = Num.Vector3.Transform(verts[i].Position, invWorld);
            cp.Add(new AU.Vector4(pL.X, pL.Y, pL.Z, 1));

            var n4 = Num.Vector4.Transform(new Num.Vector4(verts[i].Normal, 0), invWorldT);
            var n3 = new Num.Vector3(n4.X, n4.Y, n4.Z);
            if (n3.LengthSquared() > 1e-20f) n3 = Num.Vector3.Normalize(n3);
            nrmLocal.Add(new AU.Vector4(n3.X, n3.Y, n3.Z, 0));

            // UV0 with V flip
            var t0 = verts[i].UV0;
            localUV0.Add(new AU.Vector4(t0.X * uvScale.X, (1.0f - t0.Y) * uvScale.Y, 0, 0));
        }

        var meshR = new A3DE.Mesh { Name = string.IsNullOrWhiteSpace(part.MeshName) ? "Mesh" : part.MeshName };
        meshR.ControlPoints.AddRange(cp);

        // Indices (winding fix)
        for (int i = 0; i + 2 < part.Indices!.Length; i += 3)
            meshR.CreatePolygon(part.Indices[i], part.Indices[i + 2], part.Indices[i + 1]);

        // Normals
        var nrmElem = new A3DE.VertexElementNormal
        {
            MappingMode = A3DE.MappingMode.ControlPoint,
            ReferenceMode = A3DE.ReferenceMode.Direct
        };
        nrmElem.Data.AddRange(nrmLocal);
        meshR.VertexElements.Add(nrmElem);

        // UV0
        var uv0Elem = new A3DE.VertexElementUV
        {
            Name = "UV0",
            MappingMode = A3DE.MappingMode.ControlPoint,
            ReferenceMode = A3DE.ReferenceMode.Direct
        };
        uv0Elem.Data.AddRange(localUV0);
        meshR.VertexElements.Add(uv0Elem);

        // UV1 if present (with V flip)
        if (verts.Any(v => v.UV1.HasValue))
        {
            var uv1Elem = new A3DE.VertexElementUV
            {
                Name = "UV1",
                MappingMode = A3DE.MappingMode.ControlPoint,
                ReferenceMode = A3DE.ReferenceMode.Direct
            };
            foreach (var v in verts)
            {
                var uv = v.UV1 ?? default;
                uv1Elem.Data.Add(new AU.Vector4(uv.X * uvScale.X, (1.0f - uv.Y) * uvScale.Y, 0, 0));
            }
            meshR.VertexElements.Add(uv1Elem);
        }

        return meshR;
    }

    private static void StampPartMetadata(A3D.Node node, MmpMesh part)
    {
        node.SetProperty("mmp:meshType", part.MeshType); // 0=regular, 2=billboard
        node.SetProperty("mmp:isEmissiveAdditive", (int)part.AdditiveEmissive);
        node.SetProperty("mmp:isAlphaBlend", (int)part.AlphaBlend);
        node.SetProperty("mmp:isEnabled", (int)part.Enabled);
        node.SetProperty("mmp:uvSetCount", part.UVSetCount);
        node.SetProperty("mmp:materialIdx", part.MaterialIdx);
    }
    #endregion

    #region NAVI
    private static async Task AttachNaviAsync(MmpModel mmp, A3D.Node mmpNode, string outFilePath)
    {
        try
        {
            var mmpStem = Path.GetFileNameWithoutExtension(outFilePath);
            var naviPath = Path.Combine(mmp.BaseDirectory, mmpStem + ".navi");
            if (!File.Exists(naviPath)) return;

            var navi = await NaviReader.ReadAsync(naviPath);

            // Build quick lookup
            var naviXform = new Dictionary<uint, ModelNodeXform>();
            foreach (var nx in navi.Nodes)
                if (nx.NameHash != 0) naviXform[nx.NameHash] = nx;

            foreach (var e in navi.Entries)
            {
                if (!naviXform.TryGetValue(e.NameKey, out var nx))
                    throw new InvalidOperationException($"No matching Type-3 node found for object '{e.Name}' (hash: {e.NameKey}).");

                var naviNode = new A3D.Node("_NavigationMesh_");
                mmpNode.AddChildNode(naviNode);

                var mesh = new A3DE.Mesh { Name = e.Name };

                // Prepare world→local for geometry bake
                Num.Matrix4x4.Invert(nx.MWorld, out var invWorld);

                foreach (var v in e.Vertices)
                {
                    var pLocal = Num.Vector3.Transform(new Num.Vector3(v.X, v.Y, v.Z), invWorld);
                    mesh.ControlPoints.Add(new AU.Vector4(pLocal.X, pLocal.Y, pLocal.Z, 1));
                }

                for (int i = 0; i < e.Indices.Length; i++)
                    mesh.CreatePolygon(e.Indices[i].A, e.Indices[i].B, e.Indices[i].C);

                naviNode.CreateChildNode(e.Name ?? "NavMesh", mesh);
                // Metadata
                naviNode.SetProperty("navi:isNavMesh", 1);
                naviNode.SetProperty("navi:version", navi.Header.Version);
                naviNode.SetProperty("navi:name", e.Name);
                naviNode.SetProperty("navi:nodeKind", nx.Kind);
                naviNode.SetProperty("navi:nodeFlag", nx.Flag);

                naviNode.Transform.TransformMatrix = ToAspose(nx.MWorld);
            }
        }
        catch
        {
            // swallow intentionally: NAVI is optional and should not break export
        }
    }
    #endregion

    #region Save
    private static Task SaveSceneAsync(A3D.Scene scene, string outFilePath, bool embedTextures)
    {
        return Task.Run(() =>
        {
            var fbxOpts = new FbxSaveOptions(FileFormat.FBX7400Binary)
            {
                EnableCompression = false,
                EmbedTextures = embedTextures,
            };
            scene.Save(outFilePath, fbxOpts);
        });
    }
    #endregion

    #region Helpers
    /// <summary>
    /// Convert Num.Matrix4x4 to Aspose.ThreeD.Utilities.Matrix4
    /// </summary>
    private static AU.Matrix4 ToAspose(Num.Matrix4x4 m)
    {
        return new AU.Matrix4(
            m.M11, m.M12, m.M13, m.M14,
            m.M21, m.M22, m.M23, m.M24,
            m.M31, m.M32, m.M33, m.M34,
            m.M41, m.M42, m.M43, m.M44
        );
    }
    #endregion
}
