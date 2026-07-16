namespace PolicyPlatform.Domain.Common;

/// <summary>Domain validation failure tied to a specific request field, carrying the
/// machine-readable error code the API contract requires in its fieldErrors body.</summary>
public sealed class FieldValidationException : DomainException
{
    public string Field { get; }
    public string Code { get; }

    public FieldValidationException(string field, string code, string message) : base(message)
    {
        Field = field;
        Code = code;
    }
}
