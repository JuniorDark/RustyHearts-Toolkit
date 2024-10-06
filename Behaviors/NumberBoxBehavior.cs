using System.Windows.Controls;
using Wpf.Ui.Controls;

namespace RHToolkit.Behaviors;

//Fix for NumberBox Caret Reset
public static class NumberBoxBehavior
{
    public static readonly DependencyProperty MaintainCaretProperty =
        DependencyProperty.RegisterAttached(
            "MaintainCaret",
            typeof(bool),
            typeof(NumberBoxBehavior),
            new PropertyMetadata(false, OnMaintainCaretChanged));

    public static bool GetMaintainCaret(DependencyObject obj)
    {
        return (bool)obj.GetValue(MaintainCaretProperty);
    }

    public static void SetMaintainCaret(DependencyObject obj, bool value)
    {
        obj.SetValue(MaintainCaretProperty, value);
    }

    private static void OnMaintainCaretChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is NumberBox numberBox)
        {
            if (e.NewValue is true)
            {
                numberBox.TextChanged += NumberBox_TextChanged;
            }
            else
            {
                numberBox.TextChanged -= NumberBox_TextChanged;
            }
        }
    }

    private static void NumberBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is NumberBox numberBox)
        {
            numberBox.Dispatcher.BeginInvoke(new Action(() =>
            {
                numberBox.CaretIndex = numberBox.Text.Length;
            }), DispatcherPriority.Input);
        }
    }
}
