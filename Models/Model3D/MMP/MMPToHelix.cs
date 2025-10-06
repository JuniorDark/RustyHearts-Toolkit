using HelixToolkit.Wpf.SharpDX;
using static RHToolkit.Models.Model3D.MMP.MMP;
using SDX = SharpDX;

namespace RHToolkit.Models.Model3D.MMP;

public static class MMPToHelix
{
    public static IEnumerable<Element3D> CreateMMPNodes(MmpModel model)
    {
        foreach (var obj in model.Objects)
        {
            var group = new GroupModel3D { Tag = obj.NodeName };

            foreach (var part in obj.Meshes)
            {
                // geometry
                var positions = new Vector3Collection(part.Vertices.Select(v =>
                    new SDX.Vector3(-v.Position.X, v.Position.Y, v.Position.Z))); // flip X for LH coord
                var normals = new Vector3Collection(part.Vertices.Select(v =>
                    new SDX.Vector3(-v.Normal.X, v.Normal.Y, v.Normal.Z))); // flip X for LH coord

                // Base UVs
                var (x, y) = GetTexScale(part.Material); // (u1,v1,u2,v2) -> use (u1,v1) for UV0 tiling
                var texcoords = new Vector2Collection(part.Vertices.Select(v =>
                    new SDX.Vector2(v.UV0.X * x, v.UV0.Y * y)));

                var indices = new IntCollection(part.Indices.Select(i => (int)i));

                var meshGeom = new MeshGeometry3D
                {
                    Positions = positions,
                    Normals = normals,
                    TextureCoordinates = texcoords,
                    Indices = indices,
                };

                var mesh = new MeshGeometryModel3D
                {
                    Geometry = meshGeom
                };

                // material
                if (part.Material != null)
                {
                    var mat = part.Material;
                    var phong = new PhongMaterial();

                    // --- TEXTURES ---
                    // Diffuse
                    var diffusePath = ResolveTexturePath(model.BaseDirectory,
                        Texture(mat, "DiffuseMap")?.TexturePath,
                        mat.MaterialName);
                    if (!string.IsNullOrEmpty(diffusePath) && File.Exists(diffusePath))
                    {
                        try { phong.DiffuseMap = new MemoryStream(File.ReadAllBytes(diffusePath)); } catch { }
                    }

                    // Normal (Bump)
                    var normalPath = ResolveTexturePath(model.BaseDirectory,
                        Texture(mat, "BumpMap")?.TexturePath, null);
                    if (!string.IsNullOrEmpty(normalPath) && File.Exists(normalPath))
                    {
                        try { phong.NormalMap = new MemoryStream(File.ReadAllBytes(normalPath)); } catch { }
                    }

                    // Diffuse color multiplier
                    var pDiffuse = Shader(mat, "Diffuse");
                    if (pDiffuse != null)
                    {
                        var c = pDiffuse.Payload;
                        phong.DiffuseColor = new SDX.Color4(
                            Clamp01(c.X <= 0 ? 1f : c.X),
                            Clamp01(c.Y <= 0 ? 1f : c.Y),
                            Clamp01(c.Z <= 0 ? 1f : c.Z),
                            Clamp01(c.W <= 0 ? 1f : c.W));
                    }
                    else
                    {
                        phong.DiffuseColor = new SDX.Color4(1, 1, 1, 1);
                    }

                    // Ambient
                    var pAmbient = Shader(mat, "Ambient");
                    if (pAmbient != null)
                    {
                        var a = pAmbient.Payload;
                        phong.AmbientColor = new SDX.Color4(
                            Clamp01(a.X) * 0.6f,
                            Clamp01(a.Y) * 0.6f,
                            Clamp01(a.Z) * 0.6f,
                            1f);
                    }
                    else
                    {
                        phong.AmbientColor = new SDX.Color4(0.25f, 0.25f, 0.25f, 1f);
                    }

                    // Water color (when present) – acts as a tint over diffuse
                    var pWater = Shader(mat, "WaterColor");
                    if (pWater != null)
                    {
                        var w = pWater.Payload;
                        var dc = phong.DiffuseColor;
                        phong.DiffuseColor = new SDX.Color4(
                            Clamp01(dc.Red * w.X),
                            Clamp01(dc.Green * w.Y),
                            Clamp01(dc.Blue * w.Z),
                            Clamp01(dc.Alpha * (w.W <= 0 ? 1f : w.W)));
                    }

                    // SunFactor / SunPower 
                    float specIntensity = 0.15f;
                    float shininess = 32f;
                    var pSun = Shader(mat, "SunFactor");
                    if (pSun != null) specIntensity = Clamp01(pSun.Payload.Y); // use Y as spec weight
                    var pSunPow = Shader(mat, "SunPower");
                    if (pSunPow != null && pSunPow.Payload.X > 0) shininess = SDX.MathUtil.Clamp(pSunPow.Payload.X, 4f, 128f);

                    phong.SpecularColor = new SDX.Color4(specIntensity, specIntensity, specIntensity, 1f);
                    phong.SpecularShininess = shininess;

                    // --- TRANSPARENCY ---
                    bool isTransparent = false;
                    float alpha = phong.DiffuseColor.Alpha;

                    var pAlphaBlend = Shader(mat, "AlphaBlending");
                    if (pAlphaBlend != null && pAlphaBlend.Payload.X >= 0.5f) isTransparent = true;

                    var pAlphaType = Shader(mat, "AlphaType"); // 0=opaque, 1=alpha, 2=add? (engine-specific)
                    int alphaType = (pAlphaType != null) ? (int)System.Math.Round(pAlphaType.Payload.X) : 0;
                    if (alphaType >= 1) isTransparent = true; // treat >=1 as blended

                    var pAlphaVal = Shader(mat, "AlphaValue");
                    if (pAlphaVal != null && pAlphaVal.Payload.X > 0)
                    {
                        alpha = SDX.MathUtil.Clamp(pAlphaVal.Payload.X, 0.0f, 1.0f);
                        if (alpha < 0.999f) isTransparent = true;
                    }

                    // Apply final alpha to DiffuseColor
                    var dc0 = phong.DiffuseColor;
                    phong.DiffuseColor = new SDX.Color4(dc0.Red, dc0.Green, dc0.Blue, alpha);

                    // --- DOUBLESIDED / CULLING ---
                    var pTwoSide = Shader(mat, "Twoside");
                    if (pTwoSide != null && pTwoSide.Payload.X >= 0.5f)
                        mesh.CullMode = SharpDX.Direct3D11.CullMode.None;

                    // Attach material + transparency
                    mesh.Material = phong;
                    mesh.IsTransparent = isTransparent;
                }
                else
                {
                    mesh.Material = PhongMaterials.Gray;
                }

                group.Children.Add(mesh);
            }

            yield return group;
        }
    }

