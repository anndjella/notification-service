using NotificationService.Application.Abstractions;
using NotificationService.Application.Notifications;
using NotificationService.Application.RegistrationReminders;
using NotificationService.Domain.Notifications;

namespace NotificationService.UnitTests.Application;

public sealed class RegistrationReminderServiceTests
{
    [Fact]
    public async Task ExecuteAsync_EnqueuesOneMessagePerCandidate()
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
        var publisher = new RecordingPublisher();
        var service = new RegistrationReminderService(
            new StubCandidateReader(candidates),
            publisher);

        var result = await service.ExecuteAsync(
            new DateOnly(2026, 7, 16),
            new DateTime(2026, 7, 15, 8, 0, 0, DateTimeKind.Utc));

        Assert.Equal(1, result.CandidateCount);
        Assert.Equal(1, result.EnqueuedCount);
        var message = Assert.Single(publisher.Messages);
        Assert.Equal(NotificationType.RegistrationDeadlineReminder, message.Type);
        Assert.Equal("registration-deadline:5:42", message.DeduplicationKey);
        Assert.Equal("Test Student", message.RecipientName);
        Assert.Contains("Databases", message.Message);
        Assert.Contains("Mathematics", message.Message);
    }

    [Fact]
    public async Task ExecuteAsync_WhenNoCandidates_EnqueuesNothing()
    {
        var publisher = new RecordingPublisher();
        var service = new RegistrationReminderService(
            new StubCandidateReader([]),
            publisher);

        var result = await service.ExecuteAsync(
            new DateOnly(2026, 7, 16),
            new DateTime(2026, 7, 15, 8, 0, 0, DateTimeKind.Utc));

        Assert.Equal(0, result.CandidateCount);
        Assert.Equal(0, result.EnqueuedCount);
        Assert.Empty(publisher.Messages);
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

    private sealed class RecordingPublisher : INotificationMessagePublisher
    {
        public List<NotificationMessage> Messages { get; } = [];

        public Task PublishAsync(
            IReadOnlyCollection<NotificationMessage> messages,
            CancellationToken cancellationToken = default)
        {
            Messages.AddRange(messages);
            return Task.CompletedTask;
        }
    }
}
