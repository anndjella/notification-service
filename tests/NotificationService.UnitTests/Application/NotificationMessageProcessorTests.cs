using NotificationService.Application.Abstractions;
using NotificationService.Application.Notifications;
using NotificationService.Domain.Notifications;

namespace NotificationService.UnitTests.Application;

public sealed class NotificationMessageProcessorTests
{
    [Fact]
    public async Task ExecuteAsync_CreatesNotificationAndSendsEmail()
    {
        var repository = new FakeRepository();
        var emailSender = new RecordingEmailSender();
        var processor = new NotificationMessageProcessor(
            new NotificationDispatcher(repository, emailSender));

        var created = await processor.ExecuteAsync(CreateMessage());

        Assert.True(created);
        var notification = Assert.Single(repository.Notifications);
        Assert.Equal("missing-exam-results:8:4:3:20260617", notification.DeduplicationKey);
        Assert.Equal(NotificationDeliveryStatus.Sent, notification.DeliveryStatus);
        Assert.Equal("teacher@example.com", Assert.Single(emailSender.Recipients));
    }

    [Fact]
    public async Task ExecuteAsync_WhenNotificationExists_DoesNotSendEmail()
    {
        var repository = new FakeRepository(alwaysDuplicate: true);
        var emailSender = new RecordingEmailSender();
        var processor = new NotificationMessageProcessor(
            new NotificationDispatcher(repository, emailSender));

        var created = await processor.ExecuteAsync(CreateMessage());

        Assert.False(created);
        Assert.Empty(emailSender.Recipients);
    }

    private static NotificationMessage CreateMessage()
        => new(
            UserId: 15,
            RecipientEmail: "teacher@example.com",
            RecipientName: "Test Teacher",
            Type: NotificationType.MissingExamResultReminder,
            Title: "Missing exam results",
            Message: "A result is still missing.",
            DeduplicationKey: "missing-exam-results:8:4:3:20260617",
            CreatedAtUtc: new DateTime(2026, 7, 17, 8, 0, 0, DateTimeKind.Utc));

    private sealed class RecordingEmailSender : IEmailSender
    {
        public bool IsEnabled => true;
        public List<string> Recipients { get; } = [];

        public Task SendAsync(
            string recipientEmail,
            string recipientName,
            string subject,
            string message,
            CancellationToken cancellationToken = default)
        {
            Recipients.Add(recipientEmail);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeRepository : INotificationRepository
    {
        private readonly bool _alwaysDuplicate;

        public FakeRepository(bool alwaysDuplicate = false)
        {
            _alwaysDuplicate = alwaysDuplicate;
        }

        public List<Notification> Notifications { get; } = [];

        public Task<bool> TryAddAsync(
            Notification notification,
            CancellationToken cancellationToken = default)
        {
            if (_alwaysDuplicate)
                return Task.FromResult(false);

            Notifications.Add(notification);
            return Task.FromResult(true);
        }

        public Task<IReadOnlyList<Notification>> ListForUserAsync(
            int userId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Notification>>(Notifications);

        public Task<int> CountUnreadAsync(
            int userId,
            CancellationToken cancellationToken = default)
            => Task.FromResult(0);

        public Task<Notification?> GetOwnedAsync(
            Guid notificationId,
            int userId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<Notification?>(null);

        public Task<IReadOnlyList<Notification>> ListEmailRetryCandidatesAsync(
            int maxAttempts,
            int take,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Notification>>([]);

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