    // ---------- helpers ----------
    private static MmpShader? Shader(MmpMaterial m, string slotPrefix)
        => m.Shaders.FirstOrDefault(s => s.Slot.StartsWith(slotPrefix, StringComparison.OrdinalIgnoreCase));

    private static MmpTexture? Texture(MmpMaterial m, string slotExact)
        => m.Textures.FirstOrDefault(t => t.Slot.Equals(slotExact, StringComparison.OrdinalIgnoreCase));

    // TexScale payload → (u1, v1, u2, v2). We use u1,v1 for UV0 tiling.
    private static (float x, float y) GetTexScale(MmpMaterial? m)
    {
        if (m == null) return (1f, 1f);
        var ts = Shader(m, "TexScale");
        if (ts == null) return (1f, 1f);
        var p = ts.Payload;
        float ux = (p.X == 0) ? 1f : p.X;
        float vy = (p.Y == 0) ? 1f : p.Y;
        ux = SDX.MathUtil.Clamp(ux, 0.01f, 20f);
        vy = SDX.MathUtil.Clamp(vy, 0.01f, 20f);
        return (ux, vy);
    }

    private static float Clamp01(float v) => v < 0 ? 0 : (v > 1 ? 1 : v);

    /// <summary>
    /// Resolve relative texture paths against the MMP folder.
    /// ".\\texture\\foo.dds"  => BaseDir\\texture\\foo.dds
    /// "..\\..\\WaterTexture\\wave1.dds" => BaseDir\\..\\..\\WaterTexture\\wave1.dds  (normalized)
    /// If refPath is null, we try BaseDir\\<materialName>.dds and BaseDir\\texture\\<materialName>.dds
    /// </summary>
    private static string? ResolveTexturePath(string baseDir, string? refPath, string? materialNameFallback)
    {
        if (!string.IsNullOrWhiteSpace(refPath))
        {
            var norm = refPath.Replace('/', Path.DirectorySeparatorChar)
                              .Replace('\\', Path.DirectorySeparatorChar);
            var combined = Path.GetFullPath(Path.Combine(baseDir, norm));
            if (File.Exists(combined)) return combined;

            // also try BaseDir/texture/<file>
            var fileOnly = Path.GetFileName(norm);
            if (!string.IsNullOrEmpty(fileOnly))
            {
                var alt = Path.GetFullPath(Path.Combine(baseDir, "texture", fileOnly));
                if (File.Exists(alt)) return alt;
            }
        }

        if (!string.IsNullOrWhiteSpace(materialNameFallback))
        {
            var file = materialNameFallback.EndsWith(".dds", System.StringComparison.OrdinalIgnoreCase)
                ? materialNameFallback
                : materialNameFallback + ".dds";

            var p1 = Path.GetFullPath(Path.Combine(baseDir, file));
            if (File.Exists(p1)) return p1;

            var p2 = Path.GetFullPath(Path.Combine(baseDir, "texture", file));
            if (File.Exists(p2)) return p2;
        }

        return null;
    }
}
