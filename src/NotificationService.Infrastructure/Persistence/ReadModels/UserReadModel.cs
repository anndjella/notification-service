namespace NotificationService.Infrastructure.Persistence.ReadModels;

internal sealed class UserReadModel
{
    public int Id { get; set; }
    public int PersonId { get; set; }
    public byte Role { get; set; }
    public bool IsActive { get; set; }
}
