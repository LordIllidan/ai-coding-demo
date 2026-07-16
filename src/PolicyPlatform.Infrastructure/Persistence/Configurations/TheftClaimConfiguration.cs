using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PolicyPlatform.Domain.Claims;

namespace PolicyPlatform.Infrastructure.Persistence.Configurations;

public sealed class TheftClaimConfiguration : IEntityTypeConfiguration<TheftClaim>
{
    public void Configure(EntityTypeBuilder<TheftClaim> builder)
    {
        builder.ToTable("theft_claim");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id");

        builder.Property(c => c.PolicyId).HasColumnName("policy_id").IsRequired();

        builder.Property(c => c.PoliceReportNumber)
            .HasConversion(number => number.Value, value => new PoliceReportNumber(value))
            .HasColumnName("police_report_number")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(c => c.Status).HasColumnName("status").HasMaxLength(20).IsRequired();
        builder.Property(c => c.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(c => c.UpdatedAt).HasColumnName("updated_at").IsRequired();
    }
}
