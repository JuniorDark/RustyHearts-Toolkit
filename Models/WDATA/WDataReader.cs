using static RHToolkit.Models.ObservablePrimitives;

namespace RHToolkit.Models.WDATA;

/// <summary>
/// Read and parse Rusty Hearts binary <c>.wdata</c> files.
/// </summary>
public static class WDataReader
{
    private const string FILE_HEADER = "stairwaygames.";

    public static async Task<WData> ReadAsync(
        string path,
        WData? wData = null,
        CancellationToken ct = default)
    {
        byte[] bytes = await File.ReadAllBytesAsync(path, ct)
                                  .ConfigureAwait(false);

        using var ms = new MemoryStream(bytes, writable: false);
        using var br = new BinaryReader(ms, Encoding.UTF8, leaveOpen: false);

        return ReadWdata(br, wData);
    }

    private static WData ReadWdata(BinaryReader br, WData? wData)
    {
        string header = ReadWString(br);

        if (header != FILE_HEADER)
        {
            throw new InvalidDataException($"{string.Format(Resources.InvalidFileDesc, "WData")}");
        }

        int nVersion = br.ReadInt32();

        wData = new WData
        {
            Version = nVersion
        };

        if (nVersion >= 7)
        {
            wData.EventBoxVersion = br.ReadInt32();
            wData.AniBGVersion = br.ReadInt32();
            wData.ItemBoxVersion = br.ReadInt32();
        }
        if (nVersion >= 8)
        {
            wData.GimmickVersion = br.ReadInt32();
        }
        if (nVersion >= 9)
        {
            wData.ScriptCount1 = br.ReadInt32();
        }
        if (nVersion >= 16)
        {
            wData.ScriptConditionCount1 = br.ReadInt32();
        }
        if (nVersion >= 18)
        {
            wData.ScriptCount2 = br.ReadInt32();
            wData.ScriptConditionCount2 = br.ReadInt32();
        }

        /* 1) Paths */
        wData.ModelPath = ReadWString(br, emptyIfDot: true);
        wData.NavMeshPath = ReadWString(br, emptyIfDot: true);
        if (wData.Version >= 2)
            wData.NavHeightPath = ReadWString(br, emptyIfDot: true);
        wData.EventBoxPath = ReadWString(br, emptyIfDot: true);

        /* 2) EventBoxes */
        var evBoxes = ReadEventBoxes(br, wData.EventBoxVersion, wData.Version, wData);
        foreach (EventBoxType t in Enum.GetValues<EventBoxType>())
        {
            var grp = new EventBoxGroup { Type = t };
            foreach (var box in evBoxes.Where(b => b.Type == t))
                grp.Boxes.Add(box);
            wData.EventBoxGroups.Add(grp);
        }

        /* 3) AniBG, ItemBoxes, Gimmicks */
        ReadAniBGSection(br, wData);
        ReadItemBoxSection(br, wData);
        ReadGimmickSection(br, wData);

        /* 4) obstacle path (v2+) */
        if (wData.Version >= 2)
            wData.ObstaclePath = ReadWString(br, emptyIfDot: true);

        /* 5) Misc. trailing paths  */
        wData.MocPath = ReadWString(br, emptyIfDot: true);
        wData.AniBGPath = ReadWString(br, emptyIfDot: true);

        /* 6) Trigger tables, Scenes, Scene-resources */
        ReadTriggers(br, wData);
        ReadScenes(br, wData);
        ReadSceneResources(br, wData);

        return wData;
    }

    #region Helpers

    /// <summary>
    /// Read a 16-bit length-prefixed Unicode string (UTF-16LE) from the binary stream.
    /// </summary>
    /// <param name="br"></param>
    /// <param name="emptyIfDot"></param>
    /// <returns> Returns the string read from the stream, or an empty string if the read value is ".\".</returns>
    private static string ReadWString(BinaryReader br, bool emptyIfDot = false)
    {
        int charCount = br.ReadUInt16();
        if (charCount == 0) return string.Empty;

        var bytes = br.ReadBytes(charCount * 2);
        var s = Encoding.Unicode.GetString(bytes).TrimEnd('\0');

        return emptyIfDot && s == ".\\" ? string.Empty : s;
    }

