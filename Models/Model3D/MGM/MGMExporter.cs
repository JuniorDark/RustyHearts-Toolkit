using HelixToolkit.Maths;
using RHToolkit.Models.Model3D.Animation;
using SharpAssimp;
using System.Numerics;
using static RHToolkit.Models.Model3D.ModelMaterial;
using Num = System.Numerics;

namespace RHToolkit.Models.Model3D.MGM;

public class MGMExporter
{
    public static async Task ExportMgmToFbx(MgmModel mgm, string outFilePath, bool embedTextures = false, bool exportAnimation = false, bool copyTextures = false,
    CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(mgm);
        if (string.IsNullOrWhiteSpace(outFilePath))
            throw new ArgumentException("Output path is empty.", nameof(outFilePath));

        Scene? scene = null;
        Node? root = null;

        MGMReader.ValidateMgmModel(mgm);

        try
        {
            ct.ThrowIfCancellationRequested();

            var result = CreateSceneAndRoot(mgm, outFilePath);
            scene = result.scene;
            root = result.root;
            Node mgmNode = result.mgmNode;

            var getOrCreateMaterialIndex = CreateMaterialResolver(mgm, result.outDir, embedTextures, copyTextures, scene);
            var (boneNodes, bonesArray) = BuildSkeleton(mgmNode, mgm);
            BuildMeshes(scene, mgmNode, mgm, boneNodes, bonesArray, getOrCreateMaterialIndex);

            if (exportAnimation)
            {
                await BuildAnimationsFromDsAsync(scene, mgm, boneNodes, ct);
                //await BuildAnimationsFromMaAsync(scene, mgm, boneNodes, ct);
            }

            SaveScene(scene, outFilePath);
        }
        finally
        {
            scene?.Clear();
            root?.Children.Clear();
        }
    }

    #region Scene / Root
    private static (Scene scene, Node root, Node mgmNode, string outDir, string fileName)
        CreateSceneAndRoot(MgmModel mgm, string outFilePath)
    {
        var outDir = Path.GetDirectoryName(outFilePath)!;
        Directory.CreateDirectory(outDir);
        var fileName = Path.GetFileNameWithoutExtension(outFilePath);

        var scene = new Scene();
        var root = new Node("Root");
        scene.RootNode = root;

        var mgmNode = new Node(fileName);
        root.Children.Add(mgmNode);
        mgmNode.Metadata.Add("mgm:version", new Metadata.Entry(MetaDataType.Int32, mgm.Version));
        mgmNode.Metadata.Add("mgm:exporter", new Metadata.Entry(MetaDataType.String, "RHToolkit"));

        return (scene, root, mgmNode, outDir, fileName);
    }
    #endregion

    #region Materials / Textures
    /// <summary>
    /// Creates a material resolver function that maps MGM materials to Assimp materials,
    /// </summary>
    /// <param name="mgm"></param>
    /// <param name="outDir"></param>
    /// <param name="embedTextures"></param>
    /// <param name="copyTextures"></param>
    /// <param name="scene"></param>
    /// <returns></returns>
    private static Func<ModelMaterial?, int> CreateMaterialResolver(MgmModel mgm, string outDir, bool embedTextures, bool copyTextures, Scene scene)
    {
        var matCache = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        int GetOrCreateMaterialIndex(ModelMaterial? m)
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
                Shininess = 0,
                Opacity = 1
            };

            // Textures
            var diffuseAbs = ResolveTextureAbsolute(mgm.BaseDirectory, Texture(m, "DiffuseMap")?.TexturePath);
            var specularAbs = ResolveTextureAbsolute(mgm.BaseDirectory, Texture(m, "SpecularMap")?.TexturePath);

            if (!string.IsNullOrWhiteSpace(diffuseAbs) && File.Exists(diffuseAbs))
            {
                mat.TextureDiffuse = MakeTextureSlot(scene, outDir, diffuseAbs, embedTextures, copyTextures, TextureType.Diffuse);
            }

            if (!string.IsNullOrWhiteSpace(specularAbs) && File.Exists(specularAbs))
            {
                mat.TextureSpecular = MakeTextureSlot(scene, outDir, specularAbs, embedTextures, copyTextures, TextureType.Specular);
            }

