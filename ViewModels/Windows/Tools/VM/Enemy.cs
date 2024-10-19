using static RHToolkit.Models.EnumService;

namespace RHToolkit.ViewModels.Windows.Tools.VM;

public partial class Enemy : ObservableObject
{
    [ObservableProperty]
    private EnemyType _enemyType;

    [ObservableProperty]
    private string? _conditionResistance;

    [ObservableProperty]
    private double _conditionResistanceValue;

    [ObservableProperty]
    private int _hiddenItemDropGroup;

    [ObservableProperty]
    private int _hiddenItemDropCount;

    [ObservableProperty]
    private int _itemDropGroup;

    [ObservableProperty]
    private int _itemDropCount;

    [ObservableProperty]
    private int _fatigueItemDropGroup;

    [ObservableProperty]
    private int _fatigueItemDropCount;

    [ObservableProperty]
    private int _itemDropGroupF;

    [ObservableProperty]
    private int _itemDropCountF;

    [ObservableProperty]
    private int _fatigueItemDropGroupF;

    [ObservableProperty]
    private int _fatigueItemDropCountF;

    [ObservableProperty]
    private int _hiddenItemDropGroupF;

    [ObservableProperty]
    private int _hiddenItemDropCountF;
}
