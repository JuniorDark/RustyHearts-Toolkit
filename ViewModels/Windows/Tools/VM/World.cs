namespace RHToolkit.ViewModels.Windows.Tools.VM;

public partial class WorldData : ObservableObject
{
    //World
    [ObservableProperty]
    private int _stageClearTime;

    [ObservableProperty]
    private int _stageClearPoint;

    [ObservableProperty]
    private string? _preloadShoot;

    [ObservableProperty]
    private string? _preloadEnemy;

    //mapselect_curtis
    [ObservableProperty]
    private int _selectType;

    [ObservableProperty]
    private string? _mc;

    [ObservableProperty]
    private int _worldID;

    [ObservableProperty]
    private string? _bigSprite;

}
