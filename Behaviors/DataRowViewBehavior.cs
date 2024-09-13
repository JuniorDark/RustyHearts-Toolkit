using Microsoft.Xaml.Behaviors;
using System.Windows.Controls;

namespace RHToolkit.Behaviors;

public class DataRowViewBehavior : Behavior<FrameworkElement>
{
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
                newValue = checkBox.IsChecked ?? false;
            }
            else if (AssociatedObject is Wpf.Ui.Controls.NumberBox numberBox)
            {
                newValue = numberBox.Value;
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
}