            // Shader colors
            var shDiffuse = Shader(m, "Diffuse");
            if (shDiffuse != null)
            {
                var c = Clamp01(ToRGB(shDiffuse.Base));
                mat.ColorDiffuse = new Color4(c.X, c.Y, c.Z, 1);
            }
            var shAmbient = Shader(m, "Ambient");
            if (shAmbient != null)
            {
                var c = Clamp01(ToRGB(shAmbient.Base));
                mat.ColorAmbient = new Color4(c.X, c.Y, c.Z, 1);
            }

            // Alpha / emissive
            var shAlphaValue = Shader(m, "AlphaValue");
            if (shAlphaValue != null)
                mat.Opacity = Math.Clamp(shAlphaValue.Scalar, 0f, 1f);
            var shStar = Shader(m, "StarEffect");
            if (shStar != null)
            {
                var e = Math.Clamp(shStar.Scalar, 0f, 1f);
                mat.ColorEmissive = new Color4(e, e, e, 1);
            }

            scene.Materials.Add(mat);
            var idx = scene.Materials.Count - 1;
            matCache[key] = idx;
            return idx;
        }

        return GetOrCreateMaterialIndex;
    }

    private static TextureSlot MakeTextureSlot(Scene scene, string outDir, string absPath, bool embedTextures, bool copyTextures, TextureType texType)
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
            var textureDir = "texture";
            var destDir = Path.Combine(outDir, textureDir);
            
            if (!Directory.Exists(destDir))
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

    #region Bones / Skeleton
    /// <summary>
    /// Builds the skeleton hierarchy (nodes + bind pose) under the provided mgmNode.
    /// </summary>
    private static (Dictionary<string, Node> boneNodes, MgmBone[] bonesArray)
        BuildSkeleton(Node mgmNode, MgmModel mgm)
    {
        var boneNodes = new Dictionary<string, Node>(StringComparer.Ordinal);
        var bonesArray = mgm.Bones.ToArray();

        // Create bone nodes
        foreach (var b in bonesArray)
        {
            var local = Matrix4x4.Transpose(b.LocalRestPoseMatrix);

            var node = new Node(string.IsNullOrWhiteSpace(b.Name) ? "Bone" : b.Name)
            {
                Transform = local
            };

            boneNodes[b.Name] = node;
        }

        // Parent bone nodes
        foreach (var b in bonesArray)
        {
            var me = boneNodes[b.Name];
            var parent = (!string.IsNullOrEmpty(b.ParentName) && boneNodes.TryGetValue(b.ParentName, out var p))
                ? p : mgmNode;
            parent.Children.Add(me);
        }

        return (boneNodes, bonesArray);
    }

    #endregion

    #region Meshes
    /// <summary>
    /// Converts meshes, assigns materials, and creates skinning bindings.
    /// </summary>
    private static void BuildMeshes(
        Scene scene,
        Node mgmNode,
        MgmModel mgm,
        Dictionary<string, Node> boneNodes,
        MgmBone[] bonesArray,
        Func<ModelMaterial?, int> GetOrCreateMaterialIndex)
    {
        foreach (var mesh in mgm.Meshes)
        {
            var meshName = string.IsNullOrWhiteSpace(mesh.Name) ? "Mesh" : mesh.Name;
            var altName = mesh.AltName;
            var material = mesh.Material ?? mgm.Materials.FirstOrDefault(m => m.Id == mesh.MaterialId);

            if (mesh.Vertices == null || mesh.Vertices.Length == 0) continue;
            if (mesh.Indices == null || mesh.Indices.Length < 3) continue;

            var verts = mesh.Vertices;

            // Is this mesh skinned?
            bool isSkinned = verts.Any(v => v.Weights.HasValue && v.BoneIdxU16.HasValue &&
                                            (v.Weights.Value.X > 0 || v.Weights.Value.Y > 0 ||
                                             v.Weights.Value.Z > 0 || v.Weights.Value.W > 0));

            // Parent selection
            Node parentNode = mgmNode;
            if (!isSkinned && !string.IsNullOrEmpty(altName) && boneNodes.TryGetValue(altName, out var boneNode))
                parentNode = boneNode;

            // Create node
            var meshNode = new Node(meshName);
            parentNode.Children.Add(meshNode);

            // Tags
            meshNode.Metadata.Add("mgm:flags", new Metadata.Entry(MetaDataType.Int32, mesh.Flag));

            // Assimp mesh
            var aMesh = new Mesh(meshName, PrimitiveType.Triangle);

            // Set capacities to avoid list resizes
            aMesh.Vertices.Capacity = verts.Length;
            aMesh.Normals.Capacity = verts.Length;
            var triCount = mesh.Indices.Length / 3;
            aMesh.Faces.Capacity = triCount;

            // Positions
            foreach (var v in verts)
                aMesh.Vertices.Add(new Num.Vector3(v.Position.X, v.Position.Y, v.Position.Z));

            // Indices
            for (int i = 0; i + 2 < mesh.Indices.Length; i += 3)
            {
                var face = new Face();
                face.Indices.Capacity = 3;
                face.Indices.Add(mesh.Indices[i]);
                face.Indices.Add(mesh.Indices[i + 1]);
                face.Indices.Add(mesh.Indices[i + 2]);
                aMesh.Faces.Add(face);
            }

            // Normals
            foreach (var v in verts)
                aMesh.Normals.Add(new Num.Vector3(v.Normal.X, v.Normal.Y, v.Normal.Z));

            // UV0
            aMesh.TextureCoordinateChannels[0].Capacity = verts.Length;
            foreach (var v in verts)
            {
                var uv = v.UV0;
                aMesh.TextureCoordinateChannels[0].Add(new Num.Vector3(uv.X, uv.Y, 0));
            }
            aMesh.UVComponentCount[0] = 2;

            // UV1 (if present)
            if (verts.Any(v => v.UV1.HasValue))
            {
                aMesh.TextureCoordinateChannels[1].Capacity = verts.Length;
                foreach (var v in verts)
                {
                    var uv = v.UV1 ?? default;
                    aMesh.TextureCoordinateChannels[1].Add(new Num.Vector3(uv.X, uv.Y, 0));
                }
                aMesh.UVComponentCount[1] = 2;
            }

            // Tangents if present
            if (verts.Any(v => v.Tangent.HasValue))
            {
                aMesh.Tangents.Capacity = verts.Length;
                aMesh.BiTangents.Capacity = verts.Length;
                foreach (var v in verts)
                {
                    var t = v.Tangent ?? default;
                    var tan = new Num.Vector3(t.X, t.Y, t.Z);
                    var n = new Num.Vector3(v.Normal.X, v.Normal.Y, v.Normal.Z);
                    var handed = t.W >= 0 ? 1f : -1f;
                    var bit = Num.Vector3.Cross(n, tan) * handed;
                    aMesh.Tangents.Add(new Num.Vector3(tan.X, tan.Y, tan.Z));
                    aMesh.BiTangents.Add(new Num.Vector3(bit.X, bit.Y, bit.Z));
                }
            }

            aMesh.MaterialIndex = GetOrCreateMaterialIndex(material);

            if (isSkinned)
            {
                var boneIndexToSkinBone = new Bone[bonesArray.Length];
                var transformCache = new Dictionary<Node, Num.Matrix4x4>();
                var meshGlobalInverse = GetGlobalTransform(meshNode, transformCache).Inverted();

                for (int bi = 0; bi < bonesArray.Length; bi++)
                {
                    var b = bonesArray[bi];

                    if (!boneNodes.TryGetValue(b.Name, out var bNode)) continue;

                    var bone = new Bone
                    {
                        Name = bNode.Name
                    };

                    var boneGlobalBind = GetGlobalTransform(bNode, transformCache);
                    bone.OffsetMatrix = meshGlobalInverse * boneGlobalBind;

                    boneIndexToSkinBone[bi] = bone;
                    aMesh.Bones.Add(bone);
                }

                // Push weights (4 influences max per vertex)
                for (int cp = 0; cp < verts.Length; cp++)
                {
                    var w = verts[cp].Weights!.Value;
                    var (X, Y, Z, W) = verts[cp].BoneIdxU16!.Value;

                    if (w.X > 0 && X < boneIndexToSkinBone.Length && boneIndexToSkinBone[X] != null)
                        boneIndexToSkinBone[X].VertexWeights.Add(new VertexWeight(cp, w.X));
                    if (w.Y > 0 && Y < boneIndexToSkinBone.Length && boneIndexToSkinBone[Y] != null)
                        boneIndexToSkinBone[Y].VertexWeights.Add(new VertexWeight(cp, w.Y));
                    if (w.Z > 0 && Z < boneIndexToSkinBone.Length && boneIndexToSkinBone[Z] != null)
                        boneIndexToSkinBone[Z].VertexWeights.Add(new VertexWeight(cp, w.Z));
                    if (w.W > 0 && W < boneIndexToSkinBone.Length && boneIndexToSkinBone[W] != null)
                        boneIndexToSkinBone[W].VertexWeights.Add(new VertexWeight(cp, w.W));
                }
            }

            scene.Meshes.Add(aMesh);
            meshNode.MeshIndices.Add(scene.Meshes.Count - 1);
        }
    }
    
    static Num.Matrix4x4 GetGlobalTransform(Node node, Dictionary<Node, Num.Matrix4x4> cache)
    {
        if (cache.TryGetValue(node, out var cachedTransform))
        {
            return cachedTransform;
        }

        // Accumulate parent * local up to the root
        var m = node.Transform;
        if (node.Parent != null)
        {
            m = GetGlobalTransform(node.Parent, cache) * m;
        }
        
        cache[node] = m;
        return m;
    }
    #endregion

    #region Animations

    #region Ds Animations
    /// <summary>
    /// Builds animations for dummy bones from .ds files found in the model's base directory.
    /// </summary>
    /// <param name="scene"></param>
    /// <param name="model"></param>
    /// <param name="boneNodes"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    private static async Task BuildAnimationsFromDsAsync(
        Scene scene,
        MgmModel model,
        Dictionary<string, Node> boneNodes,
        CancellationToken ct)
    {
        var baseDir = model.BaseDirectory;
        if (string.IsNullOrWhiteSpace(baseDir) || !Directory.Exists(baseDir)) return;

        var dsFiles = Directory.EnumerateFiles(baseDir, "*.ds", SearchOption.TopDirectoryOnly)
                               .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
                               .ToArray();
        if (dsFiles.Length == 0) return;

        var reader = new DSReader();

        foreach (var dsPath in dsFiles)
        {
            ct.ThrowIfCancellationRequested();
            DSFile? ds;
            try
            {
                ds = await reader.ReadAsync(dsPath, ct).ConfigureAwait(false);
            }
            catch
            {
                continue;
            }
            if (ds == null || ds.Animations.Count == 0) continue;

            foreach (var anim in ds.Animations)
            {
                var clip = new SharpAssimp.Animation
                {
                    Name = anim.Name,
                    TicksPerSecond = 1.0
                };

                double maxTime = 0.0;

                foreach (var track in anim.Tracks)
                {
                    // Map DS track -> MGM bone node by bone name
                    if (!boneNodes.TryGetValue(track.BoneName, out var node))
                        continue;

                    var ch = new NodeAnimationChannel { NodeName = node.Name };

                    // Emit one TRS key per frame
                    foreach (var frame in track.Frames)
                    {
                        // DS matrices are COLUMN-MAJOR in the file → transpose to row-major
                        var m = frame.M;
                        var mRow = Matrix4x4.Transpose(m);

                        if (!Matrix4x4.Decompose(mRow, out var T, out var R, out var S))
                            continue;

                        double t = frame.Time;
                        ch.PositionKeys.Add(new VectorKey(t, T));
                        ch.RotationKeys.Add(new QuaternionKey(t, Quaternion.Normalize(R)));
                        ch.ScalingKeys.Add(new VectorKey(t, S));

                        if (t > maxTime) maxTime = t;
                    }

                    // Ensure the channel has at least one key per component
                    EnsureChannelHasFallbackKeys(ch, node);

                    ch.PositionKeys.Sort((a, b) => a.Time.CompareTo(b.Time));
                    ch.RotationKeys.Sort((a, b) => a.Time.CompareTo(b.Time));
                    ch.ScalingKeys.Sort((a, b) => a.Time.CompareTo(b.Time));

                    if (ch.PositionKeys.Count + ch.RotationKeys.Count + ch.ScalingKeys.Count > 0)
                        clip.NodeAnimationChannels.Add(ch);
                }

                clip.DurationInTicks = Math.Max(clip.DurationInTicks, maxTime);

                if (clip.NodeAnimationChannels.Count > 0)
                    scene.Animations.Add(clip);
            }
        }
    }

    #endregion

    #region Ma Animations (WIP)
    /// <summary>
    /// Builds animations from .ma files found in the model's motion subdirectory.
    /// </summary>
    /// <param name="scene"></param>
    /// <param name="model"></param>
    /// <param name="boneNodes"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    private static async Task BuildAnimationsFromMaAsync(
    Scene scene,
    MgmModel model,
    Dictionary<string, Node> boneNodes,
    CancellationToken ct)
    {
        var baseDir = model.BaseDirectory;
        if (string.IsNullOrWhiteSpace(baseDir) || !Directory.Exists(baseDir)) return;

        var motionDir = Path.Combine(baseDir, "motion");
        if (!Directory.Exists(motionDir)) return;

        var maFiles = Directory.EnumerateFiles(motionDir, "*.ma", SearchOption.AllDirectories)
                               .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
                               .ToArray();
        if (maFiles.Length == 0) return;

        var reader = new MAReader();

        foreach (var maPath in maFiles)
        {
            ct.ThrowIfCancellationRequested();
            MaAnimation? ma;
            try
            {
                ma = await reader.ReadAsync(maPath, ct).ConfigureAwait(false);
            }
            catch { continue; }
            if (ma == null || ma.Tracks.Count == 0) continue;

            var clip = new SharpAssimp.Animation
            {
                Name = Path.GetFileNameWithoutExtension(maPath),
                DurationInTicks = ma.ClipLength,
                TicksPerSecond = 1.0
            };

            foreach (var track in ma.Tracks)
            {
                // Map MA track -> MGM bone node by name
                if (!boneNodes.TryGetValue(track.Name, out var node))
                    continue;

                // Skip if no keys in any channel (static track)
                if (track.Position.Count == 0 && track.Rotation.Count == 0 && track.Scale.Count == 0)
                    continue;

                var ch = new NodeAnimationChannel
                {
                    NodeName = node.Name
                };

                // --- Position ---
                if (track.Position.Count > 0)
                {
                    foreach (var (t, p) in track.Position.Keys) 
                        ch.PositionKeys.Add(new VectorKey(t, p));
                }

                // --- Rotation ---
                if (track.Rotation.Count > 0)
                {
                    foreach (var (t, q) in track.Rotation.Keys)
                        ch.RotationKeys.Add(new QuaternionKey(t, q));
                }

                // --- Scale ---
                if (track.Scale.Count > 0)
                {
                    foreach (var (t, s) in track.Scale.Keys)
                        ch.ScalingKeys.Add(new VectorKey(t, s));
                }

                // Ensure the channel has at least one key per component
                EnsureChannelHasFallbackKeys(ch, node);

                ch.PositionKeys.Sort((a, b) => a.Time.CompareTo(b.Time));
                ch.RotationKeys.Sort((a, b) => a.Time.CompareTo(b.Time));
                ch.ScalingKeys.Sort((a, b) => a.Time.CompareTo(b.Time));

                clip.NodeAnimationChannels.Add(ch);
            }

            if (clip.NodeAnimationChannels.Count > 0)
                scene.Animations.Add(clip);
        }
    }

    static void EnsureChannelHasFallbackKeys(NodeAnimationChannel ch, Node node)
    {
        // Use the node’s bind pose (local) as fallback
        Matrix4x4.Decompose(node.Transform, out Num.Vector3 s, out Num.Quaternion r, out Num.Vector3 t);

        if (ch.PositionKeys.Count == 0)
            ch.PositionKeys.Add(new VectorKey(0.0, t));

        if (ch.RotationKeys.Count == 0)
            ch.RotationKeys.Add(new QuaternionKey(0.0, r));

        if (ch.ScalingKeys.Count == 0)
            ch.ScalingKeys.Add(new VectorKey(0.0, s));
    }
    #endregion

    #endregion

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
}