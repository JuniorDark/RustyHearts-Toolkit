using RHToolkit.Models;
using RHToolkit.Models.Database;
using RHToolkit.Properties;
using RHToolkit.Services;
using RHToolkit.Views.Windows;
using Wpf.Ui.Controls;

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

            if (!ItemDataManager.GetDatabaseFilePath())
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
