using NotificationService.Domain.Notifications;

namespace NotificationService.Application.Notifications;

public sealed class NotificationMessageProcessor
{
    private readonly NotificationDispatcher _dispatcher;

    public NotificationMessageProcessor(NotificationDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public Task<bool> ExecuteAsync(
        NotificationMessage message,
        CancellationToken cancellationToken = default)
    {
        var notification = Notification.Create(
            message.UserId,
            message.RecipientEmail,
            message.Type,
            message.Title,
            message.Message,
            message.DeduplicationKey,
            message.CreatedAtUtc);

        return _dispatcher.DispatchAsync(
            notification,
            message.RecipientName,
            cancellationToken);
    }
}
