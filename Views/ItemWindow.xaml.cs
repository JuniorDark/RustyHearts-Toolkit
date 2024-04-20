using Microsoft.Extensions.DependencyInjection;
using RHToolkit.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace RHToolkit.Views
{
    public partial class ItemWindow : Window
    {
        public ItemWindow(ItemWindowViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
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
