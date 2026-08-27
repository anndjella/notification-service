using NotificationService.Application.Abstractions;
using NotificationService.Application.Notifications;
using NotificationService.Domain.Notifications;

namespace NotificationService.Application.MissingExamResults;

public sealed class MissingExamResultReminderService
{
    private readonly IMissingExamResultCandidateReader _candidateReader;
    private readonly INotificationMessagePublisher _publisher;

    public MissingExamResultReminderService(
        IMissingExamResultCandidateReader candidateReader,
        INotificationMessagePublisher publisher)
    {
        _candidateReader = candidateReader;
        _publisher = publisher;
    }

    public async Task<NotificationScheduleRunResult> ExecuteAsync(
        DateOnly examDate,
        DateTime createdAtUtc,
        CancellationToken cancellationToken = default)
    {
        var candidates = await _candidateReader.ListAsync(examDate, cancellationToken);
        var messages = candidates.Select(candidate => new NotificationMessage(
                candidate.UserId,
                candidate.Email,
                candidate.RecipientName,
                NotificationType.MissingExamResultReminder,
                "Missing exam results",
                $"It has been 30 days since the {candidate.SubjectName} exam in {candidate.TermName}. " +
                $"A result or absence is still missing for {candidate.MissingResultCount} student(s).",
                $"missing-exam-results:{candidate.TeacherId}:{candidate.SubjectId}:{candidate.TermId}:{candidate.ExamDate:yyyyMMdd}",
                createdAtUtc))
            .ToArray();

        await _publisher.PublishAsync(messages, cancellationToken);

        return new NotificationScheduleRunResult(
            candidates.Count,
            messages.Length);
    }
}
