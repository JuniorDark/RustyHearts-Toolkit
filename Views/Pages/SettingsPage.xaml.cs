using RHToolkit.ViewModels.Pages;
using System.Windows.Controls;
using Wpf.Ui.Controls;

namespace RHToolkit.Views.Pages;

public partial class SettingsPage : INavigableView<SettingsViewModel>
{
    public SettingsViewModel ViewModel { get; }

    public SettingsPage(SettingsViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;

        InitializeComponent();
    }

    private void CmbLanguage_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ViewModel.HandleLanguageSelectionChange();
    }
}
