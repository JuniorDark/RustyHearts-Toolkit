using RHToolkit.ViewModels.Windows;
using Wpf.Ui.Appearance;

namespace RHToolkit.Views.Windows
{
    public partial class MailWindow : Window
    {
        public MailWindow(MailWindowViewModel viewModel)
        {
            SystemThemeWatcher.Watch(this);

            InitializeComponent();
            DataContext = viewModel;
        }

    }
}
