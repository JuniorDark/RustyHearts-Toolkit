using RHToolkit.ViewModels.Windows;

namespace RHToolkit.Views.Windows
{
    public partial class MailWindow : Window
    {
        public MailWindow(MailWindowViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

    }
}
