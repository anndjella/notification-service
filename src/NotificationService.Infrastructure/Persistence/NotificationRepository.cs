using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NotificationService.Application.Abstractions;
using NotificationService.Domain.Notifications;

namespace NotificationService.Infrastructure.Persistence;

public sealed class NotificationRepository : INotificationRepository
{
    private readonly NotificationDbContext _db;

    public NotificationRepository(NotificationDbContext db)
    {
        _db = db;
    }

    public async Task<bool> TryAddAsync(
        Notification notification,
        CancellationToken cancellationToken = default)
    {
        _db.Notifications.Add(notification);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            _db.Entry(notification).State = EntityState.Detached;
            return false;
        }
    }

    public async Task<IReadOnlyList<Notification>> ListForUserAsync(
        int userId,
        CancellationToken cancellationToken = default)
        => await _db.Notifications
            .AsNoTracking()
            .Where(notification => notification.UserId == userId)
            .OrderByDescending(notification => notification.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public Task<int> CountUnreadAsync(
        int userId,
        CancellationToken cancellationToken = default)
        => _db.Notifications.CountAsync(
            notification => notification.UserId == userId && notification.ReadAtUtc == null,
            cancellationToken);

    public Task<Notification?> GetOwnedAsync(
        Guid notificationId,
        int userId,
        CancellationToken cancellationToken = default)
        => _db.Notifications.FirstOrDefaultAsync(
            notification => notification.Id == notificationId && notification.UserId == userId,
            cancellationToken);

    public async Task<IReadOnlyList<Notification>> ListEmailRetryCandidatesAsync(
        int maxAttempts,
        int take,
        CancellationToken cancellationToken = default)
        => await _db.Notifications
            .Where(notification =>
                (notification.DeliveryStatus == NotificationDeliveryStatus.Pending ||
                 notification.DeliveryStatus == NotificationDeliveryStatus.Failed) &&
                notification.DeliveryAttemptCount < maxAttempts)
            .OrderBy(notification => notification.CreatedAtUtc)
            .Take(take)
            .ToListAsync(cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _db.SaveChangesAsync(cancellationToken);
}
