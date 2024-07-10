using RHToolkit.ViewModels.Windows;

namespace RHToolkit.Views.Windows
{
    public partial class FortuneWindow : Window
    {
        public FortuneWindow(FortuneWindowViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
