using RHGMTool.ViewModels;
using System.Windows.Controls;

namespace RHGMTool.Views
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
