namespace NotificationService.Infrastructure.Persistence.ReadModels;

internal sealed class EnrollmentReadModel
{
    public int StudentId { get; set; }
    public int SubjectId { get; set; }
    public bool IsPassed { get; set; }
}
