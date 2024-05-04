using RHToolkit.Services;
using RHToolkit.Models;
using RHToolkit.Views.Windows;
using Wpf.Ui.Controls;
using RHToolkit.Properties;

namespace RHToolkit.ViewModels.Pages
{
    public partial class DatabaseViewModel(WindowsProviderService windowsProviderService) : ObservableObject
    {
        [ObservableProperty]
        private WindowCard[] _windowCards =
        [
            new(Resources.ItemDatabase, Resources.ItemDatabaseDesc, SymbolRegular.BookDatabase24, "item"),
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