    /// <summary>
    /// Read Oriented Bounding Box (OBB).
    /// </summary>
    private static void ReadOBB(BinaryReader br,
                                out string name,
                                out Vector3 pos,
                                out Vector3 scale,
                                out Quaternion rot,
                                out Vector3 ext)
    {
        name = ReadWString(br);
        pos = new() { X = br.ReadSingle(), Y = br.ReadSingle(), Z = br.ReadSingle() };
        scale = new() { X = br.ReadSingle(), Y = br.ReadSingle(), Z = br.ReadSingle() };
        rot = new() { X = br.ReadSingle(), Y = br.ReadSingle(), Z = br.ReadSingle(), W = br.ReadSingle() };
        ext = new() { X = br.ReadSingle(), Y = br.ReadSingle(), Z = br.ReadSingle() };
    }
    #endregion

    #region EventBoxes

    /// <summary>
    /// Reads the eventboxes section of the WData file.
    /// </summary>
    /// <param name="br"></param>
    /// <param name="evVer"></param>
    /// <param name="mainVer"></param>
    /// <param name="m"></param>
    /// <returns>returns a list of <see cref="EventBox"/> instances.</returns>
    /// <exception cref="InvalidDataException"></exception>
    private static List<EventBox> ReadEventBoxes(BinaryReader br, int evVer, int mainVer, WData m)
    {
        var list = new List<EventBox>();
        if (mainVer < 7 || br.BaseStream.Length - br.BaseStream.Position < 4)
            return list;

        int types = br.ReadInt32();
        var table = new List<(int Offset, int Type, int Count)>();
        for (int i = 0; i < types; i++)
            table.Add((br.ReadInt32(), i, br.ReadInt32()));
        table.Sort((a, b) => a.Offset.CompareTo(b.Offset));

        foreach (var (offset, type, count) in table)
        {
            if (count == 0) continue;
            br.BaseStream.Seek(offset, SeekOrigin.Begin);
            for (int j = 0; j < count; j++)
            {
                EventBox box = type switch
                {
                    0 => ReadCameraBox(br, evVer, (EventBoxType)type, m),
                    1 => ReadRespawnBox(br, (EventBoxType)type),
                    2 => ReadStartPointBox(br, (EventBoxType)type),
                    3 => ReadTriggerBox(br, evVer, (EventBoxType)type),
                    4 => ReadSkidBox(br, (EventBoxType)type),
                    5 => ReadEventHitBox(br, (EventBoxType)type),
                    6 => ReadNpcBox(br, (EventBoxType)type),
                    7 => ReadPortalBox(br, (EventBoxType)type),
                    8 => ReadSelectMapPortalBox(br, (EventBoxType)type),
                    9 => ReadInAreaBox(br, (EventBoxType)type),
                    10 => ReadEtcBox(br, (EventBoxType)type),
                    11 => ReadCameraBlockBox(br, (EventBoxType)type),
                    12 => ReadCutoffBox(br, (EventBoxType)type),
                    13 => ReadCameraTargetBox(br, evVer, (EventBoxType)type),
                    15 => ReadMiniMapIconBox(br, (EventBoxType)type),
                    16 => ReadEnvironmentReverbBox(br, (EventBoxType)type),
                    17 => ReadWaypointBox(br, (EventBoxType)type),
                    18 => ReadObstacleBox(br, (EventBoxType)type),
                    _ => throw new InvalidDataException($"Unknown eventbox type: {type}")
                };
                list.Add(box);
            }
        }
        return list;
    }

