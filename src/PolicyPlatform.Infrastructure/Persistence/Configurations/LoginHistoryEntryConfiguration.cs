using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PolicyPlatform.Domain.LoginHistory;

namespace PolicyPlatform.Infrastructure.Persistence.Configurations;

/// <summary>EF Core mapping for <see cref="LoginHistoryEntry"/>, including the mandatory (user_id, occurred_at DESC) index.</summary>
public sealed class LoginHistoryEntryConfiguration : IEntityTypeConfiguration<LoginHistoryEntry>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<LoginHistoryEntry> builder)
    {
        builder.ToTable("LoginHistoryEntries");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.UserId).IsRequired();
        builder.Property(e => e.OccurredAt).IsRequired();

        builder.Property(e => e.DeviceLabel).HasMaxLength(200);
        builder.Property(e => e.DeviceType).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(e => e.OsName).HasMaxLength(100);
        builder.Property(e => e.OsVersion).HasMaxLength(50);
        builder.Property(e => e.IpAddress).HasMaxLength(45);

        builder.Property(e => e.CreatedAt).IsRequired().HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(e => new { e.UserId, e.OccurredAt })
            .IsDescending(false, true)
            .HasDatabaseName("IX_LoginHistoryEntries_UserId_OccurredAt");
    }
}
