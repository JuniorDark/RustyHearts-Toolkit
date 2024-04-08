using RHGMTool.ViewModels;
using System.Windows.Controls;

namespace RHGMTool.Views
{
    /// <summary>
    /// Interaction logic for ItemFrame.xaml
    /// </summary>
    public partial class ItemFrame : UserControl
    {
        private readonly FrameViewModel? _viewModel;

        public ItemFrame()
        {
            InitializeComponent();
            _viewModel = new FrameViewModel();
            DataContext = _viewModel;
        }

    }
}
