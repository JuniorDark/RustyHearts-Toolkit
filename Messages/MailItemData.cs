using CommunityToolkit.Mvvm.Messaging.Messages;
using RHGMTool.Models;

namespace RHGMTool.Messages
{
    public class MailItemData(MailData value, ViewModelType recipient) : ValueChangedMessage<MailData>(value)
    {
        public ViewModelType Recipient { get; } = recipient;
    }

    public enum ViewModelType
    {
        ItemViewModel,
        MailWindowViewModel
    }
}
