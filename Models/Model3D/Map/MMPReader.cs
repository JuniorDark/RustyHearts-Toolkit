using System.Numerics;
using static RHToolkit.Models.Model3D.Map.MMP;
using static RHToolkit.Models.Model3D.ModelExtensions;

namespace RHToolkit.Models.Model3D.Map
{
    /// <summary>
    /// Reader for MMP map files.
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

            var model = new MmpModel { BaseDirectory = baseDir };

            var version = br.ReadInt32();

            model.Header.Version = version;
            model.Version = version;

            int objCount = br.ReadInt32();
            model.Header.NumObjects = objCount;

            // Object tables: offsets, sizes, class IDs
            model.Header.Index = new int[objCount];
            model.Header.Length = new int[objCount];
            model.Header.TypeId = new int[objCount];

            // object table: offsets, sizes, types
            int[] offsets = new int[objCount];
            int[] sizes   = new int[objCount];
            int[] types   = new int[objCount];

            // read object table
            for (int i = 0; i < objCount; i++) offsets[i] = br.ReadInt32();
            for (int i = 0; i < objCount; i++) sizes[i]   = br.ReadInt32();
            for (int i = 0; i < objCount; i++) types[i]   = br.ReadInt32();

            // flag if the file have type 4
            bool hasType4 = Array.IndexOf(types, 4) >= 0;

            for (int i = 0; i < objCount; i++)
            {
                int type = types[i];
                int off = offsets[i];
                int size = sizes[i];
                br.BaseStream.Seek(off, SeekOrigin.Begin);
                long start = br.BaseStream.Position;

                try
                {
                    switch (type)
                    {
                        case 1: model.Materials = ModelSharedTypeReader.ReadMaterialData(br, size, version); break;
                        case 3: model.Nodes.Add(ModelSharedTypeReader.ReadNodeTransformData(br, size, version)); break;
                        case 4: _ = br.ReadBytes(size); break; // unknown, found on entrance_04.mmp
                        case 19: ReadGeometryNodes(br, model, size, model.Header.Version); break;
                        default: throw new NotSupportedException($"Unknown/Unsupported MMP object type: {type} (at index {i})");
                    }
                    int consumed = (int)(br.BaseStream.Position - start);
                    int remain = Math.Max(0, size - consumed);

                    long read = br.BaseStream.Position - start;
                    if (remain != 0)
                        throw new InvalidDataException($"Type {type}: expected {size} bytes, read {read}.");
                }
                catch (Exception ex)
                {
                    throw new Exception($"Type {type}: Error on Position: 0x{br.BaseStream.Position:X8}: {ex}");
                }
            }
            return model;
        }

        #region Readers
        
        // ---- Type 19 reader ----
        private static void ReadGeometryNodes(BinaryReader br, MmpModel model, int size, int version)
        {
            long start = br.BaseStream.Position;
            int consumed, remain;

            // node name lengths & hashes
            string objectNodeName, objectNodeName2;
            uint objectNodeNameHash, objectNodeName2Hash;
            if (version >= 6)
            {
                int objectNodeNameLen = br.ReadInt32();
                int objectNodeName2Len = br.ReadInt32();
                objectNodeNameHash = br.ReadUInt32();
                objectNodeName2Hash = br.ReadUInt32();
                objectNodeName = BinaryReaderExtensions.ReadUtf16String(br, objectNodeNameLen);
                objectNodeName2 = BinaryReaderExtensions.ReadUtf16String(br, objectNodeName2Len);
            }
            else
            {
                objectNodeNameHash = br.ReadUInt32();
                objectNodeName2Hash = br.ReadUInt32();
                objectNodeName = BinaryReaderExtensions.ReadUnicode256Count(br);
                objectNodeName2 = BinaryReaderExtensions.ReadUnicode256Count(br);
            }

            // Bounds block: center(3), radius, min(3), max(3)
            var objCenter = BinaryReaderExtensions.ReadVector3(br);
            var objRadius = br.ReadSingle();
            var objMin = BinaryReaderExtensions.ReadVector3(br);
            var objMax = BinaryReaderExtensions.ReadVector3(br);
            var objSize = objMax - objMin;

            var obj = new MmpObjectGroup
            {
                NodeName = objectNodeName,
                AltNodeName = objectNodeName2,
                NodeNameHash = objectNodeNameHash,
                AltNodeNameHash = objectNodeName2Hash,
                GeometryBounds = new GeometryBounds { Min = objMin, Max = objMax, Size = objSize, SphereRadius = objRadius, Center = objCenter }
            };

            // meshes in this node
            int meshCount = br.ReadInt32();

            for (int p = 0; p < meshCount; p++)
            {
                int meshNameLen = br.ReadInt32();
                string meshName = BinaryReaderExtensions.ReadUtf16String(br, meshNameLen);

                // mesh attributes
                int vertexCount = br.ReadInt32();
                int faceCount = br.ReadInt32();
                int meshType = br.ReadInt32(); // 0 = regular mesh, 2 = billboard

                // flags
                byte isEmissiveAdditive = br.ReadByte();  // 1 = additive/emissive path
                byte isAlphaBlend = br.ReadByte();  // 1 = alpha-blend path
                byte isEnabled = br.ReadByte();  // 1 = visible/enabled?

                // material & UVs
                int uvSetCount = br.ReadInt32(); // 1 = one UV set (stride 32), 2 = two UV sets (stride 40)
                int materialIdx = br.ReadInt32();

                // Bounds block: center(3), radius, min(3), max(3)
                var pCenter = BinaryReaderExtensions.ReadVector3(br);
                var pRadius = br.ReadSingle();
                var pMin = BinaryReaderExtensions.ReadVector3(br);
                var pMax = BinaryReaderExtensions.ReadVector3(br);
                var pSize = pMax - pMin;

                int stride = ComputeMMPStride(meshType, uvSetCount); // bytes per vertex

                // --- read raw vertex/index data ---
                var vtxBytes = br.ReadBytes(checked(vertexCount * stride));
                var idxBytes = br.ReadBytes(checked(faceCount * 3 * 2));

                var mesh = new MmpMesh
                {
                    MeshName = meshName,
                    MeshType = meshType,
                    UVSetCount = uvSetCount,
                    AdditiveEmissive = isEmissiveAdditive,
                    AlphaBlend = isAlphaBlend,
                    Enabled = isEnabled,
                    MaterialIdx = materialIdx,
                    Stride = stride,
                    Vertices = new MmpVertex[vertexCount],
                    Indices = new ushort[faceCount * 3],
                    Material = model.Materials.FirstOrDefault(m => m.Id == materialIdx),
                    GeometryBounds = new GeometryBounds { Min = pMin, Max = pMax, Size = pSize, SphereRadius = pRadius, Center = pCenter }
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

                obj.Meshes.Add(mesh);
            }

            consumed = (int)(br.BaseStream.Position - start);
            remain = Math.Max(0, size - consumed);
            long read = br.BaseStream.Position - start;
            if (read != size)
                throw new InvalidDataException($"Type 19: {objectNodeName}: expected {size} bytes, read {read}.");

            model.Objects.Add(obj);
        }

        #endregion
    }
}
