using RHToolkit.ViewModels.Controls;
using System.Windows.Controls;

namespace RHToolkit.Views.Controls
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
            "CommandParameter", typeof(string), typeof(ItemSlot), new PropertyMetadata(string.Empty, OnCommandParameterChanged));

        public static readonly DependencyProperty SlotIconProperty = DependencyProperty.Register(
            "SlotIcon", typeof(string), typeof(ItemSlot), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty FrameViewModelProperty = DependencyProperty.Register(
            "FrameViewModel", typeof(FrameViewModel), typeof(ItemSlot), new PropertyMetadata(null));

        public static readonly DependencyProperty InventorySizeProperty = DependencyProperty.Register(
            "InventorySize", typeof(string), typeof(ItemSlot), new PropertyMetadata(null, OnInventorySizeChanged));

        public FrameViewModel FrameViewModel
        {
            get => (FrameViewModel)GetValue(FrameViewModelProperty);
            set => SetValue(FrameViewModelProperty, value);
        }

        public string InventorySize
        {
            get => (string)GetValue(InventorySizeProperty);
            set => SetValue(InventorySizeProperty, value);
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

        private static void OnCommandParameterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var itemSlot = d as ItemSlot;
            itemSlot?.UpdateUIState();
        }

        private static void OnInventorySizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var itemSlot = d as ItemSlot;
            itemSlot?.UpdateUIState();
        }

        private void UpdateUIState()
        {
            if (int.TryParse(CommandParameter, out int commandParameterIndex) && int.TryParse(InventorySize, out int inventorySize))
            {
                bool isEnabled = commandParameterIndex < inventorySize;
                buttonItem.IsEnabled = isEnabled;
            }
        }
    }

}
