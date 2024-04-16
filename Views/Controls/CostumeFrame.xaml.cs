using RHGMTool.ViewModels;
using System.Windows.Controls;

namespace RHGMTool.Views
{
    /// <summary>
    /// Interaction logic for CostumeFrame.xaml
    /// </summary>
    public partial class CostumeFrame : UserControl
    {
        private readonly ItemWindowViewModel? _viewModel;

        public CostumeFrame()
        {
            InitializeComponent();
            _viewModel = new ItemWindowViewModel();
            DataContext = _viewModel;
        }
    }
}
