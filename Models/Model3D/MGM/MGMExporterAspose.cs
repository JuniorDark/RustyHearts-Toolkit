using Aspose.ThreeD;
using Aspose.ThreeD.Formats;
using static RHToolkit.Models.Model3D.ModelMaterial;
using A3D = Aspose.ThreeD;
using A3DD = Aspose.ThreeD.Deformers;
using A3DE = Aspose.ThreeD.Entities;
using A3DS = Aspose.ThreeD.Shading;
using AU = Aspose.ThreeD.Utilities;
using Num = System.Numerics;

namespace RHToolkit.Models.Model3D.MGM;

public class MGMExporterAspose
{
    public async Task ExportMgmToFbx(MgmModel mgm, string outFilePath, bool embedTextures = false)
    {
        ArgumentNullException.ThrowIfNull(mgm);
        if (string.IsNullOrWhiteSpace(outFilePath))
            throw new ArgumentException("Output path is empty.", nameof(outFilePath));

        // --- Scene & root container ---
        var (scene, root, mgmNode, outDir, fileName) = CreateSceneAndRoot(mgm, outFilePath);

        // --- Materials / textures (factory) ---
        var getOrCreateMaterial = CreateMaterialResolver(mgm, outDir, embedTextures);

        // --- Bones / skeleton ---
        var (boneNodes, bonesArray, sceneBindPose, poseHasNode) = await BuildSkeletonAsync(scene, mgmNode, mgm);

        // --- Meshes ---
        await BuildMeshesAsync(scene, mgmNode, mgm, boneNodes, bonesArray, sceneBindPose, poseHasNode, getOrCreateMaterial);

        // --- Save FBX ---
        await SaveSceneAsync(scene, outFilePath, embedTextures);
    }

    #region Scene / Root
    private static (A3D.Scene scene, A3D.Node root, A3D.Node mgmNode, string outDir, string fileName)
        CreateSceneAndRoot(MgmModel mgm, string outFilePath)
    {
        var outDir = Path.GetDirectoryName(outFilePath)!;
        Directory.CreateDirectory(outDir);
        var fileName = Path.GetFileNameWithoutExtension(outFilePath);

        var scene = new A3D.Scene();
        var root = scene.RootNode;

        var mgmNode = new A3D.Node(fileName);
        root.AddChildNode(mgmNode);

        mgmNode.SetProperty("mgm:version", mgm.Version);
        mgmNode.SetProperty("mgm:exporter", "RHToolkit");

        return (scene, root, mgmNode, outDir, fileName);
    }
    #endregion

    #region Materials / Textures
    /// <summary>
    /// Creates a closure that caches and constructs materials and sets up textures.
    /// Texture I/O happens inside MakeTexture/ResolveTextureAbsolute provided by the host project.
    /// </summary>
    private Func<ModelMaterial?, A3DS.Material> CreateMaterialResolver(MgmModel mgm, string outDir, bool embedTextures)
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

            // Textures
            var diffuseAbs = ResolveTextureAbsolute(mgm.BaseDirectory, Texture(m, "DiffuseMap")?.TexturePath);
            var specularAbs = ResolveTextureAbsolute(mgm.BaseDirectory, Texture(m, "SpecularMap")?.TexturePath);

            if (!string.IsNullOrWhiteSpace(diffuseAbs))
                mat.SetTexture("DiffuseColor", MakeTexture(outDir, diffuseAbs, embedTextures));

            if (!string.IsNullOrWhiteSpace(specularAbs))
                mat.SetTexture("SpecularColor", MakeTexture(outDir, specularAbs, embedTextures));

            // Shader colors
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

            // Alpha / emissive
            var shAlphaValue = Shader(m, "AlphaValue");
            if (shAlphaValue != null)
                mat.Transparency = Math.Clamp(shAlphaValue.Scalar, 0f, 1f);
            var shStar = Shader(m, "StarEffect");
            if (shStar != null)
            {
                var e = Math.Clamp(shStar.Scalar, 0f, 1f);
                mat.EmissiveColor = new AU.Vector3(e, e, e);
            }

