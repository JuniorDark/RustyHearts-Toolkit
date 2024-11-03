using RHToolkit.Views.Windows;

namespace RHToolkit.Models.MessageBox
{
    public class RHMessageBoxHelper
    {
        public static bool ConfirmMessage(string message)
        {
            MessageBoxResult result = RHMessageBox.Show(Resources.Confirmation, message, RHMessageBox.MessageBoxType.ConfirmationWithYesNo);
            return result == MessageBoxResult.Yes;
        }

        public static MessageBoxResult ConfirmMessageYesNoCancel(string message)
        {
            return RHMessageBox.Show(Resources.Confirmation, message, RHMessageBox.MessageBoxType.ConfirmationWithYesNoCancel);
        }

        public static void ShowOKMessage(string message, string caption = "Information")
        {
            RHMessageBox.Show(caption, message, RHMessageBox.MessageBoxType.Information);
        }

        public static MessageBoxResult ShowOKCancelMessage(string message, string caption = "Confirmation")
        {
            return RHMessageBox.Show(caption, message, MessageBoxButton.OKCancel, RHMessageBox.MessageBoxImage.Question);
        }
    }
}
