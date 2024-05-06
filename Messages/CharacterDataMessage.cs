using CommunityToolkit.Mvvm.Messaging.Messages;
using RHToolkit.Models;

namespace RHToolkit.Messages
{
    public class CharacterDataMessage(CharacterData value) : ValueChangedMessage<CharacterData>(value)
    {
    }
}
