using PolicyPlatform.Domain.Common;

namespace PolicyPlatform.Domain.Claims;

/// <summary>Thrown by <see cref="PoliceReportNumber"/> when the raw input fails the
/// required/format check. Carries the machine-readable code the API maps to a
/// VALIDATION_ERROR field error (see AISDLC-51 contract).</summary>
public sealed class PoliceReportNumberValidationException : DomainException
{
    public const string ValidationMessage = "Numer zgłoszenia Policji jest wymagany i musi być poprawny.";

    public string Code { get; }

    public PoliceReportNumberValidationException(string code) : base(ValidationMessage) => Code = code;
}
