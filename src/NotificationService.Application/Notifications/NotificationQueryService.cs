using NotificationService.Application.Abstractions;

namespace NotificationService.Application.Notifications;

public sealed class NotificationQueryService
{
    private readonly INotificationRepository _repository;

    public NotificationQueryService(INotificationRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<NotificationResponse>> ListAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var notifications = await _repository.ListForUserAsync(userId, cancellationToken);
        return notifications.Select(notification => new NotificationResponse(
            notification.Id,
            notification.Type,
            notification.Title,
            notification.Message,
            notification.CreatedAtUtc,
            notification.ReadAtUtc)).ToList();
    }

    public Task<int> CountUnreadAsync(int userId, CancellationToken cancellationToken = default)
        => _repository.CountUnreadAsync(userId, cancellationToken);

    public async Task<bool> MarkAsReadAsync(
        Guid notificationId,
        int userId,
        DateTime readAtUtc,
        CancellationToken cancellationToken = default)
    {
        var notification = await _repository.GetOwnedAsync(notificationId, userId, cancellationToken);
        if (notification is null) return false;

        notification.MarkAsRead(readAtUtc);
        await _repository.SaveChangesAsync(cancellationToken);
        return true;
    }
}
