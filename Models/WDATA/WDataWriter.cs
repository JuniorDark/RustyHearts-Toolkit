using static RHToolkit.Models.ObservablePrimitives;

namespace RHToolkit.Models.WDATA;

/// <summary>
/// Writes WData objects to a byte array in the WDATA format.
/// </summary>
public static class WDataWriter
{
    private const string FILE_HEADER = "stairwaygames.";

    public static byte[] Write(WData wData)
    {
        ArgumentNullException.ThrowIfNull(wData);

        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms, Encoding.Unicode, leaveOpen: true);

        WriteHeader(bw, wData);
        WritePaths(bw, wData);
        WriteEventBoxes(bw, wData);
        WriteAniBGs(bw, wData);
        WriteItemBoxes(bw, wData);
        WriteGimmicks(bw, wData);

        if (wData.Version >= 2)
            WriteWString(bw, wData.ObstaclePath, dotIfEmpty: true);

        WriteWString(bw, wData.MocPath, dotIfEmpty: true);
        WriteWString(bw, wData.AniBGPath, dotIfEmpty: true);
        WriteTriggers(bw, wData);
        WriteScenes(bw, wData);
        WriteSceneResources(bw, wData);
        bw.Flush();

        return ms.ToArray();
    }

    #region Helpers
    private static readonly byte[] DotSlashUtf16 = [0x2E, 0x00, 0x5C, 0x00]; // ".\"

    private static void WriteWString(BinaryWriter bw,
                                     string? value,
                                     bool dotIfEmpty = false)
    {
        if (string.IsNullOrEmpty(value))
        {
            if (dotIfEmpty)
            {
                bw.Write((ushort)2);
                bw.Write(DotSlashUtf16);
            }
            else
            {
                bw.Write((ushort)0);
            }
            return;
        }

        bw.Write((ushort)value.Length);
        bw.Write(Encoding.Unicode.GetBytes(value));
    }

    private static void WriteOBB(BinaryWriter bw, string name, Vector3 pos, Vector3 scale, Quaternion rot, Vector3 ext)
    {
        WriteWString(bw, name);
        bw.Write(pos.X); bw.Write(pos.Y); bw.Write(pos.Z);
        bw.Write(scale.X); bw.Write(scale.Y); bw.Write(scale.Z);
        bw.Write(rot.X); bw.Write(rot.Y); bw.Write(rot.Z); bw.Write(rot.W);
        bw.Write(ext.X); bw.Write(ext.Y); bw.Write(ext.Z);
    }
    #endregion

    #region Header/Path writers
    private static void WriteHeader(BinaryWriter bw, WData wData)
    {
        WriteWString(bw, FILE_HEADER);
        bw.Write(wData.Version);
        if (wData.Version >= 7)
        {
            bw.Write(wData.EventBoxVersion);
            bw.Write(wData.AniBGVersion);
            bw.Write(wData.ItemBoxVersion);
        }
        if (wData.Version >= 8) bw.Write(wData.GimmickVersion);
        if (wData.Version >= 9) bw.Write(0);
        if (wData.Version >= 16) bw.Write(0);
        if (wData.Version >= 18) { bw.Write(0); bw.Write(0); }
    }

    private static void WritePaths(BinaryWriter bw, WData wData)
    {
        WriteWString(bw, wData.ModelPath, dotIfEmpty: true);
        WriteWString(bw, wData.NavMeshPath, dotIfEmpty: true);
        if (wData.Version >= 2)
            WriteWString(bw, wData.NavHeightPath, dotIfEmpty: true);
        WriteWString(bw, wData.EventBoxPath, dotIfEmpty: true);
    }
    #endregion

    #region EventBox writers

    /// <summary>
    /// Writes all event boxes to the WDATA file.
    /// </summary>
    private static void WriteEventBoxes(BinaryWriter bw, WData wData)
    {
        /* 1) gather groups ordered by enum value */
        var ordered = Enum.GetValues<EventBoxType>()
                          .Select(t => wData.EventBoxGroups.FirstOrDefault(g => g.Type == t) ?? new EventBoxGroup { Type = t })
                          .ToList();
        int typeCount = ordered.Count;
        long tableStart = bw.BaseStream.Position;
        bw.Write(typeCount);
        // reserve space for (offset,count) per type
        for (int i = 0; i < typeCount; i++) { bw.Write(0); bw.Write(0); }

        /* 2) write blocks & capture offsets */
        var offsets = new int[typeCount];
        for (int i = 0; i < typeCount; i++)
        {
            offsets[i] = (int)bw.BaseStream.Position;
            var grp = ordered[i];
            foreach (var box in grp.Boxes)
            {
                switch (box.Type)
                {
                    case EventBoxType.CameraBox: WriteCameraBox((CameraBox)box, bw, wData); break;
                    case EventBoxType.RespawnBox: WriteRespawnBox((RespawnBox)box, bw); break;
                    case EventBoxType.StartPointBox: WriteStartPointBox((StartPointBox)box, bw); break;
                    case EventBoxType.TriggerBox: WriteTriggerBox((TriggerBox)box, bw, wData); break;
                    case EventBoxType.SkidBox: WriteSkidBox((SkidBox)box, bw); break;
                    case EventBoxType.EventHitBox: WriteEventHitBox((EventHitBox)box, bw); break;
                    case EventBoxType.NpcBox: WriteNpcBox((NpcBox)box, bw); break;
                    case EventBoxType.PortalBox: WritePortalBox((PortalBox)box, bw); break;
                    case EventBoxType.SelectMapPortalBox: WriteSelectMapPortalBox((SelectMapPortalBox)box, bw); break;
                    case EventBoxType.InAreaBox: WriteInAreaBox((InAreaBox)box, bw); break;
                    case EventBoxType.EtcBox: WriteEtcBox((EtcBox)box, bw); break;
                    case EventBoxType.CameraBlockBox: WriteCameraBlockBox((CameraBlockBox)box, bw); break;
                    case EventBoxType.CutoffBox: WriteCutoffBox((CutoffBox)box, bw); break;
                    case EventBoxType.CameraTargetBox: WriteCameraTargetBox((CameraTargetBox)box, bw, wData); break;
                    case EventBoxType.MiniMapIconBox: WriteMiniMapIconBox((MiniMapIconBox)box, bw); break;
                    case EventBoxType.EnvironmentReverbBox: WriteEnvironmentReverbBox((EnvironmentReverbBox)box, bw); break;
                    case EventBoxType.WaypointBox: WriteWaypointBox((WaypointBox)box, bw); break;
                    case EventBoxType.ObstacleBox: WriteObstacleBox((ObstacleBox)box, bw); break;
                }
            }
        }
        long end = bw.BaseStream.Position;

        /* 3) patch table */
        bw.BaseStream.Seek(tableStart + 4, SeekOrigin.Begin); // skip typeCount
        for (int i = 0; i < typeCount; i++)
        {
            bw.Write(offsets[i]);
            bw.Write(ordered[i].Boxes.Count);
        }
        bw.BaseStream.Seek(end, SeekOrigin.Begin);
    }

    #region EventBox writers

    private static void WriteCameraBox(CameraBox box, BinaryWriter bw, WData wData)
    {
        WriteOBB(bw, box.Name, box.Position, box.Scale, box.Rotation, box.Extents);
        if (wData.EventBoxVersion >= 7) bw.Write(box.Category);
        var infos = box.CameraInfos.ToList();
        while (infos.Count < 4) infos.Add(new CameraInfo()); // pad
        for (int idx = 0; idx < 4; idx++)
        {
            var ci = infos[idx];
            if (wData.EventBoxVersion < 3)
            {
                if (idx == 0)
                    WriteWString(bw, ci.CameraTarget);
            }
            else
                WriteWString(bw, ci.CameraTarget);
            WriteWString(bw, ci.CameraName);
            bw.Write(ci.CameraPos.X); bw.Write(ci.CameraPos.Y); bw.Write(ci.CameraPos.Z);
            bw.Write(ci.CameraRot.X); bw.Write(ci.CameraRot.Y); bw.Write(ci.CameraRot.Z);
            bw.Write(ci.FOV);
            for (int i = 0; i < 14; i++) bw.Write(ci.Frustum.Count > i ? ci.Frustum[i].Value : 0f);
            if (wData.EventBoxVersion >= 5) bw.Write(ci.BuildPVS ? 1u : 0u);
            
            if (wData.EventBoxVersion >= 4)
            {
                // counts
                bw.Write((uint)ci.PVS_BG.Count);
                bw.Write((uint)(wData.EventBoxVersion < 6 ? ci.PVS_EventBox.Count : ci.PVS_EventEntries.Count));
                bw.Write((uint)ci.RenderBg.Count);
                bw.Write((uint)ci.RenderAniBg.Count);
                bw.Write((uint)ci.RenderItem.Count);
                bw.Write((uint)ci.RenderGimmick.Count);
                bw.Write((uint)ci.NoRenderBg.Count);
                bw.Write((uint)ci.NoRenderAniBg.Count);
                bw.Write((uint)ci.NoRenderItem.Count);
                bw.Write((uint)ci.NoRenderGimmick.Count);
                bw.Write((uint)ci.BuildPos.Count);

                foreach (var u in ci.PVS_BG) bw.Write(u.ID);
                if (wData.EventBoxVersion < 6)
                    foreach (var u in ci.PVS_EventBox) bw.Write(u.ID);
                else
                    foreach (var e in ci.PVS_EventEntries) { bw.Write(e.ID); WriteWString(bw, e.Name); }
                void WriteList(IEnumerable<Entry> list)
                {
                    foreach (var s in list)
                    {
                        if (wData.Version >= 22) bw.Write(s.ID);
                        WriteWString(bw, s.Name);
                    }
                }
                WriteList(ci.RenderBg); WriteList(ci.RenderAniBg); WriteList(ci.RenderItem); WriteList(ci.RenderGimmick);
                WriteList(ci.NoRenderBg); WriteList(ci.NoRenderAniBg); WriteList(ci.NoRenderItem); WriteList(ci.NoRenderGimmick);
                foreach (var v in ci.BuildPos) { bw.Write(v.X); bw.Write(v.Y); bw.Write(v.Z); }
            }
        }
    }

    private static void WriteRespawnBox(RespawnBox box, BinaryWriter bw)
    {
        WriteOBB(bw, box.Name, box.Position, box.Scale, box.Rotation, box.Extents);
        bw.Write(box.TotalEnemyNum);
        bw.Write(box.EnemyNum);
        WriteWString(bw, box.EnemyName);
        bw.Write(box.RespawnTime);
        WriteWString(bw, box.RespawnMotion);
        bw.Write(box.InCheck ? 1 : 0);
        bw.Write(box.RandomDirection ? 1 : 0);
        foreach (var df in box.Difficulty) bw.Write(df.Value ? 1 : 0);
    }

    private static void WriteStartPointBox(StartPointBox box, BinaryWriter bw)
    {
        WriteOBB(bw, box.Name, box.Position, box.Scale, box.Rotation, box.Extents);
        bw.Write(box.ID);
    }

    private static void WriteTriggerBox(TriggerBox box, BinaryWriter bw, WData wData)
    {
        WriteOBB(bw, box.Name, box.Position, box.Scale, box.Rotation, box.Extents);
        bw.Write(box.State);
        WriteWString(bw, box.ActionMotion);
        WriteWString(bw, box.Motion);
        if (wData.EventBoxVersion >= 9)
        {
            bw.Write(box.SignpostTextID);
            bw.Write(box.SignpostReposition.X); bw.Write(box.SignpostReposition.Y); bw.Write(box.SignpostReposition.Z);
        }
    }

    private static void WriteSkidBox(SkidBox box, BinaryWriter bw)
    {
        WriteOBB(bw, box.Name, box.Position, box.Scale, box.Rotation, box.Extents);
        bw.Write(box.State);
        bw.Write(box.Velocity.X); bw.Write(box.Velocity.Y); bw.Write(box.Velocity.Z);
        bw.Write(box.EndTime);
        bw.Write(box.Duration);
    }

    private static void WriteEventHitBox(EventHitBox box, BinaryWriter bw)
    {
        WriteOBB(bw, box.Name, box.Position, box.Scale, box.Rotation, box.Extents);
        bw.Write(box.State);
        WriteWString(bw, box.AniBGName);
        bw.Write(box.Damage);
        bw.Write(box.Direction.X); bw.Write(box.Direction.Y); bw.Write(box.Direction.Z);
        WriteWString(bw, box.DamageMotion);
        bw.Write(box.HitTime.Count);
        bw.Write(box.TempHitTime.Count);
        foreach (var f in box.HitTime) bw.Write(f.Value);
        foreach (var f in box.TempHitTime) bw.Write(f.Value);
    }

    private static void WriteNpcBox(NpcBox box, BinaryWriter bw)
    {
        WriteOBB(bw, box.Name, box.Position, box.Scale, box.Rotation, box.Extents);
        WriteWString(bw, box.NpcName);
        bw.Write(box.ID);
        bw.Write(box.InstanceID);
    }

    private static void WritePortalBox(PortalBox box, BinaryWriter bw)
    {
        WriteOBB(bw, box.Name, box.Position, box.Scale, box.Rotation, box.Extents);
        WriteWString(bw, box.WarpMapName);
        bw.Write(box.ID);
        bw.Write(box.MsgType);
        bw.Write(box.WarpMapID);
        bw.Write(box.WarpPortalID);
        bw.Write(box.Active ? 1 : 0);
    }

    private static void WriteSelectMapPortalBox(SelectMapPortalBox box, BinaryWriter bw)
    {
        WriteOBB(bw, box.Name, box.Position, box.Scale, box.Rotation, box.Extents);
        bw.Write(box.ID);
        bw.Write(box.MsgType);
        bw.Write(box.Active ? 1 : 0);
    }

    private static void WriteInAreaBox(InAreaBox box, BinaryWriter bw)
    {
        WriteOBB(bw, box.Name, box.Position, box.Scale, box.Rotation, box.Extents);
        WriteWString(bw, box.WarpMapName);
        bw.Write(box.ID);
        bw.Write(box.Active ? 1 : 0);
    }

    private static void WriteEtcBox(EtcBox box, BinaryWriter bw)
    {
        WriteOBB(bw, box.Name, box.Position, box.Scale, box.Rotation, box.Extents);
        bw.Write(box.ID);
    }

    private static void WriteCameraBlockBox(CameraBlockBox box, BinaryWriter bw)
    {
        WriteOBB(bw, box.Name, box.Position, box.Scale, box.Rotation, box.Extents);
    }

    private static void WriteCutoffBox(CutoffBox box, BinaryWriter bw)
    {
        WriteOBB(bw, box.Name, box.Position, box.Scale, box.Rotation, box.Extents);
        bw.Write(box.CutoffType);
    }

    private static void WriteCameraTargetBox(CameraTargetBox box, BinaryWriter bw, WData wData)
    {
        WriteOBB(bw, box.Name, box.Position, box.Scale, box.Rotation, box.Extents);
        if (wData.EventBoxVersion < 8)
            WriteWString(bw, string.Empty);
        else
            bw.Write(box.NameTextID);
        if (wData.EventBoxVersion >= 2)
            bw.Write(box.TargetLocalPC ? 1 : 0);
    }

    private static void WriteMiniMapIconBox(MiniMapIconBox box, BinaryWriter bw)
    {
        WriteOBB(bw, box.Name, box.Position, box.Scale, box.Rotation, box.Extents);
        bw.Write(box.IconType);
    }

    private static void WriteEnvironmentReverbBox(EnvironmentReverbBox box, BinaryWriter bw)
    {
        WriteOBB(bw, box.Name, box.Position, box.Scale, box.Rotation, box.Extents);
        bw.Write(box.ReverbType);
    }

    private static void WriteWaypointBox(WaypointBox box, BinaryWriter bw)
    {
        WriteOBB(bw, box.Name, box.Position, box.Scale, box.Rotation, box.Extents);
        bw.Write(box.ID);
        bw.Write(box.Range);
        bw.Write(box.Links.Count);
        foreach (var l in box.Links) bw.Write(l.ID);
        foreach (var d in box.LinkDistances) bw.Write(d.Value);
    }

    private static void WriteObstacleBox(ObstacleBox box, BinaryWriter bw)
    {
        WriteOBB(bw, box.Name, box.Position, box.Scale, box.Rotation, box.Extents);
    }

    #endregion
    
    #endregion

    #region AniBG, ItemBox, Gimmick writers

    private static void WriteAniBGs(BinaryWriter bw, WData wData)
    {
        bw.Write(wData.AniBGs.Count);
        foreach (var a in wData.AniBGs)
        {
            WriteOBB(bw, a.Name, a.Position, a.Scale, a.Rotation, a.Extents);
            WriteWString(bw, a.Model);
            WriteWString(bw, a.Motion);
            bw.Write(a.Loop ? 1 : 0);
            bw.Write(a.LightIndex);
            bw.Write(a.CoverIndex);
            if (wData.AniBGVersion >= 3) bw.Write(a.Shadow ? 1 : 0);
            if (wData.AniBGVersion >= 4) bw.Write(a.MoveWeight ? 1 : 0);
            if (wData.AniBGVersion >= 5) bw.Write(a.PVSRad);
        }
    }

    private static void WriteItemBoxes(BinaryWriter bw, WData wData)
    {
        bw.Write(wData.ItemBoxes.Count);
        foreach (var b in wData.ItemBoxes)
        {
            WriteOBB(bw, b.Name, b.Position, b.Scale, b.Rotation, b.Extents);
            WriteWString(bw, b.Model);
            WriteWString(bw, b.Motion);
            WriteWString(bw, b.TablePath);
            bw.Write(b.Loop ? 1 : 0);
            if (wData.ItemBoxVersion >= 3) bw.Write(b.OpenEnable ? 1 : 0);
        }
    }

    private static void WriteGimmicks(BinaryWriter bw, WData wData)
    {
        bw.Write(wData.Gimmicks.Count);
        foreach (var g in wData.Gimmicks)
        {
            WriteOBB(bw, g.Name, g.Position, g.Scale, g.Rotation, g.Extents);
            WriteWString(bw, g.Model);
            WriteWString(bw, g.Motion);
            bw.Write(g.LoopFlag);
            bw.Write(g.LightIndex);
            bw.Write(g.Cover);
            bw.Write(g.Shadow);
            bw.Write(g.MoveWeight);
            bw.Write(g.TemplateID);
        }
    }

    private static void WriteTriggers(BinaryWriter bw, WData wData)
    {
        bw.Write(0);
        WriteWString(bw, string.Empty, dotIfEmpty: true);
        bw.Write(0);
        var root = wData.Triggers.FirstOrDefault() ?? new TriggerElement();
        WriteWString(bw, root.MainScript);
        bw.Write(root.Triggers.Count);
        foreach (var tr in root.Triggers)
        {
            WriteWString(bw, tr.Name);
            WriteWString(bw, tr.Comment);
            bw.Write(tr.Events.Count);
            bw.Write(tr.Conditions.Count);
            bw.Write(tr.Actions.Count);
            foreach (var s in tr.Events) WriteWString(bw, s.Name);
            foreach (var s in tr.Conditions) WriteWString(bw, s.Name);
            foreach (var s in tr.Actions) WriteWString(bw, s.Name);
        }
    }

    #endregion

    #region Scene writers

    private static void WriteScenes(BinaryWriter bw, WData wData)
    {
        bw.Write(wData.Scenes.Count);
        foreach (var sc in wData.Scenes)
        {
            WriteWString(bw, sc.File);
            bw.Write(sc.FadeInPreview);
            bw.Write(sc.FadeHoldPreview);
            bw.Write(sc.FadeOutPreview);
            bw.Write(sc.Category);
            bw.Write(sc.SceneFadeIn);
            bw.Write(sc.SceneFadeHold);
            bw.Write(sc.SceneFadeOut);
            bw.Write(sc.BlendTime);
            bw.Write(sc.FogNear);
            bw.Write(sc.FogFar);
            bw.Write(sc.Position.X); bw.Write(sc.Position.Y); bw.Write(sc.Position.Z);
            bw.Write(sc.Rotation.X); bw.Write(sc.Rotation.Y); bw.Write(sc.Rotation.Z);
            bw.Write(sc.FOV);
            bw.Write(sc.AspectRatio);
        }

        // second phase
        foreach (var sc in wData.Scenes)
        {
            WriteWString(bw, sc.Name);
            bw.Write(sc.EventScenes.Count);
            bw.Write(sc.IndexList.Count);
            /* counts render */
            bw.Write(sc.RenderBgUser.Count);
            bw.Write(sc.RenderAniBgUser.Count);
            bw.Write(sc.RenderItemBoxUser.Count);
            bw.Write(sc.RenderGimmickUser.Count);
            bw.Write(sc.NoRenderBgUser.Count);
            bw.Write(sc.NoRenderAniBgUser.Count);
            bw.Write(sc.NoRenderItemBoxUser.Count);
            bw.Write(sc.NoRenderGimmickUser.Count);
            foreach (var idx in sc.IndexList) bw.Write(idx.ID);
            foreach (var es in sc.EventScenes) { bw.Write(es.Category); WriteWString(bw, es.Key); }
            void WL(IEnumerable<SceneElement> lst) { foreach (var e in lst) { bw.Write(e.Category); WriteWString(bw, e.Key); } }
            WL(sc.RenderBgUser); WL(sc.RenderAniBgUser); WL(sc.RenderItemBoxUser); WL(sc.RenderGimmickUser);
            WL(sc.NoRenderBgUser); WL(sc.NoRenderAniBgUser); WL(sc.NoRenderItemBoxUser); WL(sc.NoRenderGimmickUser);
        }
    }

    private static void WriteSceneResources(BinaryWriter bw, WData wData)
    {
        bw.Write(wData.SceneResources.Count);
        foreach (var e in wData.SceneResources)
        {
            WriteWString(bw, e.Key);
            bw.Write(e.Aliases.Count);
            foreach (var a in e.Aliases) WriteWString(bw, a.Name);
            bw.Write(e.Paths.Count);
            if (e.Paths.Count > 0) bw.Write(e.Delay);
            for (int p = 0; p < e.Paths.Count; p++)
            {
                var sd = e.Paths[p];
                WriteWString(bw, sd.Model);
                WriteWString(bw, sd.Motion);
                WriteWString(bw, sd.Name);
                WriteWString(bw, sd.EventName);
                bw.Write(sd.Time);
                bw.Write(sd.Hold);
                if (p < e.Paths.Count - 1)
                    bw.Write(sd.BlendTime);
            }
            bw.Write(e.Cues.Count);
            foreach (var c in e.Cues) { bw.Write(c.End); WriteWString(bw, c.Name); bw.Write(c.ID); bw.Write(c.Start); }
            bw.Write(e.Sounds.Count);
            foreach (var s in e.Sounds)
            {
                bw.Write(s.Start);
                bw.Write(s.FadeIn);
                bw.Write(s.FadeOut);
                bw.Write(s.VolMax);
                bw.Write(s.VolMin);
                WriteWString(bw, s.Path);
            }
            bw.Write(e.Unk1);
            bw.Write(e.Unk2);
            bw.Write(e.Unk3);
            bw.Write(e.Unk4);

            bw.Write(e.Ambients.Count);
            foreach (var a in e.Ambients)
            { 
                bw.Write(a.Start);
                WriteWString(bw, a.Path);
                bw.Write(a.PlayOnStart ? 1 : 0);
                bw.Write(a.Loop ? 1 : 0); 
            }
        }
    }
    #endregion
}