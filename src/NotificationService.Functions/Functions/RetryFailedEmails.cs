using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NotificationService.Application.Notifications;

namespace NotificationService.Functions.Functions;

public sealed class RetryFailedEmails
{
    private readonly EmailRetryService _retryService;
    private readonly ILogger<RetryFailedEmails> _logger;
    private readonly int _maxAttempts;
    private readonly int _batchSize;

    public RetryFailedEmails(
        EmailRetryService retryService,
        ILogger<RetryFailedEmails> logger,
        IConfiguration configuration)
    {
        _retryService = retryService;
        _logger = logger;
        _maxAttempts = int.TryParse(configuration["EmailMaxDeliveryAttempts"], out var attempts)
            ? attempts
            : 3;
        _batchSize = int.TryParse(configuration["EmailRetryBatchSize"], out var batchSize)
            ? batchSize
            : 50;
    }

    [Function(nameof(RetryFailedEmails))]
    public async Task Run(
        [TimerTrigger("%EmailRetrySchedule%")] TimerInfo timerInfo,
        CancellationToken cancellationToken)
    {
        var retried = await _retryService.ExecuteAsync(
            _maxAttempts,
            _batchSize,
            cancellationToken);
        _logger.LogInformation("Email retry completed. Retried {RetriedCount} notification(s).", retried);
    }
}
