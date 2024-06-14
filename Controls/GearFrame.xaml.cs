using RHToolkit.ViewModels.Controls;
using System.Windows.Controls;

namespace RHToolkit.Controls
{
    public partial class GearFrame : UserControl
    {
        public GearFrame()
        {
            InitializeComponent();
        }

        public GearFrame(FrameViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
