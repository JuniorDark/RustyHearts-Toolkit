using RHGMTool.ViewModels;
using System.Windows.Controls;

namespace RHGMTool.Views
{
    public partial class GearFrameUserControl : UserControl
    {
        private readonly FrameViewModel? _viewModel;

        public GearFrameUserControl()
        {
            InitializeComponent();
            _viewModel = new FrameViewModel();
            DataContext = _viewModel;
        }
    }
}
