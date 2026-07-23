using NotificationService.Application.Abstractions;
using NotificationService.Application.RegistrationReminders;
using NotificationService.Application.Notifications;
using NotificationService.Domain.Notifications;

namespace NotificationService.UnitTests.Application;

public sealed class RegistrationReminderServiceTests
{
    [Fact]
    public async Task ExecuteAsync_CreatesOneNotificationPerCandidate()
    {
        var candidates = new[]
        {
            new RegistrationReminderCandidate(
                UserId: 42,
                Email: "student@example.com",
                RecipientName: "Test Student",
                TermId: 5,
                TermName: "July term",
                RegistrationEndDate: new DateOnly(2026, 7, 16),
                SubjectNames: new[] { "Databases", "Mathematics" })
        };
        var repository = new FakeNotificationRepository();
        var service = new RegistrationReminderService(
            new StubCandidateReader(candidates),
            new NotificationDispatcher(repository, new FakeEmailSender()));

        var result = await service.ExecuteAsync(
            new DateOnly(2026, 7, 16),
            new DateTime(2026, 7, 15, 8, 0, 0, DateTimeKind.Utc));

        Assert.Equal(1, result.CandidateCount);
        Assert.Equal(1, result.CreatedCount);
        var notification = Assert.Single(repository.Notifications);
        Assert.Equal("registration-deadline:5:42", notification.DeduplicationKey);
        Assert.Contains("Databases", notification.Message);
        Assert.Contains("Mathematics", notification.Message);
    }

    [Fact]
    public async Task ExecuteAsync_WhenNotificationAlreadyExists_ReportsDuplicate()
    {
        var candidates = new[]
        {
            new RegistrationReminderCandidate(
                UserId: 42,
                Email: "student@example.com",
                RecipientName: "Test Student",
                TermId: 5,
                TermName: "July term",
                RegistrationEndDate: new DateOnly(2026, 7, 16),
                SubjectNames: new[] { "Databases" })
        };
        var service = new RegistrationReminderService(
            new StubCandidateReader(candidates),
            new NotificationDispatcher(
                new FakeNotificationRepository(alwaysDuplicate: true),
                new FakeEmailSender()));

        var result = await service.ExecuteAsync(
            new DateOnly(2026, 7, 16),
            new DateTime(2026, 7, 15, 8, 0, 0, DateTimeKind.Utc));

        Assert.Equal(0, result.CreatedCount);
        Assert.Equal(1, result.DuplicateCount);
    }

    private sealed class StubCandidateReader : IRegistrationReminderCandidateReader
    {
        private readonly IReadOnlyList<RegistrationReminderCandidate> _candidates;

        public StubCandidateReader(IReadOnlyList<RegistrationReminderCandidate> candidates)
        {
            _candidates = candidates;
        }

        public Task<IReadOnlyList<RegistrationReminderCandidate>> ListAsync(
            DateOnly registrationEndsOn,
            CancellationToken cancellationToken = default)
            => Task.FromResult(_candidates);
    }

    private sealed class FakeNotificationRepository : INotificationRepository
    {
        private readonly bool _alwaysDuplicate;

        public FakeNotificationRepository(bool alwaysDuplicate = false)
        {
            _alwaysDuplicate = alwaysDuplicate;
        }

        public List<Notification> Notifications { get; } = new();

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
            => Task.FromResult<IReadOnlyList<Notification>>(
                Notifications.Where(notification => notification.UserId == userId).ToList());

        public Task<int> CountUnreadAsync(
            int userId,
            CancellationToken cancellationToken = default)
            => Task.FromResult(Notifications.Count(notification =>
                notification.UserId == userId && notification.ReadAtUtc is null));

        public Task<Notification?> GetOwnedAsync(
            Guid notificationId,
            int userId,
            CancellationToken cancellationToken = default)
            => Task.FromResult(Notifications.FirstOrDefault(notification =>
                notification.Id == notificationId && notification.UserId == userId));

        public Task<IReadOnlyList<Notification>> ListEmailRetryCandidatesAsync(
            int maxAttempts,
            int take,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Notification>>([]);

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class FakeEmailSender : IEmailSender
    {
        public bool IsEnabled => true;

        public Task SendAsync(
            string recipientEmail,
            string recipientName,
            string subject,
            string message,
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
