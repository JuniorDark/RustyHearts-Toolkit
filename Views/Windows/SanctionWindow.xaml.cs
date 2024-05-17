using RHToolkit.ViewModels.Windows;

namespace RHToolkit.Views.Windows
{
    public partial class SanctionWindow : Window
    {
        public SanctionWindow(SanctionWindowViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
