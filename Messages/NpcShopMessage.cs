using CommunityToolkit.Mvvm.Messaging.Messages;
using RHToolkit.Models;

namespace RHToolkit.Messages
{
    public class NpcShopMessage(NameID id, Guid? token, NameID? tabName) : ValueChangedMessage<NameID>(id)
    {
        public Guid? Token { get; } = token;

        public NameID? TabName { get; } = tabName;
    }

}
