namespace PolicyPlatform.Application.Abstractions;

/// <summary>Resolves the authenticated caller's identity from the current request's
/// JWT — implemented in the API layer so the Application layer stays HTTP-agnostic.</summary>
public interface ICurrentUserAccessor
{
    Guid GetUserId();
}
