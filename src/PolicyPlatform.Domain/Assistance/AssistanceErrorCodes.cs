namespace PolicyPlatform.Domain.Assistance;

/// <summary>Error codes from the assistance-reports API contract (TechLeadAgent, AISDLC-69).</summary>
public static class AssistanceErrorCodes
{
    public const string InvalidIncidentType = "ASSISTANCE_001";
    public const string GpsLocationRequired = "ASSISTANCE_002";
    public const string InvalidCoordinates = "ASSISTANCE_003";
    public const string MissingIdempotencyKey = "ASSISTANCE_004";
    public const string DuplicateSubmission = "ASSISTANCE_005";
    public const string Unauthorized = "AUTH_001";
}
