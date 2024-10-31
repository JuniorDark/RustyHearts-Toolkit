using RHToolkit.ViewModels.Controls;

namespace RHToolkit.ViewModels.Windows.Tools.VM;

public partial class Skill : ObservableObject
{
    //Skill
    [ObservableProperty]
    private SkillDataViewModel? _skillDataViewModel;

    [ObservableProperty]
    private int _skillMotionCategory;

    [ObservableProperty]
    private string? _skillMotion;

    [ObservableProperty]
    private int _skillMotionCategorySG;

    [ObservableProperty]
    private string? _skillMotionSG;

    [ObservableProperty]
    private string? _paramName;

    [ObservableProperty]
    private double _paramValue;

    //SkillTree
    [ObservableProperty]
    private int _skillID;

    [ObservableProperty]
    private int _skillLevel;

}
