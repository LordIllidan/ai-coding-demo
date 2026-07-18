namespace PolicyPlatform.Application.Claims;

public sealed record LastPaidTrancheDto(
    Guid TrancheId,
    int TrancheNumber,
    string Status,
    DateTimeOffset PaidAt,
    decimal GrossAmount,
    string Currency);

public sealed record LastPaidTrancheResult(Guid ClaimId, LastPaidTrancheDto? LastPaidTranche, DateTimeOffset FetchedAt);

public sealed record ErrorEnvelope(string Code, string Message, bool Retryable, string CorrelationId);
