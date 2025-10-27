using System.Numerics;
using static RHToolkit.Models.Model3D.ModelMaterial;

namespace RHToolkit.Models.Model3D;

public class ModelSharedTypeReader
{
    /// <summary>
    /// Reads a model Material data block.
    /// </summary>
    /// <param name="br"></param>
    /// <param name="version"></param>
    /// <param name="size"></param>
    /// <returns>The read Material data. </returns>
    /// <exception cref="InvalidDataException"></exception>
    public static List<ModelMaterial> ReadMaterialData(BinaryReader br, int size, int version)
    {
        long start = br.BaseStream.Position;

        int unkCount = br.ReadInt32();
        int matCount = br.ReadInt32();

        var materials = new List<ModelMaterial>();

        for (int j = 0; j < unkCount; j++)
        {
            int id = br.ReadInt32();
            string name;
            if (version >= 6)
            {
                int nameLen = br.ReadInt32();
                name = BinaryReaderExtensions.ReadUtf16String(br, nameLen);
            }
            else
            {
                name = BinaryReaderExtensions.ReadUnicode256Count(br);
            }

            br.ReadBytes(16);
            br.ReadBytes(16);
            br.ReadBytes(16);

            var unk1 = br.ReadInt32();
            var unk2 = br.ReadInt32();
            var unk3 = br.ReadByte();
            int unkFlag = br.ReadInt32();
            int unkCount3 = br.ReadInt32();

            if (unkFlag <= 0)
            {
                for (int k = 0; k < unkCount3; k++)
                {
                    var unk01 = br.ReadInt32();
                    var unk02 = br.ReadInt32();
                }
            }
            else
            {
                var unk01 = br.ReadInt32();
                int sizes = br.ReadInt32();
                if (sizes <= 0x800)
                {
                    br.ReadBytes(sizes);
                }
            }
        }

        for (int j = 0; j < matCount; j++)
        {
            var material = new ModelMaterial
            {
                Id = br.ReadInt32()
            };

            if (version >= 6)
            {
                int nameLen = br.ReadInt32();
                int shLen = br.ReadInt32();
                material.MaterialName = BinaryReaderExtensions.ReadUtf16String(br, nameLen);
                material.ShaderName = BinaryReaderExtensions.ReadUtf16String(br, shLen);
            }
            else
            {
                material.MaterialName = BinaryReaderExtensions.ReadUnicode256Count(br, 64);
                material.ShaderName = BinaryReaderExtensions.ReadUnicode256Count(br);
            }

            material.MaterialFlags = br.ReadInt32();
            material.MaterialVariant = br.ReadByte();

            int shaderCount = br.ReadInt32();
            int textureCount = br.ReadInt32();
            int lightCount = br.ReadInt32();

            for (int p = 0; p < shaderCount; p++)
            {
                string name = BinaryReaderExtensions.ReadAsciiZ(br.ReadBytes(16));
                float[] v = new float[9];
                for (int fidx = 0; fidx < 9; fidx++) v[fidx] = br.ReadSingle();

                material.Shaders.Add(new ModelShader
                {
                    Slot = name,
                    Base = new Quaternion { X = v[0], Y = v[1], Z = v[2], W = v[3] },
                    Scalar = v[4],
                    Payload = new Quaternion { X = v[5], Y = v[6], Z = v[7], W = v[8] }
                });
            }

            for (int t = 0; t < textureCount; t++)
            {
                string slot = BinaryReaderExtensions.ReadAsciiZ(br.ReadBytes(16));
                uint texId = br.ReadUInt32();
                uint samp = br.ReadUInt32();
                uint uv = br.ReadUInt32();
                ushort off = br.ReadUInt16();
                ushort sz = br.ReadUInt16();
                byte[] payload = br.ReadBytes(512);

                material.Textures.Add(new ModelTexture
                {
                    Slot = slot,
                    TextureId = texId,
                    SamplerStateId = samp,
                    UVSourceOrTransformId = uv,
                    ShaderParamOffsetBytes = off,
                    ShaderParamSizeBytes = sz,
                    RawPayload = payload,
                    TexturePath = BinaryReaderExtensions.ReadUtf16ZFromBuffer(payload)
                });
            }

            for (int t = 0; t < lightCount; t++)
            {
                var lr = new ModelLight
                {
                    Semantic = BinaryReaderExtensions.ReadAsciiZ(br.ReadBytes(16)),
                    I0 = br.ReadUInt32(),
                    I1 = br.ReadUInt32(),
                    I2 = br.ReadUInt32(),
                    OffsetBytes = br.ReadUInt16(),
                    SizeBytes = br.ReadUInt16()
                };
                for (int k = 0; k < 18; k++) lr.Basis18[k] = br.ReadSingle();
                material.Lights.Add(lr);
            }

            materials.Add(material);
        }

        long read = br.BaseStream.Position - start;
        if (read != size)
            throw new InvalidDataException($"Material Type: expected {size} bytes, read {read}.");

        return materials;

    }

