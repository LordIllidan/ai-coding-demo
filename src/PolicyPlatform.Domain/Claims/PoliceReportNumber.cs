using PolicyPlatform.Domain.Common;

namespace PolicyPlatform.Domain.Claims;

public readonly record struct PoliceReportNumber
{
    public string Value { get; }

    public PoliceReportNumber(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException(
                "A theft claim cannot be registered without a police report number (numer zgłoszenia Policji).");
        }

        Value = value.Trim();
    }

    public override string ToString() => Value;
}
