using RHToolkit.ViewModels.Controls;
using System.Windows.Controls;

namespace RHToolkit.Controls
{
    public partial class ItemSlot : UserControl
    {
        public static readonly DependencyProperty IsButtonEnabledProperty = DependencyProperty.Register(
            "IsButtonEnabled", typeof(bool), typeof(ItemSlot), new PropertyMetadata(true));

        public static readonly DependencyProperty AddItemCommandProperty = DependencyProperty.Register(
            "AddItemCommand", typeof(ICommand), typeof(ItemSlot));

        public static readonly DependencyProperty RemoveItemCommandProperty = DependencyProperty.Register(
            "RemoveItemCommand", typeof(ICommand), typeof(ItemSlot));

        public static readonly DependencyProperty CommandParameterProperty = DependencyProperty.Register(
            "CommandParameter", typeof(string), typeof(ItemSlot), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty SlotIconProperty = DependencyProperty.Register(
           "SlotIcon", typeof(string), typeof(ItemSlot), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty FrameViewModelProperty = DependencyProperty.Register(
            "FrameViewModel", typeof(FrameViewModel), typeof(ItemSlot), new PropertyMetadata(null));

        public FrameViewModel FrameViewModel
        {
            get => (FrameViewModel)GetValue(FrameViewModelProperty);
            set => SetValue(FrameViewModelProperty, value);
        }

        public bool IsButtonEnabled
        {
            get => (bool)GetValue(IsButtonEnabledProperty);
            set => SetValue(IsButtonEnabledProperty, value);
        }

        public ICommand AddItemCommand
        {
            get => (ICommand)GetValue(AddItemCommandProperty);
            set => SetValue(AddItemCommandProperty, value);
        }

        public ICommand RemoveItemCommand
        {
            get => (ICommand)GetValue(RemoveItemCommandProperty);
            set => SetValue(RemoveItemCommandProperty, value);
        }

        public string CommandParameter
        {
            get => (string)GetValue(CommandParameterProperty);
            set => SetValue(CommandParameterProperty, value);
        }

        public string SlotIcon
        {
            get => (string)GetValue(SlotIconProperty);
            set => SetValue(SlotIconProperty, value);
        }

        public ItemSlot()
        {
            InitializeComponent();
        }

    }
}
