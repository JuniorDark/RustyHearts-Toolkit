using System;

namespace RHToolkit.Models.MMP
{
    public struct Vec3 { public float X, Y, Z; }
    public struct Vec4 { public float X, Y, Z, W; }

    public struct BoundingBox
    {
        public Vec3 Min;
        public Vec3 Max;
    }

    public struct BoundingSphere
    {
        public Vec3 Center;
        public float Radius;
    }

    public struct RgbaColor
    {
        public float R, G, B, A;
        public static RgbaColor FromVec4(Vec4 v) => new() { R = v.X, G = v.Y, B = v.Z, A = v.W };
        public Vec4 ToVec4() => new() { X = R, Y = G, Z = B, W = A };
    }

    /// <summary>First 14 floats are scene/global; last 4 form a tint color.</summary>
    public struct LightBasis
    {
        public float G0, G1, G2, G3, G4, G5, G6, G7, G8, G9, G10, G11, G12, G13;
        public RgbaColor Tint;
    }

    public sealed class MmpModel
    {
        public int Version { get; set; }
        public string BaseDirectory { get; set; } = string.Empty;
        public List<MmpObject> Objects { get; } = [];
        public List<MmpMaterial> Materials { get; } = [];
    }

    // ---------- Geometry ----------
    public sealed class MmpObject
    {
        public string Name { get; set; } = string.Empty;
        public string AltName { get; set; } = string.Empty;
        /// <summary>Stable identifier that groups identical geometry across files.</summary>
        public uint GeometryResourceId { get; set; }

        /// <summary>Format/template code for this object's metadata block.</summary>
        public int MetaFormatCode { get; set; }

        public List<MmpPart> Parts { get; } = [];
    }

    public sealed class MmpPart
    {
        public string Name { get; set; } = string.Empty;
        public int FormatId { get; set; }
        public int FormatFlag { get; set; }
        public int Stride { get; set; }
        public MmpVertex[] Vertices { get; set; } = [];
        public ushort[] Indices { get; set; } = [];
        public MmpMaterial? Material { get; set; }

        public byte PartFlagsA { get; set; }
        public byte PartFlagsB { get; set; }
        public byte PartFlagsC { get; set; }

        /// <summary>Axis-aligned bounding box in world (or object) space.</summary>
        public BoundingBox AxisAlignedBounds { get; set; }

        /// <summary>Bounding sphere used for culling/collision.</summary>
        public BoundingSphere BoundingSphere { get; set; }
    }

    public sealed class MmpVertex
    {
        public Vec3 Position;
        public Vec3 Normal;
        public TexCoord UV0;          // primary UVs
        public TexCoord? UV1;         // optional second UV set (e.g., lightmap)
    }

    public struct TexCoord { public float U, V; }

    // ---------- Materials ----------
    public sealed class MmpMaterial
    {
        public int Id { get; set; }
        public string MaterialName { get; set; } = string.Empty;
        public string ShaderName { get; set; } = string.Empty;
        public int MaterialFlags { get; set; }
        public byte MaterialVariant { get; set; }
        public List<MmpShader> Shaders { get; } = [];
        public List<MmpTexture> Textures { get; } = [];
        public List<MmpLight> Lights { get; } = [];
    }

    public sealed class MmpShader
    {
        public string Slot { get; set; } = string.Empty;

        public Vec4 Base;    // Values[0..3]
        public float Scalar; // Values[4]
        public Vec4 Payload; // Values[5..8]

        public bool TryGetPayloadAsColor(out RgbaColor color)
        {
            color = RgbaColor.FromVec4(Payload);
            return Payload.W >= 0f && Payload.W <= 1.0001f &&
                   Payload.X >= 0f && Payload.X <= 1.0001f &&
                   Payload.Y >= 0f && Payload.Y <= 1.0001f &&
                   Payload.Z >= 0f && Payload.Z <= 1.0001f;
        }
    }

    public sealed class MmpTexture
    {
        public string Slot { get; set; } = string.Empty;
        public uint TextureId { get; set; }
        public uint SamplerStateId { get; set; }
        public uint UVSourceOrTransformId { get; set; }
        public ushort ShaderParamOffsetBytes { get; set; } 
        public ushort ShaderParamSizeBytes { get; set; }
        public byte[] RawPayload { get; set; } = [];
        public string? TexturePath { get; set; }
    }

    public sealed class MmpLight
    {
        public string Semantic { get; set; } = string.Empty;   // usually "Light_Direction"

