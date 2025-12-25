using static RHToolkit.Models.ObservablePrimitives;

namespace RHToolkit.Models.WDATA;

/// <summary>
/// Represents an ANIBG object from a .wdata file.
/// </summary>
public partial class AniBG : ObservableObject
{
    [ObservableProperty] private bool _isVisible = false;
    /// <summary>Name identifier of this background.</summary>
    [ObservableProperty] private string _Name = string.Empty;

    /// <summary>Path to the model file.</summary>
    [ObservableProperty] private string _Model = string.Empty;

    /// <summary>Path to the motion/animation file.</summary>
    [ObservableProperty] private string _Motion = string.Empty;

    /// <summary>Whether the animation should loop.</summary>
    [ObservableProperty] private bool _Loop;

    /// <summary>Index of the light to use.</summary>
    [ObservableProperty] private int _LightIndex;

    /// <summary>Cover/shadow layer index.</summary>
    [ObservableProperty] private int _CoverIndex;

    /// <summary>Whether to render shadows.</summary>
    [ObservableProperty] private bool _Shadow;

    /// <summary>Whether to apply move-weight optimization.</summary>
    [ObservableProperty] private bool _MoveWeight;

    /// <summary>Precomputed PVS radius (only for versions ≥5).</summary>
    [ObservableProperty] private float _PVSRad;
    /// <summary>Transform data (position, scale, rotation, extents).</summary>
    [ObservableProperty] private Vector3 _Position = new();
    [ObservableProperty] private Vector3 _Scale = new ();
    [ObservableProperty] private Quaternion _Rotation = new();
    [ObservableProperty] private Vector3 _Extents = new();
}
