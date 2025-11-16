using RHToolkit.Models.Model3D;
using System.ComponentModel;

namespace RHToolkit.Views.Windows;

public partial class ModelViewWindow : Window
{
    public ModelViewWindow(ModelViewManager viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    protected override async void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);

        if (DataContext is ModelViewManager viewModel)
        {
            bool canClose = await viewModel.CloseFile();

            if (!canClose)
            {
                e.Cancel = true;
            }
        }
    }

}
