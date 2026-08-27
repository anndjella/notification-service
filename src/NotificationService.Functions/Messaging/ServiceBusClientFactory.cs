extern alias AzureIdentity;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using DefaultAzureCredential = AzureIdentity::Azure.Identity.DefaultAzureCredential;

namespace NotificationService.Functions.Messaging;

/// <summary>
/// Builds the <see cref="ServiceBusClient"/> used by the publisher.
/// Prefers managed identity / RBAC (a fully qualified namespace) and falls back to a
/// connection string only when no namespace is configured, which keeps local development
/// practical without checking a secret into source control.
/// </summary>
public static class ServiceBusClientFactory
{
    public static ServiceBusClient Create(IConfiguration configuration)
    {
        // The Functions Service Bus trigger reads "ServiceBusConnection__fullyQualifiedNamespace"
        // for identity-based connections; configuration exposes the "__" segment as ":".
        var fullyQualifiedNamespace =
            configuration["ServiceBusConnection:fullyQualifiedNamespace"]
            ?? configuration["ServiceBusFullyQualifiedNamespace"];

        if (!string.IsNullOrWhiteSpace(fullyQualifiedNamespace))
        {
            return new ServiceBusClient(
                fullyQualifiedNamespace,
                new DefaultAzureCredential());
        }

        var connectionString = configuration["ServiceBusConnection"];
        if (!string.IsNullOrWhiteSpace(connectionString))
            return new ServiceBusClient(connectionString);

        throw new InvalidOperationException(
            "Missing Service Bus configuration. Set 'ServiceBusConnection__fullyQualifiedNamespace' " +
            "(managed identity, recommended) or 'ServiceBusConnection' (connection string, local only).");
    }
}
