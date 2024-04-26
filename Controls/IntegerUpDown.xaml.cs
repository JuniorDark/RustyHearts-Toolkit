using System.Windows.Controls;

namespace RHToolkit.Controls
{
    public partial class IntegerUpDown : UserControl
    {
        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register("Minimum", typeof(int), typeof(IntegerUpDown), new PropertyMetadata(int.MinValue));

        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register("Maximum", typeof(int), typeof(IntegerUpDown), new PropertyMetadata(int.MaxValue));

        public static readonly DependencyProperty IncrementProperty =
            DependencyProperty.Register("Increment", typeof(int), typeof(IntegerUpDown), new PropertyMetadata(1));

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(int), typeof(IntegerUpDown), new PropertyMetadata(0, OnValueChanged));

        public int Minimum
        {
            get { return (int)GetValue(MinimumProperty); }
            set { SetValue(MinimumProperty, value); }
        }

        public int Maximum
        {
            get { return (int)GetValue(MaximumProperty); }
            set { SetValue(MaximumProperty, value); }
        }

        public int Increment
        {
            get { return (int)GetValue(IncrementProperty); }
            set { SetValue(IncrementProperty, value); }
        }

        public int Value
        {
            get { return (int)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        public IntegerUpDown()
        {
            InitializeComponent();
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (IntegerUpDown)d;
            control.PART_TextBox.Text = e.NewValue.ToString();
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
                if (!int.TryParse(textBox.Text, out int value))
                {
                    textBox.Text = Value.ToString();
                    return;
                }

                Value = Math.Min(Math.Max(Minimum, value), Maximum);
            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                if (!int.TryParse(textBox.Text, out int value))
                {
                    textBox.Text = Value.ToString();
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
                await Task.Delay(500);
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
            return int.TryParse(text, out _);
        }

        private void UpdateButtonStates()
        {
            if (Value >= Maximum)
            {
                PART_RepeatButtonUp.IsEnabled = false;
            }
            else
            {
                PART_RepeatButtonUp.IsEnabled = true;
            }

            if (Value <= Minimum)
            {
                PART_RepeatButtonDown.IsEnabled = false;
            }
            else
            {
                PART_RepeatButtonDown.IsEnabled = true;
            }
        }

    }
}
