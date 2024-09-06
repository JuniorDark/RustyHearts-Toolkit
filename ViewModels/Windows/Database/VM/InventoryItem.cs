using RHToolkit.ViewModels.Controls;

namespace RHToolkit.ViewModels.Windows.Database.VM;

public partial class InventoryItem : ObservableObject
{
    [ObservableProperty]
    private ItemDataViewModel? _itemDataViewModel;

    [ObservableProperty]
    private int _slotIndex;

    [ObservableProperty]
    private int _pageIndex;
}
