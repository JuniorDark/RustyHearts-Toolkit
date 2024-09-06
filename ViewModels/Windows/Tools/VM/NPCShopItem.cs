using RHToolkit.ViewModels.Controls;

namespace RHToolkit.ViewModels.Windows.Tools.VM;

public partial class NPCShopItem : ObservableObject
{
    [ObservableProperty]
    private int _itemCode;

    [ObservableProperty]
    private int _itemCount;

    [ObservableProperty]
    private int _questID;

    [ObservableProperty]
    private int _questCondition;

    [ObservableProperty]
    private double _itemMixPro;

    [ObservableProperty]
    private double _itemMixCo;

    [ObservableProperty]
    private ItemDataViewModel? _itemDataViewModel;
}