    // 0: CameraBox
    private static CameraBox ReadCameraBox(BinaryReader br, int evVer, EventBoxType t, WData m)
    {
        ReadOBB(br, out string name, out var pos, out var scl, out var rot, out var ext);
        var box = new CameraBox
        {
            Name = name,
            Position = pos,
            Scale = scl,
            Rotation = rot,
            Extents = ext,
            Type = t,
            Category = evVer >= 7 ? br.ReadUInt32() : 0
        };

        // Read the camera info entries
        for (int idx = 0; idx < 4; idx++)
        {
            var ci = new CameraInfo();
            if (evVer < 3)
            {
                if (idx == 0) ci.CameraTarget = ReadWString(br);
            }
            else
            {
                ci.CameraTarget = ReadWString(br);
            }
            ci.CameraName = ReadWString(br);
            float[] f = new float[21];
            for (int i = 0; i < 21; i++) f[i] = br.ReadSingle();
            ci.CameraPos = new Vector3 { X = f[0], Y = f[1], Z = f[2] };
            ci.CameraRot = new Vector3 { X = f[3], Y = f[4], Z = f[5] };
            ci.FOV = f[6];
            ci.Frustum = [.. f.Skip(7).Take(14).Select(value => new Float { Value = value })];
            if (evVer >= 5) ci.BuildPVS = br.ReadUInt32() != 0;
            if (evVer >= 4)
            {
                uint pvsBgCnt = br.ReadUInt32();
                uint pvsEvCnt = br.ReadUInt32();
                uint renderBgCnt = br.ReadUInt32();
                uint renderAniCnt = br.ReadUInt32();
                uint renderItemCnt = br.ReadUInt32();
                uint renderGimmickCnt = br.ReadUInt32();
                uint noRenderBgCnt = br.ReadUInt32();
                uint noRenderAniCnt = br.ReadUInt32();
                uint noRenderItemCnt = br.ReadUInt32();
                uint noRenderGimmickCnt = br.ReadUInt32();
                uint buildPosCnt = br.ReadUInt32();

                static void ReadStringList(BinaryReader r, ICollection<Entry> sink, bool withId)
                {
                    uint cat = 0;
                    if (withId)
                    {
                        cat = r.ReadUInt32();
                    }
                    sink.Add(new() { Name = ReadWString(r), ID = cat });
                }

                for (int i = 0; i < pvsBgCnt; i++) ci.PVS_BG.Add(new UInt { ID = br.ReadUInt32() });
                if (evVer < 6)
                {
                    for (int i = 0; i < pvsEvCnt; i++) ci.PVS_EventBox.Add(new UInt { ID = br.ReadUInt32() });
                }
                else
                {
                    for (int i = 0; i < pvsEvCnt; i++)
                    {
                        uint cat = br.ReadUInt32();
                        string nm = ReadWString(br);
                        ci.PVS_EventEntries.Add(new Entry { ID = cat, Name = nm });
                    }
                }

                for (int i = 0; i < renderBgCnt; i++) ReadStringList(br, ci.RenderBg, m.Version >= 22);
                for (int i = 0; i < renderAniCnt; i++) ReadStringList(br, ci.RenderAniBg, m.Version >= 22);
                for (int i = 0; i < renderItemCnt; i++) ReadStringList(br, ci.RenderItem, m.Version >= 22);
                for (int i = 0; i < renderGimmickCnt; i++) ReadStringList(br, ci.RenderGimmick, m.Version >= 22);
                for (int i = 0; i < noRenderBgCnt; i++) ReadStringList(br, ci.NoRenderBg, m.Version >= 22);
                for (int i = 0; i < noRenderAniCnt; i++) ReadStringList(br, ci.NoRenderAniBg, m.Version >= 22);
                for (int i = 0; i < noRenderItemCnt; i++) ReadStringList(br, ci.NoRenderItem, m.Version >= 22);
                for (int i = 0; i < noRenderGimmickCnt; i++) ReadStringList(br, ci.NoRenderGimmick, m.Version >= 22);

                for (int i = 0; i < buildPosCnt; i++)
                {
                    var p = new Vector3 { X = br.ReadSingle(), Y = br.ReadSingle(), Z = br.ReadSingle() };
                    ci.BuildPos.Add(p);
                }
            }
            box.CameraInfos.Add(ci);
        }
        return box;
    }

