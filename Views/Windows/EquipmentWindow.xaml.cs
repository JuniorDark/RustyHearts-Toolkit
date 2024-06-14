using RHToolkit.ViewModels.Windows;

namespace RHToolkit.Views.Windows;

public partial class EquipmentWindow : Window
{
    public EquipmentWindow(EquipmentWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

}
