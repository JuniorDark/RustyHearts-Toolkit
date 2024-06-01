using RHToolkit.Models;
using RHToolkit.Models.MessageBox;
using RHToolkit.Properties;
using RHToolkit.Services;
using RHToolkit.Views.Windows;
using Wpf.Ui.Controls;

namespace RHToolkit.ViewModels.Pages
{
    public partial class DatabaseViewModel(ISqLiteDatabaseService sqLiteDatabaseService, WindowsProviderService windowsProviderService) : ObservableObject
    {
        private readonly ISqLiteDatabaseService _sqLiteDatabaseService = sqLiteDatabaseService;

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

            try
            {
                if (!_sqLiteDatabaseService.ValidateDatabase())
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
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"Error: {ex.Message}", "Error");
            }
        }
    }
}
