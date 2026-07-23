using NotificationService.Application.Abstractions;
using NotificationService.Application.Notifications;
using NotificationService.Domain.Notifications;

namespace NotificationService.Application.RegistrationReminders;

public sealed class RegistrationReminderService
{
    private readonly IRegistrationReminderCandidateReader _candidateReader;
    private readonly NotificationDispatcher _dispatcher;

    public RegistrationReminderService(
        IRegistrationReminderCandidateReader candidateReader,
        NotificationDispatcher dispatcher)
    {
        _candidateReader = candidateReader;
        _dispatcher = dispatcher;
    }

    public async Task<RegistrationReminderRunResult> ExecuteAsync(
        DateOnly registrationEndsOn,
        DateTime createdAtUtc,
        CancellationToken cancellationToken = default)
    {
        var candidates = await _candidateReader.ListAsync(registrationEndsOn, cancellationToken);
        var createdCount = 0;

        foreach (var candidate in candidates)
        {
            var subjects = string.Join(", ", candidate.SubjectNames.Order(StringComparer.OrdinalIgnoreCase));
            var notification = Notification.Create(
                candidate.UserId,
                candidate.Email,
                NotificationType.RegistrationDeadlineReminder,
                "Exam registration closes tomorrow",
                $"Registration for {candidate.TermName} closes on {candidate.RegistrationEndDate:dd.MM.yyyy}. " +
                $"You can still register: {subjects}.",
                $"registration-deadline:{candidate.TermId}:{candidate.UserId}",
                createdAtUtc);

            if (await _dispatcher.DispatchAsync(notification, candidate.RecipientName, cancellationToken))
                createdCount++;
        }

        return new RegistrationReminderRunResult(
            candidates.Count,
            createdCount,
            candidates.Count - createdCount);
    }
}
