using RHToolkit.Models;

public partial class NpcInstance : ObservableObject
{
    [ObservableProperty]
    private int _randomQuestGroup;

    [ObservableProperty]
    private float _randomProbability;

    [ObservableProperty]
    private int _questGroup;

    [ObservableProperty]
    private int _missionGroup;

    [ObservableProperty]
    private int _shopID;

    [ObservableProperty]
    private NameID? _selectedShop;

}
