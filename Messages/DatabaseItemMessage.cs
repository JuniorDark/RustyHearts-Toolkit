using CommunityToolkit.Mvvm.Messaging.Messages;
using RHToolkit.Models;

namespace RHToolkit.Messages
{
    public enum ItemStorageType
    {
        Inventory,
        Equipment,
        Storage
    }

    public class DatabaseItemMessage(List<ItemData> value, ItemStorageType itemStorageType) : ValueChangedMessage<List<ItemData>>(value)
    {
        public ItemStorageType ItemStorageType { get; } = itemStorageType;
    }

}