        public uint LightBlockIndex0 { get; set; }
        public uint LightBlockIndex1 { get; set; }
        public uint LightBlockIndex2 { get; set; }

        public ushort LightBlockOffsetBytes { get; set; } 
        public ushort LightBlockSizeBytes  { get; set; }

        public LightBasis Basis { get; set; } 
    }

    // ---------- Reader ----------
    public static class MMPReader
    {
        private const string HEADER = "DoBal";

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
            if (header != HEADER)
                throw new InvalidDataException("Not an MMP file.");

            int version = br.ReadInt32();
            if (version < 6)
                throw new InvalidDataException($"MMP file version '{version}' is not supported.");

            int objCount = br.ReadInt32();

            int[] offsets = new int[objCount];
            int[] sizes   = new int[objCount];
            int[] types   = new int[objCount];

            for (int i = 0; i < objCount; i++) offsets[i] = br.ReadInt32();
            for (int i = 0; i < objCount; i++) sizes[i]   = br.ReadInt32();
            for (int i = 0; i < objCount; i++) types[i]   = br.ReadInt32();

            var model = new MmpModel { Version = version, BaseDirectory = baseDir };

            for (int i = 0; i < objCount; i++)
            {
                switch (types[i])
                {
                    // -------- Type 1: Material --------
                    case 1:
                        br.BaseStream.Seek(offsets[i], SeekOrigin.Begin);
                        ReadMaterial(br, model, version);
                        break;
                    // -------- Type 3: ??? --------
                    case 3:
                        int specint = 0;
                        if (version >= 6)
                        {
                            int unk8 = br.ReadInt32(); //count
                            int unk9 = br.ReadInt32(); //count
                            int unk10 = br.ReadInt32(); //count
                            specint = br.ReadInt32(); // some flag
                            br.ReadInt32(); //unknown
                            br.ReadInt32(); //unknown

                            if (unk8 > 0)
                            {
                                string test1 = Encoding.Unicode.GetString(br.ReadBytes(unk8 * 2));
                            }

                            if (unk9 > 0)
                            {
                                string test2 = Encoding.Unicode.GetString(br.ReadBytes(unk9 * 2));
                            }

                            if (unk10 > 0)
                            {
                                string test3 = Encoding.Unicode.GetString(br.ReadBytes(unk10 * 2));
                            }
                        }
                        else
                        {
                            specint = br.ReadInt32(); //some flag
                            br.ReadInt32();
                            br.ReadBytes(512);
                            br.ReadBytes(512);
                            br.ReadBytes(512);
                        }

                        br.ReadInt32(); // no clue
                        br.ReadInt32(); // no clue
                        int unk11 = br.ReadInt32(); // count
                        int unk12 = br.ReadInt32(); // no clue
                        int unk13 = br.ReadInt32(); // no clue
                        int unk14 = br.ReadInt32(); // no clue
                        byte specbyte = br.ReadByte();
                        byte specbyte2 = 0;

                        if (version >= 7)
                        {
                            specbyte2 = br.ReadByte(); // no clue
                        }

                        br.ReadBytes(232); // no clue

                        if (unk11 > 0)
                        {
                            if (version >= 8)
                            {
                                byte unk15 = br.ReadByte(); // some flag
                                br.ReadBytes(unk11 * 2);
                                if (unk15 > 0)
                                {
                                    br.ReadBytes(12 * unk11);
                                }
                                else
                                {
                                    br.ReadBytes(6 * unk11);
                                }
                            }
                            else
                            {
                                br.ReadBytes(16 * unk11);

                            }
                        }

                        if (unk12 > 0)
                        {
                            if (version >= 8)
                            {
                                br.ReadBytes(2 * unk12);
                                br.ReadBytes(8 * unk12);
                            }
                            else
                            {
                                br.ReadBytes(20 * unk12);
                            }
                        }

                        if (unk13 > 0)
                        {
                            if (version >= 8)
                            {
                                br.ReadBytes(2 * unk13);
                                br.ReadBytes(6 * unk13);
                            }
                            else
                            {
                                br.ReadBytes(16 * unk13);
                            }
                        }

                        if (unk14 > 0)
                        {
                            if (version >= 8)
                            {
                                br.ReadBytes(2 * unk14);
                                br.ReadBytes(2 * unk14);
                            }
                            else
                            {
                                br.ReadBytes(8 * unk14);
                            }
                        }

                        if (specint == 0)
                        {
                            if (specbyte > 0)
                            {
                                int unk15 = br.ReadInt32();
                                if (unk15 > 0)
                                {
                                    br.ReadInt32();
                                    if (version < 8)
                                    {
                                        br.ReadBytes(32 * unk15);
                                    }
                                    else
                                    {
                                        for (int j = 0; j < unk15; j++)
                                        {
                                            float unk16 = br.ReadSingle();
                                            if (j > 0 && unk16 == -1.0f)
                                            {
                                                br.ReadInt32();
                                                br.ReadBytes(12);
                                            }
                                            else
                                            {
                                                br.ReadBytes(28);
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (specbyte2 > 0)
                                {
                                    int unk17 = br.ReadInt32();
                                    if (unk17 > 0)
                                    {
                                        br.ReadBytes(16 * unk17);
                                    }
                                }
                            }
                        }
                        break;
                    // -------- Type 19: Geometry --------
                    case 19:
                        {
                            br.BaseStream.Seek(offsets[i], SeekOrigin.Begin);
                            ReadGeometryObject(br, model);
                            break;
                        }
                    default:
                        br.ReadBytes(sizes[i]);
                        break;
                }
            }
            return model;
        }

        // ---- Type 1 reader ----
        private static void ReadMaterial(BinaryReader br, MmpModel model, int version)
        {
            int legacyCount = br.ReadInt32();
            int matCount = br.ReadInt32();

            for (int j = 0; j < legacyCount; j++)
            {
                _ = br.ReadInt32();
                if (version >= 6)
                {
                    int oldLen = br.ReadInt32();
                    if (oldLen > 0) br.ReadBytes(oldLen * 2);
                }
                else
                {
                    br.ReadBytes(512);
                }
                br.ReadBytes(16); br.ReadBytes(16); br.ReadBytes(16);
                br.ReadInt32(); br.ReadInt32();
                br.ReadByte();
                int subCnt = br.ReadInt32();
                int subCnt2 = br.ReadInt32();
                for (int k = 0; k < subCnt; k++)
                {
                    _ = br.ReadInt32();
                    int nm = br.ReadInt32();
                    if (nm <= 0x800) br.ReadBytes(nm);
                }
            }

            for (int j = 0; j < matCount; j++)
            {
                int id = br.ReadInt32();
                int matNameLen = br.ReadInt32();
                int shaderNameLen = br.ReadInt32();

                string matName = ReadUtf16String(br, matNameLen);
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
                        Base = new Vec4 { X = v[0], Y = v[1], Z = v[2], W = v[3] },
                        Scalar = v[4],
                        Payload = new Vec4 { X = v[5], Y = v[6], Z = v[7], W = v[8] }
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
        }

        // ---- Type 19 reader ----
        private static void ReadGeometryObject(BinaryReader br, MmpModel model)
        {
            int objectNameLen = br.ReadInt32();
            int objectName2Len = br.ReadInt32();

            uint geometryResourceId = br.ReadUInt32();
            int metaFormatCode = br.ReadInt32();

            string objectName = ReadUtf16String(br, objectNameLen);
            string objectName2 = ReadUtf16String(br, objectName2Len);

            // --- bounding block at object-scope: 3x vec3 + 1 float ---
            // interpret as Min, Max, Center, Radius
            var objMin = new Vec3
            {
                X = br.ReadSingle(),
                Y = br.ReadSingle(),
                Z = br.ReadSingle()
            };
            var objMax = new Vec3
            {
                X = br.ReadSingle(),
                Y = br.ReadSingle(),
                Z = br.ReadSingle()
            };
            var objCenter = new Vec3
            {
                X = br.ReadSingle(),
                Y = br.ReadSingle(),
                Z = br.ReadSingle()
            };
            float objRadius = br.ReadSingle();

            int partCount = br.ReadInt32();
            var obj = new MmpObject
            {
                Name = objectName,
                AltName = objectName2,
                GeometryResourceId = geometryResourceId,
                MetaFormatCode = metaFormatCode
            };

            for (int p = 0; p < partCount; p++)
            {
                int subNameLen = br.ReadInt32();
                string subName = ReadUtf16String(br, subNameLen);

                int vCount = br.ReadInt32();
                int fCount = br.ReadInt32();
                int fmt = br.ReadInt32();

                byte partFlagsA = br.ReadByte();
                byte partFlagsB = br.ReadByte();
                byte partFlagsC = br.ReadByte();

                int flag = br.ReadInt32();
                int materialId = br.ReadInt32();

                // --- per-part bounding block: 3x vec3 + 1 float ---
                var pMin = new Vec3
                {
                    X = br.ReadSingle(),
                    Y = br.ReadSingle(),
                    Z = br.ReadSingle()
                };
                var pMax = new Vec3
                {
                    X = br.ReadSingle(),
                    Y = br.ReadSingle(),
                    Z = br.ReadSingle()
                };
                var pCenter = new Vec3
                {
                    X = br.ReadSingle(),
                    Y = br.ReadSingle(),
                    Z = br.ReadSingle()
                };
                float pRadius = br.ReadSingle();

                int stride = ComputeStride(fmt, flag);

                var vtxBytes = br.ReadBytes(checked(vCount * stride));
                var idxBytes = br.ReadBytes(checked(fCount * 3 * 2)); // 16-bit tri-list

                var part = new MmpPart
                {
                    Name = subName,
                    FormatId = fmt,
                    FormatFlag = flag,
                    Stride = stride,
                    Vertices = new MmpVertex[vCount],
                    Indices = new ushort[fCount * 3],
                    Material = model.Materials.FirstOrDefault(m => m.Id == materialId),
                    PartFlagsA = partFlagsA,
                    PartFlagsB = partFlagsB,
                    PartFlagsC = partFlagsC,
                    AxisAlignedBounds = new BoundingBox { Min = pMin, Max = pMax },
                    BoundingSphere = new BoundingSphere { Center = pCenter, Radius = pRadius }
                };

                for (int v = 0; v < vCount; v++)
                {
                    int o = v * stride;
                    float px = BitConverter.ToSingle(vtxBytes, o + 0);
                    float py = BitConverter.ToSingle(vtxBytes, o + 4);
                    float pz = BitConverter.ToSingle(vtxBytes, o + 8);
                    float nx = BitConverter.ToSingle(vtxBytes, o + 12);
                    float ny = BitConverter.ToSingle(vtxBytes, o + 16);
                    float nz = BitConverter.ToSingle(vtxBytes, o + 20);

                    TexCoord uv0, uv1 = default;

                    switch (stride)
                    {
                        case 44:
                            uv0.U = BitConverter.ToSingle(vtxBytes, o + 36);
                            uv0.V = BitConverter.ToSingle(vtxBytes, o + 40);
                            break;
                        case 40:
                            // Base UV0 at later slot; UV1 (lightmap) at earlier slot
                            uv0.U = BitConverter.ToSingle(vtxBytes, o + 32);
                            uv0.V = BitConverter.ToSingle(vtxBytes, o + 36);

                            uv1.U = BitConverter.ToSingle(vtxBytes, o + 24);
                            uv1.V = BitConverter.ToSingle(vtxBytes, o + 28);
                            break;
                        case 32:
                            uv0.U = BitConverter.ToSingle(vtxBytes, o + 24);
                            uv0.V = BitConverter.ToSingle(vtxBytes, o + 28);
                            break;
                        case 28:
                            uv0.U = BitConverter.ToSingle(vtxBytes, o + 20);
                            uv0.V = BitConverter.ToSingle(vtxBytes, o + 24);
                            break;
                        default:
                            uv0 = default;
                            break;
                    }

                    part.Vertices[v] = new MmpVertex
                    {
                        Position = new Vec3 { X = -px, Y = py, Z = pz },
                        Normal = new Vec3 { X = -nx, Y = ny, Z = nz },
                        UV0 = uv0,
                        UV1 = (stride == 40) ? uv1 : null
                    };

                }

                Buffer.BlockCopy(idxBytes, 0, part.Indices, 0, idxBytes.Length);
                obj.Parts.Add(part);
            }

            model.Objects.Add(obj);
        }

        private static int ComputeStride(int fmt, int flag)
        {
            if (flag <= 1)
            {
                return fmt switch
                {
                    0 => 32,
                    1 => 44,
                    2 => 28,
                    _ => throw new InvalidDataException($"Unknown vertex fmt {fmt} with flag={flag}")
                };
            }
            if (fmt <= 0)
                return 40; // second UV set present (lightmap UVs)
            throw new InvalidDataException($"Unknown vertex layout: fmt={fmt}, flag={flag}");
        }

        // ---- helpers ----
        private static string ReadUtf16String(BinaryReader br, int charCount)
            => charCount <= 0 ? string.Empty : Encoding.Unicode.GetString(br.ReadBytes(charCount * 2));

        private static string ReadAsciiZ(byte[] bytes)
        {
            int len = Array.IndexOf<byte>(bytes, 0);
            return Encoding.ASCII.GetString(bytes, 0, len >= 0 ? len : bytes.Length);
        }
    }
}
