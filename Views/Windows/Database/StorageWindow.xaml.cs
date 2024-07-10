using RHToolkit.ViewModels.Windows;

namespace RHToolkit.Views.Windows;

public partial class StorageWindow : Window
{
    public StorageWindow(StorageWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

}
