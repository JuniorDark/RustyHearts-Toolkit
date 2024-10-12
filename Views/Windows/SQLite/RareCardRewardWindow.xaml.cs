using RHToolkit.ViewModels.Windows;

namespace RHToolkit.Views.Windows;

public partial class RareCardRewardWindow : Window
{
    public RareCardRewardWindow(RareCardRewardViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

}
