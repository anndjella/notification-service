using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace NotificationService.Infrastructure.Persistence.Migrations;

[DbContext(typeof(NotificationDbContext))]
public sealed class NotificationDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
#pragma warning disable 612, 618
        modelBuilder
            .HasAnnotation("ProductVersion", "9.0.9")
            .HasAnnotation("Relational:MaxIdentifierLength", 128);

        SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

        modelBuilder.Entity("NotificationService.Domain.Notifications.Notification", entity =>
        {
            entity.Property<Guid>("Id")
                .HasColumnName("ID")
                .HasColumnType("uniqueidentifier");

            entity.Property<DateTime>("CreatedAtUtc").HasColumnType("datetime2");
            entity.Property<string>("DeduplicationKey").IsRequired().HasMaxLength(200).HasColumnType("nvarchar(200)");
            entity.Property<int>("DeliveryAttemptCount").HasColumnType("int");
            entity.Property<int>("DeliveryStatus").HasColumnType("int");
            entity.Property<DateTime?>("EmailSentAtUtc").HasColumnType("datetime2");
            entity.Property<string>("LastDeliveryError").HasMaxLength(1000).HasColumnType("nvarchar(1000)");
            entity.Property<string>("Message").IsRequired().HasMaxLength(1000).HasColumnType("nvarchar(1000)");
            entity.Property<DateTime?>("ReadAtUtc").HasColumnType("datetime2");
            entity.Property<string>("RecipientEmail").IsRequired().HasMaxLength(254).HasColumnType("nvarchar(254)");
            entity.Property<string>("Title").IsRequired().HasMaxLength(150).HasColumnType("nvarchar(150)");
            entity.Property<int>("Type").HasColumnType("int");
            entity.Property<int>("UserId").HasColumnName("UserID").HasColumnType("int");

            entity.HasKey("Id");
            entity.HasIndex("DeduplicationKey").IsUnique();
            entity.HasIndex("UserId", "ReadAtUtc", "CreatedAtUtc");
            entity.ToTable("Notification", "notification");
        });
#pragma warning restore 612, 618
    }
}
