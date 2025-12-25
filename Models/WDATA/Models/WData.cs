namespace RHToolkit.Models.WDATA;

/// <summary>
/// Represents the WData file model.
/// </summary>
public partial class WData : ObservableObject
{
    // Version fields
    [ObservableProperty] private int _version = 0;

    [ObservableProperty] private int _eventBoxVersion = 0;

    [ObservableProperty] private int _aniBGVersion = 0;

    [ObservableProperty] private int _itemBoxVersion = 0;

    [ObservableProperty] private int _gimmickVersion = 0;

    // Script count fields
    [ObservableProperty] private int _ScriptCount1 = 0;

    [ObservableProperty] private int _ScriptConditionCount1 = 0;

    [ObservableProperty] private int _ScriptCount2 = 0;

    [ObservableProperty] private int _ScriptConditionCount2 = 0;

    // Path fields
    [ObservableProperty] private string _modelPath = string.Empty;

    [ObservableProperty] private string _navMeshPath = string.Empty;

    [ObservableProperty] private string _navHeightPath = string.Empty;

    [ObservableProperty] private string _eventBoxPath = string.Empty;

    [ObservableProperty] private string _mocPath = string.Empty;

    [ObservableProperty] private string _aniBGPath = string.Empty;

    [ObservableProperty] private string _obstaclePath = string.Empty;

    // Data collections
    [ObservableProperty] private ObservableCollection<EventBox> _eventBoxes = [];

    [ObservableProperty] private ObservableCollection<EventBoxGroup> _eventBoxGroups = [];

    [ObservableProperty] private ObservableCollection<ItemBox> _itemBoxes = [];

    [ObservableProperty] private ObservableCollection<Gimmick> _gimmicks = [];

    [ObservableProperty] private ObservableCollection<AniBG> _aniBGs = [];

    [ObservableProperty] private ObservableCollection<TriggerElement> _triggers = [];

    [ObservableProperty] private ObservableCollection<Scene> _scenes = [];

    [ObservableProperty] private ObservableCollection<SceneResource> _sceneResources = [];
}
