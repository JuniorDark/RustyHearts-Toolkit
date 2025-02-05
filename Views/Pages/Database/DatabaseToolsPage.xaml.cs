using RHToolkit.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;

namespace RHToolkit.Views.Pages;

public partial class DatabaseToolsPage : INavigableView<DatabaseToolsViewModel>
{
    public DatabaseToolsViewModel ViewModel { get; }

    public DatabaseToolsPage(DatabaseToolsViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;

        InitializeComponent();
    }
}
