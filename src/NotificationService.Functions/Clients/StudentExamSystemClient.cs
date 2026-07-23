using System.Net.Http.Json;
using NotificationService.Application.Abstractions;
using NotificationService.Application.MissingExamResults;
using NotificationService.Application.RegistrationReminders;

namespace NotificationService.Functions.Clients;

public sealed class StudentExamSystemClient :
    IRegistrationReminderCandidateReader,
    IMissingExamResultCandidateReader
{
    private readonly HttpClient _httpClient;

    public StudentExamSystemClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<RegistrationReminderCandidate>> ListAsync(
        DateOnly registrationEndsOn,
        CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<List<RegistrationReminderCandidate>>(
            $"api/internal/notification-candidates/registration-deadline?registrationEndsOn={registrationEndsOn:yyyy-MM-dd}",
            cancellationToken) ?? [];
    }

    async Task<IReadOnlyList<MissingExamResultCandidate>> IMissingExamResultCandidateReader.ListAsync(
        DateOnly examDate,
        CancellationToken cancellationToken)
    {
        return await _httpClient.GetFromJsonAsync<List<MissingExamResultCandidate>>(
            $"api/internal/notification-candidates/missing-exam-results?examDate={examDate:yyyy-MM-dd}",
            cancellationToken) ?? [];
    }
}
