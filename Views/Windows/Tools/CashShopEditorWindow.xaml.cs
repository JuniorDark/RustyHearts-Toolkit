using RHToolkit.ViewModels.Windows;
using System.ComponentModel;
using System.Windows.Controls;

namespace RHToolkit.Views.Windows
{
    public partial class CashShopEditorWindow : Window
    {
        public CashShopEditorWindow(CashShopEditorViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        private void DataGridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            dataGridView.Focus();
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
