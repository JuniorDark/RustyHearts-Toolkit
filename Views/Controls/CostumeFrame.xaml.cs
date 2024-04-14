using RHGMTool.ViewModels;
using System.Windows.Controls;

namespace RHGMTool.Views
{
    /// <summary>
    /// Interaction logic for CostumeFrame.xaml
    /// </summary>
    public partial class CostumeFrame : UserControl
    {
        private readonly FrameViewModel? _viewModel;

        public CostumeFrame()
        {
            InitializeComponent();
            _viewModel = new FrameViewModel();
            DataContext = _viewModel;
        }
    }
}
