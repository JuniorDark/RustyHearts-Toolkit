using Aspose.ThreeD;
using Aspose.ThreeD.Formats;
using static RHToolkit.Models.Model3D.Map.MMP;
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
    /// <summary>
    /// Exports the given MMP model to an FBX file.
    /// </summary>
    /// <param name="mmp"></param>
    /// <param name="outFilePath"></param>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task ExportMmpToFbx(MmpModel mmp, string outFilePath)
    {
        ArgumentNullException.ThrowIfNull(mmp);
        if (string.IsNullOrWhiteSpace(outFilePath)) throw new ArgumentException("Output path is empty.", nameof(outFilePath));

        var outDir = Path.GetDirectoryName(outFilePath)!;
        Directory.CreateDirectory(outDir);

        var fileName = Path.GetFileNameWithoutExtension(outFilePath);

        var scene = new A3D.Scene();

        var root = scene.RootNode;

        // Root container node (metadata for traceability)
        var mmpNode = new A3D.Node(fileName);
        root.AddChildNode(mmpNode);
        mmpNode.SetProperty("mmp:version", mmp.Version);
        mmpNode.SetProperty("mmp:exporter", "RHToolkit");

        // --- Material cache (Phong) ---
        var matCache = new Dictionary<string, A3DS.Material>(StringComparer.OrdinalIgnoreCase);

        A3DS.Material GetOrCreateMaterial(MmpMaterial? m)
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
            var diffuseRel = ResolveTexture(outDir, Texture(m, "DiffuseMap")?.TexturePath, m?.MaterialName);
            var normalRel = ResolveTexture(outDir, Texture(m, "BumpMap")?.TexturePath, null);
            var specularRel = ResolveTexture(outDir, Texture(m, "SpecularMap")?.TexturePath, null);

            if (!string.IsNullOrWhiteSpace(diffuseRel))
                mat.SetTexture("DiffuseColor", new A3DS.Texture(diffuseRel));
            if (!string.IsNullOrWhiteSpace(normalRel))
                mat.SetTexture("NormalMap", new A3DS.Texture(normalRel));
            if (!string.IsNullOrWhiteSpace(specularRel))
                mat.SetTexture("SpecularColor", new A3DS.Texture(specularRel));

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
                // use scalar (0..1) to raise emissive
                var e = Math.Clamp(shStar.Scalar, 0f, 1f);
                mat.EmissiveColor = new AU.Vector3(e, e, e);
            }

            // Lightmap: if present, attach to AmbientColor channel
            var shLightmap = Shader(m, "LightmapTexturem");
            if (shLightmap != null)
            {
                var lmRel = ResolveTexture(outDir, Texture(m, "LightmapTextureMap")?.TexturePath, m?.MaterialName + "_lm");
                if (!string.IsNullOrWhiteSpace(lmRel))
                    mat.SetTexture("AmbientColor", new A3DS.Texture(lmRel));
            }

            // WaterColor → make it slightly emissive/diffuse tinted
            var shWater = Shader(m, "WaterColor");
            if (shWater != null)
            {
                var c = Clamp01(ToRGB(shWater.Base));
                // soft tint
                mat.DiffuseColor = new AU.Vector3(c.X, c.Y, c.Z);
                // gentle emissive to fake water glow/spec interplay
                mat.EmissiveColor = new AU.Vector3(c.X * 0.15f, c.Y * 0.15f, c.Z * 0.15f);
            }

            matCache[key] = mat;
            return mat;
        }

        // Map Type-3 transforms by hash (required for world→local conversion)
        var xformByHash = new Dictionary<uint, MmpNodeXform>();
        foreach (var nx in mmp.Nodes)
            if (nx.NameHash != 0) xformByHash[nx.NameHash] = nx;

        foreach (var obj in mmp.Objects)
        {
            var objName = string.IsNullOrWhiteSpace(obj.NodeName) ? "Object" : obj.NodeName;

            if (!xformByHash.TryGetValue(obj.NodeNameHash, out var nx))
                throw new InvalidOperationException($"No matching Type-3 node found for object '{obj.NodeName}' (hash: {obj.NodeNameHash}).");

            // FBX node: carries WORLD transform. Geometry we emit is localized (multiplied by invWorld).
            var objNode = new A3D.Node(objName);
            mmpNode.AddChildNode(objNode);
            objNode.Transform.TransformMatrix = ToAspose(nx.MWorld);

            // Metadata
            objNode.SetProperty("mmp:nodeGroupName", obj.AltNodeName ?? string.Empty);
            objNode.SetProperty("mmp:nodeKind", nx.Kind);
            objNode.SetProperty("mmp:nodeFlag", nx.Flag);

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
                var uvScale = GetTexScale(part.Material);

                // Prepare world→local for geometry bake
                Num.Matrix4x4.Invert(nx.MWorld, out var invWorld);
                var invWorldT = Num.Matrix4x4.Transpose(invWorld);

                // --- MeshType 2 = billboard quad ---
                if (part.MeshType == 2)
                {
                    var mesh = new A3DE.Mesh { Name = meshName };

                    var p0 = part.Vertices![0].Position;
                    var p0Local4 = Num.Vector4.Transform(new Num.Vector4(p0.X, p0.Y, p0.Z, 1), invWorld);
                    var p0Local = new AU.Vector4(p0Local4.X, p0Local4.Y, p0Local4.Z, 1);

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

                    var node = objNode.CreateChildNode(meshName, mesh);
                    node.Material = GetOrCreateMaterial(part.Material);

                    // metadata
                    node.SetProperty("mmp:meshType", part.MeshType); // 2
                    node.SetProperty("mmp:isEmissiveAdditive", (int)part.AdditiveEmissive);
                    node.SetProperty("mmp:isAlphaBlend", (int)part.AlphaBlend);
                    node.SetProperty("mmp:isEnabled", (int)part.Enabled);
                    node.SetProperty("mmp:uvSetCount", part.UVSetCount);
                    node.SetProperty("mmp:materialIdx", part.MaterialIdx);

                    continue;
                }

                // --- Regular mesh path ---
                var verts = part.Vertices;

                // Locals for tangent build
                var localPos = new Num.Vector3[verts.Length];
                var localNrm = new Num.Vector3[verts.Length];
                var localUV0 = new Num.Vector2[verts.Length];

                // Aspose payload
                var cp = new List<AU.Vector4>(verts.Length);
                var nrmLocal = new List<AU.Vector4>(verts.Length);
                for (int i = 0; i < verts.Length; i++)
                {
                    var pL = Num.Vector3.Transform(verts[i].Position, invWorld);
                    localPos[i] = pL;
                    cp.Add(new AU.Vector4(pL.X, pL.Y, pL.Z, 1));

                    var n4 = Num.Vector4.Transform(new Num.Vector4(verts[i].Normal, 0), invWorldT);
                    var n3 = new Num.Vector3(n4.X, n4.Y, n4.Z);
                    if (n3.LengthSquared() > 1e-20f) n3 = Num.Vector3.Normalize(n3);
                    localNrm[i] = n3;
                    nrmLocal.Add(new AU.Vector4(n3.X, n3.Y, n3.Z, 0));

                    // UV0 with V flip
                    var t0 = verts[i].UV0;
                    localUV0[i] = new Num.Vector2(t0.X * uvScale.X, (1.0f - t0.Y) * uvScale.Y);
                }

                var meshR = new A3DE.Mesh
                {
                    Name = meshName
                };
                meshR.ControlPoints.AddRange(cp);

                // Indices
                for (int i = 0; i + 2 < part.Indices.Length; i += 3)
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
                foreach (var uv in localUV0)
                    uv0Elem.Data.Add(new AU.Vector4(uv.X, uv.Y, 0, 0));
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

                var partNode = objNode.CreateChildNode(meshName, meshR);
                partNode.Material = GetOrCreateMaterial(part.Material);

                partNode.SetProperty("mmp:meshType", part.MeshType); // 0=regular, 2=billboard
                partNode.SetProperty("mmp:isEmissiveAdditive", (int)part.AdditiveEmissive);
                partNode.SetProperty("mmp:isAlphaBlend", (int)part.AlphaBlend);
                partNode.SetProperty("mmp:isEnabled", (int)part.Enabled);
                partNode.SetProperty("mmp:uvSetCount", part.UVSetCount);
                partNode.SetProperty("mmp:materialIdx", part.MaterialIdx);
            }
        }

        // --- Attach NAVI mesh into FBX ---
        try
        {
            var mmpDir = Path.GetDirectoryName(outFilePath)!;
            var mmpStem = Path.GetFileNameWithoutExtension(outFilePath);
            var naviPath = Path.Combine(mmpDir, mmpStem + ".navi");
            if (File.Exists(naviPath))
            {
                var navi = await NaviReader.ReadAsync(naviPath);
                // Map class-3: NodeHash -> MWorld
                var worldByHash = navi.Nodes.GroupBy(n => n.NameKey)
                                            .ToDictionary(g => g.Key, g => g.First().MWorld);

                var naviXform = new Dictionary<uint, NaviNodeXform>();
                foreach (var nx in navi.Nodes)
                    if (nx.NameKey != 0) naviXform[nx.NameKey] = nx;

                foreach (var e in navi.Entries)
                {
                    if(!naviXform.TryGetValue(e.NameKey, out var nx))
                    throw new InvalidOperationException($"No matching Type-3 node found for object '{e.Name}' (hash: {e.NameKey}).");

                    var naviNode = new A3D.Node("_NavigationMesh_");

                    mmpNode.AddChildNode(naviNode);

                    var mesh = new A3DE.Mesh
                    {
                        Name = e.Name
                    };

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
        }
        catch { /* non-fatal if NAVI is missing */ }


        // --- Save FBX ---
        var fbxOpts = new FbxSaveOptions(FileFormat.FBX7600Binary);
        scene.Save(outFilePath, fbxOpts);
    }

    #region Helpers
    /// <summary>
    /// Convert Num.Matrix4x4 to Aspose.ThreeD.Utilities.Matrix4
    /// </summary>
    /// <param name="m"></param>
    /// <returns></returns>
    private static AU.Matrix4 ToAspose(Num.Matrix4x4 m)
    {
        return new AU.Matrix4(
            m.M11, m.M12, m.M13, m.M14,
            m.M21, m.M22, m.M23, m.M24,
            m.M31, m.M32, m.M33, m.M34,
            m.M41, m.M42, m.M43, m.M44
        );
    }

    #region Material / Shader Helpers
    private static MmpTexture? Texture(MmpMaterial? m, string slotExact)
        => m?.Textures.FirstOrDefault(t =>
            t.Slot.Equals(slotExact, StringComparison.OrdinalIgnoreCase));

    static MmpShader? Shader(MmpMaterial? m, string slotPrefix)
        => m?.Shaders.FirstOrDefault(s => s.Slot.StartsWith(slotPrefix, StringComparison.OrdinalIgnoreCase));

    static Num.Vector3 ToRGB(Num.Quaternion q) => new(q.X, q.Y, q.Z);
    static Num.Vector3 Clamp01(Num.Vector3 v)
        => new(Math.Clamp(v.X, 0, 1), Math.Clamp(v.Y, 0, 1), Math.Clamp(v.Z, 0, 1));

    // Returns UV scale from TexScale shader (X,Y in Base.XY). Defaults to (1,1).
    static Num.Vector2 GetTexScale(MmpMaterial? m)
    {
        var s = Shader(m, "TexScale");
        if (s == null) return new Num.Vector2(1, 1);
        return new Num.Vector2(
            s.Base.X == 0 ? 1f : s.Base.X,
            s.Base.Y == 0 ? 1f : s.Base.Y
        );
    }

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

            // If it starts with ".\" or "./" → drop the leading ".\" and keep relative
            if (norm.StartsWith("." + Path.DirectorySeparatorChar))
            {
                var rel = norm.Substring(2); // remove ".\"
                var abs = Path.GetFullPath(Path.Combine(outDir, rel));
                return File.Exists(abs) ? rel.Replace(Path.DirectorySeparatorChar, '/') : null;
            }

            // If it starts with one or more "..\" → keep it as-is (relative upwards from outDir)
            if (norm.StartsWith(".." + Path.DirectorySeparatorChar))
            {
                var abs = Path.GetFullPath(Path.Combine(outDir, norm));
                return File.Exists(abs) ? norm.Replace(Path.DirectorySeparatorChar, '/') : null;
            }

            // If it's an absolute path, convert to relative if it’s inside/outside outDir
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
                // Plain relative like "texture\foo.dds" → test relative to outDir
                var abs = Path.GetFullPath(Path.Combine(outDir, norm));
                if (File.Exists(abs))
                    return norm.Replace(Path.DirectorySeparatorChar, '/');
            }

            // Special convenience: many assets store ".\texture\..." but you might only have "texture\..."
            // Try dropping any leading ".\" if present (already handled) or just force into texture/
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

            // 1) texture/<name>.dds
            var abs1 = Path.GetFullPath(Path.Combine(outDir, "texture", file));
            if (File.Exists(abs1))
                return ("texture/" + file).Replace('\\', '/');

            // 2) <name>.dds (same folder as FBX)
            var abs2 = Path.GetFullPath(Path.Combine(outDir, file));
            if (File.Exists(abs2))
                return file.Replace('\\', '/');
        }

        return null;
    }
    #endregion

    #endregion
}
