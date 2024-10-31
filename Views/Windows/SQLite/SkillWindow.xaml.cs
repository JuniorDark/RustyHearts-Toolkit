using RHToolkit.Models;
using RHToolkit.ViewModels.Windows;
using System.Windows.Controls;

namespace RHToolkit.Views.Windows;

public partial class SkillWindow : Window
{
    public SkillWindow(SkillWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void DataGridView_Loaded(object sender, RoutedEventArgs e)
    {
        if (dataGridView.Items.Count > 0)
        {
            dataGridView.SelectedItem = dataGridView.Items[0];

        }
    }

}
