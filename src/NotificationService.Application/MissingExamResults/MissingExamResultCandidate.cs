namespace NotificationService.Application.MissingExamResults;

public sealed record MissingExamResultCandidate(
    int UserId,
    int TeacherId,
    string Email,
    string RecipientName,
    int SubjectId,
    string SubjectName,
    int TermId,
    string TermName,
    DateOnly ExamDate,
    int MissingResultCount);
