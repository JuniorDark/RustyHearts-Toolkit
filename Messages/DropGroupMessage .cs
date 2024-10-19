using CommunityToolkit.Mvvm.Messaging.Messages;
using static RHToolkit.Models.EnumService;

namespace RHToolkit.Messages
{
    public class DropGroupMessage(int id, Guid? token, ItemDropGroupType dropGroupType) : ValueChangedMessage<int>(id)
    {
        public Guid? Token { get; } = token;
        public ItemDropGroupType DropGroupType { get; } = dropGroupType;
    }

}