    // 1: RespawnBox
    private static RespawnBox ReadRespawnBox(BinaryReader br, EventBoxType type)
    {

        ReadOBB(br, out string name, out var pos, out var scl, out var rot, out var ext);

        var box = new RespawnBox
        {
            Name = name,
            Position = pos,
            Scale = scl,
            Rotation = rot,
            Extents = ext,
            Type = type,
            TotalEnemyNum = br.ReadInt32(),
            EnemyNum = br.ReadInt32(),
            EnemyName = ReadWString(br),
            RespawnTime = br.ReadSingle(),
            RespawnMotion = ReadWString(br),
            InCheck = br.ReadInt32() != 0,
            RandomDirection = br.ReadInt32() != 0
        };

        for (int i = 0; i < 5; i++)
            box.Difficulty.Add(new Bool { Value = br.ReadInt32() != 0 });
        return box;
    }

    // 2: StartPointBox
    private static StartPointBox ReadStartPointBox(BinaryReader br, EventBoxType type)
    {
        ReadOBB(br, out string name, out var pos, out var scl, out var rot, out var ext);

        return new StartPointBox
        {
            Name = name,
            Position = pos,
            Scale = scl,
            Rotation = rot,
            Extents = ext,
            Type = type,
            ID = br.ReadInt32()
        };
    }

    // 3: TriggerBox
    private static TriggerBox ReadTriggerBox(BinaryReader br, int evVersion, EventBoxType type)
    {
        ReadOBB(br, out string name, out var pos, out var scl, out var rot, out var ext);

        var box = new TriggerBox
        {
            Name = name,
            Position = pos,
            Scale = scl,
            Rotation = rot,
            Extents = ext,
            Type = type,
            State = br.ReadInt32(),
            ActionMotion = ReadWString(br),
            Motion = ReadWString(br)
        };
        if (evVersion >= 9)
        {
            box.SignpostTextID = br.ReadInt32();
            var signpostReposition = new Vector3
            {
                X = br.ReadSingle(),
                Y = br.ReadSingle(),
                Z = br.ReadSingle()
            };
            box.SignpostReposition = signpostReposition;
        }
        return box;
    }

    // 4: SkidBox
    private static SkidBox ReadSkidBox(BinaryReader br, EventBoxType type)
    {
        ReadOBB(br, out string name, out var pos, out var scl, out var rot, out var ext);

        return new SkidBox
        {
            Name = name,
            Position = pos,
            Scale = scl,
            Rotation = rot,
            Extents = ext,
            Type = type,
            State = br.ReadInt32(),
            Velocity = new Vector3
            {
                X = br.ReadSingle(),
                Y = br.ReadSingle(),
                Z = br.ReadSingle()
            },
            EndTime = br.ReadSingle(),
            Duration = br.ReadSingle()
        };
    }

    // 5: EventHitBox
    private static EventHitBox ReadEventHitBox(BinaryReader br, EventBoxType type)
    {
        ReadOBB(br, out string name, out var pos, out var scl, out var rot, out var ext);

        var box = new EventHitBox
        {
            Name = name,
            Position = pos,
            Scale = scl,
            Rotation = rot,
            Extents = ext,
            Type = type,
            // 1) nState (u32)
            State = br.ReadInt32(),

            // 2) strAniBGName (wstring)
            AniBGName = ReadWString(br),

            // 3) fDamage (f32)
            Damage = br.ReadSingle(),

            // 4) vecDirection (3 × f32)
            Direction = new Vector3
            {
                X = br.ReadSingle(),
                Y = br.ReadSingle(),
                Z = br.ReadSingle()
            },

            // 5) strDamageMotion (wstring)
            DamageMotion = ReadWString(br)
        };

        // 6) counts (u32,u32)
        int hitCount = br.ReadInt32();
        int tempHitCount = br.ReadInt32();

        // 7) hit times (f32[])
        for (int i = 0; i < hitCount; i++)
            box.HitTime.Add(new Float { Value = br.ReadSingle() });

        // 8) temp hit times (f32[])
        for (int i = 0; i < tempHitCount; i++)
            box.TempHitTime.Add(new Float { Value = br.ReadSingle() });

        return box;
    }

    // 6: NpcBox
    private static NpcBox ReadNpcBox(BinaryReader br, EventBoxType type)
    {
        ReadOBB(br, out string name, out var pos, out var scl, out var rot, out var ext);

        return new NpcBox
        {
            Name = name,
            Position = pos,
            Scale = scl,
            Rotation = rot,
            Extents = ext,
            Type = type,
            NpcName = ReadWString(br),
            ID = br.ReadInt32(),
            InstanceID = br.ReadInt32()
        };
    }

