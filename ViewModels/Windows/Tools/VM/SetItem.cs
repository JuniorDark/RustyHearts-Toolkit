using RHToolkit.ViewModels.Controls;

namespace RHToolkit.ViewModels.Windows.Tools.VM;

public partial class SetItem : ObservableObject
{
    [ObservableProperty]
    private int _setItemID;

    [ObservableProperty]
    private int _setOption;

    [ObservableProperty]
    private int _setOptionValue;

    [ObservableProperty]
    private FrameViewModel? _frameViewModel;

    [ObservableProperty]
    private bool _isSetOptionEnabled = false;
}
