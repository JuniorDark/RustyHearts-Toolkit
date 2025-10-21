using System.Numerics;
using static RHToolkit.Models.Model3D.Map.MMP;

namespace RHToolkit.Models.Model3D.Map
{
    /// <summary>
    /// Reader for MMP (Multi-Mesh Package) map files.
    /// </summary>
    public static class MMPReader
    {
        private const string FILE_HEADER = "DoBal";

        public static async Task<MmpModel> ReadAsync(string path, CancellationToken ct = default)
        {
            byte[] bytes = await File.ReadAllBytesAsync(path, ct).ConfigureAwait(false);
            using var ms = new MemoryStream(bytes, writable: false);
            using var br = new BinaryReader(ms, Encoding.ASCII, leaveOpen: false);
            return ReadMMP(br, Path.GetDirectoryName(path)!);
        }

        private static MmpModel ReadMMP(BinaryReader br, string baseDir)
        {
            string header = Encoding.ASCII.GetString(br.ReadBytes(5));
            if (header != FILE_HEADER)
                throw new InvalidDataException("Not an MMP file.");

            int version = br.ReadInt32();

            // number of objects
            int objCount = br.ReadInt32();

            // object table: offsets, sizes, types
            int[] offsets = new int[objCount];
            int[] sizes   = new int[objCount];
            int[] types   = new int[objCount];

            // read object table
            for (int i = 0; i < objCount; i++) offsets[i] = br.ReadInt32();
            for (int i = 0; i < objCount; i++) sizes[i]   = br.ReadInt32();
            for (int i = 0; i < objCount; i++) types[i]   = br.ReadInt32();

            // flag if the file have type 4 (unknown, found on entrance_04.mmp)
            bool hasType4 = Array.IndexOf(types, 4) >= 0;

            var model = new MmpModel { Version = version, BaseDirectory = baseDir };

            if (model.Version < 6)
            {
                return model;
            }

            for (int i = 0; i < objCount; i++)
            {
                int type = types[i];
                int off = offsets[i];
                int size = sizes[i];
                br.BaseStream.Seek(off, SeekOrigin.Begin);
                long start = br.BaseStream.Position;

                switch (type)
                {
                    case 1:
                        ReadMaterials(br, model, version, size);
                        break;
                    case 3: ReadNodeTransformData(br, model, version, size); break;
                    case 4: _ = br.ReadBytes(size); break; // unknown, found on entrance_04.mmp
                    case 19: ReadGeometryNodes(br, model, size); break;
                    default:
                        throw new NotSupportedException($"Unknown/Unsupported MMP object type: {type} (at index {i})");
                }

                long read = br.BaseStream.Position - start;
                if (read != size)
                    throw new InvalidDataException($"Type {type} at index {i}: expected {size} bytes, read {read}.");
            }
            return model;
        }

