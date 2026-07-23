namespace NotificationService.Application.Abstractions;

public interface IEmailSender
{
    bool IsEnabled { get; }

    Task SendAsync(
        string recipientEmail,
        string recipientName,
        string subject,
        string message,
        CancellationToken cancellationToken = default);
}
