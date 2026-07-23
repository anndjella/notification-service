using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NotificationService.Application.Abstractions;

namespace NotificationService.Functions.Email;

public sealed class ConfiguredEmailSender : IEmailSender
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ConfiguredEmailSender> _logger;

    public ConfiguredEmailSender(
        IConfiguration configuration,
        ILogger<ConfiguredEmailSender> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public bool IsEnabled
        => bool.TryParse(_configuration["SmtpEnabled"], out var enabled) && enabled;

    public async Task SendAsync(
        string recipientEmail,
        string recipientName,
        string subject,
        string message,
        CancellationToken cancellationToken = default)
    {
        if (!IsEnabled)
        {
            _logger.LogInformation(
                "Email delivery is disabled. Would send to {RecipientName} <{RecipientEmail}>: {Subject} - {Message}",
                recipientName,
                recipientEmail,
                subject,
                message);
            return;
        }

        var host = Require("SmtpHost");
        var fromAddress = Require("SmtpFromAddress");
        var fromName = _configuration["SmtpFromName"] ?? "Student Exam System";
        var username = _configuration["SmtpUsername"];
        var password = _configuration["SmtpPassword"];
        var port = int.TryParse(_configuration["SmtpPort"], out var configuredPort)
            ? configuredPort
            : 587;

        using var mail = new MailMessage
        {
            From = new MailAddress(fromAddress, fromName),
            Subject = subject,
            Body = message,
            IsBodyHtml = false
        };
        mail.To.Add(new MailAddress(recipientEmail, recipientName));

        using var client = new SmtpClient(host, port)
        {
            EnableSsl = !bool.TryParse(_configuration["SmtpEnableSsl"], out var ssl) || ssl
        };

        if (!string.IsNullOrWhiteSpace(username))
            client.Credentials = new NetworkCredential(username, password);

        cancellationToken.ThrowIfCancellationRequested();
        await client.SendMailAsync(mail, cancellationToken);
    }

    private string Require(string key)
        => _configuration[key] is { Length: > 0 } value
            ? value
            : throw new InvalidOperationException($"Missing {key} configuration while SMTP is enabled.");
}