        #region Readers
        // ---- Type 1 reader ----
        private static void ReadMaterials(BinaryReader br, MmpModel model, int version, int size)
        {
            long start = br.BaseStream.Position;

            int count = br.ReadInt32();
            int matCount = br.ReadInt32();

            // skip reading for now
            if (count > 0)
            {
                br.BaseStream.Position = start;
                _ = br.ReadBytes(size);
                return;
            }

            //TODO: interpret rest of payload
            for (int j = 0; j < count; j++)
            {
                int id = br.ReadInt32();
                int mat01NameLen = br.ReadInt32();
                string mat01Name = ReadUtf16String(br, mat01NameLen);

            }

            string matName = string.Empty;

            for (int j = 0; j < matCount; j++)
            {
                int id = br.ReadInt32();
                int matNameLen = br.ReadInt32();
                int shaderNameLen = br.ReadInt32();

                matName = ReadUtf16String(br, matNameLen);
                string shaderName = ReadUtf16String(br, shaderNameLen);

                int materialFlags = br.ReadInt32();
                byte materialVariant = br.ReadByte();

                int shaderCount = br.ReadInt32();
                int textureCount = br.ReadInt32();
                int lightCount = br.ReadInt32();

                var mat = new MmpMaterial
                {
                    Id = id,
                    MaterialName = matName,
                    ShaderName = shaderName,
                    MaterialFlags = materialFlags,
                    MaterialVariant = materialVariant
                };

                // ----- Shader params: 16B name + 9 floats -----
                for (int p = 0; p < shaderCount; p++)
                {
                    string name = ReadAsciiZ(br.ReadBytes(16));
                    float[] v = new float[9];
                    for (int fidx = 0; fidx < 9; fidx++) v[fidx] = br.ReadSingle();

                    mat.Shaders.Add(new MmpShader
                    {
                        Slot = name,
                        Base = new Quaternion { X = v[0], Y = v[1], Z = v[2], W = v[3] },
                        Scalar = v[4],
                        Payload = new Quaternion { X = v[5], Y = v[6], Z = v[7], W = v[8] }
                    });
                }

                // ----- Texture refs: 16B slot + 3x u32 + 2x u16 + 512B payload
                for (int t = 0; t < textureCount; t++)
                {
                    var tex = new MmpTexture
                    {
                        Slot = ReadAsciiZ(br.ReadBytes(16)),
                        TextureId = br.ReadUInt32(),
                        SamplerStateId = br.ReadUInt32(),
                        UVSourceOrTransformId = br.ReadUInt32(),
                        ShaderParamOffsetBytes = br.ReadUInt16(),
                        ShaderParamSizeBytes = br.ReadUInt16(),

                        RawPayload = br.ReadBytes(512)
                    };

                    using (var ms = new MemoryStream(tex.RawPayload, writable: false))
                    using (var r = new BinaryReader(ms, Encoding.Unicode, leaveOpen: true))
                    {
                        var sb = new StringBuilder();
                        while (ms.Position <= ms.Length - 2)
                        {
                            ushort ch = r.ReadUInt16();
                            if (ch == 0) break;
                            sb.Append((char)ch);
                        }

                        tex.TexturePath = sb.ToString();

                        //TODO: interpret rest of payload
                        //rest of data is parameters for the material’s shader

                    }

                    mat.Textures.Add(tex);
                }

                // ----- Light refs -----
                for (int t = 0; t < lightCount; t++)
                {
                    string name = ReadAsciiZ(br.ReadBytes(16));

                    uint i0 = br.ReadUInt32();
                    uint i1 = br.ReadUInt32();
                    uint i2 = br.ReadUInt32();
                    ushort u0 = br.ReadUInt16();
                    ushort u1 = br.ReadUInt16();

                    float[] f = new float[18];
                    for (int k = 0; k < 18; k++) f[k] = br.ReadSingle();

                    var basis = new LightBasis
                    {
                        G0 = f[0],
                        G1 = f[1],
                        G2 = f[2],
                        G3 = f[3],
                        G4 = f[4],
                        G5 = f[5],
                        G6 = f[6],
                        G7 = f[7],
                        G8 = f[8],
                        G9 = f[9],
                        G10 = f[10],
                        G11 = f[11],
                        G12 = f[12],
                        G13 = f[13],
                        Tint = new RgbaColor { R = f[14], G = f[15], B = f[16], A = f[17] }
                    };

                    mat.Lights.Add(new MmpLight
                    {
                        Semantic = name,
                        LightBlockIndex0 = i0,
                        LightBlockIndex1 = i1,
                        LightBlockIndex2 = i2,
                        LightBlockOffsetBytes = u0,
                        LightBlockSizeBytes = u1,
                        Basis = basis
                    });
                }

                model.Materials.Add(mat);
            }

            long read = br.BaseStream.Position - start;
            if (read != size)
                throw new InvalidDataException($"Type 1 {matName}: expected {size} bytes, read {read}.");
        }

