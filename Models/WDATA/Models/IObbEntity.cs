using System.ComponentModel;
using static RHToolkit.Models.ObservablePrimitives;

namespace RHToolkit.Models.WDATA;

public interface IObbEntity : INotifyPropertyChanged
{
    string Name { get; }
    Vector3 Position { get; }
    Vector3 Scale { get; }
    Quaternion Rotation { get; }
    Vector3 Extents { get; }
    bool IsVisible { get; }
}
