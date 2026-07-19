using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PolicyPlatform.Domain.Auth;

namespace PolicyPlatform.Infrastructure.Persistence.Configurations;

public sealed class LoginHistoryEntryConfiguration : IEntityTypeConfiguration<LoginHistoryEntry>
{
    public void Configure(EntityTypeBuilder<LoginHistoryEntry> builder)
    {
        builder.ToTable("login_history_entries");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");

        builder.Property(e => e.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(e => e.OccurredAt).HasColumnName("occurred_at").IsRequired();
        builder.Property(e => e.DeviceLabel).HasColumnName("device_label").HasMaxLength(200);
        builder.Property(e => e.DeviceType).HasColumnName("device_type").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(e => e.OsName).HasColumnName("os_name").HasMaxLength(100);
        builder.Property(e => e.OsVersion).HasColumnName("os_version").HasMaxLength(50);
        builder.Property(e => e.SessionId).HasColumnName("session_id");
        builder.Property(e => e.IpAddress).HasColumnName("ip_address").HasMaxLength(45);
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.HasIndex(e => new { e.UserId, e.OccurredAt })
            .HasDatabaseName("IX_login_history_entries_user_id_occurred_at")
            .IsDescending(false, true);
    }
}
