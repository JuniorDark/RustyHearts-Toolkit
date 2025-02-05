using RHToolkit.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;

namespace RHToolkit.Views.Pages;

public partial class EditToolsPage : INavigableView<EditToolsViewModel>
{
    public EditToolsViewModel ViewModel { get; }

    public EditToolsPage(EditToolsViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;

        InitializeComponent();
    }
}
