using RHToolkit.ViewModels.Pages;
using Wpf.Ui.Controls;

namespace RHToolkit.Views.Pages;

public partial class RHEditorPage : INavigableView<RHEditorViewModel>
{
    public RHEditorViewModel ViewModel { get; }

    public RHEditorPage(RHEditorViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;

        InitializeComponent();
    }
}
