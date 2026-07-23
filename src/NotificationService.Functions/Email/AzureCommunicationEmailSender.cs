using Azure;
using Azure.Communication.Email;
using Microsoft.Extensions.Configuration;
using NotificationService.Application.Abstractions;

namespace NotificationService.Functions.Email;

public sealed class AzureCommunicationEmailSender : IEmailSender
{
    private readonly IConfiguration _configuration;

    public AzureCommunicationEmailSender(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public bool IsEnabled
        => !string.IsNullOrWhiteSpace(_configuration["AzureCommunicationEmailConnectionString"])
           && !string.IsNullOrWhiteSpace(_configuration["AzureCommunicationEmailSenderAddress"]);

    public async Task SendAsync(
        string recipientEmail,
        string recipientName,
        string subject,
        string message,
        CancellationToken cancellationToken = default)
    {
        var connectionString = Require("AzureCommunicationEmailConnectionString");
        var senderAddress = Require("AzureCommunicationEmailSenderAddress");
        var client = new EmailClient(connectionString);
        var content = new EmailContent(subject)
        {
            PlainText = message
        };
        var recipients = new EmailRecipients(
            [new EmailAddress(recipientEmail, recipientName)]);
        var emailMessage = new EmailMessage(senderAddress, recipients, content);

        await client.SendAsync(WaitUntil.Completed, emailMessage, cancellationToken);
    }

    private string Require(string key)
        => _configuration[key] is { Length: > 0 } value
            ? value
            : throw new InvalidOperationException($"Missing {key} configuration.");
}
