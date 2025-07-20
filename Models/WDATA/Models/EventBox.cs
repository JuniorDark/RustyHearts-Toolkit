using static RHToolkit.Models.ObservablePrimitives;

namespace RHToolkit.Models.WDATA;

/// <summary>
/// Enumeration of different types of event boxes used.
/// </summary>
public enum EventBoxType
{
    CameraBox = 0,
    RespawnBox = 1,
    StartPointBox = 2,
    TriggerBox = 3,
    SkidBox = 4,
    EventHitBox = 5,
    NpcBox = 6,
    PortalBox = 7,
    SelectMapPortalBox = 8,
    InAreaBox = 9,
    EtcBox = 10,
    CameraBlockBox = 11,
    CutoffBox = 12,
    CameraTargetBox = 13,
    UnusedBox = 14,
    MiniMapIconBox = 15,
    EnvironmentReverbBox = 16,
    WaypointBox = 17,
    ObstacleBox = 18
}

/// <summary>
/// Base OBB for all event boxes.
/// </summary>
public partial class EventBox : ObservableObject
{
    [ObservableProperty] private string _Name  = string.Empty;
    [ObservableProperty] private Vector3 _Position = new();
    [ObservableProperty] private Vector3 _Scale = new();
    [ObservableProperty] private Quaternion _Rotation = new();
    [ObservableProperty] private Vector3 _Extents = new();
    [ObservableProperty] private EventBoxType _Type;
}

/// <summary>
/// Represents a group of event boxes, allowing for categorization and management of multiple event boxes together.
/// </summary>
public partial class EventBoxGroup : ObservableObject
{
    [ObservableProperty] private EventBoxType _Type;
    [ObservableProperty] private ObservableCollection<EventBox> _Boxes = [];
}

/// <summary>
/// Represents a camera box, containing camera-related information and settings.
/// </summary>
public partial class CameraBox : EventBox
{
    [ObservableProperty] private uint _Category;
    [ObservableProperty] private List<CameraInfo> _CameraInfos = [];
}

/// <summary>
/// Represents camera information, including position, rotation, projection parameters, and PVS (Potentially Visible Set) data.
/// </summary>
public partial class CameraInfo: ObservableObject
{
    // Strings
    [ObservableProperty] private string _CameraTarget  = string.Empty;
    [ObservableProperty] private string _CameraName  = string.Empty;

    // Basic transforms
    [ObservableProperty] private Vector3 _CameraPos = new();      // fl[0..2]
    [ObservableProperty] private Vector3 _CameraRot = new();      // fl[3..5]

    // Projection parameters
    [ObservableProperty] private float _FOV;     // fl[6]
    [ObservableProperty] private float _Distance;     // fl[7]
    [ObservableProperty] private float _Height;     // fl[8]
    [ObservableProperty] private float _TargetHeight;     // fl[9]
    [ObservableProperty] private float _RotSpeed;     // fl[10]
    [ObservableProperty] private float _ZoomSpeed;     // fl[11]
    [ObservableProperty] private float _MinDistance;     // fl[12]
    [ObservableProperty] private float _MaxDistance;     // fl[13]
    [ObservableProperty] private float _MinHeight;     // fl[14]
    [ObservableProperty] private float _MaxHeight;     // fl[15]

    [ObservableProperty] private List<Float> _Frustum = [];

    [ObservableProperty] private bool _BuildPVS;

    // PVS lists
    [ObservableProperty] private List<UInt> _PVS_BG = [];
    [ObservableProperty] private List<UInt> _PVS_EventBox = [];

    [ObservableProperty] private List<Entry> _PVS_EventEntries = [];

    // render_* groups
    [ObservableProperty] private List<Entry> _RenderBg = [];
    [ObservableProperty] private List<Entry> _RenderAniBg = [];
    [ObservableProperty] private List<Entry> _RenderItem = [];
    [ObservableProperty] private List<Entry> _RenderGimmick = [];

    // nore_* groups
    [ObservableProperty] private List<Entry> _NoRenderBg = [];
    [ObservableProperty] private List<Entry> _NoRenderAniBg = [];
    [ObservableProperty] private List<Entry> _NoRenderItem = [];
    [ObservableProperty] private List<Entry> _NoRenderGimmick = [];

    [ObservableProperty] private List<Vector3> _BuildPos = [];
}

public partial class Entry : ObservableObject
{
    [ObservableProperty] private uint _ID;
    [ObservableProperty] private string _Name  = string.Empty;
}

/// <summary>
/// Represents a respawn box, containing information about enemy respawn settings such as total enemy number, respawn time, and motion.
/// </summary>
public partial class RespawnBox : EventBox
{
    [ObservableProperty] private int _TotalEnemyNum;
    [ObservableProperty] private int _EnemyNum;
    [ObservableProperty] private string _EnemyName  = string.Empty;
    [ObservableProperty] private float _RespawnTime;
    [ObservableProperty] private string _RespawnMotion  = string.Empty;
    [ObservableProperty] private bool _InCheck;
    [ObservableProperty] private bool _RandomDirection;
    [ObservableProperty] private List<Bool> _Difficulty = [];
}

