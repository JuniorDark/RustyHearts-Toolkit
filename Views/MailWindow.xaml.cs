using RHToolkit.ViewModels;
using System.Windows;

namespace RHToolkit.Views
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
