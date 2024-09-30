using RHToolkit.ViewModels.Windows;
using System.ComponentModel;

namespace RHToolkit.Views.Windows
{
    public partial class PetEditorWindow : Window
    {
        public PetEditorWindow(PetEditorViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        protected override async void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            if (DataContext is PetEditorViewModel viewModel)
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
