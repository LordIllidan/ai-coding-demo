using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace PolicyPlatform.Api.E2E.Tests;

/// <summary>
/// End-to-end coverage for AISDLC-39: the wwwroot UI must expose a claim registration
/// path where selecting "theft" reveals a mandatory police report number field, distinct
/// from the standard "communication" claim path. Since no backend Claims API exists yet
/// (this task is UI-only per Jira), these tests exercise the real HTTP static-file
/// pipeline that serves the markup, rather than a claim submission endpoint.
/// </summary>
public sealed class TheftClaimRegistrationPathTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public TheftClaimRegistrationPathTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task Root_ReturnsOk_WithHtmlContentType()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.StartsWith("text/html", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Root_ExposesClaimTypeSelect_WithCommunicationAndTheftOptions()
    {
        var client = _factory.CreateClient();

        var html = await client.GetStringAsync("/");

        Assert.Contains("id=\"claimType\"", html);
        Assert.Contains("<option value=\"Communication\">Szkoda komunikacyjna</option>", html);
        Assert.Contains("<option value=\"Theft\">Zgłoszenie kradzieży</option>", html);
    }

    [Fact]
    public async Task Root_ExposesPoliceReportNumberField_AsSeparateTheftOnlyField()
    {
        var client = _factory.CreateClient();

        var html = await client.GetStringAsync("/");

        Assert.Contains("id=\"theftFields\"", html);
        Assert.Contains("id=\"policeReportNumber\"", html);
    }

    [Fact]
    public async Task Root_ExposesCommonClaimFields_SharedByBothPaths()
    {
        var client = _factory.CreateClient();

        var html = await client.GetStringAsync("/");

        Assert.Contains("id=\"claimPolicyNumber\"", html);
        Assert.Contains("id=\"claimIncidentDate\"", html);
        Assert.Contains("id=\"claimDescription\"", html);
    }

    [Fact]
    public async Task Root_ExposesToggleAndSubmitBehaviorHooks()
    {
        var client = _factory.CreateClient();

        var html = await client.GetStringAsync("/");

        Assert.Contains("onchange=\"onClaimTypeChange()\"", html);
        Assert.Contains("onclick=\"submitClaim()\"", html);
        Assert.Contains("function onClaimTypeChange()", html);
        Assert.Contains("function submitClaim()", html);
    }

    [Fact]
    public async Task Root_ExposesClaimsRegistryTable_WithPoliceReportNumberColumn()
    {
        var client = _factory.CreateClient();

        var html = await client.GetStringAsync("/");

        Assert.Contains("id=\"claimsBody\"", html);
        Assert.Contains("Nr zgłoszenia Policji", html);
    }

    [Fact]
    public async Task PoliciesApi_StillReachable_AlongsideNewStaticClaimSection()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/policies");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
