namespace NotificationService.Infrastructure.Persistence.ReadModels;

internal sealed class SubjectReadModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
