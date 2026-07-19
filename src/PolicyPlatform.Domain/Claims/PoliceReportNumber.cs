using System.Text.RegularExpressions;
using PolicyPlatform.Domain.Common;

namespace PolicyPlatform.Domain.Claims;

/// <summary>Validated, normalized police report number attached to a theft claim.
/// Input is trimmed and upper-cased, then checked against the required format
/// before it is accepted as a value.</summary>
public readonly partial record struct PoliceReportNumber
{
    private const string RequiredMessage = "Numer zgłoszenia Policji jest wymagany i musi być poprawny.";

    /// <summary>Normalized (trimmed, upper-cased) police report number.</summary>
    public string Value { get; }

    /// <summary>Validates and normalizes a raw police report number.</summary>
    /// <param name="value">Raw, unnormalized input value.</param>
    /// <exception cref="FieldValidationException">
    /// Thrown with code <c>POLICE_REPORT_NUMBER_REQUIRED</c> when <paramref name="value"/> is null, empty,
    /// or blank after trimming; thrown with code <c>POLICE_REPORT_NUMBER_INVALID_FORMAT</c> when the
    /// normalized value does not match <c>^[A-Z0-9][A-Z0-9/ -]{2,49}$</c>.
    /// </exception>
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
