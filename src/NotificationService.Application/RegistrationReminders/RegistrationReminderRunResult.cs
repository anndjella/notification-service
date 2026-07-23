namespace NotificationService.Application.RegistrationReminders;

public sealed record RegistrationReminderRunResult(
    int CandidateCount,
    int CreatedCount,
    int DuplicateCount);
