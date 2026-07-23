using NotificationService.Domain.Notifications;

namespace NotificationService.UnitTests.Domain;

public sealed class NotificationTests
{
    [Fact]
    public void Create_WithValidData_CreatesPendingNotification()
    {
        var createdAtUtc = new DateTime(2026, 7, 13, 8, 0, 0, DateTimeKind.Utc);

        var notification = Notification.Create(
            userId: 42,
            NotificationType.RegistrationDeadlineReminder,
            "Registration closes tomorrow",
            "You still have exams available for registration.",
            "registration-deadline:5:42",
            createdAtUtc);

        Assert.NotEqual(Guid.Empty, notification.Id);
        Assert.Equal(42, notification.UserId);
        Assert.Equal(NotificationDeliveryStatus.Pending, notification.DeliveryStatus);
        Assert.Equal("registration-deadline:5:42", notification.DeduplicationKey);
        Assert.Equal(createdAtUtc, notification.CreatedAtUtc);
    }

    [Fact]
    public void MarkAsRead_WhenCalledTwice_PreservesFirstReadTime()
    {
        var notification = CreateNotification();
        var firstReadAtUtc = new DateTime(2026, 7, 13, 9, 0, 0, DateTimeKind.Utc);
        var secondReadAtUtc = firstReadAtUtc.AddHours(1);

        notification.MarkAsRead(firstReadAtUtc);
        notification.MarkAsRead(secondReadAtUtc);

        Assert.Equal(firstReadAtUtc, notification.ReadAtUtc);
    }

    [Fact]
    public void RecordEmailFailure_IncrementsAttemptAndStoresError()
    {
        var notification = CreateNotification();

        notification.RecordEmailFailure("Provider unavailable");

        Assert.Equal(NotificationDeliveryStatus.Failed, notification.DeliveryStatus);
        Assert.Equal(1, notification.DeliveryAttemptCount);
        Assert.Equal("Provider unavailable", notification.LastDeliveryError);
    }

    private static Notification CreateNotification()
        => Notification.Create(
            userId: 42,
            NotificationType.RegistrationDeadlineReminder,
            "Registration closes tomorrow",
            "You still have exams available for registration.",
            "registration-deadline:5:42",
            DateTime.UtcNow);
}
