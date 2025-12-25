using static RHToolkit.Models.ObservablePrimitives;

namespace RHToolkit.Models.WDATA;

/// <summary>
/// Represents an item box in a .wdata file.
/// </summary>
public partial class ItemBox : ObservableObject
{
    [ObservableProperty] private bool _isVisible = false;
    /// <summary>Name identifier of this item box.</summary>
    [ObservableProperty] private string _Name = string.Empty;

    /// <summary>Path to the model file for this box.</summary>
    [ObservableProperty] private string _Model = string.Empty;

    /// <summary>Path to the motion/animation file for this box.</summary>
    [ObservableProperty] private string _Motion = string.Empty;

    /// <summary>Path to the item table associated with this box.</summary>
    [ObservableProperty] private string _TablePath = string.Empty;

    /// <summary>Whether the box’s animation should loop.</summary>
    [ObservableProperty] private bool _Loop;

    /// <summary>Whether “open” functionality is enabled (version ≥3).</summary>
    [ObservableProperty] private bool _OpenEnable;

    /// <summary>
    /// Transform data including position, scale, rotation, and extents.
    /// </summary>
    [ObservableProperty] private Vector3 _Position = new();
    [ObservableProperty] private Quaternion _Rotation = new();
    [ObservableProperty] private Vector3 _Scale = new();
    [ObservableProperty] private Vector3 _Extents = new();
}
