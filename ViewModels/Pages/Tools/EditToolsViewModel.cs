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
    ];

    [ObservableProperty]
    private WindowCard[] _windowCardsTools =
    [
        new("Cash Shop Editor", "Edit cash shop .rh table file", SymbolRegular.DocumentEdit24, "cashshopeditor"),
        new("Package Editor", "Edit unionpackage .rh table file", SymbolRegular.DocumentEdit24, "packageeditor"),
        new("Set Item Editor", "Edit setitem .rh table file", SymbolRegular.DocumentEdit24, "setitemeditor"),
        new("Random Rune Editor", "Edit randomrune .rh table file", SymbolRegular.DocumentEdit24, "randomruneeditor"),
        new("Drop Group Editor", "Edit dropgroup .rh table files", SymbolRegular.DocumentEdit24, "dropgroupeditor"),
        new("NPC Editor", "Edit npc .rh table files", SymbolRegular.DocumentEdit24, "npceditor"),
        new("NPC Shop Editor", "Edit npcshop .rh table files", SymbolRegular.DocumentEdit24, "npcshopeditor"),
        new("Title Editor", "Edit title .rh table file", SymbolRegular.DocumentEdit24, "titleeditor"),

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
            }
        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"Error: {ex.Message}", "Error");
        }

    }
}