    // 7: PortalBox
    private static PortalBox ReadPortalBox(BinaryReader br, EventBoxType type)
    {
        ReadOBB(br, out string name, out var pos, out var scl, out var rot, out var ext);

        return new PortalBox
        {
            Name = name,
            Position = pos,
            Scale = scl,
            Rotation = rot,
            Extents = ext,
            Type = type,
            WarpMapName = ReadWString(br),
            ID = br.ReadInt32(),
            MsgType = br.ReadInt32(),
            WarpMapID = br.ReadInt32(),
            WarpPortalID = br.ReadInt32(),
            Active = br.ReadInt32() != 0
        };
    }

    // 8: SelectMapPortalBox
    private static SelectMapPortalBox ReadSelectMapPortalBox(BinaryReader br, EventBoxType type)
    {
        ReadOBB(br, out string name, out var pos, out var scl, out var rot, out var ext);

        return new SelectMapPortalBox
        {
            Name = name,
            Position = pos,
            Scale = scl,
            Rotation = rot,
            Extents = ext,
            Type = type,
            ID = br.ReadInt32(),
            MsgType = br.ReadInt32(),
            Active = br.ReadInt32() != 0
        };
    }

    // 9: InAreaBox
    private static InAreaBox ReadInAreaBox(BinaryReader br, EventBoxType type)
    {
        ReadOBB(br, out string name, out var pos, out var scl, out var rot, out var ext);

        return new InAreaBox
        {
            Name = name,
            Position = pos,
            Scale = scl,
            Rotation = rot,
            Extents = ext,
            Type = type,
            WarpMapName = ReadWString(br),
            ID = br.ReadInt32(),
            Active = br.ReadInt32() != 0
        };
    }

    // 10: EtcBox
    private static EtcBox ReadEtcBox(BinaryReader br, EventBoxType type)
    {
        ReadOBB(br, out string name, out var pos, out var scl, out var rot, out var ext);

        return new EtcBox
        {
            Name = name,
            Position = pos,
            Scale = scl,
            Rotation = rot,
            Extents = ext,
            Type = type,
            ID = br.ReadInt32()
        };
    }

    // 11: CameraBlockBox
    private static CameraBlockBox ReadCameraBlockBox(BinaryReader br, EventBoxType type)
    {
        ReadOBB(br, out string name, out var pos, out var scl, out var rot, out var ext);

        return new CameraBlockBox
        {
            Name = name,
            Position = pos,
            Scale = scl,
            Rotation = rot,
            Type = type,
            Extents = ext
        };
    }

    // 12: CutoffBox
    private static CutoffBox ReadCutoffBox(BinaryReader br, EventBoxType type)
    {
        ReadOBB(br, out string name, out var pos, out var scl, out var rot, out var ext);

        return new CutoffBox
        {
            Name = name,
            Position = pos,
            Scale = scl,
            Rotation = rot,
            Extents = ext,
            Type = type,
            CutoffType = br.ReadInt32()
        };
    }

    // 13: CameraTargetBox
    private static CameraTargetBox ReadCameraTargetBox(BinaryReader br, int evVersion, EventBoxType type)
    {
        ReadOBB(br, out string name, out var pos, out var scl, out var rot, out var ext);

        var box = new CameraTargetBox
        {
            Name = name,
            Position = pos,
            Scale = scl,
            Rotation = rot,
            Extents = ext,
            Type = type
        };
        if (evVersion < 8)
            box.TargetName = ReadWString(br);
        else
            box.NameTextID = br.ReadInt32();
        box.TargetLocalPC = evVersion >= 2 && br.ReadInt32() != 0;
        return box;
    }

    // 15: MiniMapIconBox
    private static MiniMapIconBox ReadMiniMapIconBox(BinaryReader br, EventBoxType type)
    {
        ReadOBB(br, out string name, out var pos, out var scl, out var rot, out var ext);

        return new MiniMapIconBox
        {
            Name = name,
            Position = pos,
            Scale = scl,
            Rotation = rot,
            Extents = ext,
            Type = type,
            IconType = br.ReadInt32()
        };
    }

