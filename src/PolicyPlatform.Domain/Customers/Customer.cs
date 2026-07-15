using System.Text.RegularExpressions;
using PolicyPlatform.Domain.Common;

namespace PolicyPlatform.Domain.Customers;

public sealed partial class Customer : Entity
{
    public string FullName { get; }
    public string Email { get; }

    private Customer(Guid id, string fullName, string email) : base(id)
    {
        FullName = fullName;
        Email = email;
    }

    public static Customer Create(Guid id, string fullName, string email)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new DomainException("Customer full name is required.");
        }

        if (!EmailPattern().IsMatch(email))
        {
            throw new DomainException($"'{email}' is not a valid email address.");
        }

        return new Customer(id, fullName.Trim(), email.Trim());
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailPattern();
}
