using RHToolkit.ViewModels.Controls;
using static RHToolkit.Models.EnumService;

namespace RHToolkit.ViewModels.Windows.Tools.VM;

public partial class ItemDropGroup : ObservableObject
{
    [ObservableProperty]
    private ItemDropGroupType _dropItemGroupType;
    
    [ObservableProperty]
    private int _dropItemCode;

    [ObservableProperty]
    private double _fDropItemCount;

    [ObservableProperty]
    private int _nDropItemCount;

    [ObservableProperty]
    private int _link;

    [ObservableProperty]
    private int _start;

    [ObservableProperty]
    private int _end;

    [ObservableProperty]
    private ItemDataViewModel? _itemDataViewModel;
}
