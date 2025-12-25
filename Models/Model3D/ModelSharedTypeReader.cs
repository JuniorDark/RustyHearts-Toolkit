using System;
using System.Numerics;
using static RHToolkit.Models.Model3D.ModelMaterial;

namespace RHToolkit.Models.Model3D;

public class ModelSharedTypeReader
{
    public static List<ModelMaterial> ReadMaterialData(BinaryReader br, int size, int version)
    {
        long start = br.BaseStream.Position;

        int libraryCount = br.ReadInt32();
        int matCount = br.ReadInt32();

        var library = new List<MaterialLibrary>(libraryCount);
        var materials = new List<ModelMaterial>(matCount);

        // ---- Material Libraries ----
        for (int j = 0; j < libraryCount; j++)
        {
            var e = new MaterialLibrary
            {
                Id = br.ReadInt32()
            };

            if (version >= 6)
            {
                int nameLen = br.ReadInt32();
                e.Name = BinaryReaderExtensions.ReadUtf16String(br, nameLen);
            }
            else
            {
                e.Name = BinaryReaderExtensions.ReadUnicodeFixedString(br);
            }

            // 3 x 16 bytes: float3 + u32 tag
            (e.V0, e.V0Tag) = ReadVec3Tag(br);
            (e.V1, e.V1Tag) = ReadVec3Tag(br);
            (e.V2, e.V2Tag) = ReadVec3Tag(br);

            e.ScalarRawI32 = br.ReadInt32();
            e.ScalarF32 = BitConverter.Int32BitsToSingle(e.ScalarRawI32);

            e.Unk2 = br.ReadInt32();
            e.Unk3 = br.ReadByte();

            e.TailMode = br.ReadInt32();
            e.TailCount = br.ReadInt32();

            if (e.TailMode <= 0)
            {
                for (int k = 0; k < e.TailCount; k++)
                {
                    int a = br.ReadInt32();
                    int b = br.ReadInt32();
                    e.Pairs.Add((a, b));
                }
            }
            else
            {
                e.TailHeadI32 = br.ReadInt32();
                e.TailBlobSize = br.ReadInt32();
                if (e.TailBlobSize > 0 && e.TailBlobSize <= 0x800)
                    e.TailBlob = br.ReadBytes(e.TailBlobSize);
                else if (e.TailBlobSize > 0)
                    br.ReadBytes(Math.Min(e.TailBlobSize, 0x800));
            }

            library.Add(e);
        }

        // ---- Materials ----
        for (int j = 0; j < matCount; j++)
        {
            var material = new ModelMaterial { MaterialIndex = br.ReadInt32() };

            if (version >= 6)
            {
                int nameLen = br.ReadInt32();
                int shLen = br.ReadInt32();
                material.MaterialName = BinaryReaderExtensions.ReadUtf16String(br, nameLen);
                material.ShaderName = BinaryReaderExtensions.ReadUtf16String(br, shLen);
            }
            else
            {
                material.MaterialName = BinaryReaderExtensions.ReadUnicodeFixedString(br, 64);
                material.ShaderName = BinaryReaderExtensions.ReadUnicodeFixedString(br);
            }

            material.MaterialFlags = br.ReadInt32();
            material.MaterialVariant = br.ReadByte();

            int shaderCount = br.ReadInt32();
            int textureCount = br.ReadInt32();
            int lightCount = br.ReadInt32();

            // ---- Shader params ----
            for (int p = 0; p < shaderCount; p++)
            {
                string slot = BinaryReaderExtensions.ReadAsciiZString(br.ReadBytes(16));

                // Read 9 floats; decode first 5 as uint32 keys/type via float-bit-patterns
                float f0 = br.ReadSingle();
                float f1 = br.ReadSingle();
                float f2 = br.ReadSingle();
                float f3 = br.ReadSingle();
                float f4 = br.ReadSingle();
                float vx = br.ReadSingle();
                float vy = br.ReadSingle();
                float vz = br.ReadSingle();
                float vw = br.ReadSingle();

                material.Shaders.Add(new MaterialShader
                {
                    Slot = slot,
                    Key0 = BitConverter.SingleToUInt32Bits(f0),
                    Key1 = BitConverter.SingleToUInt32Bits(f1),
                    Key2 = BitConverter.SingleToUInt32Bits(f2),
                    Key3 = BitConverter.SingleToUInt32Bits(f3),
                    ValueType = (ShaderValueType)BitConverter.SingleToUInt32Bits(f4),
                    Value = new Quaternion(vx, vy, vz, vw)
                });
            }

            // ---- Texture bindings ----
            for (int t = 0; t < textureCount; t++)
            {
                long tstart = br.BaseStream.Position;

                string slot = BinaryReaderExtensions.ReadAsciiZString(br.ReadBytes(16));
                uint key0 = br.ReadUInt32();
                uint key1 = br.ReadUInt32();
                uint key2 = br.ReadUInt32();
                ushort key3Lo = br.ReadUInt16();
                ushort key3Hi = br.ReadUInt16();
                var texture = BinaryReaderExtensions.ReadUnicodeFixedString(br,256);

                long bytesRead = br.BaseStream.Position - tstart;
                var bytesRemaining = 544 - bytesRead;

                var payload = br.ReadBytes((int)bytesRemaining);

                material.Textures.Add(new MaterialTexture
                {
                    Slot = slot,
                    Key0 = key0,
                    Key1 = key1,
                    Key2 = key2,
                    Key3Lo = key3Lo,
                    Key3Hi = key3Hi,
                    TexturePath = texture,
                    Payload = payload
                });
            }


            // ---- Light bindings ----
            for (int t = 0; t < lightCount; t++)
            {
                var lr = new MaterialLight
                {
                    Semantic = BinaryReaderExtensions.ReadAsciiZString(br.ReadBytes(16)),
                    Key0 = br.ReadUInt32(),
                    Key1 = br.ReadUInt32(),
                    Key2 = br.ReadUInt32(),
                    Key3Lo = br.ReadUInt16(),
                    Key3Hi = br.ReadUInt16(),
                    Basis18 = new float[18]
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

        // ---- local helpers ----
        static (Vector3 v, uint tag) ReadVec3Tag(BinaryReader br)
        {
            float x = br.ReadSingle();
            float y = br.ReadSingle();
            float z = br.ReadSingle();
            uint tag = br.ReadUInt32();
            return (new Vector3(x, y, z), tag);
        }
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
            objectName = BinaryReaderExtensions.ReadUnicodeFixedString(br);
            groupName = BinaryReaderExtensions.ReadUnicodeFixedString(br);
            objectName2 = BinaryReaderExtensions.ReadUnicodeFixedString(br);
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
