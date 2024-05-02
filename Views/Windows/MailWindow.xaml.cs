using RHToolkit.ViewModels.Windows;
using Wpf.Ui.Appearance;

namespace RHToolkit.Views.Windows
{
    public partial class MailWindow : Window
    {
        public MailWindowViewModel ViewModel { get; }

        public MailWindow(MailWindowViewModel viewModel)
        {
            SystemThemeWatcher.Watch(this);

            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();
        }

    }
}
