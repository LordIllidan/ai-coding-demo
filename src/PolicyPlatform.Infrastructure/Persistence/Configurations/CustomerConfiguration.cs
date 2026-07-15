using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PolicyPlatform.Domain.Customers;

namespace PolicyPlatform.Infrastructure.Persistence.Configurations;

public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.FullName).HasMaxLength(200).IsRequired();
        builder.Property(c => c.Email).HasMaxLength(320).IsRequired();
        builder.HasIndex(c => c.Email).IsUnique();
    }
}
