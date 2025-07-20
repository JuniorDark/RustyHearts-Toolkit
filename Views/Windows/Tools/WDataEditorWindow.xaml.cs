using RHToolkit.ViewModels.Windows;
using System.ComponentModel;

namespace RHToolkit.Views.Windows;

public partial class WDataEditorWindow : Window
{
    public WDataEditorWindow(WDataEditorViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    protected override async void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);

        if (DataContext is WDataEditorViewModel viewModel)
        {
            bool canClose = await viewModel.WDataManager.CloseFile();

            if (!canClose)
            {
                e.Cancel = true;
            }
        }
    }

}
