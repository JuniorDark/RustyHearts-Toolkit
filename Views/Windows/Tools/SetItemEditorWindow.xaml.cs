using RHToolkit.ViewModels.Windows;
using System.ComponentModel;
using System.Windows.Controls;

namespace RHToolkit.Views.Windows
{
    public partial class SetItemEditorWindow : Window
    {
        public SetItemEditorWindow(SetItemEditorViewModel viewModel)
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

        private void DataGridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            dataGridView.Focus();
        }

        protected override async void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            if (DataContext is SetItemEditorViewModel viewModel)
            {
                bool canClose = await viewModel.CloseFile();

                if (!canClose)
                {
                    e.Cancel = true;
                }
            }
        }
    }
}
