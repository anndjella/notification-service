using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NotificationService.Application.Abstractions;
using NotificationService.Infrastructure.Persistence;

namespace NotificationService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddNotificationInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<NotificationDbContext>(options =>
            options.UseSqlServer(
                connectionString,
                sqlOptions => sqlOptions.MigrationsHistoryTable(
                    "__NotificationServiceMigrationsHistory",
                    "notification")));
        services.AddScoped<INotificationRepository, NotificationRepository>();

        return services;
    }
}
