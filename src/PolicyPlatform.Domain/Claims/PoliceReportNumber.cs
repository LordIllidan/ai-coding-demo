using System.Text.RegularExpressions;

namespace PolicyPlatform.Domain.Claims;

/// <summary>Validated, normalized "numer zgłoszenia Policji" for a theft claim. Trims the
/// input, uppercases it, and rejects it (via <see cref="PoliceReportNumberValidationException"/>)
/// if empty or if it fails the required format.</summary>
public readonly partial record struct PoliceReportNumber
{
    /// <summary>Error code used when the raw input is empty after trimming.</summary>
    public const string RequiredCode = "POLICE_REPORT_NUMBER_REQUIRED";

    /// <summary>Error code used when the trimmed, uppercased input fails the format regex.</summary>
    public const string InvalidFormatCode = "POLICE_REPORT_NUMBER_INVALID_FORMAT";

    /// <summary>The trimmed, UPPERCASE report number.</summary>
    public string Value { get; }

    /// <param name="value">Raw report number as submitted; may be null, empty, or unnormalized.</param>
    /// <exception cref="PoliceReportNumberValidationException">
    /// <paramref name="value"/> is empty after trimming, or does not match
    /// <c>^[A-Z0-9][A-Z0-9/ -]{2,49}$</c> once trimmed and uppercased.</exception>
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
