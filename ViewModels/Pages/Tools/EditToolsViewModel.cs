using RHToolkit.Models;
using RHToolkit.Models.MessageBox;
using RHToolkit.Services;
using RHToolkit.Views.Windows;
using Wpf.Ui.Controls;

namespace RHToolkit.ViewModels.Pages;

public partial class EditToolsViewModel(WindowsProviderService windowsProviderService) : ObservableObject
{
    [ObservableProperty]
    private WindowCard[] _windowCards =
    [
        new("RH Table Editor", "Edit .rh table files", SymbolRegular.DocumentEdit24, "rheditor"),
        new("Cash Shop Editor", "Edit cash shop .rh table file", SymbolRegular.DocumentEdit24, "cashshopeditor"),
        new("Set Item Editor", "Edit setitem .rh table file", SymbolRegular.DocumentEdit24, "setitemeditor"),
        new("Package Editor", "Edit unionpackage .rh table file", SymbolRegular.DocumentEdit24, "packageeditor"),
];

    [RelayCommand]
    public void OnOpenWindow(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        try
        {
            switch (value)
            {
                case "rheditor":
                    windowsProviderService.Show<RHEditorWindow>(true);
                    break;
                case "cashshopeditor":
                    windowsProviderService.Show<CashShopEditorWindow>(true);
                    break;
                case "packageeditor":
                    windowsProviderService.Show<PackageEditorWindow>(true);
                    break;
                case "setitemeditor":
                    windowsProviderService.Show<SetItemEditorWindow>(true);
                    break;
            }
        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"Error: {ex.Message}", "Error");
        }

    }
}
