using HelixToolkit;
using HelixToolkit.Geometry;
using HelixToolkit.Maths;
using HelixToolkit.SharpDX;
using HelixToolkit.Wpf.SharpDX;
using System.Numerics;
using static RHToolkit.Models.Model3D.ModelMaterial;

namespace RHToolkit.Models.Model3D.MGM
{
    public class MGMToHelix
    {
        /// <summary>
        /// Build geometry nodes for all MGM meshes.
        /// </summary>
        public static IEnumerable<Element3D> CreateMGMNodes(MgmModel model)
        {
            foreach (var mesh in model.Meshes)
            {
                var elem = BuildMeshGroup(model, mesh);
                if (elem != null) yield return elem;
            }
        }

        // ===========================
        // Mesh builder
        // ===========================

        private static Element3D? BuildMeshGroup(MgmModel model, MgmMesh mesh)
        {
            if (mesh.Vertices == null || mesh.Vertices.Count() == 0 ||
                mesh.Indices == null || mesh.Indices.Count() == 0)
            {
                return null;
            }

            var group = new GroupModel3D { Tag = mesh.Name };

            // --- Positions/Normals with X-mirror + sanitation ---
            var positions = new Vector3Collection(mesh.Vertices.Select(v =>
                SanitizeVec(new Vector3(-v.Position.X, v.Position.Y, v.Position.Z))));

            var normals = new Vector3Collection(mesh.Vertices.Select(v =>
                SafeNormal(new Vector3(-v.Normal.X, v.Normal.Y, v.Normal.Z))));

            // --- Base UVs (with material tiling) ---
            var txtScale = GetTexScale(mesh.Material); // (u1,v1,u2,v2) -> use (u1,v1) for UV0 tiling
            var texcoords = new Vector2Collection(mesh.Vertices.Select(v =>
                SanitizeVec2(new Vector2(v.UV0.X * txtScale.X, v.UV0.Y * txtScale.Y))));

            // --- Indices ---
            var indices = new IntCollection(mesh.Indices.Select(i => unchecked((int)i)));

            // ---- Ensure winding matches normals (prevents backface holes) ----
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
                    var t = v.Tangent!.Value;
                    return SanitizeVec(new Vector3(-t.X, t.Y, t.Z));
                }));

                bitangents = new Vector3Collection(mesh.Vertices.Select(v =>
                {
                    var t = v.Tangent!.Value;
                    float w = t.W >= 0f ? 1f : -1f;
                    var t3 = new Vector3(-t.X, t.Y, t.Z);
                    var n3 = SafeNormal(new Vector3(-v.Normal.X, v.Normal.Y, v.Normal.Z));
                    var b3 = Vector3.Cross(n3, t3) * w;
                    return SanitizeVec(b3);
                }));
            }
            else
            {
                (tangents, bitangents) = GenerateTangents(positions, normals, texcoords, indices);
            }

            // --- Build mesh ---
            var meshGeom = new HelixToolkit.SharpDX.MeshGeometry3D
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
                Geometry = meshGeom,
                // --- Material ---
                Material = BuildPhongMaterial(model, mesh, out bool isTransparent),
                IsTransparent = isTransparent
            };

            // Two-sided?
            var pTwoSide = Shader(mesh.Material, "Twoside");
            if (pTwoSide != null && pTwoSide.Payload.X >= 0.5f)
                meshModel.CullMode = SharpDX.Direct3D11.CullMode.None;

            group.Children.Add(meshModel);
            return group;
        }

        private static PhongMaterial BuildPhongMaterial(MgmModel model, MgmMesh mesh, out bool isTransparent)
        {
            var phong = new PhongMaterial();

            // Diffuse map
            var diffusePath = ResolveTextureAbsolute(model.BaseDirectory, Texture(mesh.Material, "DiffuseMap")?.TexturePath);
            if (!string.IsNullOrEmpty(diffusePath) && File.Exists(diffusePath))
            {
                try { phong.DiffuseMap = new MemoryStream(File.ReadAllBytes(diffusePath)); } catch { /* ignore */ }
            }

            // Diffuse color
            var pDiffuse = Shader(mesh.Material, "Diffuse");
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
            var pAmbient = Shader(mesh.Material, "Ambient");
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

            // Specular
            var pSpec = Shader(mesh.Material, "Specular");
            if (pSpec != null)
            {
                var s = pSpec.Payload;
                phong.SpecularColor = new Color4(Clamp01(s.X), Clamp01(s.Y), Clamp01(s.Z), 1f);
                phong.SpecularShininess = 20f;
            }
            else
            {
                phong.SpecularColor = new Color4(0f, 0f, 0f, 1f);
                phong.SpecularShininess = 1f;
            }

            // Transparency
            isTransparent = false;
            float alpha = phong.DiffuseColor.Alpha;

            if (mesh.Flags is { Length: > 1 } && mesh.Flags[1] == 1)
                isTransparent = true;

            var pAlphaBlend = Shader(mesh.Material, "AlphaBlending");
            if (pAlphaBlend != null && pAlphaBlend.Payload.X >= 0.5f)
                isTransparent = true;

            var pAlphaVal = Shader(mesh.Material, "AlphaValue");
            if (pAlphaVal != null)
            {
                alpha = MathUtil.Clamp(pAlphaVal.Payload.X, 0f, 1f);
                if (alpha < 0.999f) isTransparent = true;
            }

            bool hasAlphaControl = pAlphaVal != null;
            if (!isTransparent && hasAlphaControl && (pAlphaBlend == null || pAlphaBlend.Payload.X < 0.5f))
            {
                // treat as cutout; avoids depth holes
                isTransparent = true;
            }

            var dc = phong.DiffuseColor;
            phong.DiffuseColor = new Color4(dc.Red, dc.Green, dc.Blue, alpha);

            return phong;
        }

        // ===========================
        // Winding & Tangents
        // ===========================

        private static bool NeedsReverse(Vector3Collection pos, Vector3Collection nrm, IntCollection idx)
        {
            int neg = 0, total = 0;
            for (int i = 0; i + 2 < idx.Count; i += 3)
            {
                var i0 = idx[i]; var i1 = idx[i + 1]; var i2 = idx[i + 2];
                if (i0 < 0 || i1 < 0 || i2 < 0 || i0 >= pos.Count || i1 >= pos.Count || i2 >= pos.Count) continue;

                var p0 = pos[i0]; var p1 = pos[i1]; var p2 = pos[i2];
                var n0 = nrm[i0]; var n1 = nrm[i1]; var n2 = nrm[i2];

                var e1 = p1 - p0;
                var e2 = p2 - p0;
                var fn = Vector3.Cross(e1, e2);

                var an = n0 + n1 + n2;
                if (!IsFinite(fn) || !IsFinite(an)) continue;

                if (Vector3.Dot(fn, an) < 0) neg++;
                total++;
            }

            return total > 0 && neg > total * 0.6f;
        }

        private static (Vector3Collection tangents, Vector3Collection bitangents)
        GenerateTangents(Vector3Collection positions, Vector3Collection normals,
                         Vector2Collection uvs, IntCollection indices)
        {
            int vcount = positions.Count;
            var tan1 = new Vector3[vcount];
            var tan2 = new Vector3[vcount];

            for (int i = 0; i + 2 < indices.Count; i += 3)
            {
                int i0 = indices[i], i1 = indices[i + 1], i2 = indices[i + 2];
                if (i0 < 0 || i1 < 0 || i2 < 0 || i0 >= vcount || i1 >= vcount || i2 >= vcount) continue;

                var p0 = positions[i0]; var p1 = positions[i1]; var p2 = positions[i2];
                var w0 = uvs[i0]; var w1 = uvs[i1]; var w2 = uvs[i2];

                float x1 = p1.X - p0.X, x2 = p2.X - p0.X;
                float y1 = p1.Y - p0.Y, y2 = p2.Y - p0.Y;
                float z1 = p1.Z - p0.Z, z2 = p2.Z - p0.Z;

                float s1 = w1.X - w0.X, s2 = w2.X - w0.X;
                float t1 = w1.Y - w0.Y, t2 = w2.Y - w0.Y;

                float r = (s1 * t2 - s2 * t1);
                if (Math.Abs(r) < 1e-8f) r = 1e-8f;
                r = 1.0f / r;

                var sdir = new Vector3(
                    (t2 * x1 - t1 * x2) * r,
                    (t2 * y1 - t1 * y2) * r,
                    (t2 * z1 - t1 * z2) * r);

                var tdir = new Vector3(
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
                var n = SafeNormal(normals[v]);
                var t = tan1[v];

                var tang = t - Vector3.Dot(n, t) * n;
                tang = SafeNormal(tang);

                var bitan = Vector3.Cross(n, tang);
                float w = (Vector3.Dot(bitan, tan2[v]) < 0.0f) ? -1.0f : 1.0f;
                bitan *= w;

                tangents.Add(SanitizeVec(tang));
                bitangents.Add(SanitizeVec(bitan));
            }

            return (tangents, bitangents);
        }

        // ===========================
        // Skeleton (Octahedral)
        // ===========================

        public sealed class SkeletonOptions
        {
            public Color4 BoneColor { get; set; } = new Color4(0.10f, 0.1f, 1f, 1f);
            public double BoneRadius { get; set; } = 0.2;
            public bool ShowJoints { get; set; } = true;
            public Color4 JointColor { get; set; } = new Color4(1f, 0.9f, 0.1f, 1f);
            public double JointRadius { get; set; } = 0.35;
        }

        public static Element3D CreateSkeletonModel(MgmModel model, SkeletonOptions opt)
        {
            var group = new GroupModel3D();
            if (model.Bones == null || model.Bones.Count == 0) return group;

            // name -> index
            var indexByName = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < model.Bones.Count; i++) indexByName[model.Bones[i].Name] = i;

            // world positions
            var joints = new Vector3[model.Bones.Count];
            var jointValid = new bool[model.Bones.Count];

            for (int i = 0; i < model.Bones.Count; i++)
            {
                var m = model.Bones[i].GlobalRestPoseMatrix;
                var p = new Vector3(-m.M41, m.M42, m.M43);
                if (IsFinite(p))
                {
                    joints[i] = ClampPos(p);
                    jointValid[i] = true;
                }
            }

            // Bones (octahedrals)
            var bonesBuilder = new MeshBuilder();
            for (int i = 0; i < model.Bones.Count; i++)
            {
                var b = model.Bones[i];
                if (string.IsNullOrEmpty(b.ParentName)) continue;
                if (!indexByName.TryGetValue(b.ParentName, out var pIdx)) continue;
                if (!jointValid[pIdx] || !jointValid[i]) continue;

                var a = joints[pIdx];
                var c = joints[i];

                var axis = c - a;
                var len = axis.Length();
                if (len < 1e-6f || !IsFinite(a) || !IsFinite(c)) continue;

                AddOctahedronBetweenSafe(bonesBuilder, a, c, SafeRadius((float)opt.BoneRadius, len));
            }

            var bonesMesh = bonesBuilder.ToMeshGeometry3D();
            var bonesMat = new PhongMaterial
            {
                DiffuseColor = opt.BoneColor,
                AmbientColor = new Color4(0.15f, 0.15f, 0.15f, 1f),
                SpecularColor = new Color4(0.2f, 0.2f, 0.2f, 1f),
                SpecularShininess = 20f
            };
            group.Children.Add(new MeshGeometryModel3D { Geometry = bonesMesh, Material = bonesMat, IsHitTestVisible = false });

            // Joints (tiny diamonds)
            if (opt.ShowJoints)
            {
                var jointsBuilder = new MeshBuilder();
                for (int i = 0; i < joints.Length; i++)
                    if (jointValid[i])
                        AddOctahedronAtSafe(jointsBuilder, joints[i], SafeRadius((float)opt.JointRadius, 1.0f));

                var jm = jointsBuilder.ToMeshGeometry3D();
                var jmat = new PhongMaterial
                {
                    DiffuseColor = opt.JointColor,
                    AmbientColor = new Color4(0.15f, 0.15f, 0.15f, 1f)
                };
                group.Children.Add(new MeshGeometryModel3D { Geometry = jm, Material = jmat, IsHitTestVisible = false });
            }

            return group;
        }

        // --- Safe octahedron helpers ---

        private static void AddOctahedronBetweenSafe(MeshBuilder mb, Vector3 a, Vector3 b, float radius)
        {
            if (!IsFinite(a) || !IsFinite(b)) return;

            var axis = b - a;
            float len = axis.Length();
            if (len < 1e-6f) return;

            if (!TryBuildBasis(axis, out var v, out var w)) return;

            var mid = (a + b) * 0.5f;
            if (!IsFinite(mid)) return;

            var e1 = mid + v * radius;
            var e2 = mid - v * radius;
            var e3 = mid + w * radius;
            var e4 = mid - w * radius;

            if (!IsFinite(e1) || !IsFinite(e2) || !IsFinite(e3) || !IsFinite(e4)) return;

            // head fan
            mb.AddTriangle(a, e1, e3);
            mb.AddTriangle(a, e3, e2);
            mb.AddTriangle(a, e2, e4);
            mb.AddTriangle(a, e4, e1);
            // tail fan
            mb.AddTriangle(b, e3, e1);
            mb.AddTriangle(b, e2, e3);
            mb.AddTriangle(b, e4, e2);
            mb.AddTriangle(b, e1, e4);
        }

        private static void AddOctahedronAtSafe(MeshBuilder mb, Vector3 p, float r)
        {
            if (!IsFinite(p) || r <= 0f || float.IsNaN(r) || float.IsInfinity(r)) return;

            var x = p + new Vector3(r, 0, 0);
            var nx = p + new Vector3(-r, 0, 0);
            var y = p + new Vector3(0, r, 0);
            var ny = p + new Vector3(0, -r, 0);
            var z = p + new Vector3(0, 0, r);
            var nz = p + new Vector3(0, 0, -r);

            if (!IsFinite(x) || !IsFinite(nx) || !IsFinite(y) || !IsFinite(ny) || !IsFinite(z) || !IsFinite(nz)) return;

            mb.AddTriangle(x, y, z);
            mb.AddTriangle(x, z, ny);
            mb.AddTriangle(x, ny, nz);
            mb.AddTriangle(x, nz, y);

            mb.AddTriangle(nx, z, y);
            mb.AddTriangle(nx, ny, z);
            mb.AddTriangle(nx, nz, ny);
            mb.AddTriangle(nx, y, nz);
        }

        // ===========================
        // Math & safety helpers
        // ===========================

        private static bool IsFinite(Vector3 v)
            => !(float.IsNaN(v.X) || float.IsNaN(v.Y) || float.IsNaN(v.Z)
              || float.IsInfinity(v.X) || float.IsInfinity(v.Y) || float.IsInfinity(v.Z));

        private static Vector3 SanitizeVec(Vector3 v)
        {
            static float Fix(float x)
            {
                return (float.IsNaN(x) || float.IsInfinity(x)) ? 0f : x;
            }

            return new Vector3(Fix(v.X), Fix(v.Y), Fix(v.Z));
        }

        private static Vector2 SanitizeVec2(Vector2 v)
        {
            static float Fix(float x)
            {
                return (float.IsNaN(x) || float.IsInfinity(x)) ? 0f : x;
            }

            return new Vector2(Fix(v.X), Fix(v.Y));
        }

        private static Vector3 SafeNormal(Vector3 v)
        {
            if (!IsFinite(v)) return new Vector3(0, 1, 0);
            float len = v.Length();
            if (len < 1e-12f) return new Vector3(0, 1, 0);
            var n = v / len;
            return IsFinite(n) ? n : new Vector3(0, 1, 0);
        }

        private static bool TrySafeNormalize(in Vector3 v, out Vector3 n)
        {
            float len = v.Length();
            if (len < 1e-12f || !IsFinite(v)) { n = default; return false; }
            n = v / len;
            return IsFinite(n);
        }

        private static bool TryBuildBasis(in Vector3 axis, out Vector3 v, out Vector3 w)
        {
            v = default; w = default;
            if (!TrySafeNormalize(axis, out var U)) return false;

            var up = Math.Abs(U.Y) < 0.9f ? new Vector3(0, 1, 0) : new Vector3(1, 0, 0);
            var Wraw = Vector3.Cross(U, up);
            if (!TrySafeNormalize(Wraw, out w)) return false;

            var Vraw = Vector3.Cross(w, U);
            if (!TrySafeNormalize(Vraw, out v)) return false;

            return IsFinite(v) && IsFinite(w);
        }

        private static float SafeRadius(float requested, float segLen)
        {
            if (float.IsNaN(requested) || requested <= 0f || float.IsInfinity(requested))
                return Math.Max(1e-4f, segLen * 0.05f);
            return Math.Min(requested, Math.Max(1e-4f, segLen * 0.45f));
        }

        private static Vector3 ClampPos(Vector3 p, float maxAbs = 1e6f)
        {
            float Clamp(float v) => Math.Max(-maxAbs, Math.Min(maxAbs, v));
            return new Vector3(Clamp(p.X), Clamp(p.Y), Clamp(p.Z));
        }

        private static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
    }
}
