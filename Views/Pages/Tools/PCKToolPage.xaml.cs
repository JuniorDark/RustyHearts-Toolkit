using RHToolkit.Models.PCK;
using RHToolkit.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;
using System.Windows.Controls;

namespace RHToolkit.Views.Pages;

public partial class PCKToolPage : INavigableView<PCKToolViewModel>
{
    public PCKToolViewModel ViewModel { get; }

    public PCKToolPage(PCKToolViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;

        InitializeComponent();
    }
}
