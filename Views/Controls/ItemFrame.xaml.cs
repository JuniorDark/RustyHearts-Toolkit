using RHToolkit.ViewModels;
using System.Windows.Controls;

namespace RHToolkit.Views
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
