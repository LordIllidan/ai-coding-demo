using PolicyPlatform.Application.Abstractions;
using PolicyPlatform.Application.Sms;
using PolicyPlatform.Domain.Customers;
using PolicyPlatform.Domain.Policies;
using Xunit;

namespace PolicyPlatform.Application.Tests;

public class PolicyStatusRequestServiceTests
{
    private const string ValidPolicyNumber = "POL-2026-000001";
    private const string ValidPesel = "44051401359";

    private sealed class FakePolicyStatusLookupRepository : IPolicyStatusLookupRepository
    {
        public PolicyStatus? Result { get; set; }
        public Exception? ThrowOnLookup { get; set; }
        public int CallCount { get; private set; }

        public Task<PolicyStatus?> FindPolicyStatusAsync(PolicyNumber policyNumber, Pesel pesel, CancellationToken ct = default)
        {
            CallCount++;
            if (ThrowOnLookup is not null)
            {
                throw ThrowOnLookup;
            }

            return Task.FromResult(Result);
        }
    }

    [Fact]
    public async Task HandleAsync_PolicyFoundWithMatchingPesel_ReturnsFound()
    {
        var repo = new FakePolicyStatusLookupRepository { Result = PolicyStatus.Active };
        var service = new PolicyStatusRequestService(repo);

        var reply = await service.HandleAsync(new PolicyStatusRequest(ValidPolicyNumber, ValidPesel));

        Assert.Equal(SmsDecisionCode.Replied, reply.DecisionCode);
        Assert.Equal(SmsReplyCode.PolicyStatusFound, reply.ReplyCode);
        Assert.Equal(PolicyStatus.Active, reply.PolicyStatusCode);
        Assert.Equal("Aktywna", reply.PolicyStatusLabel);
    }

    [Fact]
    public async Task HandleAsync_NoMatchingPolicy_ReturnsNotVerifiedWithoutCallingLookupDifferently()
    {
        var repo = new FakePolicyStatusLookupRepository { Result = null };
        var service = new PolicyStatusRequestService(repo);

        var reply = await service.HandleAsync(new PolicyStatusRequest(ValidPolicyNumber, ValidPesel));

        Assert.Equal(SmsDecisionCode.Replied, reply.DecisionCode);
        Assert.Equal(SmsReplyCode.PolicyNotVerified, reply.ReplyCode);
        Assert.Null(reply.PolicyStatusCode);
        Assert.Null(reply.PolicyStatusLabel);
        Assert.Equal(1, repo.CallCount);
    }

    [Fact]
    public async Task HandleAsync_InvalidPolicyNumberFormat_ReturnsNotVerifiedWithoutCallingLookup()
    {
        var repo = new FakePolicyStatusLookupRepository { Result = PolicyStatus.Active };
        var service = new PolicyStatusRequestService(repo);

        var reply = await service.HandleAsync(new PolicyStatusRequest("BAD", ValidPesel));

        Assert.Equal(SmsReplyCode.PolicyNotVerified, reply.ReplyCode);
        Assert.Null(reply.PolicyStatusCode);
        Assert.Equal(0, repo.CallCount);
    }

    [Fact]
    public async Task HandleAsync_InvalidPeselFormat_ReturnsNotVerifiedWithoutCallingLookup()
    {
        var repo = new FakePolicyStatusLookupRepository { Result = PolicyStatus.Active };
        var service = new PolicyStatusRequestService(repo);

        var reply = await service.HandleAsync(new PolicyStatusRequest(ValidPolicyNumber, "not-a-pesel"));

        Assert.Equal(SmsReplyCode.PolicyNotVerified, reply.ReplyCode);
        Assert.Null(reply.PolicyStatusCode);
        Assert.Equal(0, repo.CallCount);
    }

    [Fact]
    public async Task HandleAsync_LookupUnavailable_ReturnsServiceUnavailable()
    {
        var repo = new FakePolicyStatusLookupRepository
        {
            ThrowOnLookup = new PolicyStatusLookupUnavailableException("downstream down"),
        };
        var service = new PolicyStatusRequestService(repo);

        var reply = await service.HandleAsync(new PolicyStatusRequest(ValidPolicyNumber, ValidPesel));

        Assert.Equal(SmsDecisionCode.Error, reply.DecisionCode);
        Assert.Equal(SmsReplyCode.ServiceUnavailable, reply.ReplyCode);
        Assert.Null(reply.PolicyStatusCode);
        Assert.Null(reply.PolicyStatusLabel);
    }

    [Fact]
    public async Task HandleAsync_ReturnsFreshRequestIdEachCall()
    {
        var repo = new FakePolicyStatusLookupRepository { Result = null };
        var service = new PolicyStatusRequestService(repo);

        var first = await service.HandleAsync(new PolicyStatusRequest(ValidPolicyNumber, ValidPesel));
        var second = await service.HandleAsync(new PolicyStatusRequest(ValidPolicyNumber, ValidPesel));

        Assert.NotEqual(first.RequestId, second.RequestId);
    }
}
