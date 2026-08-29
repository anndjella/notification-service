extern alias AzureIdentity;
using Azure;
using Azure.Communication.Email;
using Microsoft.Extensions.Configuration;
using NotificationService.Application.Abstractions;
using DefaultAzureCredential = AzureIdentity::Azure.Identity.DefaultAzureCredential;

namespace NotificationService.Functions.Email;

public sealed class AzureCommunicationEmailSender : IEmailSender
{
    private readonly IConfiguration _configuration;

    public AzureCommunicationEmailSender(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public bool IsEnabled
        => !string.IsNullOrWhiteSpace(_configuration["AzureCommunicationEmailSenderAddress"])
           && (!string.IsNullOrWhiteSpace(_configuration["AzureCommunicationEmailEndpoint"])
               || !string.IsNullOrWhiteSpace(_configuration["AzureCommunicationEmailConnectionString"]));

    public async Task SendAsync(
        string recipientEmail,
        string recipientName,
        string subject,
        string message,
        CancellationToken cancellationToken = default)
    {
        var senderAddress = Require("AzureCommunicationEmailSenderAddress");
        var client = CreateClient();
        var content = new EmailContent(subject)
        {
            PlainText = message
        };
        var recipients = new EmailRecipients(
            [new EmailAddress(recipientEmail, recipientName)]);
        var emailMessage = new EmailMessage(senderAddress, recipients, content);

        await client.SendAsync(WaitUntil.Completed, emailMessage, cancellationToken);
    }

    private EmailClient CreateClient()
    {
        // Prefer managed identity / RBAC (an endpoint URI) and fall back to a connection
        // string only when no endpoint is configured, so local development stays practical.
        var endpoint = _configuration["AzureCommunicationEmailEndpoint"];
        if (!string.IsNullOrWhiteSpace(endpoint))
            return new EmailClient(new Uri(endpoint), new DefaultAzureCredential());

        return new EmailClient(Require("AzureCommunicationEmailConnectionString"));
    }

    private string Require(string key)
        => _configuration[key] is { Length: > 0 } value
            ? value
            : throw new InvalidOperationException($"Missing {key} configuration.");
}
