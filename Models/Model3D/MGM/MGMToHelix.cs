using HelixToolkit.Wpf.SharpDX;
using static RHToolkit.Models.Model3D.ModelMaterial;
using SDX = SharpDX;

namespace RHToolkit.Models.Model3D.MGM;

public class MGMToHelix
{
    public static IEnumerable<Element3D> CreateMGMNodes(MgmModel model)
    {
        foreach (var mesh in model.Meshes)
        {
            var group = new GroupModel3D { Tag = mesh.Name };

            // geometry
            var positions = new Vector3Collection(mesh.Vertices.Select(v =>
                new SDX.Vector3(-v.Position.X, v.Position.Y, v.Position.Z))); // flip X for LH coord
            var normals = new Vector3Collection(mesh.Vertices.Select(v =>
                new SDX.Vector3(-v.Normal.X, v.Normal.Y, v.Normal.Z))); // flip X for LH coord

            // Base UVs
            var txtScale = GetTexScale(mesh.Material); // (u1,v1,u2,v2) -> use (u1,v1) for UV0 tiling
            var texcoords = new Vector2Collection(mesh.Vertices.Select(v =>
                new SDX.Vector2(v.UV0.X * txtScale.X, v.UV0.Y * txtScale.Y)));

            // Build indices (original order first)
            var indices = new IntCollection(mesh.Indices.Select(i => (int)i));

            // ---- Ensure winding matches normals (prevents backface holes) ----
            static bool NeedsReverse(Vector3Collection pos, Vector3Collection nrm, IntCollection idx)
            {
                int neg = 0, total = 0;
                for (int i = 0; i + 2 < idx.Count; i += 3)
                {
                    var i0 = idx[i]; var i1 = idx[i + 1]; var i2 = idx[i + 2];
                    var p0 = pos[i0]; var p1 = pos[i1]; var p2 = pos[i2];

                    // face normal from geometry
                    var e1 = p1 - p0;
                    var e2 = p2 - p0;
                    var fn = SDX.Vector3.Cross(e1, e2);

                    // average of vertex normals on the tri
                    var an = nrm[i0] + nrm[i1] + nrm[i2];

                    // if the dot is negative, winding is opposite of normals
                    if (SDX.Vector3.Dot(fn, an) < 0) neg++;
                    total++;
                }
                // If most triangles disagree with their normals, reverse
                return neg > total * 0.6f;
            }

            if (NeedsReverse(positions, normals, indices))
            {
                for (int i = 0; i + 2 < indices.Count; i += 3)
                {
                    (indices[i + 2], indices[i + 1]) = (indices[i + 1], indices[i + 2]);
                }
            }

            // --- Tangents ---
            bool hasTangents = mesh.Vertices.All(v => v.Tangent.HasValue);

            Vector3Collection tangents, bitangents;

            if (hasTangents)
            {
                tangents = new Vector3Collection(mesh.Vertices.Select(v =>
                {
                    var t = v.Tangent.GetValueOrDefault(); // System.Numerics.Vector4
                    return new SDX.Vector3(-t.X, t.Y, t.Z); // SharpDX.Vector3
                }));

                bitangents = new Vector3Collection(mesh.Vertices.Select(v =>
                {
                    var n = new SDX.Vector3(-v.Normal.X, v.Normal.Y, v.Normal.Z);
                    var t = v.Tangent.GetValueOrDefault();
                    float handed = t.W >= 0f ? 1f : -1f;
                    handed = -handed; // flip because we mirrored X
                    var t3 = new SDX.Vector3(-t.X, t.Y, t.Z);
                    var n3 = new SDX.Vector3(-v.Normal.X, v.Normal.Y, v.Normal.Z);
                    var b3 = SDX.Vector3.Cross(n3, t3) * handed;
                    float w = t.W >= 0f ? 1f : -1f;         // handedness from .W
                    return SDX.Vector3.Cross(n, t3) * w;    // bitangent = (N x T) * w
                }));
            }
            else
            {
                // Fallback generator when tangents aren't present in the file
                (tangents, bitangents) = GenerateTangents(positions, normals, texcoords, indices);
            }

            var meshGeom = new MeshGeometry3D
            {
                Positions = positions,
                Normals = normals,
                TextureCoordinates = texcoords,
                Indices = indices,
                Tangents = tangents,
                BiTangents = bitangents
            };

            var meshModel = new MeshGeometryModel3D
            {
                Geometry = meshGeom
            };

            // material
            if (mesh.Material != null)
            {
                var mat = mesh.Material;
                var phong = new PhongMaterial();

                // --- TEXTURES ---
                // Diffuse
                var diffusePath = ResolveTextureAbsolute(model.BaseDirectory,
                    Texture(mat, "DiffuseMap")?.TexturePath);
                if (!string.IsNullOrEmpty(diffusePath) && File.Exists(diffusePath))
                {
                    try { phong.DiffuseMap = new MemoryStream(File.ReadAllBytes(diffusePath)); } catch { }
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

                // --- SPECULAR / SHININESS ---
                var pSpec = Shader(mat, "Specular");
                if (pSpec != null)
                {
                    var s = pSpec.Payload;
                    phong.SpecularColor = new SDX.Color4(Clamp01(s.X), Clamp01(s.Y), Clamp01(s.Z), 1f);
                }
                else
                {
                    phong.SpecularColor = new SDX.Color4(0f, 0f, 0f, 1f);
                }

                // --- TRANSPARENCY ---
                bool isTransparent = false;
                float alpha = phong.DiffuseColor.Alpha;

                // Mesh flag
                if (mesh.Flags != null && mesh.Flags.Length > 1 && mesh.Flags[1] == 1)
                    isTransparent = true;

                // Explicit blend on
                var pAlphaBlend = Shader(mat, "AlphaBlending");
                if (pAlphaBlend != null && pAlphaBlend.Payload.X >= 0.5f)
                    isTransparent = true;

                // Constant alpha
                var pAlphaVal = Shader(mat, "AlphaValue");
                if (pAlphaVal != null)
                {
                    alpha = SharpDX.MathUtil.Clamp(pAlphaVal.Payload.X, 0f, 1f);
                    if (alpha < 0.999f) isTransparent = true;
                }

                bool hasAlphaControl = pAlphaVal != null;
                if (!isTransparent && hasAlphaControl && (pAlphaBlend == null || pAlphaBlend.Payload.X < 0.5f))
                {
                    isTransparent = true; // treat as cutout; avoids depth holes
                }

                // Apply alpha
                var dc = phong.DiffuseColor;
                phong.DiffuseColor = new SharpDX.Color4(dc.Red, dc.Green, dc.Blue, alpha);
                meshModel.IsTransparent = isTransparent;

                // Culling only from Twoside
                var pTwoSide = Shader(mat, "Twoside");
                if (pTwoSide != null && pTwoSide.Payload.X >= 0.5f)
                    meshModel.CullMode = SharpDX.Direct3D11.CullMode.None;

                // Attach material + transparency
                meshModel.Material = phong;
                meshModel.IsTransparent = isTransparent;
            }
            else
            {
                meshModel.Material = PhongMaterials.Gray;
            }

            group.Children.Add(meshModel);

            yield return group;
        }
    }

    private static (Vector3Collection tangents, Vector3Collection bitangents)
    GenerateTangents(Vector3Collection positions, Vector3Collection normals,
                 Vector2Collection uvs, IntCollection indices)
    {
        int vcount = positions.Count;
        var tan1 = new SDX.Vector3[vcount];
        var tan2 = new SDX.Vector3[vcount];

        for (int i = 0; i < indices.Count; i += 3)
        {
            int i0 = indices[i + 0], i1 = indices[i + 1], i2 = indices[i + 2];

            var p0 = positions[i0]; var p1 = positions[i1]; var p2 = positions[i2];
            var w0 = uvs[i0]; var w1 = uvs[i1]; var w2 = uvs[i2];

            float x1 = p1.X - p0.X, x2 = p2.X - p0.X;
            float y1 = p1.Y - p0.Y, y2 = p2.Y - p0.Y;
            float z1 = p1.Z - p0.Z, z2 = p2.Z - p0.Z;

            float s1 = w1.X - w0.X, s2 = w2.X - w0.X;
            float t1 = w1.Y - w0.Y, t2 = w2.Y - w0.Y;

            float r = (s1 * t2 - s2 * t1);
            if (System.Math.Abs(r) < 1e-8f) r = 1e-8f;
            r = 1.0f / r;

            var sdir = new SDX.Vector3(
                (t2 * x1 - t1 * x2) * r,
                (t2 * y1 - t1 * y2) * r,
                (t2 * z1 - t1 * z2) * r);

            var tdir = new SDX.Vector3(
                (s1 * x2 - s2 * x1) * r,
                (s1 * y2 - s2 * y1) * r,
                (s1 * z2 - s2 * z1) * r);

            tan1[i0] += sdir; tan1[i1] += sdir; tan1[i2] += sdir;
            tan2[i0] += tdir; tan2[i1] += tdir; tan2[i2] += tdir;
        }

        var tangents = new Vector3Collection(vcount);
        var bitangents = new Vector3Collection(vcount);

        for (int v = 0; v < vcount; v++)
        {
            var n = normals[v];
            var t = tan1[v];

            // Gram–Schmidt orthonormalize
            var nDotT = SDX.Vector3.Dot(n, t);
            var tang = SDX.Vector3.Normalize(t - nDotT * n);

            // Calculate handedness (w) and bitangent
            var bitan = SDX.Vector3.Cross(n, tang);
            float w = (SDX.Vector3.Dot(bitan, tan2[v]) < 0.0f) ? -1.0f : 1.0f;
            bitan *= w;

            tangents.Add(tang);
            bitangents.Add(bitan);
        }

        return (tangents, bitangents);
    }

    
}
