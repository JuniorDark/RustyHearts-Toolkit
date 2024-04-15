using RHGMTool.Models;
using RHGMTool.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace RHGMTool.Views
{
    public partial class ItemWindow : Window
    {
        private readonly FrameViewModel _viewModel;

        public ItemWindow()
        {
            InitializeComponent();
            _viewModel = new FrameViewModel();
            DataContext = _viewModel;
            cmbItemType.SelectedIndex = 0;
            cmbItemTrade.SelectedIndex = 0;
        }

        private void UpdateItemFrameValues(ItemData? item)
        {
            var frameViewModel = _viewModel;
            frameViewModel.Item = item;
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

        private void DataGridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_viewModel != null && dataGridView.SelectedItem != null)
            {
                ItemData? selectedItem = dataGridView.SelectedItem as ItemData;

                UpdateItemFrameValues(selectedItem);
            }
            
        }

        private void DataGridView_Loaded(object sender, RoutedEventArgs e)
        {
            if (dataGridView.Items.Count > 0)
            {
                var frameViewModel = _viewModel;

                int itemId = frameViewModel.ItemId;

                if (itemId != 0)
                {
                    // Find the item with the matching ID
                    var selectedItem = dataGridView.Items.Cast<ItemData>().FirstOrDefault(item => item.ID == itemId);

                    if (selectedItem != null)
                    {
                        // Scroll to the selected item
                        dataGridView.ScrollIntoView(selectedItem);

                        // Set the selected item
                        dataGridView.SelectedItem = selectedItem;
                    }
                }
                else
                {
                    dataGridView.SelectedItem = dataGridView.Items[0];
                }
                
            }
        }
    }
}
