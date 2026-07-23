using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NotificationService.Application.RegistrationReminders;

namespace NotificationService.Functions.Functions;

public sealed class ScheduleRegistrationReminders
{
    private readonly ILogger<ScheduleRegistrationReminders> _logger;
    private readonly RegistrationReminderService _reminderService;
    private readonly TimeProvider _timeProvider;
    private readonly TimeZoneInfo _timeZone;

    public ScheduleRegistrationReminders(
        ILogger<ScheduleRegistrationReminders> logger,
        RegistrationReminderService reminderService,
        TimeProvider timeProvider,
        IConfiguration configuration)
    {
        _logger = logger;
        _reminderService = reminderService;
        _timeProvider = timeProvider;

        var timeZoneId = configuration["ReminderTimeZone"] ?? "Europe/Budapest";
        _timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
    }

    [Function(nameof(ScheduleRegistrationReminders))]
    public async Task Run(
        [TimerTrigger("%RegistrationReminderSchedule%")] TimerInfo timerInfo,
        CancellationToken cancellationToken)
    {
        var utcNow = _timeProvider.GetUtcNow();
        var localNow = TimeZoneInfo.ConvertTime(utcNow, _timeZone);
        var registrationEndsOn = DateOnly.FromDateTime(localNow.DateTime).AddDays(1);

        _logger.LogInformation(
            "Registration reminder scheduler triggered at {TriggeredAtUtc}. Looking for terms ending on {RegistrationEndsOn}. Next execution: {NextExecutionUtc}.",
            utcNow,
            registrationEndsOn,
            timerInfo.ScheduleStatus?.Next);

        var result = await _reminderService.ExecuteAsync(
            registrationEndsOn,
            utcNow.UtcDateTime,
            cancellationToken);

        _logger.LogInformation(
            "Registration reminder scheduler completed. Candidates: {CandidateCount}, created: {CreatedCount}, duplicates: {DuplicateCount}.",
            result.CandidateCount,
            result.CreatedCount,
            result.DuplicateCount);
    }
}
