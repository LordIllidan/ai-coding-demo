using System.Text.RegularExpressions;
using PolicyPlatform.Domain.Common;

namespace PolicyPlatform.Domain.Claims;

public readonly partial record struct PoliceReportNumber
{
    private const string RequiredMessage = "Numer zgłoszenia Policji jest wymagany i musi być poprawny.";

    public string Value { get; }

    public PoliceReportNumber(string? value)
    {
        var trimmed = (value ?? string.Empty).Trim();
        if (trimmed.Length == 0)
        {
            throw new FieldValidationException(
                "policeReportNumber", "POLICE_REPORT_NUMBER_REQUIRED", RequiredMessage);
        }

        var normalized = trimmed.ToUpperInvariant();
        if (!ValidFormat().IsMatch(normalized))
        {
            throw new FieldValidationException(
                "policeReportNumber", "POLICE_REPORT_NUMBER_INVALID_FORMAT", RequiredMessage);
        }

        Value = normalized;
    }

    public override string ToString() => Value;

    [GeneratedRegex("^[A-Z0-9][A-Z0-9/ -]{2,49}$")]
    private static partial Regex ValidFormat();
}
