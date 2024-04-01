using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RHGMTool.ViewModels
{
    public class ItemWindowViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private int _numSocketCount;
        public int NumSocketCount
        {
            get { return _numSocketCount; }
            set
            {
                if (_numSocketCount != value)
                {
                    _numSocketCount = value;
                    OnPropertyChanged(nameof(NumSocketCount));
                    
                }
            }
        }

    }
}
