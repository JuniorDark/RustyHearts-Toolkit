using System.Windows.Controls;

namespace RHToolkit.Controls
{
    public partial class ItemSlot : UserControl
    {
        public static readonly DependencyProperty IsButtonEnabledProperty = DependencyProperty.Register(
            "IsButtonEnabled", typeof(bool), typeof(ItemSlot), new PropertyMetadata(true));

        public static readonly DependencyProperty ItemNameProperty = DependencyProperty.Register(
            "ItemName", typeof(string), typeof(ItemSlot), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty AddItemCommandProperty = DependencyProperty.Register(
            "AddItemCommand", typeof(ICommand), typeof(ItemSlot));

        public static readonly DependencyProperty AddItemCommandParameterProperty = DependencyProperty.Register(
            "AddItemCommandParameter", typeof(string), typeof(ItemSlot), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty RemoveItemCommandProperty = DependencyProperty.Register(
            "RemoveItemCommand", typeof(ICommand), typeof(ItemSlot));

        public static readonly DependencyProperty RemoveItemCommandParameterProperty = DependencyProperty.Register(
            "RemoveItemCommandParameter", typeof(string), typeof(ItemSlot), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty ItemIconProperty = DependencyProperty.Register(
            "ItemIcon", typeof(string), typeof(ItemSlot), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty ItemIconBranchProperty = DependencyProperty.Register(
            "ItemIconBranch", typeof(string), typeof(ItemSlot), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty ItemAmountProperty = DependencyProperty.Register(
            "ItemAmount", typeof(string), typeof(ItemSlot), new PropertyMetadata(string.Empty));

        public bool IsButtonEnabled
        {
            get => (bool)GetValue(IsButtonEnabledProperty);
            set => SetValue(IsButtonEnabledProperty, value);
        }

        public string ItemName
        {
            get => (string)GetValue(ItemNameProperty);
            set => SetValue(ItemNameProperty, value);
        }

        public ICommand AddItemCommand
        {
            get => (ICommand)GetValue(AddItemCommandProperty);
            set => SetValue(AddItemCommandProperty, value);
        }

        public string AddItemCommandParameter
        {
            get => (string)GetValue(AddItemCommandParameterProperty);
            set => SetValue(AddItemCommandParameterProperty, value);
        }

        public ICommand RemoveItemCommand
        {
            get => (ICommand)GetValue(RemoveItemCommandProperty);
            set => SetValue(RemoveItemCommandProperty, value);
        }

        public string RemoveItemCommandParameter
        {
            get => (string)GetValue(RemoveItemCommandParameterProperty);
            set => SetValue(RemoveItemCommandParameterProperty, value);
        }

        public string ItemIcon
        {
            get => (string)GetValue(ItemIconProperty);
            set => SetValue(ItemIconProperty, value);
        }

        public string ItemIconBranch
        {
            get => (string)GetValue(ItemIconBranchProperty);
            set => SetValue(ItemIconBranchProperty, value);
        }

        public string ItemAmount
        {
            get => (string)GetValue(ItemAmountProperty);
            set => SetValue(ItemAmountProperty, value);
        }

        public ItemSlot()
        {
            InitializeComponent();
        }
    }
}
