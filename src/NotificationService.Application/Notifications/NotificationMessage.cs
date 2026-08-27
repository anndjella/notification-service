using NotificationService.Domain.Notifications;

namespace NotificationService.Application.Notifications;

public sealed record NotificationMessage(
    int UserId,
    string RecipientEmail,
    string RecipientName,
    NotificationType Type,
    string Title,
    string Message,
    string DeduplicationKey,
    DateTime CreatedAtUtc);
