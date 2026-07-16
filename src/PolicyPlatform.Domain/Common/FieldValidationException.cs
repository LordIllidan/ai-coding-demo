namespace PolicyPlatform.Domain.Common;

/// <summary>Domain validation failure tied to a specific request field, carrying the
/// machine-readable error code the API contract requires in its fieldErrors body.</summary>
public sealed class FieldValidationException : DomainException
{
    /// <summary>Name of the request field that failed validation, as expected in the API's fieldErrors body.</summary>
    public string Field { get; }

    /// <summary>Machine-readable error code identifying the validation failure (e.g. <c>POLICE_REPORT_NUMBER_REQUIRED</c>).</summary>
    public string Code { get; }

    /// <summary>Creates a field-scoped validation exception.</summary>
    /// <param name="field">Name of the invalid request field.</param>
    /// <param name="code">Machine-readable error code for the failure.</param>
    /// <param name="message">Human-readable message returned to the API caller.</param>
    public FieldValidationException(string field, string code, string message) : base(message)
    {
        Field = field;
        Code = code;
    }
}
