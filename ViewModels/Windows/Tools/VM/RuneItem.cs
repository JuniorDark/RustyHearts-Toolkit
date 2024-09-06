using RHToolkit.ViewModels.Controls;

namespace RHToolkit.ViewModels.Windows.Tools.VM;

public partial class RuneItem : ObservableObject
{
    [ObservableProperty]
    private int _itemCode;

    [ObservableProperty]
    private int _itemCodeCount;

    [ObservableProperty]
    private int _itemCount;

    [ObservableProperty]
    private ItemDataViewModel? _itemDataViewModel;

    [ObservableProperty]
    private bool _isEnabled = false;
}
