using RHToolkit.ViewModels.Windows;

namespace RHToolkit.Views.Windows;

public partial class CharacterWindow : Window
{
    public CharacterWindow(CharacterWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

}
