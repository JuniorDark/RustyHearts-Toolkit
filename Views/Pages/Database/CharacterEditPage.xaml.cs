using RHToolkit.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;

namespace RHToolkit.Views.Pages;

public partial class CharacterEditPage : INavigableView<CharacterEditViewModel>
{
    public CharacterEditViewModel ViewModel { get; }

    public CharacterEditPage(CharacterEditViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;

        InitializeComponent();
    }
}
