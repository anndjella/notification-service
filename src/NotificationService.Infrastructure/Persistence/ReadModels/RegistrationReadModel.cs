namespace NotificationService.Infrastructure.Persistence.ReadModels;

internal sealed class RegistrationReadModel
{
    public int StudentId { get; set; }
    public int SubjectId { get; set; }
    public int TermId { get; set; }
    public bool IsActive { get; set; }
}
