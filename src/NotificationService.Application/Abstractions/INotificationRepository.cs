using NotificationService.Domain.Notifications;

namespace NotificationService.Application.Abstractions;

public interface INotificationRepository
{
    Task<bool> TryAddAsync(
        Notification notification,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Notification>> ListForUserAsync(
        int userId,
        CancellationToken cancellationToken = default);

    Task<int> CountUnreadAsync(
        int userId,
        CancellationToken cancellationToken = default);

    Task<Notification?> GetOwnedAsync(
        Guid notificationId,
        int userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Notification>> ListEmailRetryCandidatesAsync(
        int maxAttempts,
        int take,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
