namespace NotificationService.Infrastructure.Persistence.ReadModels;

internal sealed class TermReadModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateOnly RegistrationEndDate { get; set; }
}
