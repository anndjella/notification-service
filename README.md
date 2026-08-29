# Notification Service

An event-driven, serverless notification service for the
[Student Exam System](https://github.com/anndjella/student-exam-system). It runs as an
**Azure Functions** app (.NET 8 isolated worker) and sends two kinds of reminder e-mails:

| Reminder | Sent to | When |
|---|---|---|
| Exam-registration deadline | students | the day before a term's registration window closes, if they are enrolled in a subject they have not passed and have not registered for |
| Missing exam result | teachers | when an exam has had no grade entered and has not been signed 30+ days after the exam date |

Plus an e-mail retry sweep for deliveries that previously failed.

## Architecture

```
 Timer (daily)                         Service Bus                    Service Bus trigger
 ────────────────                    ───────────────                 ─────────────────────
 ScheduleRegistrationReminders   ──▶  notification-delivery   ──▶    ProcessNotificationMessage
 ScheduleMissingExamResultRemind      (queue)                          │
   │  asks the API for candidates                                      ├─ insert Notification row
   │  (HTTP + X-Internal-Api-Key)                                      │   (unique index on DeduplicationKey
   └─ enqueues one small message per candidate                        │    → duplicates are ignored)
                                                                      └─ send e-mail (Azure Communication Services)

 RetryFailedEmails (timer) ── re-sends notifications whose e-mail previously failed (bounded by EmailMaxDeliveryAttempts)
```

Splitting "decide who to notify" (timer) from "notify them" (queue consumer) gives crash
resilience, automatic per-message retries (`maxDeliveryCount`, then dead-letter), and
back-pressure. Duplicate e-mails are prevented by a unique index on `DeduplicationKey` in
the `notification` database schema, so repeated scheduler runs are safe.

The queue and the database are reached with **managed identity** in Azure (no connection
strings): `ServiceBusConnection__fullyQualifiedNamespace` + the *Azure Service Bus Data
Sender / Receiver* roles.

## Projects

| Project | Contents |
|---|---|
| `NotificationService.Domain` | `Notification` entity, delivery status, notification type |
| `NotificationService.Application` | reminder services, dispatcher, message contracts, abstractions |
| `NotificationService.Infrastructure` | EF Core `NotificationDbContext`, repository, migrations (`notification` schema) |
| `NotificationService.Functions` | the Functions host: triggers, HTTP endpoints, Service Bus publisher, e-mail senders |
| `tests/NotificationService.UnitTests` | xUnit tests (domain, application, migration, persistence) |

## Running locally

**Prerequisites**

- .NET 8 SDK
- [Azure Functions Core Tools v4](https://learn.microsoft.com/azure/azure-functions/functions-run-local)
- [Azurite](https://learn.microsoft.com/azure/storage/common/storage-use-azurite) (local storage emulator)
- the Student Exam System **API running** (default `https://localhost:7008`) with a seeded database
- a real Azure **Service Bus namespace** with a `notification-delivery` queue, and
  `az login` as a principal that has *Azure Service Bus Data Owner* on it
  (the local emulator does not cover Service Bus)

**Steps**

```bash
cd src/NotificationService.Functions
cp local.settings.json.example local.settings.json
# edit local.settings.json:
#   - InternalApiKey            -> the same value as the API's ServiceAuthentication:ApiKey
#   - ServiceBusConnection__fullyQualifiedNamespace -> <your-namespace>.servicebus.windows.net
func start
```

`local.settings.json` is git-ignored and never deployed.

### Configuration

| Setting | Purpose | Local default |
|---|---|---|
| `SqlConnectionString` | shared Student Exam System database | LocalDB `StudentExamDB` |
| `StudentExamSystemBaseUrl` | API base URL for candidate lookups | `https://localhost:7008/` |
| `InternalApiKey` | shared secret sent as `X-Internal-Api-Key` | — |
| `ServiceBusConnection__fullyQualifiedNamespace` | Service Bus namespace (managed identity) | — |
| `NotificationQueueName` | queue name | `notification-delivery` |
| `EmailProvider` | `Log` (console) \| `AzureCommunicationServices` \| `Smtp` | `Log` |
| `AzureCommunicationEmailConnectionString` / `AzureCommunicationEmailSenderAddress` | ACS e-mail (when provider = `AzureCommunicationServices`) | — |
| `ReminderTimeZone` | IANA time zone for the schedules | `Europe/Belgrade` |
| `MissingExamResultReminderDays` | grace period before the teacher reminder | `30` |
| `RegistrationReminderSchedule` / `MissingExamResultReminderSchedule` / `EmailRetrySchedule` | NCRONTAB timers | every few minutes locally |
| `EmailMaxDeliveryAttempts` / `EmailRetryBatchSize` | retry sweep bounds | `3` / `50` |

For SMTP set `EmailProvider=Smtp` and add `SmtpEnabled=true`, `SmtpHost`, `SmtpPort`,
`SmtpFromAddress`, `SmtpUsername`, `SmtpPassword`.

## Tests

```bash
dotnet test
```

SQLite in-memory is used for persistence/migration tests; the application tests use test
doubles for the API client and the publisher.

## Deployment

GitHub Actions (`.github/workflows/ci.yml`) builds, tests, and on `master` deploys to the
Azure **Flex Consumption** function app. All infrastructure (function app, Service Bus,
Key Vault, roles, App Insights) is defined as Bicep in the
[main repository](https://github.com/anndjella/student-exam-system) under `infra/`.
