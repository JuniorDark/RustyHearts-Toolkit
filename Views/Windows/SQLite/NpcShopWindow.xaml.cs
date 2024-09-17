using RHToolkit.ViewModels.Windows;

namespace RHToolkit.Views.Windows;

public partial class NpcShopWindow : Window
{
    public NpcShopWindow(NPCShopViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

}
