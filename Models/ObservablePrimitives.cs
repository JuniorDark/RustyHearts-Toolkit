namespace RHToolkit.Models;

public partial class ObservablePrimitives
{
    public partial class StringModel : ObservableObject
    {
        [ObservableProperty] private string _Name = string.Empty;
    }


    public partial class Int : ObservableObject
    {
        [ObservableProperty] private int _ID;
    }

    public partial class UInt : ObservableObject
    {
        [ObservableProperty] private uint _ID;
    }

    public partial class Float : ObservableObject
    {
        [ObservableProperty] private float _Value;
    }

    public partial class Bool : ObservableObject
    {
        [ObservableProperty] private bool _Value;
    }

    public partial class Vector3 : ObservableObject
    {
        [ObservableProperty] private float _X;
        [ObservableProperty] private float _Y;
        [ObservableProperty] private float _Z;
    }

    public partial class Quaternion : ObservableObject
    {
        [ObservableProperty] private float _X;
        [ObservableProperty] private float _Y;
        [ObservableProperty] private float _Z;
        [ObservableProperty] private float _W;
    }

}
