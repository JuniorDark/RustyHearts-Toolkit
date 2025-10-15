using HelixToolkit.Wpf.SharpDX;
using SharpDX;
using MeshGeometry3D = HelixToolkit.Wpf.SharpDX.MeshGeometry3D;
using Num = System.Numerics;
using SDX = SharpDX;
using TextInfo = HelixToolkit.Wpf.SharpDX.TextInfo;

namespace RHToolkit.Models.Model3D.Map
{
    public static class NaviToHelix
    {
        public static IEnumerable<Element3D> CreateNaviNodes(
    NaviMeshFile model,
    IReadOnlyDictionary<uint, Num.Matrix4x4>? worldByHash = null,
    float alpha = 0.25f)
        {
            // Shared transparent material
            var translucent = new PhongMaterial
            {
                DiffuseColor = new Color4(0.4f, 0.9f, 0.4f, alpha),
                AmbientColor = new Color4(0.25f, 0.6f, 0.25f, alpha),
                SpecularColor = new Color4(0, 0, 0, alpha), 
            };

            foreach (var obj in model.Entries)
            {
                var group = new GroupModel3D { Tag = obj.Name };

                var world = (worldByHash != null && worldByHash.TryGetValue(obj.NameKey, out var m))
                    ? m : Num.Matrix4x4.Identity;

                var positions = new Vector3Collection(obj.Vertices.Select(v =>
                {
                    var p = Num.Vector3.Transform(new Num.Vector3(v.X, v.Y, v.Z), world);
                    return new SDX.Vector3(-p.X, p.Y, p.Z);
                }));

                var indices = new IntCollection(obj.Indices.SelectMany(t => new[] { t.A, t.C, t.B }));

                var meshGeom = new MeshGeometry3D { Positions = positions, Indices = indices };

                var mesh = new MeshGeometryModel3D
                {
                    Geometry = meshGeom,
                    Material = translucent,
                    IsTransparent = true,
                    CullMode = SharpDX.Direct3D11.CullMode.None,     
                    DepthBias = -1
                };

                group.Children.Add(mesh);
                yield return group;
            }
        }

        public static Element3D CreateNavTrianglesWireframe(NaviMeshFile model, IReadOnlyDictionary<uint, Num.Matrix4x4>? worldByHash)
        {
            var group = new GroupModel3D { Tag = "NAVI_Triangles_Wire" };
            var wireColor = Colors.LimeGreen;

            HashSet<int>? allowedGlobalTris = null;
            allowedGlobalTris = [];
            foreach (var n in model.Octrees)
            {
                if (n.Subdivided || n.NaviIndexCount <= 0) continue;
                foreach (var g in n.Indices) allowedGlobalTris.Add(g);
            }

            var entries = model.Entries;
            var triCountPerEntry = entries.Select(e => e.Indices.Length).ToArray();
            var prefix = new int[triCountPerEntry.Length + 1];
            for (int i = 0; i < triCountPerEntry.Length; i++) prefix[i + 1] = prefix[i] + triCountPerEntry[i];

            var lb = new LineBuilder();

            for (int e = 0; e < entries.Count; e++)
            {
                var entry = entries[e];
                var tris = entry.Indices;
                var verts = entry.Vertices;

                var world = (worldByHash != null && worldByHash.TryGetValue(entry.NameKey, out var m)) ? m : Num.Matrix4x4.Identity;

                int baseId = prefix[e];
                for (int t = 0; t < tris.Length; t++)
                {
                    int gId = baseId + t;
                    if (allowedGlobalTris != null && !allowedGlobalTris.Contains(gId)) continue;

                    var tri = tris[t];

                    Num.Vector3 Xform(Num.Vector3 p)
                    {
                        var w = Num.Vector3.Transform(p, world);
                        return new Num.Vector3(-w.X, w.Y, w.Z); // flip
                    }

                    var a = Xform(new Num.Vector3(verts[tri.A].X, verts[tri.A].Y, verts[tri.A].Z));
                    var b = Xform(new Num.Vector3(verts[tri.B].X, verts[tri.B].Y, verts[tri.B].Z));
                    var c = Xform(new Num.Vector3(verts[tri.C].X, verts[tri.C].Y, verts[tri.C].Z));

                    // Edges in RH
                    lb.AddLine(new SDX.Vector3(a.X, a.Y, a.Z), new SDX.Vector3(b.X, b.Y, b.Z));
                    lb.AddLine(new SDX.Vector3(b.X, b.Y, b.Z), new SDX.Vector3(c.X, c.Y, c.Z));
                    lb.AddLine(new SDX.Vector3(c.X, c.Y, c.Z), new SDX.Vector3(a.X, a.Y, a.Z));
                }
            }

            var geom = lb.ToLineGeometry3D();
            return new LineGeometryModel3D
            {
                Geometry = geom,
                Color = wireColor,
                Thickness = 1.0
            };
        }