    // 16: EnvironmentReverbBox
    private static EnvironmentReverbBox ReadEnvironmentReverbBox(BinaryReader br, EventBoxType type)
    {
        ReadOBB(br, out string name, out var pos, out var scl, out var rot, out var ext);

        return new EnvironmentReverbBox
        {
            Name = name,
            Position = pos,
            Scale = scl,
            Rotation = rot,
            Extents = ext,
            Type = type,
            ReverbType = br.ReadInt32()
        };
    }

    // 17: WaypointBox
    private static WaypointBox ReadWaypointBox(BinaryReader br, EventBoxType type)
    {
        ReadOBB(br, out string name, out var pos, out var scl, out var rot, out var ext);

        var box = new WaypointBox
        {
            Name = name,
            Position = pos,
            Scale = scl,
            Rotation = rot,
            Extents = ext,
            Type = type,
            ID = br.ReadInt32(),
            Range = br.ReadSingle()
        };
        int lc = br.ReadInt32();
        for (int i = 0; i < lc; i++) box.Links.Add(new Int { ID = br.ReadInt32() });
        for (int i = 0; i < lc; i++) box.LinkDistances.Add(new Float { Value = br.ReadSingle() });
        return box;
    }

    // 18: ObstacleBox
    private static ObstacleBox ReadObstacleBox(BinaryReader br, EventBoxType type)
    {
        ReadOBB(br, out string name, out var pos, out var scl, out var rot, out var ext);

        return new ObstacleBox
        {
            Name = name,
            Position = pos,
            Scale = scl,
            Rotation = rot,
            Extents = ext,
            Type = type,
        };
    }
    #endregion

    #region AniBG, ItemBox, Gimmick
    private static void ReadAniBGSection(BinaryReader br, WData m)
    {
        int cnt = br.ReadInt32();
        for (int i = 0; i < cnt; i++)
        {
            ReadOBB(br, out string name, out var pos, out var scl, out var rot, out var ext);
            string model = ReadWString(br);
            string motion = ReadWString(br);
            bool loop = br.ReadInt32() != 0;
            int light = br.ReadInt32();
            int cover = br.ReadInt32();
            bool shadow = m.AniBGVersion >= 3 && br.ReadInt32() != 0;
            bool moveW = m.AniBGVersion >= 4 && br.ReadInt32() != 0;
            float pvsRad = m.AniBGVersion >= 5 ? br.ReadSingle() : 0f;
            m.AniBGs.Add(new AniBG
            {
                Name = name,
                Model = model,
                Motion = motion,
                Loop = loop,
                LightIndex = light,
                CoverIndex = cover,
                Shadow = shadow,
                MoveWeight = moveW,
                PVSRad = pvsRad,
                Position = pos,
                Scale = scl,
                Rotation = rot,
                Extents = ext
            });
        }
    }

    private static void ReadItemBoxSection(BinaryReader br, WData m)
    {
        int cnt = br.ReadInt32();
        for (int i = 0; i < cnt; i++)
        {
            ReadOBB(br, out string name, out var pos, out var scl, out var rot, out var ext);
            string model = ReadWString(br);
            string motion = ReadWString(br);
            string table = ReadWString(br);
            bool loop = br.ReadInt32() != 0;
            bool openE = m.ItemBoxVersion >= 3 && br.ReadInt32() != 0;
            m.ItemBoxes.Add(new ItemBox
            {
                Name = name,
                Model = model,
                Motion = motion,
                TablePath = table,
                Loop = loop,
                OpenEnable = openE,
                Position = pos,
                Scale = scl,
                Rotation = rot,
                Extents = ext
            });
        }
    }

