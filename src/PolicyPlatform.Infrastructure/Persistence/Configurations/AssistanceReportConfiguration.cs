using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PolicyPlatform.Domain.Assistance;

namespace PolicyPlatform.Infrastructure.Persistence.Configurations;

public sealed class AssistanceReportConfiguration : IEntityTypeConfiguration<AssistanceReport>
{
    public void Configure(EntityTypeBuilder<AssistanceReport> builder)
    {
        builder.ToTable("assistance_report");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id).HasColumnName("id");
        builder.Property(r => r.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(r => r.IdempotencyKey).HasColumnName("idempotency_key").IsRequired();
        builder.Property(r => r.IncidentType).HasColumnName("incident_type")
            .HasConversion<string>().HasMaxLength(32).IsRequired();

        builder.OwnsOne(r => r.Gps, gps =>
        {
            gps.Property(g => g.Latitude).HasColumnName("gps_latitude").HasPrecision(9, 6).IsRequired();
            gps.Property(g => g.Longitude).HasColumnName("gps_longitude").HasPrecision(9, 6).IsRequired();
            gps.Property(g => g.AccuracyMeters).HasColumnName("gps_accuracy_m").HasPrecision(8, 2);
        });
        builder.Navigation(r => r.Gps).IsRequired();

        builder.Property(r => r.OccurredAt).HasColumnName("occurred_at");
        builder.Property(r => r.Status).HasColumnName("status")
            .HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(r => r.PartnerDispatchStatus).HasColumnName("partner_dispatch_status")
            .HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(r => r.PartnerCaseId).HasColumnName("partner_case_id").HasMaxLength(64);
        builder.Property(r => r.PartnerDispatchAttempts).HasColumnName("partner_dispatch_attempts");
        builder.Property(r => r.NextPartnerDispatchAttemptAt).HasColumnName("next_partner_dispatch_attempt_at");
        builder.Property(r => r.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(r => r.UpdatedAt).HasColumnName("updated_at").IsRequired();

        builder.HasIndex(r => new { r.UserId, r.IdempotencyKey }).IsUnique();
    }
}
