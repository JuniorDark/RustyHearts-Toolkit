using CommunityToolkit.Mvvm.Messaging.Messages;

namespace RHToolkit.Messages
{
    public class IDMessage(int id, Guid? token, string? messageType) : ValueChangedMessage<int>(id)
    {
        public Guid? Token { get; } = token;
        public string? MessageType { get; } = messageType;
    }

}
