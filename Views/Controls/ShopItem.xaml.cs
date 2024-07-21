using RHToolkit.ViewModels.Controls;
using System.Windows.Controls;

namespace RHToolkit.Views.Controls
{
    public partial class ShopItem : UserControl
    {
        public static readonly DependencyProperty IsButtonEnabledProperty = DependencyProperty.Register(
            "IsButtonEnabled", typeof(bool), typeof(ShopItem), new PropertyMetadata(true));

        public static readonly DependencyProperty AddItemCommandProperty = DependencyProperty.Register(
            "AddItemCommand", typeof(ICommand), typeof(ShopItem));

        public static readonly DependencyProperty IconNameProperty = DependencyProperty.Register(
            "IconName", typeof(string), typeof(ShopItem), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty ShopDescriptionProperty = DependencyProperty.Register(
            "ShopDescription", typeof(string), typeof(ShopItem), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty CashPriceProperty = DependencyProperty.Register(
            "CashPrice", typeof(int), typeof(ShopItem), new PropertyMetadata(0));

        public static readonly DependencyProperty ShopCategoryProperty = DependencyProperty.Register(
            "ShopCategory", typeof(int), typeof(ShopItem), new PropertyMetadata(0));

        public static readonly DependencyProperty PaymentTypeProperty = DependencyProperty.Register(
           "PaymentType", typeof(int), typeof(ShopItem), new PropertyMetadata(0));

        public static readonly DependencyProperty ItemAmountProperty = DependencyProperty.Register(
            "ItemAmount", typeof(int), typeof(ShopItem), new PropertyMetadata(0));

        public static readonly DependencyProperty ItemStateProperty = DependencyProperty.Register(
            "ItemState", typeof(int), typeof(ShopItem), new PropertyMetadata(0));

        public static readonly DependencyProperty FrameViewModelProperty = DependencyProperty.Register(
            "FrameViewModel", typeof(FrameViewModel), typeof(ShopItem), new PropertyMetadata(null));

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

        public string ShopDescription
        {
            get => (string)GetValue(ShopDescriptionProperty);
            set => SetValue(ShopDescriptionProperty, value);
        }

        public string IconName
        {
            get => (string)GetValue(IconNameProperty);
            set => SetValue(IconNameProperty, value);
        }

        public int ShopCategory
        {
            get => (int)GetValue(ShopCategoryProperty);
            set => SetValue(ShopCategoryProperty, value);
        }

        public int CashPrice
        {
            get => (int)GetValue(CashPriceProperty);
            set => SetValue(CashPriceProperty, value);
        }

        public int PaymentType
        {
            get => (int)GetValue(PaymentTypeProperty);
            set => SetValue(PaymentTypeProperty, value);
        }

        public int ItemAmount
        {
            get => (int)GetValue(ItemAmountProperty);
            set => SetValue(ItemAmountProperty, value);
        }

        public int ItemState
        {
            get => (int)GetValue(ItemStateProperty);
            set => SetValue(ItemStateProperty, value);
        }

        public ShopItem()
        {
            InitializeComponent();
        }

    }

}
