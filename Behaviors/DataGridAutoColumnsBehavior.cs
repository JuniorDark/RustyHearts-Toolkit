using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Controls;

namespace RHToolkit.Behaviors;

public static class DataGridAutoColumnsBehavior
{
    public static readonly DependencyProperty EnableAutoColumnsProperty =
        DependencyProperty.RegisterAttached(
            "EnableAutoColumns",
            typeof(bool),
            typeof(DataGridAutoColumnsBehavior),
            new PropertyMetadata(false, OnEnableAutoColumnsChanged));

    public static void SetEnableAutoColumns(DependencyObject element, bool value)
        => element.SetValue(EnableAutoColumnsProperty, value);
    public static bool GetEnableAutoColumns(DependencyObject element)
        => (bool)element.GetValue(EnableAutoColumnsProperty);

    private static void OnEnableAutoColumnsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not DataGrid dg) return;

        var itemsSourceDpd = DependencyPropertyDescriptor
            .FromProperty(ItemsControl.ItemsSourceProperty, typeof(DataGrid));

        if ((bool)e.NewValue)
        {
            dg.AutoGenerateColumns = false;
            dg.RowDetailsVisibilityMode = DataGridRowDetailsVisibilityMode.Collapsed;
            dg.Loaded += OnGridLoaded;
            dg.DataContextChanged += OnDataContextChanged;
            itemsSourceDpd.AddValueChanged(dg, OnItemsSourceChanged);
        }
        else
        {
            dg.Loaded -= OnGridLoaded;
            dg.DataContextChanged -= OnDataContextChanged;
            itemsSourceDpd.RemoveValueChanged(dg, OnItemsSourceChanged);
        }
    }

    private static void OnGridLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is DataGrid dg) BuildColumns(dg);
    }
    private static void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (sender is DataGrid dg) BuildColumns(dg);
    }
    private static void OnItemsSourceChanged(object? sender, EventArgs e)
    {
        if (sender is DataGrid dg) BuildColumns(dg);
    }

    private static bool IsSimple(Type t) =>
    t.IsPrimitive || t.IsEnum || t == typeof(string) || t == typeof(decimal);

    private static void BuildColumns(DataGrid dg)
    {
        dg.Columns.Clear();

        if (dg.ItemsSource is not IEnumerable items) return;

        // ------------------------------------------------------------------
        // 1) Determine the type of each row
        // ------------------------------------------------------------------
        Type? rowType = null;

        // a)  If ItemsSource is IList and already has rows, use the first row
        if (items is IList list && list.Count > 0)
        {
            rowType = list[0]?.GetType();
        }

        // b) Otherwise try to get the generic argument of IEnumerable<T>
        if (rowType == null)
        {
            var ienumT = items.GetType()
                              .GetInterfaces()
                              .FirstOrDefault(i => i.IsGenericType &&
                                                   i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
            rowType = ienumT?.GetGenericArguments()[0];
        }

        if (rowType == null) return;

        // ------------------------------------------------------------------
        // 2) Build columns from the *type*
        // ------------------------------------------------------------------
        foreach (var pi in rowType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                  .Where(p => p.CanRead))
        {
            if (pi.Name is "Type" or "Boxes") continue;

            bool isBool = pi.PropertyType == typeof(bool) || pi.PropertyType == typeof(bool?);
            bool isEnumerable = typeof(IEnumerable).IsAssignableFrom(pi.PropertyType) &&
                                pi.PropertyType != typeof(string);
            bool isComplex = !isEnumerable && !IsSimple(pi.PropertyType) && !isBool;

            if (isBool)
            {
                var cbStyle = dg.TryFindResource("lb_checkbox_style") as Style;
                dg.Columns.Add(new DataGridCheckBoxColumn
                {
                    Header = pi.Name,
                    Binding = new Binding(pi.Name)
                    {
                        Mode = BindingMode.TwoWay,
                        UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                    },
                    ElementStyle = cbStyle,
                    EditingElementStyle = cbStyle,
                    IsThreeState = pi.PropertyType == typeof(bool?)
                });
            }
            else if (!isEnumerable && !isComplex)
            {
                dg.Columns.Add(new DataGridTextColumn
                {
                    Header = pi.Name,
                    Binding = new Binding(pi.Name)
                });
            }
            else // list OR complex object → in‑cell Expander with nested DataGrid
            {
                var col = new DataGridTemplateColumn { Header = pi.Name };

                // CELL TEMPLATE
                var expF = new FrameworkElementFactory(typeof(Expander));
                expF.SetValue(Expander.MarginProperty, new Thickness(2, 0, 2, 0));

                // Header: count for lists, type name for single object
                if (isEnumerable)
                    expF.SetBinding(Expander.HeaderProperty,
                        new Binding($"{pi.Name}.Count"));
                else
                    expF.SetBinding(Expander.HeaderProperty,
                        new Binding(pi.Name) { Converter = TypeNameConverter.Instance });

                // Nested DataGrid in Expander.Content
                var innerGridF = new FrameworkElementFactory(typeof(DataGrid));
                innerGridF.SetValue(DataGrid.AutoGenerateColumnsProperty, true);
                innerGridF.SetValue(DataGrid.IsReadOnlyProperty, false);
                innerGridF.SetValue(DataGrid.CanUserAddRowsProperty, true);
                innerGridF.SetValue(Control.MaxHeightProperty, 400.0);

                // Attach behaviors / styles
                innerGridF.SetValue(EnableAutoColumnsProperty, true);
                innerGridF.SetValue(DataGridBehavior.AdjustAutoGeneratedColumnMinWidthProperty, true);
                innerGridF.SetValue(DataGridBehavior.DisplayRowNumberProperty, true);

                // ItemsSource binding
                var itemsBinding = new Binding(pi.Name);
                if (!isEnumerable)
                    itemsBinding.Converter = SingleObjectEnumerableConverter.Instance;
                innerGridF.SetBinding(ItemsControl.ItemsSourceProperty, itemsBinding);

                expF.AppendChild(innerGridF);

                col.CellTemplate = new DataTemplate { VisualTree = expF };
                dg.Columns.Add(col);
            }
        }
        
        // rebuild if the first row arrives later
        if (items is INotifyCollectionChanged incc)
        {
            // Re-attach every time BuildColumns runs so we don’t leak handlers.
            incc.CollectionChanged -= OnFirstItemAdded;
            incc.CollectionChanged += OnFirstItemAdded;
        }

        void OnFirstItemAdded(object? s, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems is { Count: > 0 })
            {
                incc.CollectionChanged -= OnFirstItemAdded;
                BuildColumns(dg);
            }
        }
    }

    private static T? FindAncestor<T>(DependencyObject child) where T : DependencyObject
    {
        var p = VisualTreeHelper.GetParent(child);
        return p == null
            ? null
            : p is T t
                ? t
                : FindAncestor<T>(p);
    }

    public sealed class SingleObjectEnumerableConverter : IValueConverter
    {
        public static readonly SingleObjectEnumerableConverter Instance = new();

        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value == null ? null : new[] { value };

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    public sealed class TypeNameConverter : IValueConverter
    {
        public static readonly TypeNameConverter Instance = new();

        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value?.GetType().Name ?? string.Empty;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }


}
