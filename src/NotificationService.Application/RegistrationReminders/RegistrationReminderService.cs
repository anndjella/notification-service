using NotificationService.Application.Abstractions;
using NotificationService.Application.Notifications;
using NotificationService.Domain.Notifications;

namespace NotificationService.Application.RegistrationReminders;

public sealed class RegistrationReminderService
{
    private readonly IRegistrationReminderCandidateReader _candidateReader;
    private readonly INotificationMessagePublisher _publisher;

    public RegistrationReminderService(
        IRegistrationReminderCandidateReader candidateReader,
        INotificationMessagePublisher publisher)
    {
        _candidateReader = candidateReader;
        _publisher = publisher;
    }

    public async Task<NotificationScheduleRunResult> ExecuteAsync(
        DateOnly registrationEndsOn,
        DateTime createdAtUtc,
        CancellationToken cancellationToken = default)
    {
        var candidates = await _candidateReader.ListAsync(registrationEndsOn, cancellationToken);
        var messages = candidates.Select(candidate =>
        {
            var subjects = string.Join(", ", candidate.SubjectNames.Order(StringComparer.OrdinalIgnoreCase));
            return new NotificationMessage(
                candidate.UserId,
                candidate.Email,
                candidate.RecipientName,
                NotificationType.RegistrationDeadlineReminder,
                "Exam registration closes tomorrow",
                $"Registration for {candidate.TermName} closes on {candidate.RegistrationEndDate:dd.MM.yyyy}. " +
                $"You can still register: {subjects}.",
                $"registration-deadline:{candidate.TermId}:{candidate.UserId}",
                createdAtUtc);
        }).ToArray();

        await _publisher.PublishAsync(messages, cancellationToken);

        return new NotificationScheduleRunResult(
            candidates.Count,
            messages.Length);
    }
}
