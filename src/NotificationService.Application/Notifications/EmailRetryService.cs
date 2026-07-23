using NotificationService.Application.Abstractions;

namespace NotificationService.Application.Notifications;

public sealed class EmailRetryService
{
    private readonly INotificationRepository _repository;
    private readonly NotificationDispatcher _dispatcher;

    public EmailRetryService(
        INotificationRepository repository,
        NotificationDispatcher dispatcher)
    {
        _repository = repository;
        _dispatcher = dispatcher;
    }

    public async Task<int> ExecuteAsync(
        int maxAttempts,
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        var candidates = await _repository.ListEmailRetryCandidatesAsync(
            maxAttempts,
            batchSize,
            cancellationToken);

        foreach (var notification in candidates)
            await _dispatcher.RetryEmailAsync(notification, cancellationToken);

        return candidates.Count;
    }
}
