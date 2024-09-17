using RHToolkit.ViewModels.Windows;

namespace RHToolkit.Views.Windows;

public partial class ItemMixWindow : Window
{
    public ItemMixWindow(ItemMixViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

}
