using RHToolkit.ViewModels;
using Wpf.Ui;
using Wpf.Ui.Appearance;

namespace RHToolkit.Views.Windows
{
    public partial class MailWindow : Window
    {
        public MailWindowViewModel ViewModel { get; }

        public MailWindow(MailWindowViewModel viewModel, ISnackbarService snackbarService)
        {
            SystemThemeWatcher.Watch(this);

            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();

            snackbarService.SetSnackbarPresenter(MailSnackbarPresenter);
        }

    }
}
