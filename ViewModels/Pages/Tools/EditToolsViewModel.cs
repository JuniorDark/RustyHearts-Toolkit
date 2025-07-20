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
        new(Resources.TableEditorTitle, Resources.TableEditorDesc, SymbolRegular.DocumentEdit24, "rheditor"),
        new(Resources.WDataEditorTitle, Resources.WDataEditorDesc, SymbolRegular.DocumentEdit24, "wdataeditor"),
    ];

    [ObservableProperty]
    private WindowCard[] _windowCardsTools =
    [
        new(Resources.AddEffectEditorTitle, Resources.AddEffectEditorDesc, SymbolRegular.DocumentEdit24, "addeffecteditor"),
        new(Resources.CashShopEditorTitle, Resources.CashShopEditorDesc, SymbolRegular.DocumentEdit24, "cashshopeditor"),
        new(Resources.DropGroupEditorTitle, Resources.DropGroupEditorDesc, SymbolRegular.DocumentEdit24, "dropgroupeditor"),
        new(Resources.EnemyEditorTitle, Resources.EnemyEditorDesc, SymbolRegular.DocumentEdit24, "enemyeditor"),
        new(Resources.ItemEditorTitle, Resources.ItemEditorDesc, SymbolRegular.DocumentEdit24, "itemeditor"),
        new(Resources.NPCEditorTitle, Resources.NPCEditorDesc, SymbolRegular.DocumentEdit24, "npceditor"),
        new(Resources.NPCShopEditorTitle, Resources.NPCShopEditorDesc, SymbolRegular.DocumentEdit24, "npcshopeditor"),
        new(Resources.PackageEditorTitle, Resources.PackageEditorDesc, SymbolRegular.DocumentEdit24, "packageeditor"),
        new(Resources.PetEditorTitle, Resources.PetEditorDesc, SymbolRegular.DocumentEdit24, "peteditor"),
        new(Resources.QuestEditorTitle, Resources.QuestEditorDesc, SymbolRegular.DocumentEdit24, "questeditor"),
        new(Resources.RandomRuneEditorTitle, Resources.RandomRuneEditorDesc, SymbolRegular.DocumentEdit24, "randomruneeditor"),
        new(Resources.SkillEditorTitle, Resources.SkillEditorDesc, SymbolRegular.DocumentEdit24, "skilleditor"),
        new(Resources.SetItemEditorTitle, Resources.SetItemEditorDesc, SymbolRegular.DocumentEdit24, "setitemeditor"),
        new(Resources.TitleEditorTitle, Resources.TitleEditorDesc, SymbolRegular.DocumentEdit24, "titleeditor"),
        new(Resources.WorldEditorTitle, Resources.WorldEditorDesc, SymbolRegular.DocumentEdit24, "worldeditor"),

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
                case "wdataeditor":
                    windowsProviderService.Show<WDataEditorWindow>(true);
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
                case "skilleditor":
                    windowsProviderService.Show<SkillEditorWindow>(true);
                    break;
            }
        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
        }

    }
}
