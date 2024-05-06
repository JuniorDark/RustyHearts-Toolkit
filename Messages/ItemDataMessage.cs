using CommunityToolkit.Mvvm.Messaging.Messages;
using RHToolkit.Models;

namespace RHToolkit.Messages
{
    public class ItemDataMessage(ItemData value, ViewModelType recipient) : ValueChangedMessage<ItemData>(value)
    {
        public ViewModelType Recipient { get; } = recipient;
    }

    public enum ViewModelType
    {
        CharacterWindowViewModel,
        ItemWindowViewModel,
        MailWindowViewModel
    }
}
