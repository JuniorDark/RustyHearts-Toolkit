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
    private ItemDataViewModel? _itemDataViewModel;

    [ObservableProperty]
    private bool _isSetOptionEnabled = false;
}
