using CommunityToolkit.Mvvm.Messaging.Messages;
using RHToolkit.Models;

namespace RHToolkit.Messages
{
    public class CharacterDataMessage(CharacterData value, string? Recipient) : ValueChangedMessage<CharacterData>(value)
    {
        public string? Recipient { get; } = Recipient;
    }
}
