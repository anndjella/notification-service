# Notification Service

Serverless notification service for the
[Student Exam System](https://github.com/anndjella/student-exam-system), implemented as an
Azure Functions app (.NET 8, isolated worker).

It sends two reminder e-mails:

- **Exam-registration deadline** — to students, the day before a term's registration window closes, if they are enrolled in a subject they have not passed and have not registered for.
- **Missing exam result** — to teachers, for each exam still unsigned 30 days after its term ends (whether or not a grade was entered — only the teacher's sign-off clears it).

A timer also retries e-mails that previously failed.

---

# Architecture

Timer functions ask the API for candidates and put one message per candidate on an Azure
Service Bus queue. A queue-triggered function then records the notification and sends the
e-mail. Repeated scheduler runs are safe because a unique index on the deduplication key
prevents duplicate notifications.

The Service Bus queue and the shared database are accessed with managed identity; e-mail is
sent through Azure Communication Services.

---

# Projects

- NotificationService.Domain
- NotificationService.Application
- NotificationService.Infrastructure
- NotificationService.Functions
- tests/NotificationService.UnitTests

---

# Running locally

Copy `src/NotificationService.Functions/local.settings.json.example` to `local.settings.json`,
fill in the values, and run `func start`. Requires the Student Exam System API running
locally and access to a Service Bus namespace.

---

# Deployment

GitHub Actions builds, tests and deploys to Azure (Flex Consumption). All infrastructure is
defined as Bicep in the [main repository](https://github.com/anndjella/student-exam-system)
under `infra/`.

Every push to `master` also runs a coverage-instrumented test pass (report published to the
run summary and as an artifact) and, after deploy, a smoke test against `/api/health/live` and
`/api/health/ready`. A failed deploy or smoke test automatically redeploys the last known-good
artifact; [rollback.yml](.github/workflows/rollback.yml) is the manual equivalent for a bad
release that only gets noticed later. CodeQL, gitleaks, a vulnerable-package check and SonarCloud run weekly in the background.
