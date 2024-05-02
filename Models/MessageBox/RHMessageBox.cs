using RHToolkit.Views.Windows;

namespace RHToolkit.Models.MessageBox
{
    public class RHMessageBox
    {
        public static bool ConfirmMessage(string message)
        {
            MessageBoxResult result = WpfMessageBox.Show("Confirmation", message, WpfMessageBox.MessageBoxType.ConfirmationWithYesNo);
            return result == MessageBoxResult.Yes;
        }

        public static void ShowOKMessage(string message, string caption = "Information")
        {
            WpfMessageBox.Show(caption, message, WpfMessageBox.MessageBoxType.Information);
        }

        public static MessageBoxResult ShowOKCancelMessage(string message, string caption = "Confirmation")
        {
            return WpfMessageBox.Show(caption, message, MessageBoxButton.OKCancel, WpfMessageBox.MessageBoxImage.Question);
        }
    }
}
