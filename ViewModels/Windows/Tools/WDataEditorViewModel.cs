using RHToolkit.Models.WDATA;
using RHToolkit.Services;

namespace RHToolkit.ViewModels.Windows;

public partial class WDataEditorViewModel: ObservableObject
{
    public WDataEditorViewModel(IWindowsService windowsService)
    {
        WDataManager = new(windowsService);
    }

    #region Properties

    [ObservableProperty]
    private WDataManager _wDataManager;

    #endregion
}
