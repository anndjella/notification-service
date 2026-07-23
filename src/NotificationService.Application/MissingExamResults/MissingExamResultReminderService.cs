using NotificationService.Application.Abstractions;
using NotificationService.Application.Notifications;
using NotificationService.Application.RegistrationReminders;
using NotificationService.Domain.Notifications;

namespace NotificationService.Application.MissingExamResults;

public sealed class MissingExamResultReminderService
{
    private readonly IMissingExamResultCandidateReader _candidateReader;
    private readonly NotificationDispatcher _dispatcher;

    public MissingExamResultReminderService(
        IMissingExamResultCandidateReader candidateReader,
        NotificationDispatcher dispatcher)
    {
        _candidateReader = candidateReader;
        _dispatcher = dispatcher;
    }

    public async Task<RegistrationReminderRunResult> ExecuteAsync(
        DateOnly examDate,
        DateTime createdAtUtc,
        CancellationToken cancellationToken = default)
    {
        var candidates = await _candidateReader.ListAsync(examDate, cancellationToken);
        var createdCount = 0;

        foreach (var candidate in candidates)
        {
            var notification = Notification.Create(
                candidate.UserId,
                candidate.Email,
                NotificationType.MissingExamResultReminder,
                "Missing exam results",
                $"It has been 30 days since the {candidate.SubjectName} exam in {candidate.TermName}. " +
                $"A result or absence is still missing for {candidate.MissingResultCount} student(s).",
                $"missing-exam-results:{candidate.TeacherId}:{candidate.SubjectId}:{candidate.TermId}:{candidate.ExamDate:yyyyMMdd}",
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
