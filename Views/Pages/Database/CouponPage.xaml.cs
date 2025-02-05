using RHToolkit.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;

namespace RHToolkit.Views.Pages;

public partial class CouponPage : INavigableView<CouponViewModel>
{
    public CouponViewModel ViewModel { get; }

    public CouponPage(CouponViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;

        InitializeComponent();
    }

}
