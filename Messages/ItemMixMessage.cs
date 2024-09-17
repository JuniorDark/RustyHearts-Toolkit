using CommunityToolkit.Mvvm.Messaging.Messages;

namespace RHToolkit.Messages
{
    public class ItemMixMessage(string group, Guid? token, string? messageType) : ValueChangedMessage<string>(group)
    {
        public Guid? Token { get; } = token;
        public string? MessageType { get; } = messageType;
    }

}
