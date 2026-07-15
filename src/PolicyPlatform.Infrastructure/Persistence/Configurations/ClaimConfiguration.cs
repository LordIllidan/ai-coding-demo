using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PolicyPlatform.Domain.Claims;

namespace PolicyPlatform.Infrastructure.Persistence.Configurations;

public sealed class ClaimConfiguration : IEntityTypeConfiguration<Claim>
{
    public void Configure(EntityTypeBuilder<Claim> builder)
    {
        builder.ToTable("Claims");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.PolicyId).IsRequired();
        builder.Property(c => c.CustomerId).IsRequired();

        builder.Property(c => c.Channel)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(c => c.IncidentDate).IsRequired();
        builder.Property(c => c.Description).HasMaxLength(2000);
        builder.Property(c => c.CreatedAtUtc).IsRequired();

        builder.HasIndex(c => c.PolicyId);
        builder.HasIndex(c => c.CustomerId);
    }
}
