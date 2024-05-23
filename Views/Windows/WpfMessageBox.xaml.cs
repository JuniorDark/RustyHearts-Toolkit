using System.Windows.Media.Imaging;

namespace RHToolkit.Views.Windows
{
    public partial class WpfMessageBox : Window
    {
        public enum MessageBoxType
        {
            ConfirmationWithYesNo = 0,
            ConfirmationWithYesNoCancel,
            Information,
            Error,
            Warning
        }

        public enum MessageBoxImage
        {
            Warning = 0,
            Question,
            Information,
            Error,
            None
        }

        private WpfMessageBox()
        {
            InitializeComponent();
        }

        static WpfMessageBox()
        {
            _messageBox = new WpfMessageBox();
        }

        static WpfMessageBox _messageBox;

        static MessageBoxResult _result = MessageBoxResult.No;
        public static MessageBoxResult Show
        (string caption, string msg, MessageBoxType type)
        {
            return type switch
            {
                MessageBoxType.ConfirmationWithYesNo => Show(caption, msg, MessageBoxButton.YesNo,
                                    MessageBoxImage.Question),
                MessageBoxType.ConfirmationWithYesNoCancel => Show(caption, msg, MessageBoxButton.YesNoCancel,
                                    MessageBoxImage.Question),
                MessageBoxType.Information => Show(caption, msg, MessageBoxButton.OK,
                                    MessageBoxImage.Information),
                MessageBoxType.Error => Show(caption, msg, MessageBoxButton.OK,
                                    MessageBoxImage.Error),
                MessageBoxType.Warning => Show(caption, msg, MessageBoxButton.OK,
                                    MessageBoxImage.Warning),
                _ => MessageBoxResult.No,
            };
        }

        public static MessageBoxResult Show(string msg, MessageBoxType type)
        {
            return Show(string.Empty, msg, type);
        }

        public static MessageBoxResult Show(string msg)
        {
            return Show(string.Empty, msg,
            MessageBoxButton.OK, MessageBoxImage.None);
        }

        public static MessageBoxResult Show
        (string caption, string text)
        {
            return Show(caption, text,
            MessageBoxButton.OK, MessageBoxImage.None);
        }

        public static MessageBoxResult Show
        (string caption, string text, MessageBoxButton button)
        {
            return Show(caption, text, button,
            MessageBoxImage.None);
        }

        public static MessageBoxResult Show
        (string caption, string text,
        MessageBoxButton button, MessageBoxImage image)
        {
            _messageBox = new WpfMessageBox
            { txtMsg = { Text = text }, MessageTitle = { Text = caption } };
            SetVisibilityOfButtons(button);
            SetImageOfMessageBox(image);
            _messageBox.ShowDialog();
            return _result;
        }

        private static void SetVisibilityOfButtons(MessageBoxButton button)
        {
            switch (button)
            {
                case MessageBoxButton.OK:
                    _messageBox.btnCancel.Visibility = Visibility.Collapsed;
                    _messageBox.btnNo.Visibility = Visibility.Collapsed;
                    _messageBox.btnYes.Visibility = Visibility.Collapsed;
                    _messageBox.btnOk.Focus();
                    break;
                case MessageBoxButton.OKCancel:
                    _messageBox.btnNo.Visibility = Visibility.Collapsed;
                    _messageBox.btnYes.Visibility = Visibility.Collapsed;
                    _messageBox.btnCancel.Focus();
                    break;
                case MessageBoxButton.YesNo:
                    _messageBox.btnOk.Visibility = Visibility.Collapsed;
                    _messageBox.btnCancel.Visibility = Visibility.Collapsed;
                    _messageBox.btnNo.Focus();
                    break;
                case MessageBoxButton.YesNoCancel:
                    _messageBox.btnOk.Visibility = Visibility.Collapsed;
                    _messageBox.btnCancel.Focus();
                    break;
                default:
                    break;
            }
        }

        private static void SetImageOfMessageBox(MessageBoxImage image)
        {
            switch (image)
            {
                case MessageBoxImage.Warning:
                    _messageBox.SetImage("Warning.png");
                    break;
                case MessageBoxImage.Question:
                    _messageBox.SetImage("Question.png");
                    break;
                case MessageBoxImage.Information:
                    _messageBox.SetImage("Information.png");
                    break;
                case MessageBoxImage.Error:
                    _messageBox.SetImage("Error.png");
                    break;
                default:
                    _messageBox.img.Visibility = Visibility.Collapsed;
                    break;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (sender == btnOk)
                _result = MessageBoxResult.OK;
            else if (sender == btnYes)
                _result = MessageBoxResult.Yes;
            else if (sender == btnNo)
                _result = MessageBoxResult.No;
            else if (sender == btnCancel)
                _result = MessageBoxResult.Cancel;
            else
                _result = MessageBoxResult.None;
            _messageBox.Close();
        }

        private void SetImage(string imageName)
        {
            string uri = string.Format("/Resources/images/{0}", imageName);
            var uriSource = new Uri(uri, UriKind.RelativeOrAbsolute);
            img.Source = new BitmapImage(uriSource);
        }
    }
}
