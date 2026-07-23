namespace NotificationService.Domain.Notifications;

public sealed class Notification
{
    private Notification()
    {
    }

    private Notification(
        int userId,
        string recipientEmail,
        NotificationType type,
        string title,
        string message,
        string deduplicationKey,
        DateTime createdAtUtc)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        RecipientEmail = recipientEmail;
        Type = type;
        Title = title;
        Message = message;
        DeduplicationKey = deduplicationKey;
        CreatedAtUtc = createdAtUtc;
        DeliveryStatus = NotificationDeliveryStatus.Pending;
    }

    public Guid Id { get; private set; }
    public int UserId { get; private set; }
    public string RecipientEmail { get; private set; } = string.Empty;
    public NotificationType Type { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public string DeduplicationKey { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? ReadAtUtc { get; private set; }
    public DateTime? EmailSentAtUtc { get; private set; }
    public NotificationDeliveryStatus DeliveryStatus { get; private set; }
    public int DeliveryAttemptCount { get; private set; }
    public string? LastDeliveryError { get; private set; }

    public static Notification Create(
        int userId,
        string recipientEmail,
        NotificationType type,
        string title,
        string message,
        string deduplicationKey,
        DateTime createdAtUtc)
    {
        if (userId <= 0)
            throw new ArgumentOutOfRangeException(nameof(userId));

        ArgumentException.ThrowIfNullOrWhiteSpace(recipientEmail);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        ArgumentException.ThrowIfNullOrWhiteSpace(deduplicationKey);

        return new Notification(
            userId,
            recipientEmail.Trim(),
            type,
            title.Trim(),
            message.Trim(),
            deduplicationKey.Trim(),
            createdAtUtc);
    }

    public static Notification Create(
        int userId,
        NotificationType type,
        string title,
        string message,
        string deduplicationKey,
        DateTime createdAtUtc)
        => Create(
            userId,
            "test@example.com",
            type,
            title,
            message,
            deduplicationKey,
            createdAtUtc);

    public void MarkAsRead(DateTime readAtUtc)
    {
        ReadAtUtc ??= readAtUtc;
    }

    public void MarkEmailAsSent(DateTime sentAtUtc)
    {
        DeliveryAttemptCount++;
        EmailSentAtUtc = sentAtUtc;
        DeliveryStatus = NotificationDeliveryStatus.Sent;
        LastDeliveryError = null;
    }

    public void RecordEmailFailure(string error)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(error);

        DeliveryAttemptCount++;
        DeliveryStatus = NotificationDeliveryStatus.Failed;
        LastDeliveryError = error.Trim();
    }

    public void MarkEmailAsDisabled()
    {
        DeliveryStatus = NotificationDeliveryStatus.Disabled;
        EmailSentAtUtc = null;
        LastDeliveryError = "Email delivery is disabled by configuration.";
    }
}
