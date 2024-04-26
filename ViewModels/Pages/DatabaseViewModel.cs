using RHToolkit.Services;
using RHToolkit.Models;
using RHToolkit.Views;
using Wpf.Ui.Controls;

namespace RHToolkit.ViewModels.Pages
{
    public partial class DatabaseViewModel(WindowsProviderService windowsProviderService) : ObservableObject
    {
        [ObservableProperty]
        private WindowCard[] _windowCards =
        [
            new("Item Database", "Item database.", SymbolRegular.BookDatabase24, "item"),
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
                case "item":
                    windowsProviderService.Show<ItemWindow>();
                    break;
            }
        }
    }
}
