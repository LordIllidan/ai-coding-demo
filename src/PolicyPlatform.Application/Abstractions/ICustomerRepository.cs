using PolicyPlatform.Domain.Customers;

namespace PolicyPlatform.Application.Abstractions;

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Customer customer, CancellationToken ct = default);
}
