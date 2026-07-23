using Microsoft.EntityFrameworkCore;
using NotificationService.Application.Abstractions;
using NotificationService.Application.Notifications;
using NotificationService.Application.RegistrationReminders;
using NotificationService.Domain.Notifications;
using NotificationService.Infrastructure.Persistence;

namespace NotificationService.UnitTests.Infrastructure;

public sealed class NotificationPersistenceIntegrationTests : IAsyncLifetime
{
    private readonly string _databaseName = $"NotificationServiceTests_{Guid.NewGuid():N}";
    private NotificationDbContext _db = null!;
    private NotificationRepository _repository = null!;

    public async Task InitializeAsync()
    {
        var connectionString =
            $"Server=(localdb)\\MSSQLLocalDB;Database={_databaseName};" +
            "Trusted_Connection=True;TrustServerCertificate=True";
        var options = new DbContextOptionsBuilder<NotificationDbContext>()
            .UseSqlServer(
                connectionString,
                sqlOptions => sqlOptions.MigrationsHistoryTable(
                    "__NotificationServiceMigrationsHistory",
                    "notification"))
            .Options;

        _db = new NotificationDbContext(options);
        await _db.Database.MigrateAsync();
        _repository = new NotificationRepository(_db);
    }

    [Fact]
    public async Task MigrationAndRepository_PersistNotificationAndRejectDuplicateKey()
    {
        var createdAtUtc = new DateTime(2026, 7, 22, 8, 0, 0, DateTimeKind.Utc);
        var first = CreateNotification("registration-deadline:5:42", createdAtUtc);
        var duplicate = CreateNotification("registration-deadline:5:42", createdAtUtc);

        var firstAdded = await _repository.TryAddAsync(first);
        var duplicateAdded = await _repository.TryAddAsync(duplicate);
        var stored = await _repository.ListForUserAsync(42);

        Assert.True(firstAdded);
        Assert.False(duplicateAdded);
        Assert.Single(stored);
        Assert.Equal(first.Id, stored[0].Id);
    }

    [Fact]
    public async Task RegistrationReminderWorkflow_CreatesOnceAndCanBeReadEndToEnd()
    {
        var registrationEndsOn = new DateOnly(2026, 7, 23);
        var candidateReader = new FixedCandidateReader(new RegistrationReminderCandidate(
            UserId: 42,
            Email: "student@example.com",
            RecipientName: "Student Test",
            TermId: 5,
            TermName: "July term",
            RegistrationEndDate: registrationEndsOn,
            SubjectNames: ["Distributed Systems"]));
        var emailSender = new RecordingEmailSender();
        var dispatcher = new NotificationDispatcher(_repository, emailSender);
        var reminderService = new RegistrationReminderService(candidateReader, dispatcher);
        var queryService = new NotificationQueryService(_repository);
        var createdAtUtc = new DateTime(2026, 7, 22, 8, 0, 0, DateTimeKind.Utc);

        var firstRun = await reminderService.ExecuteAsync(registrationEndsOn, createdAtUtc);
        var repeatedRun = await reminderService.ExecuteAsync(registrationEndsOn, createdAtUtc);
        var notifications = await queryService.ListAsync(42);
        var unreadBefore = await queryService.CountUnreadAsync(42);
        var marked = await queryService.MarkAsReadAsync(
            notifications.Single().ID,
            42,
            createdAtUtc.AddMinutes(1));
        var unreadAfter = await queryService.CountUnreadAsync(42);

        Assert.Equal(1, firstRun.CreatedCount);
        Assert.Equal(0, firstRun.DuplicateCount);
        Assert.Equal(0, repeatedRun.CreatedCount);
        Assert.Equal(1, repeatedRun.DuplicateCount);
        Assert.Single(notifications);
        Assert.Equal(1, unreadBefore);
        Assert.True(marked);
        Assert.Equal(0, unreadAfter);
        Assert.Equal(1, emailSender.SendCount);
    }

    public async Task DisposeAsync()
    {
        if (_db is null) return;

        await _db.Database.EnsureDeletedAsync();
        await _db.DisposeAsync();
    }

    private static Notification CreateNotification(string deduplicationKey, DateTime createdAtUtc)
        => Notification.Create(
            42,
            "student@example.com",
            NotificationType.RegistrationDeadlineReminder,
            "Exam registration closes tomorrow",
            "Registration closes tomorrow.",
            deduplicationKey,
            createdAtUtc);

    private sealed class FixedCandidateReader : IRegistrationReminderCandidateReader
    {
        private readonly IReadOnlyList<RegistrationReminderCandidate> _candidates;

        public FixedCandidateReader(RegistrationReminderCandidate candidate)
        {
            _candidates = [candidate];
        }

        public Task<IReadOnlyList<RegistrationReminderCandidate>> ListAsync(
            DateOnly registrationEndsOn,
            CancellationToken cancellationToken = default)
            => Task.FromResult(_candidates);
    }

    private sealed class RecordingEmailSender : IEmailSender
    {
        public bool IsEnabled => true;
        public int SendCount { get; private set; }

        public Task SendAsync(
            string recipientEmail,
            string recipientName,
            string subject,
            string message,
            CancellationToken cancellationToken = default)
        {
            SendCount++;
            return Task.CompletedTask;
        }
    }
}
