using NotificationService.Application.Notifications;

namespace NotificationService.Application.Abstractions;

public interface INotificationMessagePublisher
{
    Task PublishAsync(
        IReadOnlyCollection<NotificationMessage> messages,
        CancellationToken cancellationToken = default);
}
