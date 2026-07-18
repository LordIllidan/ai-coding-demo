using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PolicyPlatform.Domain.Claims;
using PolicyPlatform.Domain.Policies;

namespace PolicyPlatform.Infrastructure.Persistence.Configurations;

/// <summary>EF Core mapping for <see cref="ClaimPayout"/>: owns <see cref="Money"/> as
/// AmountGross/CurrencyCode columns, and indexes (CustomerId, Status, PaidAt) to support the
/// "last paid payout for customer" query.</summary>
public sealed class ClaimPayoutConfiguration : IEntityTypeConfiguration<ClaimPayout>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<ClaimPayout> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.ClaimId).IsRequired();
        builder.Property(p => p.ClaimNumber).HasMaxLength(50).IsRequired();
        builder.Property(p => p.CustomerId).IsRequired();
        builder.Property(p => p.PaidAt).IsRequired();
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.HasIndex(p => new { p.CustomerId, p.Status, p.PaidAt });

        builder.OwnsOne(p => p.Amount, (OwnedNavigationBuilder<ClaimPayout, Money> amount) =>
        {
            amount.Property(m => m.Amount).HasColumnName("AmountGross").HasPrecision(18, 2);
            amount.Property(m => m.Currency).HasColumnName("CurrencyCode").HasMaxLength(3);
        });
    }
}
