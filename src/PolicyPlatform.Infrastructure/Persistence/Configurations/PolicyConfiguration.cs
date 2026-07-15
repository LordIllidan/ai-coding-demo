using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PolicyPlatform.Domain.Policies;

namespace PolicyPlatform.Infrastructure.Persistence.Configurations;

public sealed class PolicyConfiguration : IEntityTypeConfiguration<Policy>
{
    public void Configure(EntityTypeBuilder<Policy> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Number)
            .HasConversion(number => number.Value, value => new PolicyNumber(value))
            .HasMaxLength(20)
            .IsRequired();
        builder.HasIndex(p => p.Number).IsUnique();

        builder.Property(p => p.CustomerId).IsRequired();
        builder.Property(p => p.EffectiveDate).IsRequired();
        builder.Property(p => p.ExpiryDate).IsRequired();
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.Ignore(p => p.TotalPremium);

        builder.OwnsMany(p => p.Coverages, (OwnedNavigationBuilder<Policy, Coverage> coverage) =>
        {
            coverage.WithOwner().HasForeignKey("PolicyId");
            coverage.Property<int>("Id");
            coverage.HasKey("Id");

            coverage.Property(c => c.Type).HasConversion<string>().HasMaxLength(10).IsRequired();

            coverage.OwnsOne(c => c.SumInsured, (OwnedNavigationBuilder<Coverage, Money> sumInsured) =>
            {
                sumInsured.Property(m => m.Amount).HasColumnName("SumInsuredAmount").HasPrecision(18, 2);
                sumInsured.Property(m => m.Currency).HasColumnName("SumInsuredCurrency").HasMaxLength(3);
            });
            coverage.OwnsOne(c => c.Premium, (OwnedNavigationBuilder<Coverage, Money> premium) =>
            {
                premium.Property(m => m.Amount).HasColumnName("PremiumAmount").HasPrecision(18, 2);
                premium.Property(m => m.Currency).HasColumnName("PremiumCurrency").HasMaxLength(3);
            });
        });

        builder.Navigation(p => p.Coverages)
            .HasField("_coverages")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
