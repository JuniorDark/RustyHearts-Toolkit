using CommunityToolkit.Mvvm.Messaging.Messages;
using System.Data;

namespace RHToolkit.Messages
{
    public class DataRowViewMessage(DataRowView? dataRowView, Guid? token) : ValueChangedMessage<DataRowView?>(dataRowView)
    {
        public Guid? Token { get; } = token;
    }

}
