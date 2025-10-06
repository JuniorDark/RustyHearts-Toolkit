using RHToolkit.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;

namespace RHToolkit.Views.Pages;

public partial class ModelToolsPage : INavigableView<ModelToolsViewModel>
{
    public ModelToolsViewModel ViewModel { get; }

    public ModelToolsPage(ModelToolsViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;

        InitializeComponent();
    }
}
