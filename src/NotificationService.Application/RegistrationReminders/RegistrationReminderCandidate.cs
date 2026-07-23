namespace NotificationService.Application.RegistrationReminders;

public sealed record RegistrationReminderCandidate(
    int UserId,
    string Email,
    string RecipientName,
    int TermId,
    string TermName,
    DateOnly RegistrationEndDate,
    IReadOnlyList<string> SubjectNames);
