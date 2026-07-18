using System.Text;
using System.Text.Json;
using PolicyPlatform.Application.Claims;
using PolicyPlatform.Infrastructure.Security;
using Xunit;

namespace PolicyPlatform.Application.Tests;

public class JwtCustomerIdentityResolverTests
{
    private static string Base64UrlEncode(byte[] bytes)
        => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static string BuildToken(Guid? sub, string? aud = "mobile-client", DateTimeOffset? exp = null)
    {
        var header = Base64UrlEncode(Encoding.UTF8.GetBytes("""{"alg":"none","typ":"JWT"}"""));

        var payload = new Dictionary<string, object?>();
        if (sub is not null) payload["sub"] = sub.Value.ToString();
        if (aud is not null) payload["aud"] = aud;
        payload["exp"] = (exp ?? DateTimeOffset.UtcNow.AddHours(1)).ToUnixTimeSeconds();

        var payloadJson = JsonSerializer.Serialize(payload);
        var encodedPayload = Base64UrlEncode(Encoding.UTF8.GetBytes(payloadJson));

        return $"{header}.{encodedPayload}.unsigned";
    }

    [Fact]
    public void ResolveCustomerId_NullHeader_ThrowsAuthRequired()
    {
        var resolver = new JwtCustomerIdentityResolver();

        Assert.Throws<AuthRequiredException>(() => resolver.ResolveCustomerId(null));
    }

    [Fact]
    public void ResolveCustomerId_BlankHeader_ThrowsAuthRequired()
    {
        var resolver = new JwtCustomerIdentityResolver();

        Assert.Throws<AuthRequiredException>(() => resolver.ResolveCustomerId("   "));
    }

    [Fact]
    public void ResolveCustomerId_MissingBearerPrefix_ThrowsAuthRequired()
    {
        var resolver = new JwtCustomerIdentityResolver();
        var token = BuildToken(Guid.NewGuid());

        Assert.Throws<AuthRequiredException>(() => resolver.ResolveCustomerId(token));
    }

    [Fact]
    public void ResolveCustomerId_NotThreeSegments_ThrowsAuthRequired()
    {
        var resolver = new JwtCustomerIdentityResolver();

        Assert.Throws<AuthRequiredException>(() => resolver.ResolveCustomerId("Bearer not-a-jwt"));
    }

    [Fact]
    public void ResolveCustomerId_UnparseablePayloadSegment_ThrowsAuthRequired()
    {
        var resolver = new JwtCustomerIdentityResolver();

        Assert.Throws<AuthRequiredException>(() => resolver.ResolveCustomerId("Bearer aaa.!!!not-base64!!!.ccc"));
    }

    [Fact]
    public void ResolveCustomerId_PayloadIsJsonArrayNotObject_ThrowsAuthRequired()
    {
        var resolver = new JwtCustomerIdentityResolver();
        var header = Base64UrlEncode(Encoding.UTF8.GetBytes("""{"alg":"none"}"""));
        var payload = Base64UrlEncode(Encoding.UTF8.GetBytes("[1,2,3]"));

        Assert.Throws<AuthRequiredException>(() => resolver.ResolveCustomerId($"Bearer {header}.{payload}.sig"));
    }

    [Fact]
    public void ResolveCustomerId_ExpiredToken_ThrowsAuthRequired()
    {
        var resolver = new JwtCustomerIdentityResolver();
        var token = BuildToken(Guid.NewGuid(), exp: DateTimeOffset.UtcNow.AddMinutes(-5));

        Assert.Throws<AuthRequiredException>(() => resolver.ResolveCustomerId($"Bearer {token}"));
    }

    [Fact]
    public void ResolveCustomerId_MissingSubject_ThrowsAuthRequired()
    {
        var resolver = new JwtCustomerIdentityResolver();
        var token = BuildToken(sub: null);

        Assert.Throws<AuthRequiredException>(() => resolver.ResolveCustomerId($"Bearer {token}"));
    }

    [Fact]
    public void ResolveCustomerId_SubjectNotAGuid_ThrowsAuthRequired()
    {
        var resolver = new JwtCustomerIdentityResolver();
        var header = Base64UrlEncode(Encoding.UTF8.GetBytes("""{"alg":"none"}"""));
        var payload = Base64UrlEncode(Encoding.UTF8.GetBytes("""{"sub":"not-a-guid","exp":9999999999}"""));

        Assert.Throws<AuthRequiredException>(() => resolver.ResolveCustomerId($"Bearer {header}.{payload}.sig"));
    }

    [Fact]
    public void ResolveCustomerId_WrongAudience_ThrowsForbiddenCrossCustomer()
    {
        var resolver = new JwtCustomerIdentityResolver();
        var token = BuildToken(Guid.NewGuid(), aud: "some-other-client");

        Assert.Throws<ForbiddenCrossCustomerException>(() => resolver.ResolveCustomerId($"Bearer {token}"));
    }

    [Fact]
    public void ResolveCustomerId_ValidTokenWithMatchingAudience_ReturnsSubjectAsCustomerId()
    {
        var resolver = new JwtCustomerIdentityResolver();
        var customerId = Guid.NewGuid();
        var token = BuildToken(customerId, aud: "mobile-client");

        var result = resolver.ResolveCustomerId($"Bearer {token}");

        Assert.Equal(customerId, result);
    }

    [Fact]
    public void ResolveCustomerId_ValidTokenWithoutAudienceClaim_ReturnsSubjectAsCustomerId()
    {
        var resolver = new JwtCustomerIdentityResolver();
        var customerId = Guid.NewGuid();
        var token = BuildToken(customerId, aud: null);

        var result = resolver.ResolveCustomerId($"Bearer {token}");

        Assert.Equal(customerId, result);
    }
}
