using System.Net;
using Jint;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace PolicyPlatform.Api.E2E.Tests;

// AISDLC-40: obowiązkowy numer zgłoszenia Policji w formularzu kradzieży.
// Exercises the real HTTP pipeline (WebApplicationFactory<Program>) end-to-end: fetches the
// served index.html and theft-claim-validation.js exactly as a browser would, then runs the
// served script through a JS engine to assert the actual runtime validation behavior.
public class TheftClaimFormE2ETests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public TheftClaimFormE2ETests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetIndex_ContainsRequiredPoliceReportNumberField()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("id=\"theftPoliceReportNumber\"", html);
        Assert.Contains("required", html);
        Assert.Contains("id=\"theftPoliceReportNumberError\"", html);
        Assert.Contains("theft-claim-validation.js", html);
    }

    [Fact]
    public async Task GetValidationScript_IsServedAndExecutable()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/theft-claim-validation.js");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("javascript", response.Content.Headers.ContentType?.MediaType, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("", "2026-01-01", false, true)] // missing police report number blocks submission
    [InlineData("ab", "2026-01-01", false, true)] // too short / no digit fails format
    [InlineData("L.dz. 123/26/RSD", "2026-01-01", true, false)] // valid report number + past date passes
    [InlineData("RSD-1234/26", "2026-01-01", true, false)]
    public async Task ValidateTheftClaimForm_PoliceReportNumber_MatchesAcceptanceCriteria(
        string policeReportNumber, string incidentDate, bool expectedPoliceValid, bool expectedPoliceHasError)
    {
        var engine = await LoadServedValidationEngineAsync();

        engine.SetValue("policeReportNumber", policeReportNumber);
        engine.SetValue("incidentDate", incidentDate);
        var result = engine.Evaluate(
            "TheftClaimValidation.validateTheftClaimForm({ policeReportNumber, incidentDate })");
        var obj = result.AsObject();

        var errors = obj.Get("errors").AsObject();
        var hasPoliceError = errors.HasProperty("policeReportNumber");

        Assert.Equal(expectedPoliceHasError, hasPoliceError);
        if (!expectedPoliceValid)
        {
            Assert.True(hasPoliceError);
        }
    }

    [Fact]
    public async Task ValidateTheftClaimForm_FutureIncidentDate_IsRejected()
    {
        var engine = await LoadServedValidationEngineAsync();
        var futureDate = DateTime.UtcNow.AddDays(5).ToString("yyyy-MM-dd");

        engine.SetValue("policeReportNumber", "RSD-1234/26");
        engine.SetValue("incidentDate", futureDate);
        var result = engine.Evaluate(
            "TheftClaimValidation.validateTheftClaimForm({ policeReportNumber, incidentDate })");
        var obj = result.AsObject();

        Assert.False(obj.Get("valid").AsBoolean());
        Assert.True(obj.Get("errors").AsObject().HasProperty("incidentDate"));
    }

    [Fact]
    public async Task ValidateTheftClaimForm_MissingBothFields_BlocksProgression()
    {
        var engine = await LoadServedValidationEngineAsync();

        engine.SetValue("policeReportNumber", "");
        engine.SetValue("incidentDate", "");
        var result = engine.Evaluate(
            "TheftClaimValidation.validateTheftClaimForm({ policeReportNumber, incidentDate })");
        var obj = result.AsObject();
        var errors = obj.Get("errors").AsObject();

        Assert.False(obj.Get("valid").AsBoolean());
        Assert.True(errors.HasProperty("policeReportNumber"));
        Assert.True(errors.HasProperty("incidentDate"));
        Assert.Equal(
            "Numer zgłoszenia Policji jest wymagany.",
            errors.Get("policeReportNumber").AsString());
    }

    [Fact]
    public async Task ValidateTheftClaimForm_CompleteValidForm_PassesAndReturnsNoErrors()
    {
        var engine = await LoadServedValidationEngineAsync();

        engine.SetValue("policeReportNumber", "L.dz. 123/26/RSD");
        engine.SetValue("incidentDate", "2026-01-01");
        var result = engine.Evaluate(
            "TheftClaimValidation.validateTheftClaimForm({ policeReportNumber, incidentDate })");
        var obj = result.AsObject();

        Assert.True(obj.Get("valid").AsBoolean());
        var errorCount = engine.Evaluate(
            "Object.keys(TheftClaimValidation.validateTheftClaimForm({ policeReportNumber, incidentDate }).errors).length");
        Assert.Equal(0, (int)errorCount.AsNumber());
    }

    private async Task<Engine> LoadServedValidationEngineAsync()
    {
        var client = _factory.CreateClient();
        var script = await client.GetStringAsync("/theft-claim-validation.js");

        var engine = new Engine();
        engine.Execute(script);
        return engine;
    }
}
