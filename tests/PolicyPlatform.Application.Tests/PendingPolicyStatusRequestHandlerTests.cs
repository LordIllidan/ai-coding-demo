using PolicyPlatform.Application.Sms;
using PolicyPlatform.Infrastructure.Sms;
using Xunit;

namespace PolicyPlatform.Application.Tests;

public class PendingPolicyStatusRequestHandlerTests
{
    [Fact]
    public async Task HandleAsync_AlwaysReturnsServiceUnavailable_WithoutLeakingPolicyData()
    {
        var handler = new PendingPolicyStatusRequestHandler();
        var request = new PolicyStatusRequest("POL-2026-01", "44051401359");

        var reply = await handler.HandleAsync(request);

        Assert.Equal(SmsDecisionCode.Error, reply.DecisionCode);
        Assert.Equal(SmsReplyCode.ServiceUnavailable, reply.ReplyCode);
        Assert.Null(reply.PolicyStatusCode);
        Assert.Null(reply.PolicyStatusLabel);
    }

    [Fact]
    public async Task HandleAsync_GeneratesDistinctRequestIdPerCall()
    {
        var handler = new PendingPolicyStatusRequestHandler();
        var request = new PolicyStatusRequest("POL-2026-01", "44051401359");

        var first = await handler.HandleAsync(request);
        var second = await handler.HandleAsync(request);

        Assert.NotEqual(first.RequestId, second.RequestId);
    }
}
