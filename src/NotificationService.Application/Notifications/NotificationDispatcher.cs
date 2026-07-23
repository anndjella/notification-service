using NotificationService.Application.Abstractions;
using NotificationService.Domain.Notifications;

namespace NotificationService.Application.Notifications;

public sealed class NotificationDispatcher
{
    private readonly INotificationRepository _repository;
    private readonly IEmailSender _emailSender;

    public NotificationDispatcher(
        INotificationRepository repository,
        IEmailSender emailSender)
    {
        _repository = repository;
        _emailSender = emailSender;
    }

    public async Task<bool> DispatchAsync(
        Notification notification,
        string recipientName,
        CancellationToken cancellationToken = default)
    {
        if (!await _repository.TryAddAsync(notification, cancellationToken))
            return false;

        await DeliverEmailAsync(notification, recipientName, cancellationToken);
        return true;
    }

    public Task RetryEmailAsync(
        Notification notification,
        CancellationToken cancellationToken = default)
        => DeliverEmailAsync(notification, notification.RecipientEmail, cancellationToken);

    private async Task DeliverEmailAsync(
        Notification notification,
        string recipientName,
        CancellationToken cancellationToken)
    {
        try
        {
            var emailMessage = BuildEmailMessage(recipientName, notification.Message);
            await _emailSender.SendAsync(
                notification.RecipientEmail,
                recipientName,
                notification.Title,
                emailMessage,
                cancellationToken);
            if (_emailSender.IsEnabled)
                notification.MarkEmailAsSent(DateTime.UtcNow);
            else
                notification.MarkEmailAsDisabled();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            notification.RecordEmailFailure(exception.Message);
        }

        await _repository.SaveChangesAsync(cancellationToken);
    }

    private static string BuildEmailMessage(string recipientName, string message)
        => $"Dear {recipientName},{Environment.NewLine}{Environment.NewLine}" +
           $"{message}{Environment.NewLine}{Environment.NewLine}" +
           $"Best regards,{Environment.NewLine}" +
           "Student Exam System";
}