    /// <summary>
    /// Reads a Node Transform data block.
    /// </summary>
    /// <param name="br"></param>
    /// <param name="version"></param>
    /// <param name="size"></param>
    /// <returns>The read Node Transform data. </returns>
    /// <exception cref="InvalidDataException"></exception>
    public static ModelNodeXform ReadNodeTransformData(BinaryReader br,int size, int version)
    {
        long start = br.BaseStream.Position;

        // node name lengths & hashes
        string objectName, groupName, objectName2;
        uint objectNameHash, groupNameHash, objectName2Hash;

        if (version >= 6)
        {
            int objectNameLen = br.ReadInt32();
            int groupNameLen = br.ReadInt32();
            int objectName2Len = br.ReadInt32();

            objectNameHash = br.ReadUInt32();
            groupNameHash = br.ReadUInt32();
            objectName2Hash = br.ReadUInt32();

            objectName = BinaryReaderExtensions.ReadUtf16String(br, objectNameLen);
            groupName = BinaryReaderExtensions.ReadUtf16String(br, groupNameLen);
            objectName2 = BinaryReaderExtensions.ReadUtf16String(br, objectName2Len);
        }
        else
        {
            objectNameHash = br.ReadUInt32();
            groupNameHash = br.ReadUInt32();
            objectName2Hash = br.ReadUInt32();
            objectName = BinaryReaderExtensions.ReadUnicode256Count(br);
            groupName = BinaryReaderExtensions.ReadUnicode256Count(br);
            objectName2 = BinaryReaderExtensions.ReadUnicode256Count(br);
        }

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

        var worldRestMatrix = BinaryReaderExtensions.ReadMatrix4x4(br);
        var bindPoseGlobalMatrix = BinaryReaderExtensions.ReadMatrix4x4(br);
        var worldRestMatrixDuplicate = BinaryReaderExtensions.ReadMatrix4x4(br);

        // Pose block: T(3) → R(4) → S(3)
        var translation = BinaryReaderExtensions.ReadVector3(br);
        var rotation = BinaryReaderExtensions.ReadQuaternion(br);
        var scale = BinaryReaderExtensions.ReadVector3(br);

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

        long read = br.BaseStream.Position - start;
        if (read != size)
            throw new InvalidDataException($"Node Transform: expected {size} bytes, read {read}.");

        return new ModelNodeXform
        {
            Name = objectName,
            NameHash = objectNameHash,
            GroupName = groupName,
            GroupHash = groupNameHash,
            SubName = objectName2,
            SubNameHash = objectName2Hash,
            Kind = kind,
            Flag = flag,
            MWorld = worldRestMatrix,
            MBind = bindPoseGlobalMatrix,
            MWorldDup = worldRestMatrixDuplicate,
            Translation = translation,
            Rotation = rotation,
            Scale = scale
        };
    }
}
