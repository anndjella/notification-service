using NotificationService.Application.Abstractions;
using NotificationService.Application.MissingExamResults;
using NotificationService.Application.Notifications;
using NotificationService.Domain.Notifications;

namespace NotificationService.UnitTests.Application;

public sealed class MissingExamResultReminderServiceTests
{
    [Fact]
    public async Task ExecuteAsync_CreatesGroupedTeacherReminderAndSendsEmail()
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
        var repository = new FakeRepository();
        var emailSender = new FakeEmailSender();
        var service = new MissingExamResultReminderService(
            new StubCandidateReader([candidate]),
            new NotificationDispatcher(repository, emailSender));

        var result = await service.ExecuteAsync(
            new DateOnly(2026, 6, 17),
            new DateTime(2026, 7, 17, 8, 0, 0, DateTimeKind.Utc));

        Assert.Equal(1, result.CreatedCount);
        var notification = Assert.Single(repository.Notifications);
        Assert.Equal(NotificationType.MissingExamResultReminder, notification.Type);
        Assert.Equal("missing-exam-results:8:4:3:20260617", notification.DeduplicationKey);
        Assert.Contains("4 student(s)", notification.Message);
        Assert.Equal("teacher@example.com", Assert.Single(emailSender.Recipients));
        var emailMessage = Assert.Single(emailSender.Messages);
        Assert.StartsWith("Dear Test Teacher,", emailMessage);
        Assert.Contains("Best regards,", emailMessage);
        Assert.EndsWith("Student Exam System", emailMessage);
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

    private sealed class FakeEmailSender : IEmailSender
    {
        public bool IsEnabled => true;

        public List<string> Recipients { get; } = [];
        public List<string> Messages { get; } = [];

        public Task SendAsync(
            string recipientEmail,
            string recipientName,
            string subject,
            string message,
            CancellationToken cancellationToken = default)
        {
            Recipients.Add(recipientEmail);
            Messages.Add(message);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeRepository : INotificationRepository
    {
        public List<Notification> Notifications { get; } = [];

        public Task<bool> TryAddAsync(Notification notification, CancellationToken cancellationToken = default)
        {
            Notifications.Add(notification);
            return Task.FromResult(true);
        }

        public Task<IReadOnlyList<Notification>> ListForUserAsync(int userId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Notification>>([]);

        public Task<int> CountUnreadAsync(int userId, CancellationToken cancellationToken = default)
            => Task.FromResult(0);

        public Task<Notification?> GetOwnedAsync(Guid notificationId, int userId, CancellationToken cancellationToken = default)
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
