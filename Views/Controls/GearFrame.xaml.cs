using RHGMTool.ViewModels;
using System.Windows.Controls;

namespace RHGMTool.Views
{
    public partial class GearFrame : UserControl
    {
        private readonly FrameViewModel? _viewModel;

        public GearFrame()
        {
            InitializeComponent();
            _viewModel = new FrameViewModel();
            DataContext = _viewModel;
        }
    }
}
