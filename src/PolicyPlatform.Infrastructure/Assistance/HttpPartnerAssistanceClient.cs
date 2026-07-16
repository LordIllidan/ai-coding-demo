using System.Net.Http.Json;
using PolicyPlatform.Application.Abstractions;
using PolicyPlatform.Domain.Assistance;

namespace PolicyPlatform.Infrastructure.Assistance;

/// <summary>Dispatches assistance reports to the external partner over HTTP. When no
/// partner base address is configured (local/demo runs), dispatch is treated as failed
/// so the caller schedules a retry — consistent with "partner failure never blocks
/// registration" and the app's zero-external-dependency local dev story.</summary>
public sealed class HttpPartnerAssistanceClient : IPartnerAssistanceClient
{
    private readonly HttpClient _httpClient;

    public HttpPartnerAssistanceClient(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<PartnerDispatchResult> DispatchAsync(AssistanceReport report, CancellationToken ct = default)
    {
        if (_httpClient.BaseAddress is null)
        {
            return PartnerDispatchResult.Failed("Partner assistance integration is not configured.");
        }

        var response = await _httpClient.PostAsJsonAsync(
            "dispatch",
            new
            {
                reportId = report.Id,
                incidentType = report.IncidentType.ToString(),
                gpsLatitude = report.Gps.Latitude,
                gpsLongitude = report.Gps.Longitude,
            },
            ct);

        if (!response.IsSuccessStatusCode)
        {
            return PartnerDispatchResult.Failed($"Partner responded with status {(int)response.StatusCode}.");
        }

        var body = await response.Content.ReadFromJsonAsync<PartnerDispatchResponse>(cancellationToken: ct);
        return PartnerDispatchResult.Succeeded(body?.PartnerCaseId ?? report.Id.ToString());
    }

    private sealed record PartnerDispatchResponse(string? PartnerCaseId);
}
