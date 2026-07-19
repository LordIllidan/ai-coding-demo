using System.Text.RegularExpressions;
using PolicyPlatform.Domain.Common;

namespace PolicyPlatform.Domain.Customers;

/// <summary>Polish national identification number. Validates format and the official
/// checksum so a merely well-formed-looking but bogus number is rejected at the domain
/// boundary rather than being silently compared/stored.</summary>
public readonly partial record struct Pesel
{
    private static readonly int[] Weights = [1, 3, 7, 9, 1, 3, 7, 9, 1, 3];

    public string Value { get; }

    public Pesel(string value)
    {
        if (!PeselPattern().IsMatch(value))
        {
            throw new DomainException("PESEL must be exactly 11 digits.");
        }

        if (!HasValidChecksum(value))
        {
            throw new DomainException("PESEL checksum is invalid.");
        }

        Value = value;
    }

    public override string ToString() => Value;

    private static bool HasValidChecksum(string pesel)
    {
        var sum = 0;
        for (var i = 0; i < Weights.Length; i++)
        {
            sum += (pesel[i] - '0') * Weights[i];
        }

        var checkDigit = (10 - (sum % 10)) % 10;
        return checkDigit == pesel[10] - '0';
    }

    [GeneratedRegex(@"^\d{11}$")]
    private static partial Regex PeselPattern();
}
