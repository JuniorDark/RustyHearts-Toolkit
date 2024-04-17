using RHToolkit.Models;
using RHToolkit.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace RHToolkit.Views
{
    public partial class ItemWindow : Window
    {
        private readonly ItemWindowViewModel _viewModel;

        public ItemWindow()
        {
            InitializeComponent();
            _viewModel = new ItemWindowViewModel();

            DataContext = _viewModel;
            cmbItemTrade.SelectedIndex = 0;
        }

        private void ComboBox_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is ComboBox comboBox)
            {
                if (!(bool)e.NewValue)
                {
                    if (comboBox.Items.Count > 0)
                    {
                        comboBox.SelectedIndex = 0;
                    }
                }

            }
        }

        private void DataGridView_Loaded(object sender, RoutedEventArgs e)
        {
            if (dataGridView.Items.Count > 0)
            {
                dataGridView.SelectedItem = dataGridView.Items[0];

            }
        }

    }
}
