using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NotificationService.Application.MissingExamResults;

namespace NotificationService.Functions.Functions;

public sealed class ScheduleMissingExamResultReminders
{
    private readonly ILogger<ScheduleMissingExamResultReminders> _logger;
    private readonly MissingExamResultReminderService _reminderService;
    private readonly TimeProvider _timeProvider;
    private readonly TimeZoneInfo _timeZone;
    private readonly int _daysAfterExam;

    public ScheduleMissingExamResultReminders(
        ILogger<ScheduleMissingExamResultReminders> logger,
        MissingExamResultReminderService reminderService,
        TimeProvider timeProvider,
        IConfiguration configuration)
    {
        _logger = logger;
        _reminderService = reminderService;
        _timeProvider = timeProvider;
        _timeZone = TimeZoneInfo.FindSystemTimeZoneById(
            configuration["ReminderTimeZone"] ?? "Europe/Budapest");
        _daysAfterExam = int.TryParse(configuration["MissingExamResultReminderDays"], out var days)
            ? days
            : 30;
    }

    [Function(nameof(ScheduleMissingExamResultReminders))]
    public async Task Run(
        [TimerTrigger("%MissingExamResultReminderSchedule%")] TimerInfo timerInfo,
        CancellationToken cancellationToken)
    {
        var utcNow = _timeProvider.GetUtcNow();
        var localNow = TimeZoneInfo.ConvertTime(utcNow, _timeZone);
        var examDate = DateOnly.FromDateTime(localNow.DateTime).AddDays(-_daysAfterExam);

        _logger.LogInformation(
            "Missing exam result scheduler triggered. Looking for unresolved exams from {ExamDate}.",
            examDate);

        var result = await _reminderService.ExecuteAsync(
            examDate,
            utcNow.UtcDateTime,
            cancellationToken);

        _logger.LogInformation(
            "Missing exam result scheduler completed. Candidates: {CandidateCount}, created: {CreatedCount}, duplicates: {DuplicateCount}.",
            result.CandidateCount,
            result.CreatedCount,
            result.DuplicateCount);
    }
}
