using Azure.Messaging.ServiceBus;
using NotificationService.Application.Abstractions;
using NotificationService.Application.Notifications;

namespace NotificationService.Functions.Messaging;

public sealed class ServiceBusNotificationMessagePublisher : INotificationMessagePublisher
{
    private readonly ServiceBusSender _sender;

    public ServiceBusNotificationMessagePublisher(ServiceBusSender sender)
    {
        _sender = sender;
    }

    public async Task PublishAsync(
        IReadOnlyCollection<NotificationMessage> messages,
        CancellationToken cancellationToken = default)
    {
        if (messages.Count == 0)
            return;

        ServiceBusMessageBatch? batch = null;

        try
        {
            batch = await _sender.CreateMessageBatchAsync(cancellationToken);

            foreach (var message in messages)
            {
                var serviceBusMessage = new ServiceBusMessage(BinaryData.FromObjectAsJson(message))
                {
                    MessageId = message.DeduplicationKey,
                    Subject = message.Type.ToString(),
                    ContentType = "application/json"
                };

                if (batch.TryAddMessage(serviceBusMessage))
                    continue;

                if (batch.Count == 0)
                {
                    throw new InvalidOperationException(
                        $"Notification message '{message.DeduplicationKey}' is too large for an empty Service Bus batch.");
                }

                await _sender.SendMessagesAsync(batch, cancellationToken);
                batch.Dispose();
                batch = await _sender.CreateMessageBatchAsync(cancellationToken);

                if (!batch.TryAddMessage(serviceBusMessage))
                {
                    throw new InvalidOperationException(
                        $"Notification message '{message.DeduplicationKey}' is too large for an empty Service Bus batch.");
                }
            }

            if (batch.Count > 0)
                await _sender.SendMessagesAsync(batch, cancellationToken);
        }
        finally
        {
            batch?.Dispose();
        }
    }
}
