namespace RHToolkit.Models.PCK;

public class PCKFileNodeViewModel : ObservableObject
{
    private bool? _isChecked = false;
    public bool? IsChecked
    {
        get => _isChecked;
        set
        {
            if (SetProperty(ref _isChecked, value))
            {
                UpdateChildren(value);
            }
        }
    }

    public string? Name { get; set; }
    public bool IsDir { get; set; }
    public PCKFile? PckFile { get; set; }
    public ObservableCollection<PCKFileNodeViewModel> Children { get; set; } = [];

    private void UpdateChildren(bool? isChecked)
    {
        if (Children == null || isChecked == null) return;
        foreach (var child in Children)
        {
            child.IsChecked = isChecked;
        }
    }
}
