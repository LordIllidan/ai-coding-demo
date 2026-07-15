using System.Text.RegularExpressions;
using PolicyPlatform.Domain.Common;

namespace PolicyPlatform.Domain.Policies;

public readonly partial record struct PolicyNumber
{
    public string Value { get; }

    public PolicyNumber(string value)
    {
        if (!PolicyNumberPattern().IsMatch(value))
        {
            throw new DomainException($"'{value}' is not a valid policy number (expected POL-YYYY-NNNNNN).");
        }

        Value = value;
    }

    public override string ToString() => Value;

    [GeneratedRegex(@"^POL-\d{4}-\d{6}$")]
    private static partial Regex PolicyNumberPattern();
}
