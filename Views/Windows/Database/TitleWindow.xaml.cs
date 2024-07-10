using RHToolkit.ViewModels.Windows;

namespace RHToolkit.Views.Windows
{
    public partial class TitleWindow : Window
    {
        public TitleWindow(TitleWindowViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
