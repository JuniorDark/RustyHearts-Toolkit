using RHToolkit.Models.WDATA;

namespace RHToolkit.ViewModels.Windows;

public partial class WDataEditorViewModel: ObservableObject
{
    public WDataEditorViewModel()
    {
        WDataManager = new();
    }

    #region Properties

    [ObservableProperty]
    private WDataManager _wDataManager;

    #endregion
}