/// <summary>
/// Represents a start point box, which is used to define the starting point for a player in a map.
/// </summary>
public partial class StartPointBox : EventBox
{
    [ObservableProperty] private int _ID;
}

/// <summary>
/// Represents a trigger box, which is used to trigger events or actions when a player enters a specific area.
/// </summary>
public partial class TriggerBox : EventBox
{
    [ObservableProperty] private int _State;
    [ObservableProperty] private string _ActionMotion  = string.Empty;
    [ObservableProperty] private string _Motion  = string.Empty;
    [ObservableProperty] private int _SignpostTextID;
    [ObservableProperty] private Vector3 _SignpostReposition = new();
}

/// <summary>
/// Represents a skid box, which is used to define a skid event.
/// </summary>
public partial class SkidBox : EventBox
{
    [ObservableProperty] private int _State;
    [ObservableProperty] private Vector3 _Velocity = new();
    [ObservableProperty] private double _EndTime;
    [ObservableProperty] private float _Duration;
}

/// <summary>
/// Represents an event hit box, which is used to define hit events.
/// </summary>
public partial class EventHitBox : EventBox
{
    [ObservableProperty] private int _State;
    [ObservableProperty] private string _AniBGName  = string.Empty;
    [ObservableProperty] private float _Damage;
    [ObservableProperty] private Vector3 _Direction = new();
    [ObservableProperty] private string _DamageMotion  = string.Empty;
    [ObservableProperty] private List<Float> _HitTime = [];
    [ObservableProperty] private List<Float> _TempHitTime = [];
}

/// <summary>
/// Represents an NPC box, which is used to define the spawn position in the map.
/// </summary>
public partial class NpcBox : EventBox
{
    [ObservableProperty] private string _NpcName  = string.Empty;
    [ObservableProperty] private int _ID;
    [ObservableProperty] private int _InstanceID;
}

/// <summary>
/// Represents a portal box, which is used to define warp points.
/// </summary>
public partial class PortalBox : EventBox
{
    [ObservableProperty] private string _WarpMapName  = string.Empty;
    [ObservableProperty] private int _ID;
    [ObservableProperty] private int _MsgType;
    [ObservableProperty] private int _WarpMapID;
    [ObservableProperty] private int _WarpPortalID;
    [ObservableProperty] private bool _Active;
}

/// <summary>
/// Represents a select map portal box, which is used to define a portal that allows players to select a map to warp to.
/// </summary>
public partial class SelectMapPortalBox : EventBox
{
    [ObservableProperty] private int _ID;
    [ObservableProperty] private int _MsgType;
    [ObservableProperty] private bool _Active;
}

/// <summary>
/// Represents an in-area box, which is used to define a specific area in the game where certain events or actions can occur.
/// </summary>
public partial class InAreaBox : EventBox
{
    [ObservableProperty] private string _WarpMapName  = string.Empty;
    [ObservableProperty] private int _ID;
    [ObservableProperty] private bool _Active;
}

/// <summary>
/// Represents an etc box, which is used to define miscellaneous event-related data that does not fit into other categories.
/// </summary>
public partial class EtcBox : EventBox
{
    [ObservableProperty] private int _ID;
}

/// <summary>
/// Represents a camera block box, which is used to define areas where camera movement is restricted or blocked.
/// </summary>
public partial class CameraBlockBox : EventBox
{
    [ObservableProperty] private bool _Skip;
}

/// <summary>
/// Represents a cutoff box, which is used to define areas where certain game mechanics or events are cut off or restricted.
/// </summary>
public partial class CutoffBox : EventBox
{
    [ObservableProperty] private int _CutoffType;
}

/// <summary>
/// Represents a camera target box, which is used to define a target for the camera.
/// </summary>
public partial class CameraTargetBox : EventBox
{
    [ObservableProperty] private int _NameTextID;
    [ObservableProperty] private bool _TargetLocalPC;
}

/// <summary>
/// Represents a mini-map icon box, which is used to define icons on the mini-map.
/// </summary>
public partial class MiniMapIconBox : EventBox
{
    [ObservableProperty] private int _IconType;
}

/// <summary>
/// Represents an environment reverb box, which is used to define reverb settings for the environment.
/// </summary>
public partial class EnvironmentReverbBox : EventBox
{
    [ObservableProperty] private int _ReverbType;
}

/// <summary>
/// Represents a waypoint box, which is used to define waypoints in the game world for navigation.
/// </summary>
public partial class WaypointBox : EventBox
{
    [ObservableProperty] private int _ID;
    [ObservableProperty] private float _Range;
    [ObservableProperty] private List<Int> _Links = [];
    [ObservableProperty] private List<Float> _LinkDistances = [];
}

/// <summary>
/// Represents an obstacle box, which is used to define obstacles in the game world that players must navigate around.
/// </summary>
public partial class ObstacleBox : EventBox
{

}
