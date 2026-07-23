using NotificationService.Application.RegistrationReminders;

namespace NotificationService.Application.Abstractions;

public interface IRegistrationReminderCandidateReader
{
    Task<IReadOnlyList<RegistrationReminderCandidate>> ListAsync(
        DateOnly registrationEndsOn,
        CancellationToken cancellationToken = default);
}
