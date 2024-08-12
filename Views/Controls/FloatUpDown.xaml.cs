using System.Windows.Controls;

namespace RHToolkit.Views.Controls
{
    public partial class FloatUpDown : UserControl
    {
        public static readonly DependencyProperty MinimumProperty =
        DependencyProperty.Register("Minimum", typeof(double), typeof(FloatUpDown), new PropertyMetadata(double.MinValue));

        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register("Maximum", typeof(double), typeof(FloatUpDown), new PropertyMetadata(double.MaxValue));

        public static readonly DependencyProperty IncrementProperty =
            DependencyProperty.Register("Increment", typeof(double), typeof(FloatUpDown), new PropertyMetadata(0.1));

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(double), typeof(FloatUpDown), new PropertyMetadata(0.0, OnValueChanged));

        public double Minimum
        {
            get { return (double)GetValue(MinimumProperty); }
            set { SetValue(MinimumProperty, value); }
        }

        public double Maximum
        {
            get { return (double)GetValue(MaximumProperty); }
            set { SetValue(MaximumProperty, value); }
        }

        public double Increment
        {
            get { return (double)GetValue(IncrementProperty); }
            set { SetValue(IncrementProperty, value); }
        }

        public double Value
        {
            get { return (double)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        public FloatUpDown()
        {
            InitializeComponent();
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (FloatUpDown)d;
            control.PART_TextBox.Text = ((double)e.NewValue).ToString("F");
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (e.Property == MinimumProperty || e.Property == MaximumProperty)
            {
                UpdateButtonStates();
            }
        }

        private void Increment_Click(object sender, RoutedEventArgs e)
        {
            Value += Increment;
            if (Value > Maximum)
                Value = Maximum;
        }

        private void Decrement_Click(object sender, RoutedEventArgs e)
        {
            Value -= Increment;
            if (Value < Minimum)
                Value = Minimum;
        }

        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Allowing only numeric input
            e.Handled = !IsNumeric(e.Text);
        }

        private bool _isIncrementing;
        private bool _isDecrementing;

        private async void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Handling Enter key to lose focus
            if (e.Key == Key.Enter)
            {
                Keyboard.ClearFocus();
                e.Handled = true;
            }

            // Handling arrow key to change values
            if (e.Key == Key.Up)
            {
                IncreaseValue();
                _isIncrementing = true;
                await RepeatActionAsync(IncreaseValue);
            }
            else if (e.Key == Key.Down)
            {
                DecreaseValue();
                _isDecrementing = true;
                await RepeatActionAsync(DecreaseValue);
            }
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                if (!double.TryParse(textBox.Text, out double value))
                {
                    textBox.Text = Value.ToString("F");
                    return;
                }

                Value = Math.Min(Math.Max(Minimum, value), Maximum);
            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                if (!double.TryParse(textBox.Text, out double value))
                {
                    textBox.Text = Value.ToString("F");
                    return;
                }

                Value = Math.Min(Math.Max(Minimum, value), Maximum);
                UpdateButtonStates();
            }
        }

        private void TextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Up)
            {
                _isIncrementing = false;
            }
            else if (e.Key == Key.Down)
            {
                _isDecrementing = false;
            }
        }

        private async Task RepeatActionAsync(Action action)
        {
            while (_isIncrementing || _isDecrementing)
            {
                await Task.Delay(100);
                if (_isIncrementing)
                {
                    action.Invoke();
                }
                else if (_isDecrementing)
                {
                    action.Invoke();
                }
            }
        }

        private void IncreaseValue()
        {
            Value = Math.Min(Value + Increment, Maximum);
        }

        private void DecreaseValue()
        {
            Value = Math.Max(Value - Increment, Minimum);
        }

        private static bool IsNumeric(string text)
        {
            return double.TryParse(text, out _);
        }

        private void UpdateButtonStates()
        {
            PART_RepeatButtonUp.IsEnabled = Value < Maximum;
            PART_RepeatButtonDown.IsEnabled = Value > Minimum;
        }

    }
}
