using System.Text.RegularExpressions;

namespace PolicyPlatform.Domain.Claims;

/// <summary>Reason a raw string failed to become a <see cref="PoliceReportNumber"/>.</summary>
public enum PoliceReportNumberError
{
    /// <summary>Value was null, empty, or whitespace-only after trimming.</summary>
    Required,

    /// <summary>Value did not match the allowed pattern after trim/uppercase normalization.</summary>
    InvalidFormat,
}

/// <summary>Validated, normalized police report number (AISDLC-51 contract): trimmed,
/// upper-cased, 3-50 chars, starting with a letter/digit, containing only
/// letters/digits/space/"/"/"-".</summary>
public readonly record struct PoliceReportNumber
{
    /// <summary>Trimmed, upper-cased value: 3-50 chars, letters/digits/space/"/"/"-",
    /// must start with a letter or digit (AISDLC-51 contract).</summary>
    private static readonly Regex Pattern = new("^[A-Z0-9][A-Z0-9/ -]{2,49}$", RegexOptions.Compiled);

    /// <summary>Normalized (trimmed, UPPERCASE) report number.</summary>
    public string Value { get; }

    private PoliceReportNumber(string value) => Value = value;

    /// <summary>Attempts to trim, uppercase, and validate a raw police report number.</summary>
    /// <param name="raw">Raw user-supplied value; may be null.</param>
    /// <param name="number">The normalized number on success; default on failure.</param>
    /// <param name="error">The validation failure reason on failure; null on success.</param>
    /// <returns><c>true</c> if <paramref name="raw"/> is a valid police report number.</returns>
    public static bool TryCreate(string? raw, out PoliceReportNumber number, out PoliceReportNumberError? error)
    {
        var trimmed = (raw ?? string.Empty).Trim();
        if (trimmed.Length == 0)
        {
            number = default;
            error = PoliceReportNumberError.Required;
            return false;
        }

        var normalized = trimmed.ToUpperInvariant();
        if (!Pattern.IsMatch(normalized))
        {
            number = default;
            error = PoliceReportNumberError.InvalidFormat;
            return false;
        }

        number = new PoliceReportNumber(normalized);
        error = null;
        return true;
    }

    /// <summary>Throws for values already known to be valid (e.g. round-tripping from the
    /// database). Use <see cref="TryCreate"/> at API/validation boundaries instead.</summary>
    /// <param name="raw">Raw value, expected to already satisfy the format rules.</param>
    /// <returns>The normalized police report number.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="raw"/> fails validation.</exception>
    public static PoliceReportNumber Create(string? raw)
    {
        if (!TryCreate(raw, out var number, out var error))
        {
            throw new ArgumentException($"Invalid police report number ({error}).", nameof(raw));
        }

        return number;
    }

    /// <inheritdoc/>
    public override string ToString() => Value;
}
