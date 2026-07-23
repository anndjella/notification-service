using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NotificationService.Application.RegistrationReminders;
using NotificationService.Application.Abstractions;
using NotificationService.Application.MissingExamResults;
using NotificationService.Application.Notifications;
using NotificationService.Functions.Clients;
using NotificationService.Functions.Email;
using NotificationService.Functions.Http;
using NotificationService.Infrastructure;
using NotificationService.Infrastructure.Persistence;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        var connectionString = context.Configuration["SqlConnectionString"];
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Missing SqlConnectionString. Add the shared StudentExamSystem database connection string to local.settings.json.");
        }

        services.AddNotificationInfrastructure(connectionString);
        var studentExamSystemUrl = context.Configuration["StudentExamSystemBaseUrl"]
            ?? throw new InvalidOperationException("Missing StudentExamSystemBaseUrl configuration.");
        var internalApiKey = context.Configuration["InternalApiKey"]
            ?? throw new InvalidOperationException("Missing InternalApiKey configuration.");

        services.AddTransient<InternalServiceResilienceHandler>();
        services.AddHttpClient<StudentExamSystemClient>(client =>
            {
                client.BaseAddress = new Uri(studentExamSystemUrl);
                client.Timeout = Timeout.InfiniteTimeSpan;
                client.DefaultRequestHeaders.Add("X-Internal-Api-Key", internalApiKey);
            })
            .ConfigurePrimaryHttpMessageHandler(() =>
            {
                var handler = new HttpClientHandler();
                if (bool.TryParse(
                        context.Configuration["AllowUntrustedDevelopmentCertificate"],
                        out var allowUntrusted) && allowUntrusted)
                {
                    handler.ServerCertificateCustomValidationCallback =
                        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                }

                return handler;
            })
            .AddHttpMessageHandler<InternalServiceResilienceHandler>();
        services.AddScoped<IRegistrationReminderCandidateReader>(services =>
            services.GetRequiredService<StudentExamSystemClient>());
        services.AddScoped<IMissingExamResultCandidateReader>(services =>
            services.GetRequiredService<StudentExamSystemClient>());
        services.AddScoped<ConfiguredEmailSender>();
        services.AddScoped<AzureCommunicationEmailSender>();
        services.AddScoped<IEmailSender>(services =>
        {
            var provider = context.Configuration["EmailProvider"] ?? "Log";
            return provider.Equals("AzureCommunicationServices", StringComparison.OrdinalIgnoreCase)
                ? services.GetRequiredService<AzureCommunicationEmailSender>()
                : services.GetRequiredService<ConfiguredEmailSender>();
        });
        services.AddScoped<NotificationDispatcher>();
        services.AddScoped<NotificationQueryService>();
        services.AddScoped<EmailRetryService>();
        services.AddScoped<RegistrationReminderService>();
        services.AddScoped<MissingExamResultReminderService>();
        services.AddSingleton<InternalApiKeyAuthorizer>();
        services.AddSingleton(TimeProvider.System);
    })
    .Build();

using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
    db.Database.Migrate();
}

host.Run();
