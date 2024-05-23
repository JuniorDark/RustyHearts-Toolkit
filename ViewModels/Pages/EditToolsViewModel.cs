using RHToolkit.Models;
using RHToolkit.Services;
using RHToolkit.Views.Windows;
using Wpf.Ui.Controls;

namespace RHToolkit.ViewModels.Pages;

public partial class EditToolsViewModel(WindowsProviderService windowsProviderService) : ObservableObject
{
    [ObservableProperty]
    private WindowCard[] _windowCards =
    [
        new("RH Editor", "RH Editor", SymbolRegular.DocumentEdit24, "rheditor"),
];

    [RelayCommand]
    public void OnOpenWindow(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        switch (value)
        {
            case "rheditor":
                windowsProviderService.Show<RHEditorWindow>();
                break;
        }
    }
}
