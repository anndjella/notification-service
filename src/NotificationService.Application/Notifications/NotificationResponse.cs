using NotificationService.Domain.Notifications;

namespace NotificationService.Application.Notifications;

public sealed record NotificationResponse(
    Guid ID,
    NotificationType Type,
    string Title,
    string Message,
    DateTime CreatedAtUtc,
    DateTime? ReadAtUtc);
