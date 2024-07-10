using RHToolkit.ViewModels.Windows;

namespace RHToolkit.Views.Windows;

public partial class InventoryWindow : Window
{
    public InventoryWindow(InventoryWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

}
