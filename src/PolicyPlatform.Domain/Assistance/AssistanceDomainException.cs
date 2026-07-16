using PolicyPlatform.Domain.Common;

namespace PolicyPlatform.Domain.Assistance;

/// <summary>Domain exception carrying the API error code from the assistance-reports
/// contract, so the API layer can map it to the right HTTP status without re-deriving it.</summary>
public sealed class AssistanceDomainException : DomainException
{
    public string Code { get; }

    public AssistanceDomainException(string code, string message) : base(message)
    {
        Code = code;
    }
}
