using RHToolkit.ViewModels.Windows;
using System.ComponentModel;
using System.Data;
using System.Windows.Controls;
using Wpf.Ui.Appearance;

namespace RHToolkit.Views.Windows
{
    public partial class RHEditorWindow : Window
    {
        public RHEditorWindow(RHEditorViewModel viewModel)
        {
            SystemThemeWatcher.Watch(this);
            InitializeComponent();
            DataContext = viewModel;
        }

        private void DataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (DataContext is RHEditorViewModel viewModel && e.EditAction == DataGridEditAction.Commit)
            {
                var columnIndex = e.Column.DisplayIndex;
                var rowIndex = e.Row.GetIndex();
                var oldValue = ((DataRowView)e.Row.Item).Row[columnIndex];
                var newValue = ((TextBox)e.EditingElement).Text;

                viewModel.RecordEdit(rowIndex, columnIndex, oldValue, newValue);
            }
        }

        private void DataGridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            dataGridView.Focus();
        }

        protected override async void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            if (DataContext is RHEditorViewModel viewModel)
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