    private static void ReadGimmickSection(BinaryReader br, WData m)
    {
        int cnt = br.ReadInt32();
        for (int i = 0; i < cnt; i++)
        {
            ReadOBB(br, out string name, out var pos, out var scl, out var rot, out var ext);
            var g = new Gimmick
            {
                Name = name,
                Model = ReadWString(br),
                Motion = ReadWString(br),
                LoopFlag = br.ReadInt32(),
                LightIndex = br.ReadInt32(),
                Cover = br.ReadInt32(),
                Shadow = br.ReadInt32(),
                MoveWeight = br.ReadInt32(),
                TemplateID = br.ReadInt32(),
                Position = pos,
                Scale = scl,
                Rotation = rot,
                Extents = ext
            };
            m.Gimmicks.Add(g);
        }
    }

    private static void ReadTriggers(BinaryReader br, WData m)
    {
        _ = br.ReadInt32(); // reserved0
        _ = ReadWString(br, emptyIfDot: true);
        _ = br.ReadInt32(); // reserved1
        var mainScript = ReadWString(br);
        int triggerCnt = br.ReadInt32();
        var root = new TriggerElement { MainScript = mainScript };
        for (int i = 0; i < triggerCnt; i++)
        {
            var tr = new Triggers { Name = ReadWString(br), Comment = ReadWString(br) };
            int evCnt = br.ReadInt32();
            int condCnt = br.ReadInt32();
            int actCnt = br.ReadInt32();
            for (int j = 0; j < evCnt; j++) tr.Events.Add(new StringModel { Name = ReadWString(br) });
            for (int j = 0; j < condCnt; j++) tr.Conditions.Add(new StringModel { Name = ReadWString(br) });
            for (int j = 0; j < actCnt; j++) tr.Actions.Add(new StringModel { Name = ReadWString(br) });
            root.Triggers.Add(tr);
        }
        m.Triggers.Add(root);
    }

    #endregion

    #region Scenes (Client Only)

    private static void ReadScenes(BinaryReader br, WData m)
    {
        int count = br.ReadInt32();
        for (int i = 0; i < count; i++)
        {
            string path = ReadWString(br);
            float fadeIn = br.ReadSingle();
            float fadeHold = br.ReadSingle();
            float fadeOut = br.ReadSingle();

            uint cat = 0;
            float[] scene = new float[6];
            if (m.Version >= 15)
            {
                cat = br.ReadUInt32();
                for (int j = 0; j < 6; j++) scene[j] = br.ReadSingle();
            }
            Vector3 pos = new();
            Vector3 rot = new();
            float fov = 0f;
            float asp = 0f;
            if (m.Version >= 19)
            {
                pos = new Vector3 { X = br.ReadSingle(), Y = br.ReadSingle(), Z = br.ReadSingle() };
                rot = new Vector3 { X = br.ReadSingle(), Y = br.ReadSingle(), Z = br.ReadSingle() };
                fov = br.ReadSingle();
                asp = br.ReadSingle();
            }
            
            m.Scenes.Add(new Scene
            {
                File = path,
                FadeInPreview = fadeIn,
                FadeHoldPreview = fadeHold,
                FadeOutPreview = fadeOut,
                Category = cat,
                SceneFadeIn = scene[0],
                SceneFadeHold = scene[1],
                SceneFadeOut = scene[2],
                BlendTime = scene[3],
                FogNear = scene[4],
                FogFar = scene[5],
                Position = pos,
                Rotation = rot,
                FOV = fov,
                AspectRatio = asp
            });
        }

        if (m.Version >= 20)
        {
            for (int i = 0; i < count; i++)
            {
                var cam = m.Scenes[i];
                if (m.Version >= 21)
                {
                    cam.Name = ReadWString(br);
                }
                int entryCnt = br.ReadInt32();
                int sceneIdCnt = br.ReadInt32();
                int rBgUCnt = br.ReadInt32();
                int rAniUCnt = br.ReadInt32();
                int rItemUCnt = br.ReadInt32();
                int rGimUCnt = br.ReadInt32();
                int nBgUCnt = br.ReadInt32();
                int nAniUCnt = br.ReadInt32();
                int nItemUCnt = br.ReadInt32();
                int nGimU = br.ReadInt32();
                cam.IndexList = new List<SceneIndex>(sceneIdCnt);
                for (int j = 0; j < sceneIdCnt; j++) cam.IndexList.Add(new SceneIndex { ID = br.ReadUInt32() });
                for (int j = 0; j < entryCnt; j++)
                    cam.EventScenes.Add(new SceneElement { Category = br.ReadUInt32(), Key = ReadWString(br) });
                void ReadRender(int cnt, ICollection<SceneElement> sink) { for (int n = 0; n < cnt; n++) sink.Add(new SceneElement { Category = (m.Version >= 22) ? br.ReadUInt32() : 0, Key = ReadWString(br) }); }
                ReadRender(rBgUCnt, cam.RenderBgUser);
                ReadRender(rAniUCnt, cam.RenderAniBgUser);
                ReadRender(rItemUCnt, cam.RenderItemBoxUser);
                ReadRender(rGimUCnt, cam.RenderGimmickUser);
                ReadRender(nBgUCnt, cam.NoRenderBgUser);
                ReadRender(nAniUCnt, cam.NoRenderAniBgUser);
                ReadRender(nItemUCnt, cam.NoRenderItemBoxUser);
                ReadRender(nGimU, cam.NoRenderGimmickUser);
            }
        }
    }

