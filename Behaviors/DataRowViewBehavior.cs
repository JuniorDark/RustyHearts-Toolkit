using Microsoft.Xaml.Behaviors;
using System.Windows.Controls;

namespace RHToolkit.Behaviors;

public class DataRowViewBehavior : Behavior<FrameworkElement>
{
    private bool _isUpdating;

    public static readonly DependencyProperty ColumnProperty =
        DependencyProperty.Register(nameof(Column), typeof(string), typeof(DataRowViewBehavior), new PropertyMetadata(null));

    public string Column
    {
        get => (string)GetValue(ColumnProperty);
        set => SetValue(ColumnProperty, value);
    }

    public static readonly DependencyProperty UpdateItemValueCommandProperty =
        DependencyProperty.Register(nameof(UpdateItemValueCommand), typeof(IRelayCommand<(object? newValue, string column)>), typeof(DataRowViewBehavior));

    public IRelayCommand<(object? newValue, string column)> UpdateItemValueCommand
    {
        get => (IRelayCommand<(object? newValue, string column)>)GetValue(UpdateItemValueCommandProperty);
        set => SetValue(UpdateItemValueCommandProperty, value);
    }

    public static readonly DependencyProperty SelectedItemsStringProperty =
        DependencyProperty.Register(
            nameof(SelectedItemsString),
            typeof(string),
            typeof(DataRowViewBehavior),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedItemsStringChanged));

    public string SelectedItemsString
    {
        get { return (string)GetValue(SelectedItemsStringProperty); }
        set { SetValue(SelectedItemsStringProperty, value); }
    }

    private static void OnSelectedItemsStringChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var behavior = (DataRowViewBehavior)d;
        if (behavior.AssociatedObject is ListView && !behavior._isUpdating)
        {
            behavior.UpdateSelectedItems();
        }
    }

    protected override void OnAttached()
    {
        base.OnAttached();

        if (AssociatedObject is TextBox textBox)
        {
            textBox.TextChanged += OnControlValueChanged;
        }
        else if (AssociatedObject is CheckBox checkBox)
        {
            checkBox.Checked += OnControlValueChanged;
            checkBox.Unchecked += OnControlValueChanged;
        }
        else if (AssociatedObject is Wpf.Ui.Controls.NumberBox numberBox)
        {
            numberBox.ValueChanged += OnControlValueChanged;
        }
        else if (AssociatedObject is ComboBox comboBox)
        {
            comboBox.SelectionChanged += OnControlValueChanged;
        }
        else if (AssociatedObject is ListView listView)
        {
            listView.SelectionChanged += OnListViewSelectionChanged;
        }
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();

        if (AssociatedObject is TextBox textBox)
        {
            textBox.TextChanged -= OnControlValueChanged;
        }
        else if (AssociatedObject is CheckBox checkBox)
        {
            checkBox.Checked -= OnControlValueChanged;
            checkBox.Unchecked -= OnControlValueChanged;
        }
        else if (AssociatedObject is Wpf.Ui.Controls.NumberBox numberBox)
        {
            numberBox.ValueChanged -= OnControlValueChanged;
        }
        else if (AssociatedObject is ComboBox comboBox)
        {
            comboBox.SelectionChanged -= OnControlValueChanged;
        }
        else if (AssociatedObject is ListView listView)
        {
            listView.SelectionChanged -= OnListViewSelectionChanged;
        }
    }

    private void OnControlValueChanged(object sender, RoutedEventArgs e)
    {
        if (Column != null && UpdateItemValueCommand != null)
        {
            object? newValue = null;

            if (AssociatedObject is TextBox textBox)
            {
                newValue = string.IsNullOrWhiteSpace(textBox.Text) ? string.Empty : textBox.Text;
            }
            else if (AssociatedObject is CheckBox checkBox)
            {
                newValue = checkBox.IsChecked == true ? 1 : 0;
            }
            else if (AssociatedObject is Wpf.Ui.Controls.NumberBox numberBox)
            {
                newValue = numberBox.Value ?? 0;
            }
            else if (AssociatedObject is ComboBox comboBox)
            {
                newValue = comboBox.SelectedValue ?? 0;
            }

            var parameter = (newValue, Column);
            if (UpdateItemValueCommand.CanExecute(parameter))
            {
                UpdateItemValueCommand.Execute(parameter);
            }
        }
    }

    private void OnListViewSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdating)
            return;

        _isUpdating = true;

        if (Column != null && UpdateItemValueCommand != null && AssociatedObject is ListView listView)
        {
            var selectedItems = listView.SelectedItems
                                .Cast<dynamic>()
                                .Select(item => item.ID.ToString())
                                .ToList();

            var newValue = string.Join(",", selectedItems);

            var parameter = (newValue, Column);
            if (UpdateItemValueCommand.CanExecute(parameter))
            {
                UpdateItemValueCommand.Execute(parameter);
            }
        }

        _isUpdating = false;
    }

    private void UpdateSelectedItems()
    {
        if (AssociatedObject is not ListView listView || listView.ItemsSource == null || _isUpdating)
            return;

        _isUpdating = true;

        var selectedIds = (SelectedItemsString ?? string.Empty).Split(',')
                            .Where(id => !string.IsNullOrWhiteSpace(id))
                            .Select(int.Parse)
                            .ToList();

        listView.SelectedItems.Clear();
        foreach (var item in listView.Items)
        {
            var itemId = (item as dynamic).ID;
            if (selectedIds.Contains(itemId))
            {
                listView.SelectedItems.Add(item);
            }
        }

        _isUpdating = false;
    }
}
