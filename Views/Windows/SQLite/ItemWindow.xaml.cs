using RHToolkit.Models;
using RHToolkit.ViewModels.Windows;
using System.Windows.Controls;

namespace RHToolkit.Views.Windows;

public partial class ItemWindow : Window
{
    public ItemWindow(ItemWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
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

    private void DataGridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is ItemWindowViewModel viewModel)
        {
            if (viewModel.MessageType == "CashShopItemAdd")
            {
                viewModel.ItemDataList ??= [];

                // Get selected items from the DataGrid
                var selectedItems = dataGridView.SelectedItems.Cast<ItemData>().ToList();

                // Add selected items to ItemDataList
                viewModel.ItemDataList.Clear();
                viewModel.ItemDataList.AddRange(selectedItems);
            }
        }
    }


}
