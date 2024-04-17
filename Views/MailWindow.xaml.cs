using RHToolkit.ViewModels;
using System.Windows;

namespace RHToolkit.Views
{
    public partial class MailWindow : Window
    {
        private readonly MailWindowViewModel _viewModel;

        public MailWindow()
        {
            InitializeComponent();
            _viewModel = new MailWindowViewModel();
            DataContext = _viewModel;

        }

    }
}
