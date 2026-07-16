using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PolicyPlatform.Domain.Claims;

namespace PolicyPlatform.Infrastructure.Persistence.Configurations;

/// <summary>EF Core mapping for <see cref="TheftClaim"/> to the <c>theft_claim</c> table
/// (AISDLC-51 contract columns).</summary>
public sealed class TheftClaimConfiguration : IEntityTypeConfiguration<TheftClaim>
{
    /// <summary>Configures column names, types, and constraints for <see cref="TheftClaim"/>.</summary>
    /// <param name="builder">Entity type builder supplied by EF Core.</param>
    public void Configure(EntityTypeBuilder<TheftClaim> builder)
    {
        builder.ToTable("theft_claim");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id");

        builder.Property(c => c.PolicyId).HasColumnName("policy_id").IsRequired();

        builder.Property(c => c.PoliceReportNumber)
            .HasConversion(number => number.Value, value => PoliceReportNumber.Create(value))
            .HasColumnName("police_report_number")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(c => c.Status)
            .HasConversion<string>()
            .HasColumnName("status")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(c => c.CreatedAt).HasColumnName("created_at");
        builder.Property(c => c.UpdatedAt).HasColumnName("updated_at");
    }
}
