using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NotificationService.Infrastructure.Persistence;

namespace NotificationService.UnitTests.Infrastructure;

public sealed class NotificationMigrationTests
{
    [Fact]
    public void MigrationScript_OwnsNotificationSchemaAndUsesSeparateHistory()
    {
        var options = new DbContextOptionsBuilder<NotificationDbContext>()
            .UseSqlServer(
                "Server=(localdb)\\MSSQLLocalDB;Database=MigrationScriptOnly;Trusted_Connection=True",
                sqlOptions => sqlOptions.MigrationsHistoryTable(
                    "__NotificationServiceMigrationsHistory",
                    "notification"))
            .Options;

        using var db = new NotificationDbContext(options);
        var script = db.Database.GetService<IMigrator>().GenerateScript();

        Assert.Contains("CREATE SCHEMA [notification]", script);
        Assert.Contains("[notification].[Notification]", script);
        Assert.Contains("__NotificationServiceMigrationsHistory", script);
        Assert.Contains("DROP CONSTRAINT [FK_Notification_User_UserID]", script);
    }
}