    private static void ReadSceneResources(BinaryReader br, WData m)
    {
        int entryCnt = br.ReadInt32();
        for (int i = 0; i < entryCnt; i++)
        {
            var e = new SceneResource
            {
                Key = ReadWString(br)
            };
            int aliasCnt = br.ReadInt32();
            for (int a = 0; a < aliasCnt; a++) e.Aliases.Add(new StringModel { Name = ReadWString(br) });
            int pathCnt = br.ReadInt32();
            e.Delay = pathCnt > 0 ? br.ReadSingle() : 0f;
            for (int p = 0; p < pathCnt; p++)
            {
                var sd = new SceneData
                {
                    Model = ReadWString(br),
                    Motion = ReadWString(br),
                    Name = ReadWString(br),
                    EventName = ReadWString(br),
                    Time = (m.Version >= 10) ? br.ReadUInt32() : 0,
                    Hold = (m.Version >= 10) ? br.ReadUInt32() : 0
                };
                if (p < pathCnt - 1)
                    sd.BlendTime = br.ReadSingle();
                e.Paths.Add(sd);
            }

            if (m.Version >= 11)
            {
                int cueCnt = br.ReadInt32();
                for (int c = 0; c < cueCnt; c++)
                    e.Cues.Add(new Cue { End = br.ReadSingle(), Name = ReadWString(br), ID = br.ReadUInt32(), Start = br.ReadSingle() });
            }

            if (m.Version >= 12)
            {
                int soundCnt = br.ReadInt32();
                for (int s = 0; s < soundCnt; s++)
                    e.Sounds.Add(new SoundRecord
                    {
                        Start = br.ReadSingle(),
                        FadeIn = (m.Version >= 16) ? br.ReadSingle() : 0,
                        FadeOut = (m.Version >= 16) ? br.ReadSingle() : 0,
                        VolMax = (m.Version >= 16) ? br.ReadSingle() : 0,
                        VolMin = (m.Version >= 16) ? br.ReadSingle() : 0,
                        Path = ReadWString(br)
                    });

                if (m.Version >= 13)
                {
                    e.Unk1 = br.ReadUInt32();
                }
                
                e.Unk2 = br.ReadUInt32();
                e.Unk3 = br.ReadSingle();
                e.Unk4 = br.ReadUInt32();

                if (m.Version <= 14)
                {
                    br.ReadInt32();
                    br.ReadInt32();
                    br.ReadInt32();
                    br.ReadInt32();
                    br.ReadInt32();
                    br.ReadInt32();
                    br.ReadInt32();
                }
                if (m.Version <= 13)
                {
                    ReadWString(br);
                    ReadWString(br);
                }
            }

            if (m.Version >= 17)
            {
                int ambientCnt = br.ReadInt32();
                for (int a = 0; a < ambientCnt; a++)
                    e.Ambients.Add(new AmbientRecord { Start = br.ReadSingle(), Path = ReadWString(br), PlayOnStart = br.ReadInt32() != 0, Loop = br.ReadInt32() != 0 });
                m.SceneResources.Add(e);
            } 
        }
    }

    #endregion
}
