using NotificationService.Application.MissingExamResults;

namespace NotificationService.Application.Abstractions;

public interface IMissingExamResultCandidateReader
{
    Task<IReadOnlyList<MissingExamResultCandidate>> ListAsync(
        DateOnly examDate,
        CancellationToken cancellationToken = default);
}
