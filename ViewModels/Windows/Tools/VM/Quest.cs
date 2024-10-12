using RHToolkit.ViewModels.Controls;
using static RHToolkit.Models.EnumService;

namespace RHToolkit.ViewModels.Windows.Tools.VM;

public partial class Quest : ObservableObject
{
    [ObservableProperty]
    private QuestType _questType;

    [ObservableProperty]
    private ItemDataViewModel? _itemDataViewModel;

    [ObservableProperty]
    private int _itemID;

    [ObservableProperty]
    private int _itemCount;

    [ObservableProperty]
    private int _value;

    [ObservableProperty]
    private double _ratio;

    [ObservableProperty]
    private string? _sprite;

    [ObservableProperty]
    private int _npcID;

    [ObservableProperty]
    private string? _text;

    [ObservableProperty]
    private int _questID;

}
