using System.ComponentModel;
using System.Windows.Input;

namespace RHGMTool.ViewModels
{
    public class NumericUpDownViewModel : INotifyPropertyChanged
    {
        private int _value;
        public int Value
        {
            get { return _value; }
            set
            {
                if (_value != value)
                {
                    _value = value;
                    OnPropertyChanged(nameof(Value));
                }
            }
        }

        private int _minValue;
        public int MinValue
        {
            get { return _minValue; }
            set
            {
                if (_minValue != value)
                {
                    _minValue = value;
                    OnPropertyChanged(nameof(MinValue));
                }
            }
        }

        private int _maxValue;
        public int MaxValue
        {
            get { return _maxValue; }
            set
            {
                if (_maxValue != value)
                {
                    _maxValue = value;
                    OnPropertyChanged(nameof(MaxValue));
                }
            }
        }

        public ICommand IncrementCommand { get; }
        public ICommand DecrementCommand { get; }

        public NumericUpDownViewModel()
        {
            Value = 0;
            MinValue = 0;
            MaxValue = 100;

            IncrementCommand = new RelayCommand(Increment, CanIncrement);
            DecrementCommand = new RelayCommand(Decrement, CanDecrement);
        }

        private void Increment(object parameter)
        {
            Value++;
        }

        private bool CanIncrement(object parameter)
        {
            return Value < MaxValue;
        }

        private void Decrement(object parameter)
        {
            Value--;
        }

        private bool CanDecrement(object parameter)
        {
            return Value > MinValue;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
