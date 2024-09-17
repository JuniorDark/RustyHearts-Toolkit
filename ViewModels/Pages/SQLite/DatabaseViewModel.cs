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
            new(Resources.ItemDatabase, "Item List", SymbolRegular.BookDatabase24, "item"),
            new("Item Craft Database", "Item Craft List", SymbolRegular.BookDatabase24, "itemmix"),
            new("NPC Shop Database", "NPC Shop List", SymbolRegular.BookDatabase24, "npcshop"),
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
                        windowsProviderService.ShowInstance<ItemWindow>(true);
                        break;
                    case "itemmix":
                        windowsProviderService.ShowInstance<ItemMixWindow>(true);
                        break;
                    case "npcshop":
                        windowsProviderService.ShowInstance<NpcShopWindow>(true);
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
