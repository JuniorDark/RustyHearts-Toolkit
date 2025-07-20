using static RHToolkit.Models.ObservablePrimitives;

namespace RHToolkit.Models.WDATA;

/// <summary>
/// Represents a collection of triggers with their associated events, conditions, and actions.
/// </summary>
public partial class Triggers : ObservableObject
{
    [ObservableProperty] private string _Name = string.Empty;
    [ObservableProperty] private string _Comment = string.Empty;
    [ObservableProperty] private ObservableCollection<StringModel> _Events = [];
    [ObservableProperty] private ObservableCollection<StringModel> _Conditions = [];
    [ObservableProperty] private ObservableCollection<StringModel> _Actions = [];
}

public partial class TriggerElement : ObservableObject
{
    [ObservableProperty] private string _MainScript = string.Empty;
    [ObservableProperty] private ObservableCollection<Triggers> _Triggers = [];
}
