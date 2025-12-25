using CommunityToolkit.Mvvm.Messaging.Messages;
using RHToolkit.Models;

namespace RHToolkit.Messages;

public class ModelMessage(ModelType value, string? recipient, Guid? token = null) : ValueChangedMessage<ModelType>(value)
{
    public string? Recipient { get; } = recipient;
    public Guid? Token { get; } = token;
}
