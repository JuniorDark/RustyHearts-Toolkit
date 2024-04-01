using System.Windows;
using System.Windows.Controls;

namespace RHGMTool.Views
{
    public partial class NumericUpDownControl : UserControl
    {
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(int), typeof(NumericUpDownControl), new PropertyMetadata(0));

        public int Value
        {
            get { return (int)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        public NumericUpDownControl()
        {
            InitializeComponent();
        }

        private void UpButton_Click(object sender, RoutedEventArgs e)
        {
            Value++;
        }

        private void DownButton_Click(object sender, RoutedEventArgs e)
        {
            Value--;
        }

        private void NumericTextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            // Allow only digits in the TextBox
            e.Handled = !int.TryParse(e.Text, out _);
        }

        private void NumericTextBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            // Prevent non-numeric keys from being entered
            if (e.Key == System.Windows.Input.Key.Space)
                e.Handled = true;
        }
    }
}
