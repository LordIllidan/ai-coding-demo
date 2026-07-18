namespace PolicyPlatform.Application.Claims;

/// <summary>A single paid tranche, as returned by the downstream tranche integration.</summary>
public sealed record LastPaidTrancheDto(
    Guid TrancheId,
    int TrancheNumber,
    string Status,
    DateTimeOffset PaidAt,
    decimal GrossAmount,
    string Currency);

/// <summary>Response payload for GET /api/claims/{claimId}/last-paid-tranche.
/// <see cref="LastPaidTranche"/> is <see langword="null"/> when the claim has no paid tranche yet.</summary>
public sealed record LastPaidTrancheResult(Guid ClaimId, LastPaidTrancheDto? LastPaidTranche, DateTimeOffset FetchedAt);

/// <summary>Shared error envelope for non-2xx responses across the tranche endpoint.</summary>
public sealed record ErrorEnvelope(string Code, string Message, bool Retryable, string CorrelationId);
