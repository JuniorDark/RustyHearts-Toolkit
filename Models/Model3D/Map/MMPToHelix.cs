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

                    // --- TEXTURES ---
                    // Diffuse
                    var diffusePath = ResolveTextureAbsolute(model.BaseDirectory,
                    Texture(mat, "DiffuseMap")?.TexturePath);
                    if (!string.IsNullOrEmpty(diffusePath) && File.Exists(diffusePath))
                    {
                        try { phong.DiffuseMap = new MemoryStream(File.ReadAllBytes(diffusePath)); } catch { }
                    }

                    // Normal (Bump)
                    var normalPath = ResolveTextureAbsolute(model.BaseDirectory,
                        Texture(mat, "BumpMap")?.TexturePath);
                    if (!string.IsNullOrEmpty(normalPath) && File.Exists(normalPath))
                    {
                        try { phong.NormalMap = new MemoryStream(File.ReadAllBytes(normalPath)); } catch { }
                    }

                    // Diffuse color multiplier
                    var pDiffuse = Shader(mat, "Diffuse");
                    if (pDiffuse != null)
                    {
                        var c = pDiffuse.Payload;
                        phong.DiffuseColor = new Color4(
                            Clamp01(c.X <= 0 ? 1f : c.X),
                            Clamp01(c.Y <= 0 ? 1f : c.Y),
                            Clamp01(c.Z <= 0 ? 1f : c.Z),
                            Clamp01(c.W <= 0 ? 1f : c.W));
                    }
                    else
                    {
                        phong.DiffuseColor = new Color4(1, 1, 1, 1);
                    }

                    // Ambient
                    var pAmbient = Shader(mat, "Ambient");
                    if (pAmbient != null)
                    {
                        var a = pAmbient.Payload;
                        phong.AmbientColor = new Color4(
                            Clamp01(a.X) * 0.6f,
                            Clamp01(a.Y) * 0.6f,
                            Clamp01(a.Z) * 0.6f,
                            1f);
                    }
                    else
                    {
                        phong.AmbientColor = new Color4(0.25f, 0.25f, 0.25f, 1f);
                    }

                    // Water color (when present) – acts as a tint over diffuse
                    var pWater = Shader(mat, "WaterColor");
                    if (pWater != null)
                    {
                        var w = pWater.Payload;
                        var dc = phong.DiffuseColor;
                        phong.DiffuseColor = new Color4(
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
                    if (pSunPow != null && pSunPow.Payload.X > 0) shininess = MathUtil.Clamp(pSunPow.Payload.X, 4f, 128f);

                    phong.SpecularColor = new Color4(specIntensity, specIntensity, specIntensity, 1f);
                    phong.SpecularShininess = shininess;

                    // --- TRANSPARENCY ---
                    bool isTransparent = false;
                    float alpha = phong.DiffuseColor.Alpha;

                    var pAlphaBlend = Shader(mat, "AlphaBlending");
                    if (pAlphaBlend != null && pAlphaBlend.Payload.X >= 0.5f) isTransparent = true;

                    var pAlphaType = Shader(mat, "AlphaType");
                    int alphaType = (pAlphaType != null) ? (int)System.Math.Round(pAlphaType.Payload.X) : 0;
                    if (alphaType >= 1) isTransparent = true; // treat >=1 as blended

                    var pAlphaVal = Shader(mat, "AlphaValue");
                    if (pAlphaVal != null && pAlphaVal.Payload.X > 0)
                    {
                        alpha = MathUtil.Clamp(pAlphaVal.Payload.X, 0.0f, 1.0f);
                        if (alpha < 0.999f) isTransparent = true;
                    }

                    // Apply final alpha to DiffuseColor
                    var dc0 = phong.DiffuseColor;
                    phong.DiffuseColor = new Color4(dc0.Red, dc0.Green, dc0.Blue, alpha);

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
}
