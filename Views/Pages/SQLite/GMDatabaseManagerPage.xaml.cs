using RHToolkit.ViewModels.Pages;
using Wpf.Ui.Controls;

namespace RHToolkit.Views.Pages;

public partial class GMDatabaseManagerPage : INavigableView<GMDatabaseManagerViewModel>
{
    public GMDatabaseManagerViewModel ViewModel { get; }

    public GMDatabaseManagerPage(GMDatabaseManagerViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;

        InitializeComponent();
    }
}
