using CommunityToolkit.Mvvm.Messaging.Messages;
using RHToolkit.Models;

namespace RHToolkit.Messages
{
    public class CharacterInfoMessage(CharacterInfo characterInfo, string? recipient) : ValueChangedMessage<CharacterInfo>(characterInfo)
    {
        public string? Recipient { get; } = recipient;
    }

}
