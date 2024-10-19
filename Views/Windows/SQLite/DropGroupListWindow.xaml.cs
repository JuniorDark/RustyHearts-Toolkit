using RHToolkit.ViewModels.Windows;

namespace RHToolkit.Views.Windows;

public partial class DropGroupListWindow : Window
{
    public DropGroupListWindow(DropGroupListViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

}
