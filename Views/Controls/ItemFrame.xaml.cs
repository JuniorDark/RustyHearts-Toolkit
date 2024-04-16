using RHGMTool.ViewModels;
using System.Windows.Controls;

namespace RHGMTool.Views
{
    /// <summary>
    /// Interaction logic for ItemFrame.xaml
    /// </summary>
    public partial class ItemFrame : UserControl
    {
        private readonly ItemWindowViewModel? _viewModel;

        public ItemFrame()
        {
            InitializeComponent();
            _viewModel = new ItemWindowViewModel();
            DataContext = _viewModel;
        }

    }
}
