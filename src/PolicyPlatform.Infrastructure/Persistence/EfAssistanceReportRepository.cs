using Microsoft.EntityFrameworkCore;
using PolicyPlatform.Application.Abstractions;
using PolicyPlatform.Domain.Assistance;

namespace PolicyPlatform.Infrastructure.Persistence;

public sealed class EfAssistanceReportRepository : IAssistanceReportRepository
{
    private readonly PolicyPlatformDbContext _context;

    public EfAssistanceReportRepository(PolicyPlatformDbContext context) => _context = context;

    public async Task<AssistanceReport?> FindDuplicateAsync(
        Guid userId, Guid idempotencyKey, DateTime since, CancellationToken ct = default)
        => await _context.AssistanceReports
            .Where(r => r.UserId == userId && r.IdempotencyKey == idempotencyKey && r.CreatedAt >= since)
            .FirstOrDefaultAsync(ct);

    public async Task RegisterAsync(AssistanceReport report, AssistanceReportEvent createdEvent, CancellationToken ct = default)
    {
        _context.AssistanceReports.Add(report);
        _context.AssistanceReportEvents.Add(createdEvent);
        await _context.SaveChangesAsync(ct);
    }

    public async Task RecordDispatchOutcomeAsync(
        AssistanceReport report, AssistanceReportEvent outcomeEvent, CancellationToken ct = default)
    {
        if (_context.Entry(report).State == EntityState.Detached)
        {
            _context.AssistanceReports.Attach(report);
            _context.Entry(report).State = EntityState.Modified;
        }

        _context.AssistanceReportEvents.Add(outcomeEvent);
        await _context.SaveChangesAsync(ct);
    }

    public async Task AddEventAsync(AssistanceReportEvent @event, CancellationToken ct = default)
    {
        _context.AssistanceReportEvents.Add(@event);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<AssistanceReport>> GetDueForPartnerRetryAsync(
        DateTime now, int batchSize, CancellationToken ct = default)
        => await _context.AssistanceReports
            .Where(r => r.PartnerDispatchStatus == PartnerDispatchStatus.FAILED_RETRY_SCHEDULED
                        && r.NextPartnerDispatchAttemptAt != null && r.NextPartnerDispatchAttemptAt <= now)
            .OrderBy(r => r.NextPartnerDispatchAttemptAt)
            .Take(batchSize)
            .ToListAsync(ct);
}
