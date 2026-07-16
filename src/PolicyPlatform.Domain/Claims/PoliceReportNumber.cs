using System.Text.RegularExpressions;

namespace PolicyPlatform.Domain.Claims;

public readonly partial record struct PoliceReportNumber
{
    public const string RequiredCode = "POLICE_REPORT_NUMBER_REQUIRED";
    public const string InvalidFormatCode = "POLICE_REPORT_NUMBER_INVALID_FORMAT";

    public string Value { get; }

    public PoliceReportNumber(string? value)
    {
        var trimmed = (value ?? string.Empty).Trim();
        if (trimmed.Length == 0)
        {
            throw new PoliceReportNumberValidationException(RequiredCode);
        }

        var normalized = trimmed.ToUpperInvariant();
        if (!FormatPattern().IsMatch(normalized))
        {
            throw new PoliceReportNumberValidationException(InvalidFormatCode);
        }

        Value = normalized;
    }

    public override string ToString() => Value;

    // 3-50 chars, letters/digits/space/"/"/"-" only, normalized to UPPERCASE before this runs.
    [GeneratedRegex("^[A-Z0-9][A-Z0-9/ -]{2,49}$")]
    private static partial Regex FormatPattern();
}
