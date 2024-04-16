using RHGMTool.Models;
using RHGMTool.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace RHGMTool.Views
{
    public partial class ItemWindow : Window
    {
        private readonly ItemWindowViewModel _viewModel;

        public ItemWindow()
        {
            InitializeComponent();
            _viewModel = new ItemWindowViewModel();
            _viewModel.SelectedItemChanged += ViewModel_SelectedItemChanged;
            dataGridView.SelectionChanged += DataGridView_SelectionChanged;
            DataContext = _viewModel;
            cmbItemTrade.SelectedIndex = 0;
        }

        private void ViewModel_SelectedItemChanged(object? sender, ItemData selectedItem)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                dataGridView.SelectedItem = selectedItem;
                dataGridView.ScrollIntoView(selectedItem);
                dataGridView.UpdateLayout(); // Ensure the layout is updated
                DataGridView_SelectionChanged(dataGridView, null);
            });
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
            HandleSelectionChanged();
        }

        private void HandleSelectionChanged()
        {
            if (_viewModel != null && dataGridView.SelectedItem != null)
            {
                ItemData? selectedItem = dataGridView.SelectedItem as ItemData;

                UpdateItemFrameValues(selectedItem);
            }
        }


        private void UpdateItemFrameValues(ItemData? item)
        {
            var frameViewModel = _viewModel;
            frameViewModel.Item = item;
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
