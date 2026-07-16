using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PolicyPlatform.Application.Abstractions;
using PolicyPlatform.Application.Assistance;

namespace PolicyPlatform.Infrastructure.Assistance;

/// <summary>Polls for assistance reports whose partner dispatch failed and retries them
/// with backoff. There is no message bus/job scheduler in this codebase, so a polling
/// BackgroundService is the simplest fit consistent with the app's zero-extra-infra ethos.</summary>
public sealed class PartnerDispatchRetryWorker : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(30);
    private const int BatchSize = 20;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PartnerDispatchRetryWorker> _logger;

    public PartnerDispatchRetryWorker(IServiceScopeFactory scopeFactory, ILogger<PartnerDispatchRetryWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(PollInterval);
        do
        {
            try
            {
                await RetryDueReportsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Partner dispatch retry sweep failed.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RetryDueReportsAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IAssistanceReportRepository>();
        var service = scope.ServiceProvider.GetRequiredService<AssistanceReportService>();

        var due = await repository.GetDueForPartnerRetryAsync(DateTime.UtcNow, BatchSize, ct);
        foreach (var report in due)
        {
            await service.DispatchToPartnerAsync(report, ct);
        }
    }
}