        public static Element3D CreateNavOverlay(NaviMeshFile model, IReadOnlyDictionary<uint, Num.Matrix4x4>? worldByHash)
        {
            var group = new GroupModel3D { Tag = "NAVI_Portals" };
            var pColor = Colors.Red;
            var tColor = Colors.Yellow;

            // Accumulate across all entries for fewer draw calls
            var allPortalPoints = new List<SDX.Vector3>(); // red dots (shared-edge midpoints)
            var allCenters = new List<(SDX.Vector3 pos, int id)>();  // triangle centers + ID
            int runningTriId = 0;

            foreach (var entry in model.Entries)
            {
                var verts = entry.Vertices;
                var tris = entry.Indices;

                // Choose LH world (class-3)
                var world = (worldByHash != null && worldByHash.TryGetValue(entry.NameKey, out var m))
                            ? m : Num.Matrix4x4.Identity;

                // Helper: bake world(LH) then flip X => RH
                static SDX.Vector3 Bake(Num.Vector3 p, in Num.Matrix4x4 w)
                {
                    var v = Num.Vector3.Transform(p, w);
                    return new SDX.Vector3(-v.X, v.Y, v.Z);
                }

                // 1) Build edge map: (min,max) vertex index -> number of incident triangles
                // Also keep the actual endpoints so we can compute midpoint later.
                var edgeCount = new Dictionary<(int, int), (int count, int ia, int ib)>(capacity: tris.Length * 2);

                for (int t = 0; t < tris.Length; t++)
                {
                    var tri = tris[t];
                    int a = tri.A, b = tri.B, c = tri.C;

                    void Acc(int i0, int i1)
                    {
                        int lo = i0 < i1 ? i0 : i1;
                        int hi = i0 < i1 ? i1 : i0;
                        var key = (lo, hi);
                        if (edgeCount.TryGetValue(key, out var v))
                            edgeCount[key] = (v.count + 1, lo, hi);
                        else
                            edgeCount[key] = (1, lo, hi);
                    }

                    Acc(a, b); Acc(b, c); Acc(c, a);
                }

                // 2) Emit ONE red dot per shared edge (count == 2), at the edge midpoint
                foreach (var kv in edgeCount)
                {
                    var (cnt, ia, ib) = kv.Value;
                    if (cnt != 2) continue; // portals only

                    var va = verts[ia]; var vb = verts[ib];
                    var midL = new Num.Vector3(
                        0.5f * (va.X + vb.X),
                        0.5f * (va.Y + vb.Y),
                        0.5f * (va.Z + vb.Z)
                    );
                    allPortalPoints.Add(Bake(midL, world));
                }

                // 3) Triangle centers for labels
                for (int t = 0; t < tris.Length; t++, runningTriId++)
                {
                    var tri = tris[t];
                    var a = verts[tri.A]; var b = verts[tri.B]; var c = verts[tri.C];
                    var centerL = new Num.Vector3(
                        (a.X + b.X + c.X) / 3f,
                        (a.Y + b.Y + c.Y) / 3f,
                        (a.Z + b.Z + c.Z) / 3f
                    );
                    var centerW = Bake(centerL, world);
                    allCenters.Add((centerW, runningTriId));
                }
            }

            // --- POINTS: shared-edge midpoints (red) ---
            var pointGeom = new PointGeometry3D { Positions = new Vector3Collection(allPortalPoints) };
            group.Children.Add(new PointGeometryModel3D
            {
                Geometry = pointGeom,
                Color = pColor,
                Size = new Size(4.0, 4.0)
            });

            // --- LABELS: triangle IDs at centers (yellow) ---
            var textGeom = new BillboardText3D();
            foreach (var (pos, id) in allCenters)
            {
                var ti = new TextInfo
                {
                    Text = id.ToString(CultureInfo.InvariantCulture),
                    Origin = new SDX.Vector3(pos.X + 20f, pos.Y + 20f, pos.Z),
                    Foreground = tColor.ToColor4(),
                    Scale = 0.5f
                };
                textGeom.TextInfo.Add(ti);
            }
            group.Children.Add(new BillboardTextModel3D { Geometry = textGeom, FixedSize = true });

            return group;
        }

    }
}
