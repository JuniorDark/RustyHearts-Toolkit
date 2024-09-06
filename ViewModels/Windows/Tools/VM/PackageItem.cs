using RHToolkit.ViewModels.Controls;

namespace RHToolkit.ViewModels.Windows.Tools.VM;

public partial class PackageItem : ObservableObject
{
    [ObservableProperty]
    private int _itemCode;

    [ObservableProperty]
    private int _itemCount;

    [ObservableProperty]
    private int _effectCode;

    [ObservableProperty]
    private double _effectValue;

    [ObservableProperty]
    private double _stringID;

    [ObservableProperty]
    private ItemDataViewModel? _itemDataViewModel;

    [ObservableProperty]
    private double _effectValueMin;

    [ObservableProperty]
    private double _effectValueMax;

    [ObservableProperty]
    private double _effectValueSmallIncrement;

    [ObservableProperty]
    private double _effectValueLargeIncrement;
}
