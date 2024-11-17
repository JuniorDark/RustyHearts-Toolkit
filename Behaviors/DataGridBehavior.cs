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

        /// <summary>
        /// Gets the value of the DisplayRowNumber attached property.
        /// </summary>
        /// <param name="target">The target dependency object.</param>
        /// <returns>True if row numbers should be displayed, otherwise false.</returns>
        public static bool GetDisplayRowNumber(DependencyObject target) =>
            (bool)target.GetValue(DisplayRowNumberProperty);

        /// <summary>
        /// Sets the value of the DisplayRowNumber attached property.
        /// </summary>
        /// <param name="target">The target dependency object.</param>
        /// <param name="value">True to display row numbers, otherwise false.</param>
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

        #region GetVisuals

        /// <summary>
        /// Gets a collection of visual child elements of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of visual child elements to retrieve.</typeparam>
        /// <param name="parent">The parent object.</param>
        /// <returns>A list of visual child elements of the specified type.</returns>
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

        #region AdjustAutoGeneratedColumnMinWidth

        public static readonly DependencyProperty AdjustAutoGeneratedColumnMinWidthProperty =
            DependencyProperty.RegisterAttached("AdjustAutoGeneratedColumnMinWidth", typeof(bool), typeof(DataGridBehavior), new PropertyMetadata(false, OnAdjustAutoGeneratedColumnMinWidthChanged));

        /// <summary>
        /// Gets the value of the AdjustAutoGeneratedColumnMinWidth attached property.
        /// </summary>
        /// <param name="obj">The target dependency object.</param>
        /// <returns>True if the minimum width of auto-generated columns should be adjusted, otherwise false.</returns>
        public static bool GetAdjustAutoGeneratedColumnMinWidth(DependencyObject obj)
        {
            return (bool)obj.GetValue(AdjustAutoGeneratedColumnMinWidthProperty);
        }

        /// <summary>
        /// Sets the value of the AdjustAutoGeneratedColumnMinWidth attached property.
        /// </summary>
        /// <param name="obj">The target dependency object.</param>
        /// <param name="value">True to adjust the minimum width of auto-generated columns, otherwise false.</param>
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

        /// <summary>
        /// Gets the value of the SelectedCell attached property.
        /// </summary>
        /// <param name="obj">The target dependency object.</param>
        /// <returns>The selected cell as a Point.</returns>
        public static Point GetSelectedCell(DependencyObject obj)
        {
            return (Point)obj.GetValue(SelectedCellProperty);
        }

        /// <summary>
        /// Sets the value of the SelectedCell attached property.
        /// </summary>
        /// <param name="obj">The target dependency object.</param>
        /// <param name="value">The selected cell as a Point.</param>
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

        #region SelectedRow

        public static readonly DependencyProperty SelectedRowProperty =
            DependencyProperty.RegisterAttached(
                "SelectedRow",
                typeof(int),
                typeof(DataGridBehavior),
                new FrameworkPropertyMetadata(-1, OnSelectedRowChanged));

        /// <summary>
        /// Gets the value of the SelectedRow attached property.
        /// </summary>
        /// <param name="obj">The target dependency object.</param>
        /// <returns>The selected row index.</returns>
        public static int GetSelectedRow(DependencyObject obj)
        {
            return (int)obj.GetValue(SelectedRowProperty);
        }

        /// <summary>
        /// Sets the value of the SelectedRow attached property.
        /// </summary>
        /// <param name="obj">The target dependency object.</param>
        /// <param name="value">The selected row index.</param>
        public static void SetSelectedRow(DependencyObject obj, int value)
        {
            obj.SetValue(SelectedRowProperty, value);
        }

        private static void OnSelectedRowChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DataGrid dataGrid)
            {
                int rowIndex = (int)e.NewValue;
                if (rowIndex >= 0 && dataGrid.ItemsSource != null)
                {
                    if (dataGrid.Items.Count > rowIndex)
                    {
                        // Select the row
                        dataGrid.SelectedItem = dataGrid.Items[rowIndex];

                        // Scroll to the selected row
                        dataGrid.ScrollIntoView(dataGrid.Items[rowIndex]);
                    }
                }
            }
        }

        #endregion

        #region ScrollIntoView

        public static readonly DependencyProperty EnableScrollIntoViewProperty =
            DependencyProperty.RegisterAttached("EnableScrollIntoView", typeof(bool), typeof(DataGridBehavior), new PropertyMetadata(false, OnEnableScrollIntoViewChanged));

        /// <summary>
        /// Gets the value of the EnableScrollIntoView attached property.
        /// </summary>
        /// <param name="obj">The target dependency object.</param>
        /// <returns>True if scroll into view is enabled, otherwise false.</returns>
        public static bool GetEnableScrollIntoView(DependencyObject obj)
        {
            return (bool)obj.GetValue(EnableScrollIntoViewProperty);
        }

        /// <summary>
        /// Sets the value of the EnableScrollIntoView attached property.
        /// </summary>
        /// <param name="obj">The target dependency object.</param>
        /// <param name="value">True to enable scroll into view, otherwise false.</param>
        public static void SetEnableScrollIntoView(DependencyObject obj, bool value)
        {
            obj.SetValue(EnableScrollIntoViewProperty, value);
        }

        private static void OnEnableScrollIntoViewChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DataGrid dataGrid)
            {
                if ((bool)e.NewValue)
                {
                    dataGrid.SelectionChanged += DataGrid_SelectionChanged;
                }
                else
                {
                    dataGrid.SelectionChanged -= DataGrid_SelectionChanged;
                }
            }
        }

        private static void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is DataGrid dataGrid)
            {
                if (dataGrid.SelectedItem != null)
                {
                    dataGrid.Dispatcher.InvokeAsync(() =>
                    {
                        if (dataGrid.SelectedItem != null)
                        {
                            dataGrid.ScrollIntoView(dataGrid.SelectedItem);
                        }
                    }, DispatcherPriority.ContextIdle);
                }
            }
        }
        #endregion

        #region DisableHorizontalScrollOnEditOrSelection

        public static readonly DependencyProperty DisableHorizontalScrollOnEditOrSelectionProperty =
            DependencyProperty.RegisterAttached(
                "DisableHorizontalScrollOnEditOrSelection",
                typeof(bool),
                typeof(DataGridBehavior),
                new FrameworkPropertyMetadata(false, OnDisableHorizontalScrollOnEditOrSelectionChanged)
            );

        /// <summary>
        /// Gets the value of the DisableHorizontalScrollOnEditOrSelection attached property.
        /// </summary>
        /// <param name="target">The target dependency object.</param>
        /// <returns>True if horizontal scroll should be disabled on edit or selection, otherwise false.</returns>
        public static bool GetDisableHorizontalScrollOnEditOrSelection(DependencyObject target) =>
            (bool)target.GetValue(DisableHorizontalScrollOnEditOrSelectionProperty);

        /// <summary>
        /// Sets the value of the DisableHorizontalScrollOnEditOrSelection attached property.
        /// </summary>
        /// <param name="target">The target dependency object.</param>
        /// <param name="value">True to disable horizontal scroll on edit or selection, otherwise false.</param>
        public static void SetDisableHorizontalScrollOnEditOrSelection(DependencyObject target, bool value) =>
            target.SetValue(DisableHorizontalScrollOnEditOrSelectionProperty, value);

        private static void OnDisableHorizontalScrollOnEditOrSelectionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DataGrid dataGrid)
            {
                if ((bool)e.NewValue)
                {
                    dataGrid.SelectedCellsChanged += DataGrid_SelectedCellsChanged;
                    dataGrid.PreparingCellForEdit += DataGrid_PreparingCellForEdit;
                }
                else
                {
                    dataGrid.SelectedCellsChanged -= DataGrid_SelectedCellsChanged;
                    dataGrid.PreparingCellForEdit -= DataGrid_PreparingCellForEdit;
                }
            }
        }

        private static void DataGrid_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            if (sender is DataGrid dataGrid && e.AddedCells.Count > 0)
            {
                var firstAddedCell = e.AddedCells[0];
                if (firstAddedCell.Column != null)
                {
                    dataGrid.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        dataGrid.ScrollIntoView(firstAddedCell.Item, firstAddedCell.Column);
                    }), DispatcherPriority.Background);
                }
            }
        }

        private static void DataGrid_PreparingCellForEdit(object? sender, DataGridPreparingCellForEditEventArgs e)
        {
            if (sender is DataGrid dataGrid)
            {
                dataGrid.Dispatcher.BeginInvoke(new Action(() =>
                {
                    dataGrid.ScrollIntoView(e.Row.Item, dataGrid.Columns[e.Column.DisplayIndex]);
                }), DispatcherPriority.Background);
            }
        }

        #endregion // DisableHorizontalScrollOnEditOrSelection

        #region WrapTextOnShiftEnter

        public static readonly DependencyProperty WrapTextOnShiftEnterProperty =
            DependencyProperty.RegisterAttached(
                "WrapTextOnShiftEnter",
                typeof(bool),
                typeof(DataGridBehavior),
                new FrameworkPropertyMetadata(false, OnWrapTextOnShiftEnterChanged)
            );

        /// <summary>
        /// Gets the value of the WrapTextOnShiftEnter attached property.
        /// </summary>
        /// <param name="target">The target dependency object.</param>
        /// <returns>True if text wrapping on Shift+Enter is enabled, otherwise false.</returns>
        public static bool GetWrapTextOnShiftEnter(DependencyObject target) =>
            (bool)target.GetValue(WrapTextOnShiftEnterProperty);

        /// <summary>
        /// Sets the value of the WrapTextOnShiftEnter attached property.
        /// </summary>
        /// <param name="target">The target dependency object.</param>
        /// <param name="value">True to enable text wrapping on Shift+Enter, otherwise false.</param>
        public static void SetWrapTextOnShiftEnter(DependencyObject target, bool value) =>
            target.SetValue(WrapTextOnShiftEnterProperty, value);

        private static void OnWrapTextOnShiftEnterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DataGrid dataGrid)
            {
                if ((bool)e.NewValue)
                {
                    dataGrid.PreviewKeyDown += DataGrid_PreviewKeyDown;
                }
                else
                {
                    dataGrid.PreviewKeyDown -= DataGrid_PreviewKeyDown;
                }
            }
        }

        private static void DataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)))
            {
                DataGrid dataGrid = (DataGrid)sender;
                var cell = dataGrid.CurrentCell;

                if (cell.Column.GetCellContent(cell.Item) is not TextBox editingElement)
                    return;

                string text = editingElement.Text;
                int caretIndex = editingElement.CaretIndex;

                string newText = string.Concat(text.AsSpan(0, caretIndex), Environment.NewLine, text.AsSpan(caretIndex));

                editingElement.Text = newText;
                editingElement.CaretIndex = caretIndex + Environment.NewLine.Length;

                e.Handled = true;
            }
        }

        #endregion // WrapTextOnShiftEnter
    }
}