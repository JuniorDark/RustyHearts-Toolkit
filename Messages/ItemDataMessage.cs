using CommunityToolkit.Mvvm.Messaging.Messages;
using RHToolkit.Models;

namespace RHToolkit.Messages
{
    public class ItemDataMessage(ItemData value, string? recipient, string? MessageType, Guid? token = null) : ValueChangedMessage<ItemData>(value)
    {
        public string? Recipient { get; } = recipient;
        public string? MessageType { get; } = MessageType;
        public Guid? Token { get; } = token;
    }
}
