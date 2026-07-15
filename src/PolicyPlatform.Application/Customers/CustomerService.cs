using PolicyPlatform.Application.Abstractions;
using PolicyPlatform.Domain.Customers;

namespace PolicyPlatform.Application.Customers;

public sealed record CreateCustomerRequest(string FullName, string Email);

public sealed record CustomerDto(Guid Id, string FullName, string Email)
{
    public static CustomerDto FromDomain(Customer customer) => new(customer.Id, customer.FullName, customer.Email);
}

public sealed class CustomerService
{
    private readonly ICustomerRepository _customers;

    public CustomerService(ICustomerRepository customers) => _customers = customers;

    public async Task<CustomerDto> CreateCustomerAsync(CreateCustomerRequest request, CancellationToken ct = default)
    {
        var customer = Customer.Create(Guid.NewGuid(), request.FullName, request.Email);
        await _customers.AddAsync(customer, ct);
        return CustomerDto.FromDomain(customer);
    }
}
