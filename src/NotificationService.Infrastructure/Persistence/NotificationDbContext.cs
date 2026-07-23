using Microsoft.EntityFrameworkCore;
using NotificationService.Domain.Notifications;

namespace NotificationService.Infrastructure.Persistence;

public sealed class NotificationDbContext : DbContext
{
    public NotificationDbContext(DbContextOptions<NotificationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.ToTable("Notification", "notification");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("ID").ValueGeneratedNever();
            entity.Property(x => x.UserId).HasColumnName("UserID");
            entity.Property(x => x.Title).HasMaxLength(150).IsRequired();
            entity.Property(x => x.RecipientEmail).HasMaxLength(254).IsRequired();
            entity.Property(x => x.Message).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.DeduplicationKey).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Type).HasConversion<int>();
            entity.Property(x => x.DeliveryStatus).HasConversion<int>();
            entity.Property(x => x.LastDeliveryError).HasMaxLength(1000);
            entity.HasIndex(x => x.DeduplicationKey).IsUnique();
            entity.HasIndex(x => new { x.UserId, x.ReadAtUtc, x.CreatedAtUtc });
        });

    }
}
