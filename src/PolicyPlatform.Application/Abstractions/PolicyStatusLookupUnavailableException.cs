namespace PolicyPlatform.Application.Abstractions;

/// <summary>Thrown by <see cref="IPolicyStatusLookupRepository"/> implementations when the
/// lookup could not be completed (e.g. a downstream dependency failure). Distinguished from
/// domain validation errors so the use case can map it to SERVICE_UNAVAILABLE instead of the
/// uniform POLICY_NOT_VERIFIED result.</summary>
public sealed class PolicyStatusLookupUnavailableException : Exception
{
    public PolicyStatusLookupUnavailableException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
