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

    public class DatabaseItemMessage(List<DatabaseItem> value, ItemStorageType itemStorageType) : ValueChangedMessage<List<DatabaseItem>>(value)
    {
        public ItemStorageType ItemStorageType { get; } = itemStorageType;
    }

}
