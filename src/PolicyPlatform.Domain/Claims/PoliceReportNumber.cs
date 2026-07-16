using System.Text.RegularExpressions;

namespace PolicyPlatform.Domain.Claims;

public enum PoliceReportNumberError
{
    Required,
    InvalidFormat,
}

public readonly record struct PoliceReportNumber
{
    /// <summary>Trimmed, upper-cased value: 3-50 chars, letters/digits/space/"/"/"-",
    /// must start with a letter or digit (AISDLC-51 contract).</summary>
    private static readonly Regex Pattern = new("^[A-Z0-9][A-Z0-9/ -]{2,49}$", RegexOptions.Compiled);

    public string Value { get; }

    private PoliceReportNumber(string value) => Value = value;

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
    public static PoliceReportNumber Create(string? raw)
    {
        if (!TryCreate(raw, out var number, out var error))
        {
            throw new ArgumentException($"Invalid police report number ({error}).", nameof(raw));
        }

        return number;
    }

    public override string ToString() => Value;
}
