using static RHToolkit.Models.ObservablePrimitives;

namespace RHToolkit.Models.WDATA;

/// <summary>
/// Represents a Gimmick in the WDATA model.
/// </summary>
public partial class Gimmick : ObservableObject
{
    [ObservableProperty] private string _Name = string.Empty;
    [ObservableProperty] private string _Model = string.Empty;
    [ObservableProperty] private string _Motion = string.Empty;
    [ObservableProperty] private int _LoopFlag;
    [ObservableProperty] private int _LightIndex;
    [ObservableProperty] private int _Cover;
    [ObservableProperty] private int _Shadow;
    [ObservableProperty] private int _MoveWeight;
    [ObservableProperty] private int _TemplateID;
    [ObservableProperty] private string _ModelPath = string.Empty;
    [ObservableProperty] private Vector3 _Position = new();
    [ObservableProperty] private Vector3 _Scale = new();
    [ObservableProperty] private Quaternion _Rotation = new();
    [ObservableProperty] private Vector3 _Extents = new();
    [ObservableProperty] private bool _isVisible = false;
}
