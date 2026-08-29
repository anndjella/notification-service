using NotificationService.Application.Abstractions;
using NotificationService.Application.MissingExamResults;
using NotificationService.Application.Notifications;
using NotificationService.Domain.Notifications;

namespace NotificationService.UnitTests.Application;

public sealed class MissingExamResultReminderServiceTests
{
    [Fact]
    public async Task ExecuteAsync_EnqueuesGroupedTeacherReminder()
    {
        var candidate = new MissingExamResultCandidate(
            UserId: 15,
            TeacherId: 8,
            Email: "teacher@example.com",
            RecipientName: "Test Teacher",
            SubjectId: 4,
            SubjectName: "Databases",
            TermId: 3,
            TermName: "July term",
            ExamDate: new DateOnly(2026, 6, 17),
            MissingResultCount: 4);
        var publisher = new RecordingPublisher();
        var service = new MissingExamResultReminderService(
            new StubCandidateReader([candidate]),
            publisher);

        var result = await service.ExecuteAsync(
            new DateOnly(2026, 6, 17),
            new DateTime(2026, 7, 17, 8, 0, 0, DateTimeKind.Utc));

        Assert.Equal(1, result.CandidateCount);
        Assert.Equal(1, result.EnqueuedCount);
        var message = Assert.Single(publisher.Messages);
        Assert.Equal(NotificationType.MissingExamResultReminder, message.Type);
        Assert.Equal("missing-exam-results:8:4:3:20260617", message.DeduplicationKey);
        Assert.Equal("teacher@example.com", message.RecipientEmail);
        Assert.Equal("Test Teacher", message.RecipientName);
        Assert.Contains("4 student(s)", message.Message);
    }

    private sealed class StubCandidateReader : IMissingExamResultCandidateReader
    {
        private readonly IReadOnlyList<MissingExamResultCandidate> _candidates;

        public StubCandidateReader(IReadOnlyList<MissingExamResultCandidate> candidates)
        {
            _candidates = candidates;
        }

        public Task<IReadOnlyList<MissingExamResultCandidate>> ListAsync(
            DateOnly examDate,
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
