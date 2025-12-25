using HelixToolkit;
using HelixToolkit.Maths;
using HelixToolkit.SharpDX;
using HelixToolkit.Wpf.SharpDX;
using System.Numerics;
using static RHToolkit.Models.Model3D.Map.MMP;
using static RHToolkit.Models.Model3D.ModelMaterial;

namespace RHToolkit.Models.Model3D.Map;

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
                    new Vector3(-v.Position.X, v.Position.Y, v.Position.Z))); // flip X for LH coord
                var normals = new Vector3Collection(part.Vertices.Select(v =>
                    new Vector3(-v.Normal.X, v.Normal.Y, v.Normal.Z))); // flip X for LH coord

                // Base UVs
                var txtScale = GetTexScale(part.Material); // (u1,v1,u2,v2) -> use (u1,v1) for UV0 tiling
                var texcoords = new Vector2Collection(part.Vertices.Select(v =>
                    new Vector2(v.UV0.X * txtScale.X, v.UV0.Y * txtScale.Y)));

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

                    MaterialShader? P(string name) => Shader(mat, name);

                    // Identify water shader
                    bool isWater = mat.ShaderName.Equals("WATER.FX", StringComparison.OrdinalIgnoreCase);

                    // --- TEXTURES ---
                    if (!isWater)
                    {
                        // Only non-water uses DiffuseMap as albedo
                        var diffusePath = ResolveTextureAbsolute(model.BaseDirectory, Texture(mat, "DiffuseMap")?.TexturePath);
                        if (!string.IsNullOrEmpty(diffusePath) && File.Exists(diffusePath))
                        {
                            try { phong.DiffuseMap = new MemoryStream(File.ReadAllBytes(diffusePath)); } catch { }
                        }
                    }

                    // Normal map
                    var normalPath = ResolveTextureAbsolute(model.BaseDirectory, Texture(mat, "BumpMap")?.TexturePath);
                    if (!string.IsNullOrEmpty(normalPath) && File.Exists(normalPath))
                    {
                        try { phong.NormalMap = new MemoryStream(File.ReadAllBytes(normalPath)); } catch { }
                    }

                    // --- BASE COLORS ---
                    // Diffuse multiplier (Float4)
                    var d = P("Diffuse")?.Value ?? new Quaternion(1, 1, 1, 1);
                    phong.DiffuseColor = new Color4(d.X, d.Y, d.Z, d.W);

                    // Ambient (Float4)
                    var a = P("Ambient")?.Value;
                    if (a != null)
                    {
                        phong.AmbientColor = new Color4(a.Value.X * 0.6f, a.Value.Y * 0.6f, a.Value.Z * 0.6f, a.Value.W);
                    }
                    else
                    {
                        phong.AmbientColor = new Color4(0.25f, 0.25f, 0.25f, 1f);
                    }

                    // WaterColor tint (Float4)
                    var w = P("WaterColor")?.Value;
                    if (w != null)
                    {
                        var dc = phong.DiffuseColor;
                        phong.DiffuseColor = new Color4(
                            dc.Red * w.Value.X,
                            dc.Green * w.Value.Y,
                            dc.Blue * w.Value.Z,
                            dc.Alpha * (w.Value.W <= 0 ? 1f : w.Value.W));
                    }

                    // --- SPECULAR ---
                    float specIntensity = 0.15f;
                    float shininess = 32f;

                    var pSun = P("SunFactor");
                    if (pSun != null) specIntensity = MathUtil.Clamp(pSun.Value.X / 2f, 0f, 1f); // WATER.FX has 1.5 → ~0.75

                    var pSunPow = P("SunPower");
                    if (pSunPow != null && pSunPow.Value.X > 0)
                        shininess = MathUtil.Clamp(pSunPow.Value.X, 4f, 256f);

                    phong.SpecularColor = new Color4(specIntensity, specIntensity, specIntensity, 1f);
                    phong.SpecularShininess = shininess;

                    // --- TRANSPARENCY ---
                    bool isTransparent = false;
                    float alpha = phong.DiffuseColor.Alpha;

                    if (isWater)
                    {
                        alpha = 0.5f;
                        isTransparent = true;

                        // Water planes are commonly viewed from both sides
                        mesh.CullMode = SharpDX.Direct3D11.CullMode.None;
                    }
                    else
                    {
                        var pAlphaBlend = P("AlphaBlending");
                        if (pAlphaBlend != null && pAlphaBlend.Value.X >= 0.5f) isTransparent = true;

                        var pAlphaType = P("AlphaType");
                        int alphaType = (pAlphaType != null) ? (int)Math.Round(pAlphaType.Value.X) : 0;
                        if (alphaType >= 1) isTransparent = true;

                        var pAlphaVal = P("AlphaValue");
                        if (pAlphaVal != null && pAlphaVal.Value.X > 0)
                        {
                            alpha = MathUtil.Clamp(pAlphaVal.Value.X, 0.0f, 1.0f);
                            if (alpha < 0.999f) isTransparent = true;
                        }

                        var pTwoSide = P("Twoside");
                        if (pTwoSide != null && pTwoSide.Value.X >= 0.5f)
                            mesh.CullMode = SharpDX.Direct3D11.CullMode.None;
                    }

                    // Apply alpha
                    var dc0 = phong.DiffuseColor;
                    phong.DiffuseColor = new Color4(dc0.Red, dc0.Green, dc0.Blue, alpha);

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
}
