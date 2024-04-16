using CommunityToolkit.Mvvm.Messaging.Messages;
using RHGMTool.Models;

namespace RHGMTool.Messages
{
    public class ItemDataMessage(ItemData value, ViewModelType recipient) : ValueChangedMessage<ItemData>(value)
    {
        public ViewModelType Recipient { get; } = recipient;
    }

    public enum ViewModelType
    {
        ItemWindowViewModel,
        MailWindowViewModel
    }
}
