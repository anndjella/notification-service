using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using NotificationService.Application.Notifications;

namespace NotificationService.Functions.Functions;

public sealed class ProcessNotificationMessage
{
    private readonly NotificationMessageProcessor _processor;
    private readonly ILogger<ProcessNotificationMessage> _logger;

    public ProcessNotificationMessage(
        NotificationMessageProcessor processor,
        ILogger<ProcessNotificationMessage> logger)
    {
        _processor = processor;
        _logger = logger;
    }

    [Function(nameof(ProcessNotificationMessage))]
    public async Task Run(
        [ServiceBusTrigger(
            "%NotificationQueueName%",
            Connection = "ServiceBusConnection")]
        NotificationMessage message,
        CancellationToken cancellationToken)
    {
        var created = await _processor.ExecuteAsync(message, cancellationToken);

        _logger.LogInformation(
            created
                ? "Notification {DeduplicationKey} was created and processed for user {UserId}."
                : "Notification {DeduplicationKey} already exists; the duplicate message was ignored for user {UserId}.",
            message.DeduplicationKey,
            message.UserId);
    }
}
