using CommunityToolkit.Mvvm.Messaging.Messages;
using RHToolkit.Models;

namespace RHToolkit.Messages
{
    public class CharacterInfoMessage(CharacterInfo characterInfo, string? recipient, Guid? token = null) : ValueChangedMessage<CharacterInfo>(characterInfo)
    {
        public string? Recipient { get; } = recipient;
        public Guid? Token { get; } = token;
    }

}
