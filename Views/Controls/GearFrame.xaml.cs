using RHToolkit.ViewModels.Controls;
using System.Windows.Controls;

namespace RHToolkit.Views.Controls
{
    public partial class GearFrame : UserControl
    {
        public GearFrame()
        {
            InitializeComponent();
        }

        public GearFrame(ItemDataViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
