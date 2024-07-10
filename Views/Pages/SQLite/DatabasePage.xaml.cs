using RHToolkit.ViewModels.Pages;
using Wpf.Ui.Controls;

namespace RHToolkit.Views.Pages;

public partial class DatabasePage : INavigableView<DatabaseViewModel>
{
    public DatabaseViewModel ViewModel { get; }

    public DatabasePage(DatabaseViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;

        InitializeComponent();
    }
}
