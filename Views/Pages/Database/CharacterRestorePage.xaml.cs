using RHToolkit.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;

namespace RHToolkit.Views.Pages;

public partial class CharacterRestorePage : INavigableView<CharacterRestoreViewModel>
{
    public CharacterRestoreViewModel ViewModel { get; }

    public CharacterRestorePage(CharacterRestoreViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;

        InitializeComponent();
    }
}
