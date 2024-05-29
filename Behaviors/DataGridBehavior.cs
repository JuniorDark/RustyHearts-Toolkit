﻿using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace RHToolkit.Behaviors
{
    public class DataGridBehavior
    {
        #region DisplayRowNumber

        public static readonly DependencyProperty DisplayRowNumberProperty =
            DependencyProperty.RegisterAttached(
                "DisplayRowNumber",
                typeof(bool),
                typeof(DataGridBehavior),
                new FrameworkPropertyMetadata(false, OnDisplayRowNumberChanged)
            );

        public static bool GetDisplayRowNumber(DependencyObject target) =>
            (bool)target.GetValue(DisplayRowNumberProperty);

        public static void SetDisplayRowNumber(DependencyObject target, bool value) =>
            target.SetValue(DisplayRowNumberProperty, value);

        private static void OnDisplayRowNumberChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            if (target is not DataGrid dataGrid) return;

            if ((bool)e.NewValue)
            {
                void LoadedRowHandler(object? sender, DataGridRowEventArgs ea)
                {
                    if (!GetDisplayRowNumber(dataGrid))
                    {
                        dataGrid.LoadingRow -= LoadedRowHandler;
                        return;
                    }
                    ea.Row.Header = ea.Row.GetIndex() + 1;
                }

                dataGrid.LoadingRow += LoadedRowHandler;

                void ItemsChangedHandler(object? sender, ItemsChangedEventArgs ea)
                {
                    if (!GetDisplayRowNumber(dataGrid))
                    {
                        dataGrid.ItemContainerGenerator.ItemsChanged -= ItemsChangedHandler;
                        return;
                    }

                    foreach (var row in GetVisualChildCollection<DataGridRow>(dataGrid))
                    {
                        row.Header = row.GetIndex() + 1;
                    }
                }

                dataGrid.ItemContainerGenerator.ItemsChanged += ItemsChangedHandler;
            }
        }

        #endregion // DisplayRowNumber

        #region Get Visuals

        private static List<T> GetVisualChildCollection<T>(object parent) where T : Visual
        {
            var visualCollection = new List<T>();
            if (parent is DependencyObject depObj)
            {
                GetVisualChildCollection(depObj, visualCollection);
            }
            return visualCollection;
        }

        private static void GetVisualChildCollection<T>(DependencyObject parent, List<T> visualCollection) where T : Visual
        {
            var count = VisualTreeHelper.GetChildrenCount(parent);
            for (var i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T tChild)
                {
                    visualCollection.Add(tChild);
                }
                GetVisualChildCollection(child, visualCollection);
            }
        }

        #endregion // Get Visuals

        #region Column Min Width

        public static readonly DependencyProperty AdjustAutoGeneratedColumnMinWidthProperty =
            DependencyProperty.RegisterAttached("AdjustAutoGeneratedColumnMinWidth", typeof(bool), typeof(DataGridBehavior), new PropertyMetadata(false, OnAdjustAutoGeneratedColumnMinWidthChanged));

        public static bool GetAdjustAutoGeneratedColumnMinWidth(DependencyObject obj)
        {
            return (bool)obj.GetValue(AdjustAutoGeneratedColumnMinWidthProperty);
        }

        public static void SetAdjustAutoGeneratedColumnMinWidth(DependencyObject obj, bool value)
        {
            obj.SetValue(AdjustAutoGeneratedColumnMinWidthProperty, value);
        }

        private static void OnAdjustAutoGeneratedColumnMinWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DataGrid dataGrid)
            {
                if ((bool)e.NewValue)
                {
                    dataGrid.AutoGeneratedColumns += DataGrid_AutoGeneratedColumns;
                }
                else
                {
                    dataGrid.AutoGeneratedColumns -= DataGrid_AutoGeneratedColumns;
                }
            }
        }

        private static void DataGrid_AutoGeneratedColumns(object? sender, EventArgs e)
        {
            if (sender is DataGrid dataGrid)
            {
                foreach (var column in dataGrid.Columns)
                {
                    var headerText = column.Header?.ToString() ?? string.Empty;
                    var headerTextBlock = new TextBlock { Text = headerText };
                    headerTextBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

                    double margin = 20;
                    column.MinWidth = headerTextBlock.DesiredSize.Width + margin;

                    // Set the column width to Auto initially
                    column.Width = DataGridLength.Auto;
                }

                dataGrid.LayoutUpdated += DataGrid_LayoutUpdated;
            }
        }

        private static void DataGrid_LayoutUpdated(object? sender, EventArgs e)
        {
            if (sender is DataGrid dataGrid)
            {
                foreach (var column in dataGrid.Columns)
                {
                    // Calculate the desired width with a margin
                    double desiredWidth = column.ActualWidth < column.MinWidth ? column.MinWidth : column.ActualWidth;
                    double margin = 20;
                    desiredWidth += margin;

                    // Apply the width to the column
                    column.Width = new DataGridLength(desiredWidth);
                }
            }
        }

        #endregion //Column Min Width

        #region SelectedCell

        public static readonly DependencyProperty SelectedCellProperty =
            DependencyProperty.RegisterAttached(
                "SelectedCell",
                typeof(Point),
                typeof(DataGridBehavior),
                new FrameworkPropertyMetadata(new Point(-1, -1), OnSelectedCellChanged));

        public static Point GetSelectedCell(DependencyObject obj)
        {
            return (Point)obj.GetValue(SelectedCellProperty);
        }

        public static void SetSelectedCell(DependencyObject obj, Point value)
        {
            obj.SetValue(SelectedCellProperty, value);
        }

        private static void OnSelectedCellChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DataGrid dataGrid)
            {
                Point cell = (Point)e.NewValue;
                if (cell.X >= 0 && cell.Y >= 0 && dataGrid.ItemsSource != null)
                {
                    if (dataGrid.Items.Count > (int)cell.X && dataGrid.Columns.Count > (int)cell.Y)
                    {
                        // Select the cell
                        dataGrid.SelectedCells.Clear();
                        DataGridCellInfo cellInfo = new(
                            dataGrid.Items[(int)cell.X], dataGrid.Columns[(int)cell.Y]);
                        dataGrid.SelectedCells.Add(cellInfo);

                        // Scroll to the selected cell
                        dataGrid.ScrollIntoView(cellInfo.Item);
                    }
                }
            }
        }
        #endregion
    }
}
