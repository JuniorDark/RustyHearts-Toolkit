using RHToolkit.ViewModels.Controls;
using static RHToolkit.Models.EnumService;

namespace RHToolkit.ViewModels.Windows.Tools.VM;

public partial class ItemDropGroup : ObservableObject
{
    [ObservableProperty]
    private ItemDropGroupType _dropItemGroupType;

    [ObservableProperty]
    private ItemDataViewModel? _itemDataViewModel;

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

    //RiddleBox
    [ObservableProperty]
    private int _sectionStart00;

    [ObservableProperty]
    private int _sectionEnd00;

    [ObservableProperty]
    private int _probability00;

    [ObservableProperty]
    private int _sectionStart01;

    [ObservableProperty]
    private int _sectionEnd01;

    [ObservableProperty]
    private int _probability01;

    [ObservableProperty]
    private int _sectionStart02;

    [ObservableProperty]
    private int _sectionEnd02;

    [ObservableProperty]
    private int _probability02;

    [ObservableProperty]
    private int _sectionStart03;

    [ObservableProperty]
    private int _sectionEnd03;

    [ObservableProperty]
    private int _probability03;

    //RareCard
    [ObservableProperty]
    private int _bronzeCardCode;

    [ObservableProperty]
    private double _bronzeCardProbability;

    [ObservableProperty]
    private int _silverCardCode;

    [ObservableProperty]
    private double _silverCardProbability;

    [ObservableProperty]
    private int _goldCardCode;

    [ObservableProperty]
    private double _goldCardProbability;

    [ObservableProperty]
    private ItemDataViewModel? _bronzeCard;

    [ObservableProperty]
    private ItemDataViewModel? _silverCard;

    [ObservableProperty]
    private ItemDataViewModel? _goldCard;
}