        // ---- Type 3 reader ----
        private static void ReadNodeTransformData(BinaryReader br, MmpModel model, int version, int size)
        {
            long start = br.BaseStream.Position;

            // node name lengths & hashes
            int objectNameLen = br.ReadInt32();
            int groupNameLen = br.ReadInt32();
            int objectName2Len = br.ReadInt32();

            uint objectNameHash = br.ReadUInt32();
            uint groupNameHash = br.ReadUInt32();
            uint objectName2Hash = br.ReadUInt32();

            string objectName = ReadUtf16String(br, objectNameLen);
            string groupName = ReadUtf16String(br, groupNameLen);
            string objectName2 = ReadUtf16String(br, objectName2Len);

            // flags
            int kind = br.ReadInt32();
            int flag = br.ReadInt32();

            // unknown counts, usually 0
            int unk1 = br.ReadInt32();
            int unk2 = br.ReadInt32();
            int unk3 = br.ReadInt32();
            int unk4 = br.ReadInt32();

            // byte flags, usually 0
            byte b1 = br.ReadByte();
            byte b2 = 0;

            if (version >= 7)
            {
                b2 = br.ReadByte();
            }

            float[] worldRestMatrix = ReadFloats(br, 16);
            float[] bindPoseGlobalMatrix = ReadFloats(br, 16);
            float[] worldRestMatrixDuplicate = ReadFloats(br, 16);

            // Pose block: T(3) → R(4) → S(3)
            Vector3 translation = new(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
            Quaternion rotation = new(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
            Vector3 scale = new(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());

            // unknown data blocks
            if (unk1 > 0)
            {
                if (version >= 8)
                {
                    byte unk01 = br.ReadByte();
                    byte[] b01 = br.ReadBytes(unk1 * 2);
                    if (unk01 > 0)
                    {
                        byte[] b02 = br.ReadBytes(12 * unk1);
                    }
                    else
                    {
                        byte[] b03 = br.ReadBytes(6 * unk1);
                    }
                }
                else
                {
                    byte[] b04 = br.ReadBytes(16 * unk1);

                }
            }

            if (unk2 > 0)
            {
                if (version >= 8)
                {
                    byte[] b05 = br.ReadBytes(2 * unk2);
                    byte[] b06 = br.ReadBytes(8 * unk2);
                }
                else
                {
                    byte[] b07 = br.ReadBytes(20 * unk2);
                }
            }

            if (unk3 > 0)
            {
                if (version >= 8)
                {
                    byte[] b08 = br.ReadBytes(2 * unk3);
                    byte[] b09 = br.ReadBytes(6 * unk3);
                }
                else
                {
                    byte[] b10 = br.ReadBytes(16 * unk3);
                }
            }

            if (unk4 > 0)
            {
                if (version >= 8)
                {
                    byte[] b11 = br.ReadBytes(2 * unk4);
                    byte[] b12 = br.ReadBytes(2 * unk4);
                }
                else
                {
                    byte[] b13 = br.ReadBytes(8 * unk4);
                }
            }

            if (b1 > 0)
            {
                int unk15 = br.ReadInt32();
                if (unk15 > 0)
                {
                    int unk01 = br.ReadInt32();
                    if (version < 8)
                    {
                        byte[] b14 = br.ReadBytes(32 * unk15);
                    }
                    else
                    {
                        for (int j = 0; j < unk15; j++)
                        {
                            float unk16 = br.ReadSingle();
                            if (j > 0 && unk16 == -1.0f)
                            {
                                int unk02 = br.ReadInt32();
                                byte[] b15 = br.ReadBytes(12);
                            }
                            else
                            {
                                byte[] b16 = br.ReadBytes(28);
                            }
                        }
                    }
                }
            }
            else
            {
                if (b2 > 0)
                {
                    int unk17 = br.ReadInt32();
                    if (unk17 > 0)
                    {
                        byte[] b17 = br.ReadBytes(16 * unk17);
                    }
                }
            }

            model.Nodes.Add(new MmpNodeXform
            {
                Name = objectName,
                NameHash = objectNameHash,
                GroupName = groupName,
                GroupHash = groupNameHash,
                SubName = objectName2,
                SubNameHash = objectName2Hash,
                Kind = kind,
                Flag = flag,
                MWorld = new Matrix4x4(
                    worldRestMatrix[0], worldRestMatrix[1], worldRestMatrix[2], worldRestMatrix[3],
                    worldRestMatrix[4], worldRestMatrix[5], worldRestMatrix[6], worldRestMatrix[7],
                    worldRestMatrix[8], worldRestMatrix[9], worldRestMatrix[10], worldRestMatrix[11],
                    worldRestMatrix[12], worldRestMatrix[13], worldRestMatrix[14], worldRestMatrix[15]
                ),
                MBind = new Matrix4x4(
                    bindPoseGlobalMatrix[0], bindPoseGlobalMatrix[1], bindPoseGlobalMatrix[2], bindPoseGlobalMatrix[3],
                    bindPoseGlobalMatrix[4], bindPoseGlobalMatrix[5], bindPoseGlobalMatrix[6], bindPoseGlobalMatrix[7],
                    bindPoseGlobalMatrix[8], bindPoseGlobalMatrix[9], bindPoseGlobalMatrix[10], bindPoseGlobalMatrix[11],
                    bindPoseGlobalMatrix[12], bindPoseGlobalMatrix[13], bindPoseGlobalMatrix[14], bindPoseGlobalMatrix[15]
                ),
                MWorldDup = new Matrix4x4(
                    worldRestMatrixDuplicate[0], worldRestMatrixDuplicate[1], worldRestMatrixDuplicate[2], worldRestMatrixDuplicate[3],
                    worldRestMatrixDuplicate[4], worldRestMatrixDuplicate[5], worldRestMatrixDuplicate[6], worldRestMatrixDuplicate[7],
                    worldRestMatrixDuplicate[8], worldRestMatrixDuplicate[9], worldRestMatrixDuplicate[10], worldRestMatrixDuplicate[11],
                    worldRestMatrixDuplicate[12], worldRestMatrixDuplicate[13], worldRestMatrixDuplicate[14], worldRestMatrixDuplicate[15]
                ),
                Translation = translation,
                Rotation = rotation,
                Scale = scale
            });

            long read = br.BaseStream.Position - start;
            if (read != size)
                throw new InvalidDataException($"Type 3 {objectName}: expected {size} bytes, read {read}.");

            //Debug.Write($"\nobject={objectName}, kind={kind}, flag={flag}, unks={unk1}, {unk2}, {unk3}, {unk4}");
        }

        // ---- Type 19 reader ----
        private static void ReadGeometryNodes(BinaryReader br, MmpModel model, int size)
        {
            long start = br.BaseStream.Position;
            int consumed, remain;

            // node name lengths & hashes
            int objectNodeNameLen = br.ReadInt32();
            int objectNodeName2Len = br.ReadInt32();

            uint objectNodeNameHash = br.ReadUInt32();
            uint objectNodeName2Hash = br.ReadUInt32();

            string objectNodeName = ReadUtf16String(br, objectNodeNameLen);
            string objectNodeName2 = ReadUtf16String(br, objectNodeName2Len);

            // Bounds block: center(3), radius, min(3), max(3)
            var objCenter = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
            var objRadius = br.ReadSingle();
            var objMin = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
            var objMax = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
            var objSize = objMax - objMin;

            var obj = new MmpObjectGroup
            {
                NodeName = objectNodeName,
                AltNodeName = objectNodeName2,
                NodeNameHash = objectNodeNameHash,
                AltNodeNameHash = objectNodeName2Hash,
                GeometryBounds = new MMP.GeometryBounds { Min = objMin, Max = objMax, Size = objSize, SphereRadius = objRadius, Center = objCenter }
            };

            // meshes in this node
            int meshCount = br.ReadInt32();

            for (int p = 0; p < meshCount; p++)
            {
                int meshNameLen = br.ReadInt32();
                string meshName = ReadUtf16String(br, meshNameLen);

                // mesh attributes
                int vertexCount = br.ReadInt32();
                int faceCount = br.ReadInt32();
                int vertexLayoutTag = br.ReadInt32(); // 0 = regular mesh, 2 = light/corona billboard

                // flags
                byte isEmissiveAdditive = br.ReadByte();  // 1 = additive/emissive path
                byte isAlphaBlend = br.ReadByte();  // 1 = alpha-blend path
                byte isEnabled = br.ReadByte();  // 1 = visible/enabled?

                // material & UVs
                int uvSetCount = br.ReadInt32(); // 1 = one UV set (stride 32), 2 = two UV sets (stride 40)
                int materialId = br.ReadInt32();

                // Bounds block: center(3), radius, min(3), max(3)
                var pCenter = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                var pRadius = br.ReadSingle();
                var pMin = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                var pMax = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                var pSize = pMax - pMin;

                int stride = ComputeStride(vertexLayoutTag, uvSetCount); // bytes per vertex

                // --- read raw vertex/index data ---
                var vtxBytes = br.ReadBytes(checked(vertexCount * stride));
                var idxBytes = br.ReadBytes(checked(faceCount * 3 * 2));

                var mesh = new MmpMesh
                {
                    MeshName = meshName,
                    MeshType = vertexLayoutTag,
                    UVSetCount = uvSetCount,
                    AdditiveEmissive = isEmissiveAdditive,
                    AlphaBlend = isAlphaBlend,
                    Enabled = isEnabled,
                    MaterialIdx = materialId,
                    Stride = stride,
                    Vertices = new MmpVertex[vertexCount],
                    Indices = new ushort[faceCount * 3],
                    Material = model.Materials.FirstOrDefault(m => m.Id == materialId),
                    GeometryBounds = new MMP.GeometryBounds { Min = pMin, Max = pMax, Size = pSize, SphereRadius = pRadius, Center = pCenter }
                };

                // --- decode indices ---
                Buffer.BlockCopy(idxBytes, 0, mesh.Indices, 0, idxBytes.Length);

                // --- decode vertices ---
                for (int v = 0; v < vertexCount; v++)
                {
                    int o = v * stride;

                    var pos = new Vector3
                    {
                        X = BitConverter.ToSingle(vtxBytes, o + 0),
                        Y = BitConverter.ToSingle(vtxBytes, o + 4),
                        Z = BitConverter.ToSingle(vtxBytes, o + 8)
                    };
                    var nor = new Vector3
                    {
                        X = BitConverter.ToSingle(vtxBytes, o + 12),
                        Y = BitConverter.ToSingle(vtxBytes, o + 16),
                        Z = BitConverter.ToSingle(vtxBytes, o + 20)
                    };

                    Vector2 uv0, uv1 = default;

                    switch (stride)
                    {
                       case 28: // billboard
                            uv0.X = BitConverter.ToSingle(vtxBytes, o + 24);
                            uv0.Y = default;
                            break;
                        case 32: // single UV set
                            uv0.X = BitConverter.ToSingle(vtxBytes, o + 24);
                            uv0.Y = BitConverter.ToSingle(vtxBytes, o + 28);
                            break;
                        case 40: // two UV sets, UV0 base; UV1 (lightmap)
                            uv0.X = BitConverter.ToSingle(vtxBytes, o + 24);
                            uv0.Y = BitConverter.ToSingle(vtxBytes, o + 28);
                            uv1.X = BitConverter.ToSingle(vtxBytes, o + 32);
                            uv1.Y = BitConverter.ToSingle(vtxBytes, o + 36);
                            break;
                        default:
                            uv0 = default;
                            break;
                    }

                    mesh.Vertices[v] = new MmpVertex
                    {
                        Position = pos,
                        Normal = nor,
                        UV0 = uv0,
                        UV1 = (stride == 40) ? uv1 : null
                    };
                }

                //Debug.Write($"\nobject={objectNodeName}, part: {p+1}/{meshCount}, subName: {meshName}, layoutTag: {vertexLayoutTag}, uvSetCount: {uvSetCount}, flags: [{isEmissiveAdditive},{isAlphaBlend},{isEnabled}], stride: {stride}");
                obj.Meshes.Add(mesh);
            }

            consumed = (int)(br.BaseStream.Position - start);
            remain = Math.Max(0, size - consumed);
            long read = br.BaseStream.Position - start;
            if (read != size)
                throw new InvalidDataException($"{objectNodeName}: expected {size} bytes, read {read}.");

            model.Objects.Add(obj);
        }
        #endregion

        #region Helpers
        /// <summary>
        /// Computes the vertex stride in bytes based on the vertex layout tag and UV set count.
        /// </summary>
        /// <param name="vertexLayoutTag"></param>
        /// <param name="uvSetCount"></param>
        /// <returns></returns>
        /// <exception cref="InvalidDataException"></exception>
        private static int ComputeStride(int vertexLayoutTag, int uvSetCount)
        {
            if (vertexLayoutTag == 2) return 28;
            if (vertexLayoutTag == 0 && uvSetCount == 1) return 32;
            if (vertexLayoutTag == 0 && uvSetCount == 2) return 40;
            throw new InvalidDataException($"Unknown vertex layout: tag={vertexLayoutTag}, uv={uvSetCount}");
        }

        /// <summary>
        /// Reads a UTF-16 string of the specified character count from the binary reader.
        /// </summary>
        /// <param name="br"></param>
        /// <param name="charCount"></param>
        /// <returns> The read string, or empty if charCount is 0 or less. </returns>
        private static string ReadUtf16String(BinaryReader br, int charCount)
            => charCount <= 0 ? string.Empty : Encoding.Unicode.GetString(br.ReadBytes(charCount * 2));

        /// <summary>
        /// Reads a null-terminated ASCII string from the given byte array.
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns>The read string, or the entire byte array as a string if no null terminator is found. </returns>
        private static string ReadAsciiZ(byte[] bytes)
        {
            int len = Array.IndexOf<byte>(bytes, 0);
            return Encoding.ASCII.GetString(bytes, 0, len >= 0 ? len : bytes.Length);
        }

        /// <summary>
        /// Reads an array of 'n' floats from the binary reader.
        /// </summary>
        /// <param name="br"></param>
        /// <param name="n"></param>
        /// <returns></returns>
        static float[] ReadFloats(BinaryReader br, int n)
        {
            float[] a = new float[n];
            for (int i = 0; i < n; i++) a[i] = br.ReadSingle();
            return a;
        }
        #endregion
    }
}
