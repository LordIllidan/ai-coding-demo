namespace PolicyPlatform.Domain.Assistance;

/// <summary>Error codes from the POST /api/v1/assistance/reports API contract (TechLeadAgent, AISDLC-57).</summary>
public static class AssistanceErrorCodes
{
    public const string InvalidIncidentType = "ASSISTANCE_001";
    public const string GpsLocationRequired = "ASSISTANCE_002";
    public const string InvalidCoordinates = "ASSISTANCE_003";
    public const string MissingIdempotencyKey = "ASSISTANCE_004";
    public const string DuplicateSubmission = "ASSISTANCE_005";

    /// <summary>Used for both 401 UNAUTHORIZED (missing/invalid JWT) and 403 ACCESS_DENIED
    /// (authenticated but forbidden) — the contract assigns both the same code.</summary>
    public const string Unauthorized = "AUTH_001";
}
