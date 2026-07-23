using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace NotificationService.Infrastructure.Persistence.Migrations;

[DbContext(typeof(NotificationDbContext))]
[Migration("20260721000000_TakeOwnershipOfNotificationSchema")]
public sealed class TakeOwnershipOfNotificationSchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            IF SCHEMA_ID(N'notification') IS NULL
                EXEC(N'CREATE SCHEMA [notification]');

            IF OBJECT_ID(N'[notification].[Notification]', N'U') IS NULL
            BEGIN
                CREATE TABLE [notification].[Notification]
                (
                    [ID] uniqueidentifier NOT NULL,
                    [UserID] int NOT NULL,
                    [RecipientEmail] nvarchar(254) NOT NULL,
                    [Type] int NOT NULL,
                    [Title] nvarchar(150) NOT NULL,
                    [Message] nvarchar(1000) NOT NULL,
                    [DeduplicationKey] nvarchar(200) NOT NULL,
                    [CreatedAtUtc] datetime2 NOT NULL,
                    [ReadAtUtc] datetime2 NULL,
                    [EmailSentAtUtc] datetime2 NULL,
                    [DeliveryStatus] int NOT NULL,
                    [DeliveryAttemptCount] int NOT NULL,
                    [LastDeliveryError] nvarchar(1000) NULL,
                    CONSTRAINT [PK_Notification] PRIMARY KEY ([ID])
                );
            END;

            IF COL_LENGTH(N'notification.Notification', N'RecipientEmail') IS NULL
            BEGIN
                ALTER TABLE [notification].[Notification]
                    ADD [RecipientEmail] nvarchar(254) NOT NULL
                        CONSTRAINT [DF_Notification_RecipientEmail] DEFAULT N'';
                ALTER TABLE [notification].[Notification]
                    DROP CONSTRAINT [DF_Notification_RecipientEmail];
            END;

            IF EXISTS
            (
                SELECT 1
                FROM sys.foreign_keys
                WHERE [name] = N'FK_Notification_User_UserID'
                  AND [parent_object_id] = OBJECT_ID(N'[notification].[Notification]')
            )
                ALTER TABLE [notification].[Notification]
                    DROP CONSTRAINT [FK_Notification_User_UserID];

            IF NOT EXISTS
            (
                SELECT 1
                FROM sys.indexes
                WHERE [name] = N'IX_Notification_DeduplicationKey'
                  AND [object_id] = OBJECT_ID(N'[notification].[Notification]')
            )
                CREATE UNIQUE INDEX [IX_Notification_DeduplicationKey]
                    ON [notification].[Notification] ([DeduplicationKey]);

            IF NOT EXISTS
            (
                SELECT 1
                FROM sys.indexes
                WHERE [name] = N'IX_Notification_UserID_ReadAtUtc_CreatedAtUtc'
                  AND [object_id] = OBJECT_ID(N'[notification].[Notification]')
            )
                CREATE INDEX [IX_Notification_UserID_ReadAtUtc_CreatedAtUtc]
                    ON [notification].[Notification] ([UserID], [ReadAtUtc], [CreatedAtUtc]);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // This is an ownership-transfer baseline. Rolling it back must not delete
        // a notification table that may predate this service's migration history.
    }
}
