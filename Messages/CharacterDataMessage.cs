using CommunityToolkit.Mvvm.Messaging.Messages;
using RHToolkit.Models;

namespace RHToolkit.Messages
{
    public class CharacterDataMessage(CharacterData characterData, string? recipient, Guid? token = null) : ValueChangedMessage<CharacterData>(characterData)
    {
        public string? Recipient { get; } = recipient;
        public Guid? Token { get; } = token;
    }

}
