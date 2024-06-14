using RHToolkit.Models;

namespace RHToolkit.ViewModels.Controls;

public partial class ItemSlotViewModel(FrameViewModel frameViewModel) : ObservableObject
{
    private readonly FrameViewModel _frameViewModel = frameViewModel;
    public FrameViewModel FrameViewModel => _frameViewModel;

    [ObservableProperty]
    private ItemData? _itemData;
    partial void OnItemDataChanged(ItemData? value)
    {
        _frameViewModel.ItemData = value;
    }

    [ObservableProperty]
    private bool _isButtonEnabled;

    [ObservableProperty]
    private string? _itemName;

    [ObservableProperty]
    private ICommand? _addItemCommand;

    [ObservableProperty]
    private ICommand? _removeItemCommand;

    [ObservableProperty]
    private string? _commandParameter;

    [ObservableProperty]
    private string? _itemIcon;

    [ObservableProperty]
    private string? _slotIcon;

    [ObservableProperty]
    private string? _itemIconBranch;

    [ObservableProperty]
    private string? _itemAmount;

}
