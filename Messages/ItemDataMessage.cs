using CommunityToolkit.Mvvm.Messaging.Messages;
using RHToolkit.Models;

namespace RHToolkit.Messages
{
    public class ItemDataMessage(ItemData value, ViewModelType recipient, string? MessageType) : ValueChangedMessage<ItemData>(value)
    {
        public ViewModelType Recipient { get; } = recipient;
        public string? MessageType { get; } = MessageType;
    }

    public enum ViewModelType
    {
        CharacterWindowViewModel,
        ItemWindowViewModel,
        MailWindowViewModel
    }
}
