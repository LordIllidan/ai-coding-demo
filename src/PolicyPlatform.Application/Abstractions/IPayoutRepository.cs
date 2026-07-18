namespace PolicyPlatform.Application.Abstractions;

/// <summary>Raw shape of the last-paid-installment row as selected from storage, before the
/// completeness check that decides PAID vs INCOMPLETE_DATA. Fields are nullable because a
/// partial/corrupt row must still be representable — it is not a validated domain object.</summary>
public sealed record PayoutRecord(
    Guid? InstallmentId,
    int? InstallmentNo,
    DateOnly? PaidAt,
    decimal? Amount,
    string? Currency);

public interface IPayoutRepository
{
    /// <summary>Returns the last paid installment for a claim, selected by filtering on
    /// claims.id = claimId and ordering by paid_at DESC, installment_no DESC — never by
    /// policy_id. Null when the claim has no paid installments.</summary>
    Task<PayoutRecord?> GetLastPaidInstallmentAsync(Guid claimId, CancellationToken ct = default);
}
