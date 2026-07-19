using System.Net;
using System.Text.RegularExpressions;
using Jint;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace PolicyPlatform.Api.E2E.Tests;

// AISDLC-135 / AISDLC-120: komunikat błędu i przycisk "Ponów" po nieudanym pobraniu
// ostatniej wypłaconej transzy. Exercises the real HTTP pipeline (WebApplicationFactory<Program>):
// fetches the served index.html and last-paid-tranche-client.js exactly as a browser would, then
// runs the served scripts through a JS engine (Jint) to assert the actual runtime behavior —
// including a real HTTP round-trip against the running Api for the "no backend yet" scenario.
public class LastPaidTrancheE2ETests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public LastPaidTrancheE2ETests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetIndex_ContainsLastPaidTrancheSection()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("id=\"trancheClaimId\"", html);
        Assert.Contains("id=\"trancheResult\"", html);
        Assert.Contains("id=\"trancheError\"", html);
        Assert.Contains("id=\"trancheRetryButton\"", html);
        Assert.Contains("last-paid-tranche-client.js", html);

        // Retry button must be hidden until an error is actually shown.
        var retryButtonMarkup = Regex.Match(html, "<button id=\"trancheRetryButton\"[^>]*>").Value;
        Assert.Contains("display:none", retryButtonMarkup);
    }

    [Fact]
    public async Task GetLastPaidTrancheClientScript_IsServedAsJavaScript()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/last-paid-tranche-client.js");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("javascript", response.Content.Headers.ContentType?.MediaType, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task FetchLastPaidTranche_RealBackendCall_NoEndpointImplementedYet_NormalizesToRetryableError()
    {
        // No backend route exists yet for GET /api/claims/{claimId}/last-paid-tranche (frontend-only
        // slice of AISDLC-135/120 — see ClaimsController.cs). This performs a REAL HTTP call through
        // the running Api pipeline and feeds the REAL response into the REAL served client script,
        // proving today's graceful degradation (and this test will keep exercising the real contract
        // once the endpoint ships).
        var client = _factory.CreateClient();
        const string claimId = "3fa85f64-5717-4562-b3fc-2c963f66afa6";

        var realResponse = await client.GetAsync($"/api/claims/{claimId}/last-paid-tranche");
        var realBody = await realResponse.Content.ReadAsStringAsync();

        var script = await client.GetStringAsync("/last-paid-tranche-client.js");
        var engine = new Engine();
        engine.Execute(script);
        engine.SetValue("__realOk", realResponse.IsSuccessStatusCode);
        engine.SetValue("__realStatus", (int)realResponse.StatusCode);
        engine.SetValue("__realBody", realBody);
        engine.Execute(@"
            function __fakeFetch() {
                return { ok: __realOk, status: __realStatus, text: function () { return __realBody; } };
            }
        ");

        var result = engine
            .Evaluate($"LastPaidTrancheClient.fetchLastPaidTranche('{claimId}', {{ fetchImpl: __fakeFetch }})")
            .UnwrapIfPromise();
        var obj = result.AsObject();

        Assert.False(realResponse.IsSuccessStatusCode);
        Assert.False(obj.Get("ok").AsBoolean());
        var error = obj.Get("error").AsObject();
        Assert.Equal($"HTTP_{(int)realResponse.StatusCode}", error.Get("code").AsString());
        Assert.True(error.Get("retryable").AsBoolean());
    }

    [Fact]
    public async Task PageScript_ClearsStaleDataAndRetryRepeatsOriginalClaimIdRegardlessOfInputChange()
    {
        var client = _factory.CreateClient();
        var html = await client.GetStringAsync("/");
        var pageScript = ExtractInlineScript(html);
        var clientScript = await client.GetStringAsync("/last-paid-tranche-client.js");

        var engine = new Engine();
        engine.Execute(@"
            var __elements = {};
            function __getEl(id) {
                if (!__elements[id]) {
                    __elements[id] = {
                        id: id, value: '', textContent: '', innerHTML: '', style: {},
                        classList: { toggle: function () {}, contains: function () { return false; } },
                    };
                }
                return __elements[id];
            }
            var document = {
                getElementById: __getEl,
                createElement: function (tag) { return { tagName: tag, innerHTML: '', appendChild: function () {} }; },
            };

            var __trancheCallCount = 0;
            var __lastTrancheUrl = null;
            function fetch(url) {
                if (url.indexOf('last-paid-tranche') !== -1) {
                    __trancheCallCount++;
                    __lastTrancheUrl = url;
                    var isFirstCallError = __trancheCallCount === 1;
                    return {
                        ok: !isFirstCallError,
                        status: isFirstCallError ? 503 : 200,
                        text: function () {
                            return isFirstCallError
                                ? JSON.stringify({ code: 'TRANCHE_SERVICE_UNAVAILABLE', message: 'Usluga niedostepna', retryable: true, correlationId: 'corr-9' })
                                : JSON.stringify({
                                    claimId: '11111111-1111-1111-1111-111111111111',
                                    lastPaidTranche: { trancheId: 't1', trancheNumber: 3, status: 'PAID', paidAt: '2026-01-01T00:00:00Z', grossAmount: 500, currency: 'PLN' },
                                    fetchedAt: '2026-01-01T00:00:00Z',
                                });
                        },
                    };
                }
                return { ok: true, status: 200, text: function () { return '[]'; } };
            }
        ");
        engine.Execute(clientScript);
        engine.Execute(pageScript);

        const string originalClaimId = "11111111-1111-1111-1111-111111111111";
        engine.Execute($"document.getElementById('trancheClaimId').value = '{originalClaimId}';");
        engine.Evaluate("loadLastPaidTranche()");

        Assert.Equal(
            "Usluga niedostepna (TRANCHE_SERVICE_UNAVAILABLE)",
            engine.Evaluate("document.getElementById('trancheError').textContent").AsString());
        Assert.Equal("", engine.Evaluate("document.getElementById('trancheResult').textContent").AsString());
        Assert.Equal("inline-block", engine.Evaluate("document.getElementById('trancheRetryButton').style.display").AsString());

        // User edits the input after the failed load but before clicking "Ponów" — retry must still
        // repeat the ORIGINAL claimId, not whatever is currently typed in the field.
        engine.Execute("document.getElementById('trancheClaimId').value = 'should-be-ignored-by-retry';");
        engine.Evaluate("retryLastPaidTranche()");

        Assert.Contains(originalClaimId, engine.Evaluate("__lastTrancheUrl").AsString());
        Assert.DoesNotContain("should-be-ignored-by-retry", engine.Evaluate("__lastTrancheUrl").AsString());
        Assert.Equal(2, (int)engine.Evaluate("__trancheCallCount").AsNumber());

        // Retry must refresh state: no leftover error, no stale "loading" flicker of old data, and the
        // new result is rendered.
        Assert.Equal("", engine.Evaluate("document.getElementById('trancheError').textContent").AsString());
        Assert.Equal("none", engine.Evaluate("document.getElementById('trancheRetryButton').style.display").AsString());
        var resultText = engine.Evaluate("document.getElementById('trancheResult').textContent").AsString();
        Assert.Contains("Transza #3", resultText);
        Assert.Contains("500.00 PLN", resultText);
    }

    private static string ExtractInlineScript(string html)
    {
        var matches = Regex.Matches(html, "<script>(.*?)</script>", RegexOptions.Singleline);
        var inlineScript = matches[^1].Groups[1].Value;
        Assert.Contains("loadLastPaidTranche", inlineScript);
        return inlineScript;
    }
}
