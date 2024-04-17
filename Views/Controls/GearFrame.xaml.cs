using RHToolkit.ViewModels;
using System.Windows.Controls;

namespace RHToolkit.Views
{
    public partial class GearFrame : UserControl
    {
        private readonly ItemWindowViewModel? _viewModel;

        public GearFrame()
        {
            InitializeComponent();
            _viewModel = new ItemWindowViewModel();
            DataContext = _viewModel;
        }
    }
}
