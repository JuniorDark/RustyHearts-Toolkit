using RHToolkit.ViewModels.Windows;
using System.ComponentModel;

namespace RHToolkit.Views.Windows
{
    public partial class CashShopEditorWindow : Window
    {
        public CashShopEditorWindow(CashShopEditorViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        protected override async void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            if (DataContext is CashShopEditorViewModel viewModel)
            {
                bool canClose = await viewModel.CloseFile();

                if (!canClose)
                {
                    e.Cancel = true;
                }
            }
        }
    }
}
