using System.ComponentModel;

namespace RHGMTool.ViewModels
{
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        private bool _isTextBoxEnabled = true;
        public bool IsTextBoxEnabled
        {
            get => _isTextBoxEnabled;
            set
            {
                _isTextBoxEnabled = value;
                OnPropertyChanged(nameof(IsTextBoxEnabled));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
