using RHToolkit.ViewModels.Windows;
using System.ComponentModel;

namespace RHToolkit.Views.Windows
{
    public partial class RandomRuneEditorWindow : Window
    {
        public RandomRuneEditorWindow(RandomRuneEditorViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        protected override async void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            if (DataContext is RandomRuneEditorViewModel viewModel)
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
