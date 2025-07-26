using static RHToolkit.Models.ObservablePrimitives;

namespace RHToolkit.Models.WDATA;

/// <summary>
/// Represents a scene in the game, containing various properties and collections related to the scene.
/// </summary>
public partial class Scene : ObservableObject
{
    [ObservableProperty] private string _Name = string.Empty;
    [ObservableProperty] private string _File = string.Empty;
    [ObservableProperty] private float _FadeInPreview;
    [ObservableProperty] private float _FadeHoldPreview;
    [ObservableProperty] private float _FadeOutPreview;
    [ObservableProperty] private uint _Category;
    [ObservableProperty] private float _SceneFadeIn;
    [ObservableProperty] private float _SceneFadeHold;
    [ObservableProperty] private float _SceneFadeOut;
    [ObservableProperty] private float _BlendTime;
    [ObservableProperty] private float _FogNear;
    [ObservableProperty] private float _FogFar;

    [ObservableProperty] private Vector3 _Position = new();
    [ObservableProperty] private Vector3 _Rotation = new();
    [ObservableProperty] private float _FOV;
    [ObservableProperty] private float _AspectRatio;
    [ObservableProperty] private List<SceneIndex> _IndexList = [];
    [ObservableProperty] private ObservableCollection<SceneElement> _EventScenes = [];

    [ObservableProperty] private ObservableCollection<SceneElement> _RenderBgUser = [];
    [ObservableProperty] private ObservableCollection<SceneElement> _RenderAniBgUser = [];
    [ObservableProperty] private ObservableCollection<SceneElement> _RenderItemBoxUser = [];
    [ObservableProperty] private ObservableCollection<SceneElement> _RenderGimmickUser = [];

    [ObservableProperty] private ObservableCollection<SceneElement> _NoRenderBgUser = [];
    [ObservableProperty] private ObservableCollection<SceneElement> _NoRenderAniBgUser = [];
    [ObservableProperty] private ObservableCollection<SceneElement> _NoRenderItemBoxUser = [];
    [ObservableProperty] private ObservableCollection<SceneElement> _NoRenderGimmickUser = [];

}

public partial class SceneElement : ObservableObject
{
    [ObservableProperty] private string _Key = string.Empty;
    [ObservableProperty] private uint _Category;
    
}
public partial class SceneIndex : ObservableObject
{
    [ObservableProperty] private uint _ID;

}

/// <summary>
/// Represents a resource associated with a scene, containing various properties and collections related to the scene's resources.
/// </summary>
public partial class SceneResource : ObservableObject
{
    [ObservableProperty] private string _Key = string.Empty;
    [ObservableProperty] private float _Delay;
    [ObservableProperty] private ObservableCollection<StringModel> _Aliases = [];
    [ObservableProperty] private ObservableCollection<SceneData> _Paths = [];
    [ObservableProperty] private ObservableCollection<Cue> _Cues = [];
    [ObservableProperty] private ObservableCollection<SoundRecord> _Sounds = [];
    [ObservableProperty] private ObservableCollection<AmbientRecord> _Ambients = [];
    [ObservableProperty] private uint _Unk1;
    [ObservableProperty] private uint _Unk2;
    [ObservableProperty] private float _Unk3;
    [ObservableProperty] private uint _Unk4;
}

public partial class SceneData : ObservableObject
{
    [ObservableProperty] private string _Model = string.Empty;
    [ObservableProperty] private string _Motion = string.Empty;
    [ObservableProperty] private string _Name = string.Empty;
    [ObservableProperty] private string _EventName = string.Empty;
    [ObservableProperty] private uint _Time;
    [ObservableProperty] private uint _Hold;
    [ObservableProperty] private float _BlendTime;
}

public partial class Cue : ObservableObject
{
    [ObservableProperty] private string _Name = string.Empty;
    [ObservableProperty] private uint _ID;
    [ObservableProperty] private float _Start;
    [ObservableProperty] private float _End;
}

public partial class SoundRecord : ObservableObject
{
    [ObservableProperty] private string _Path = string.Empty;
    [ObservableProperty] private float _Start;
    [ObservableProperty] private float _FadeIn;
    [ObservableProperty] private float _FadeOut;
    [ObservableProperty] private float _VolMin;
    [ObservableProperty] private float _VolMax;

}

public partial class AmbientRecord : ObservableObject
{
    [ObservableProperty] private float _Start;
    [ObservableProperty] private string _Path = string.Empty;
    [ObservableProperty] private bool _Loop;
    [ObservableProperty] private bool _PlayOnStart;
}


