namespace NotificationService.Application.Notifications;

public sealed record NotificationScheduleRunResult(
    int CandidateCount,
    int EnqueuedCount);
