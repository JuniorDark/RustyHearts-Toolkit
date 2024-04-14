using RHGMTool.Models;
using RHGMTool.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace RHGMTool.Views
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

        private void OpenItemWindowCommandExecute(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag is int slotIndex)
            {
                if (DataContext is MailWindowViewModel viewModel)
                {
                    viewModel.OpenItemWindowCommand.Execute(slotIndex);
                }
            }
        }

    }
}
