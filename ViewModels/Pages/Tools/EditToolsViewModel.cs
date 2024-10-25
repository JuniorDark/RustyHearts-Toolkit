using RHToolkit.Models;
using RHToolkit.Models.MessageBox;
using RHToolkit.Services;
using RHToolkit.Views.Windows;
using Wpf.Ui.Controls;

namespace RHToolkit.ViewModels.Pages;

public partial class EditToolsViewModel(ISqLiteDatabaseService sqLiteDatabaseService, WindowsProviderService windowsProviderService) : ObservableObject
{
    private readonly ISqLiteDatabaseService _sqLiteDatabaseService = sqLiteDatabaseService;

    [ObservableProperty]
    private WindowCard[] _windowCards =
    [
        new("RH Table Editor", "Edit .rh table files", SymbolRegular.DocumentEdit24, "rheditor"),
    ];

    [ObservableProperty]
    private WindowCard[] _windowCardsTools =
    [
        new("Add Effect Editor", "Edit addeffect .rh table files", SymbolRegular.DocumentEdit24, "addeffecteditor"),
        new("Cash Shop Editor", "Edit cash shop .rh table file", SymbolRegular.DocumentEdit24, "cashshopeditor"),
        new("Drop Group Editor", "Edit dropgroup .rh table files", SymbolRegular.DocumentEdit24, "dropgroupeditor"),
        new("Enemy Editor", "Edit enemy .rh table files", SymbolRegular.DocumentEdit24, "enemyeditor"),
        new("Item Editor", "Edit items .rh table files", SymbolRegular.DocumentEdit24, "itemeditor"),
        new("NPC Editor", "Edit npc .rh table files", SymbolRegular.DocumentEdit24, "npceditor"),
        new("NPC Shop Editor", "Edit npcshops .rh table files", SymbolRegular.DocumentEdit24, "npcshopeditor"),
        new("Package Editor", "Edit package .rh table files", SymbolRegular.DocumentEdit24, "packageeditor"),
        new("Pet Editor", "Edit pet .rh table file", SymbolRegular.DocumentEdit24, "peteditor"),
        new("Quest Editor", "Edit quest .rh table files", SymbolRegular.DocumentEdit24, "questeditor"),
        new("Random Box Editor", "Edit randomrune .rh table file", SymbolRegular.DocumentEdit24, "randomruneeditor"),
        new("Set Item Editor", "Edit setitem .rh table file", SymbolRegular.DocumentEdit24, "setitemeditor"),
        new("Title Editor", "Edit title .rh table file", SymbolRegular.DocumentEdit24, "titleeditor"),
        new("World Editor", "Edit world .rh table files", SymbolRegular.DocumentEdit24, "worldeditor"),

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
                case "rheditor":
                    windowsProviderService.Show<RHEditorWindow>(true);
                    break;
                case "cashshopeditor":
                    windowsProviderService.Show<CashShopEditorWindow>(true);
                    break;
                case "packageeditor":
                    windowsProviderService.Show<PackageEditorWindow>(true);
                    break;
                case "randomruneeditor":
                    windowsProviderService.Show<RandomRuneEditorWindow>(true);
                    break;
                case "setitemeditor":
                    windowsProviderService.Show<SetItemEditorWindow>(true);
                    break;
                case "dropgroupeditor":
                    windowsProviderService.Show<DropGroupEditorWindow>(true);
                    break;
                case "npceditor":
                    windowsProviderService.Show<NpcEditorWindow>(true);
                    break;
                case "npcshopeditor":
                    windowsProviderService.Show<NPCShopEditorWindow>(true);
                    break;
                case "titleeditor":
                    windowsProviderService.Show<TitleEditorWindow>(true);
                    break;
                case "itemeditor":
                    windowsProviderService.Show<ItemEditorWindow>(true);
                    break;
                case "peteditor":
                    windowsProviderService.Show<PetEditorWindow>(true);
                    break;
                case "questeditor":
                    windowsProviderService.Show<QuestEditorWindow>(true);
                    break;
                case "enemyeditor":
                    windowsProviderService.Show<EnemyEditorWindow>(true);
                    break;
                case "addeffecteditor":
                    windowsProviderService.Show<AddEffectEditorWindow>(true);
                    break;
                case "worldeditor":
                    windowsProviderService.Show<WorldEditorWindow>(true);
                    break;
            }
        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"Error: {ex.Message}", "Error");
        }

    }
}
