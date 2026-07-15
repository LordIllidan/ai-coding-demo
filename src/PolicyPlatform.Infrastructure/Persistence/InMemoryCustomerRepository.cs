using System.Collections.Concurrent;
using PolicyPlatform.Application.Abstractions;
using PolicyPlatform.Domain.Customers;

namespace PolicyPlatform.Infrastructure.Persistence;

public sealed class InMemoryCustomerRepository : ICustomerRepository
{
    private readonly ConcurrentDictionary<Guid, Customer> _customers = new();

    public Task<Customer?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(_customers.GetValueOrDefault(id));

    public Task AddAsync(Customer customer, CancellationToken ct = default)
    {
        _customers[customer.Id] = customer;
        return Task.CompletedTask;
    }
}