            matCache[key] = mat;
            return mat;
        }

        return GetOrCreateMaterial;
    }
    #endregion

    #region Bones / Skeleton
    /// <summary>
    /// Builds the skeleton hierarchy (nodes + bind pose) under the provided mgmNode.
    /// </summary>
    private static async Task<(Dictionary<string, A3D.Node> boneNodes, MgmBone[] bonesArray, A3D.Pose bindPose, HashSet<A3D.Node> poseHasNode)>
        BuildSkeletonAsync(A3D.Scene scene, A3D.Node mgmNode, MgmModel mgm)
    {
        return await Task.Run(() =>
        {
            mgmNode.Transform.TransformMatrix = AU.Matrix4.Identity;
            mgmNode.Entity = new A3DE.Skeleton { Type = A3DE.SkeletonType.Skeleton };

            var boneNodes = new Dictionary<string, A3D.Node>(StringComparer.Ordinal);
            var bonesArray = mgm.Bones.ToArray();

            // Create bone nodes
            foreach (var b in bonesArray)
            {
                var node = new A3D.Node(string.IsNullOrWhiteSpace(b.Name) ? "Bone" : b.Name);

                // bones that carry a reflection in their local matrix need to be adjusted
                var local = EnsureRightHanded(b.LocalRestPoseMatrix);
                node.Transform.TransformMatrix = ToAspose(local);

                node.Entity = new A3DE.Skeleton { Type = A3DE.SkeletonType.Bone };

                boneNodes[b.Name] = node;
            }

            // Parent bone nodes
            foreach (var b in bonesArray)
            {
                var me = boneNodes[b.Name];
                var parent = (!string.IsNullOrEmpty(b.ParentName) && boneNodes.TryGetValue(b.ParentName, out var p))
                    ? p : mgmNode;
                parent.AddChildNode(me);
            }

            // One pose container for bind matrices
            var sceneBindPose = new A3D.Pose("BindPose")
            {
                PoseType = Aspose.ThreeD.PoseType.BindPose
            };
            scene.Library.Add(sceneBindPose);

            sceneBindPose.AddBonePose(mgmNode, mgmNode.GlobalTransform.TransformMatrix);

            foreach (var b in bonesArray)
            {
                if (!boneNodes.TryGetValue(b.Name, out var bNode)) continue;
                sceneBindPose.AddBonePose(bNode, bNode.GlobalTransform.TransformMatrix);
            }

            var poseHasNode = new HashSet<A3D.Node>();

            return (boneNodes, bonesArray, sceneBindPose, poseHasNode);
        });
    }
    #endregion

    #region Meshes
    /// <summary>
    /// Converts meshes, assigns materials, and creates skinning bindings.
    /// </summary>
    private static async Task BuildMeshesAsync(
        A3D.Scene scene,
        A3D.Node mgmNode,
        MgmModel mgm,
        Dictionary<string, A3D.Node> boneNodes,
        MgmBone[] bonesArray,
        A3D.Pose sceneBindPose,
        HashSet<A3D.Node> poseHasNode,
        Func<ModelMaterial?, A3DS.Material> GetOrCreateMaterial)
    {
        await Task.Run(() =>
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
                A3D.Node parentNode = mgmNode;
                if (!isSkinned && !string.IsNullOrEmpty(altName) && boneNodes.TryGetValue(altName, out var boneNode))
                    parentNode = boneNode;

                // Create node
                var meshNode = parentNode.CreateChildNode(meshName);

                // Tags
                meshNode.SetProperty("mgm:flags", mesh.Flag);

                // Aspose mesh
                var aMesh = new A3DE.Mesh { Name = meshName };

                // Positions
                foreach (var v in verts)
                    aMesh.ControlPoints.Add(new AU.Vector4(v.Position.X, v.Position.Y, v.Position.Z, 1));

                // Indices
                for (int i = 0; i + 2 < mesh.Indices.Length; i += 3)
                    aMesh.CreatePolygon(mesh.Indices[i], mesh.Indices[i + 1], mesh.Indices[i + 2]);

                // Normals
                var nrmElem = new A3DE.VertexElementNormal
                {
                    MappingMode = A3DE.MappingMode.ControlPoint,
                    ReferenceMode = A3DE.ReferenceMode.Direct
                };
                foreach (var v in verts)
                    nrmElem.Data.Add(new AU.Vector4(v.Normal.X, v.Normal.Y, v.Normal.Z, 0));
                aMesh.VertexElements.Add(nrmElem);

                // UV0 (flip V)
                var uv0Elem = new A3DE.VertexElementUV
                {
                    Name = "UV0",
                    MappingMode = A3DE.MappingMode.ControlPoint,
                    ReferenceMode = A3DE.ReferenceMode.Direct
                };
                foreach (var v in verts)
                {
                    var uv = v.UV0;
                    uv0Elem.Data.Add(new AU.Vector4(uv.X, 1.0f - uv.Y, 0, 0));
                }
                aMesh.VertexElements.Add(uv0Elem);

                // UV1 (if present; flip V)
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
                        uv1Elem.Data.Add(new AU.Vector4(uv.X, 1.0f - uv.Y, 0, 0));
                    }
                    aMesh.VertexElements.Add(uv1Elem);
                }

                // Tangents if present
                if (verts.Any(v => v.Tangent.HasValue))
                {
                    var tanElem = new A3DE.VertexElementTangent
                    {
                        MappingMode = A3DE.MappingMode.ControlPoint,
                        ReferenceMode = A3DE.ReferenceMode.Direct
                    };
                    foreach (var v in verts)
                    {
                        var t = v.Tangent ?? default;
                        tanElem.Data.Add(new AU.Vector4(t.X, t.Y, t.Z, t.W));
                    }
                    aMesh.VertexElements.Add(tanElem);
                }

                meshNode.Entity = aMesh;
                meshNode.Material = GetOrCreateMaterial(material);

                if (isSkinned)
                {
                    var meshBindGlobal = meshNode.GlobalTransform.TransformMatrix;
                    if (poseHasNode.Add(meshNode))
                        sceneBindPose.AddBonePose(meshNode, meshBindGlobal);

                    var boneIndexToSkinBone = new A3DD.Bone[bonesArray.Length];

                    var skin = new A3DD.SkinDeformer(meshName + "_Skin");
                    aMesh.Deformers.Add(skin);

                    for (int bi = 0; bi < bonesArray.Length; bi++)
                    {
                        var b = bonesArray[bi];

                        if (b.BoneType != MgmBoneType.Bone) continue; // only deformers in the skin
                        if (!boneNodes.TryGetValue(b.Name, out var bNode)) continue;

                        // Create the skin 'bone' entry by name, then LINK it to the node:
                        var bone = new A3DD.Bone(bNode.Name)
                        {
                            Node = bNode,
                            BoneTransform = bNode.GlobalTransform.TransformMatrix,
                            Transform = meshBindGlobal
                        };

                        boneIndexToSkinBone[bi] = bone;
                        skin.Bones.Add(bone);
                    }

                    // Push weights (4 influences max per vertex)
                    for (int cp = 0; cp < verts.Length; cp++)
                    {
                        var w = verts[cp].Weights!.Value;
                        var (X, Y, Z, W) = verts[cp].BoneIdxU16!.Value;

                        if (w.X > 0 && X < boneIndexToSkinBone.Length && boneIndexToSkinBone[X] != null)
                            boneIndexToSkinBone[X].SetWeight(cp, w.X);
                        if (w.Y > 0 && Y < boneIndexToSkinBone.Length && boneIndexToSkinBone[Y] != null)
                            boneIndexToSkinBone[Y].SetWeight(cp, w.Y);
                        if (w.Z > 0 && Z < boneIndexToSkinBone.Length && boneIndexToSkinBone[Z] != null)
                            boneIndexToSkinBone[Z].SetWeight(cp, w.Z);
                        if (w.W > 0 && W < boneIndexToSkinBone.Length && boneIndexToSkinBone[W] != null)
                            boneIndexToSkinBone[W].SetWeight(cp, w.W);
                    }
                }
            }
        });
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

    #region Matrix Helpers
    private static AU.Matrix4 ToAspose(Num.Matrix4x4 m)
    {
        return new AU.Matrix4(
            m.M11, m.M12, m.M13, m.M14,
            m.M21, m.M22, m.M23, m.M24,
            m.M31, m.M32, m.M33, m.M34,
            m.M41, m.M42, m.M43, m.M44
        );
    }

    // Determinant of upper-left 3x3
    public static float Det3x3(Num.Matrix4x4 m)
    {
        return
            m.M11 * (m.M22 * m.M33 - m.M23 * m.M32) -
            m.M12 * (m.M21 * m.M33 - m.M23 * m.M31) +
            m.M13 * (m.M21 * m.M32 - m.M22 * m.M31);
    }

    // If det<0, push the reflection into a 180° flip on X (post-multiply).
    public static Num.Matrix4x4 EnsureRightHanded(Num.Matrix4x4 local)
    {
        if (Det3x3(local) < 0f)
        {
            var flipX = Num.Matrix4x4.CreateScale(-1, 1, 1);
            local *= flipX;   // right-multiply: adjust the bone’s *local basis*
        }
        return local;
    }
    #endregion
}
