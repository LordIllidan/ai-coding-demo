using Microsoft.EntityFrameworkCore;
using PolicyPlatform.Application.Abstractions;
using PolicyPlatform.Domain.Customers;

namespace PolicyPlatform.Infrastructure.Persistence;

public sealed class EfCustomerRepository : ICustomerRepository
{
    private readonly PolicyPlatformDbContext _db;

    public EfCustomerRepository(PolicyPlatformDbContext db) => _db = db;

    public async Task<Customer?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.Customers.FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task AddAsync(Customer customer, CancellationToken ct = default)
    {
        await _db.Customers.AddAsync(customer, ct);
        await _db.SaveChangesAsync(ct);
    }
}
