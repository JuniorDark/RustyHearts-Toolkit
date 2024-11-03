using RHToolkit.Models;
using RHToolkit.Models.MessageBox;
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
            new(Resources.ItemDatabase, Resources.ItemListDatabaseDesc, SymbolRegular.BookDatabase24, "item"),
            new(Resources.SkillsDatabase, Resources.SkillListDatabaseDesc, SymbolRegular.BookDatabase24, "skill"),
            new(Resources.ItemCraftDatabase, Resources.ItemCraftListDatabaseDesc, SymbolRegular.BookDatabase24, "itemmix"),
            new(Resources.NPCShopDatabase, Resources.NPCShopListDatabaseDesc, SymbolRegular.BookDatabase24, "npcshop"),
            new(Resources.RareCardRewardDatabase, Resources.RareCardRewardListDatabaseDesc, SymbolRegular.BookDatabase24, "rarecardreward"),
            new(Resources.DropGroupsDatabase, Resources.DropGroupItemListDatabaseDesc, SymbolRegular.BookDatabase24, "dropgroup"),
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
                    case "dropgroup":
                        windowsProviderService.ShowInstance<DropGroupListWindow>(true);
                        break;
                    case "item":
                        windowsProviderService.ShowInstance<ItemWindow>(true);
                        break;
                    case "itemmix":
                        windowsProviderService.ShowInstance<ItemMixWindow>(true);
                        break;
                    case "npcshop":
                        windowsProviderService.ShowInstance<NpcShopWindow>(true);
                        break;
                    case "rarecardreward":
                        windowsProviderService.ShowInstance<RareCardRewardWindow>(true);
                        break;
                    case "skill":
                        windowsProviderService.ShowInstance<SkillWindow>(true);
                        break;
                }
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
            }
        }
    }
}
