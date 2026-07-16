using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PolicyPlatform.Domain.Assistance;

namespace PolicyPlatform.Infrastructure.Persistence.Configurations;

public sealed class AssistanceReportEventConfiguration : IEntityTypeConfiguration<AssistanceReportEvent>
{
    public void Configure(EntityTypeBuilder<AssistanceReportEvent> builder)
    {
        builder.ToTable("assistance_report_event");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.ReportId).HasColumnName("report_id").IsRequired();
        builder.Property(e => e.EventType).HasColumnName("event_type")
            .HasConversion<string>().HasMaxLength(48).IsRequired();
        builder.Property(e => e.Payload).HasColumnName("payload").IsRequired();
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.HasOne<AssistanceReport>().WithMany().HasForeignKey(e => e.ReportId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(e => e.ReportId);
    }
}
