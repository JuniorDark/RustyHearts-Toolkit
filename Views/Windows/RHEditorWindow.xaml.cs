﻿using RHToolkit.ViewModels.Windows;
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

        private void DataGridView_AutoGeneratedColumns(object sender, EventArgs e)
        {
            foreach (var column in dataGridView.Columns)
            {
                var headerTextBlock = new TextBlock { Text = column.Header.ToString() };
                headerTextBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                column.MinWidth = headerTextBlock.DesiredSize.Width + 40;
            }
        }

        private void DataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit)
            {
                var viewModel = (RHEditorViewModel)DataContext;
                var columnIndex = e.Column.DisplayIndex;
                var rowIndex = e.Row.GetIndex();
                var oldValue = ((DataRowView)e.Row.Item).Row[columnIndex];
                var newValue = ((TextBox)e.EditingElement).Text;

                viewModel.RecordEdit(rowIndex, columnIndex, oldValue, newValue);
            }
        }

        private void DataGrid_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit)
            {
                var viewModel = (RHEditorViewModel)DataContext;
                var rowIndex = e.Row.GetIndex();

                var newRow = ((DataRowView)e.Row.Item).Row;
                var newValue = newRow.ItemArray;

                viewModel.RecordRowAddition(rowIndex, newValue);
            }
        }

        private void DataGridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            dataGridView.Focus();
        }
    }
}