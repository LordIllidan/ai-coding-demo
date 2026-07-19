using PolicyPlatform.Application.Sms;
using PolicyPlatform.Domain.Policies;
using Xunit;

namespace PolicyPlatform.Application.Tests;

public class PolicyStatusReplyMapperTests
{
    [Fact]
    public void Found_MapsToRepliedAndPolicyStatusFound()
    {
        var requestId = Guid.NewGuid();

        var reply = PolicyStatusReplyMapper.Found(requestId, PolicyStatus.Active);

        Assert.Equal(requestId, reply.RequestId);
        Assert.Equal(SmsDecisionCode.Replied, reply.DecisionCode);
        Assert.Equal(SmsReplyCode.PolicyStatusFound, reply.ReplyCode);
        Assert.Equal(PolicyStatus.Active, reply.PolicyStatusCode);
        Assert.Equal("Aktywna", reply.PolicyStatusLabel);
    }

    [Fact]
    public void NotVerified_DoesNotDiscloseAnyPolicyStatus()
    {
        var reply = PolicyStatusReplyMapper.NotVerified(Guid.NewGuid());

        Assert.Equal(SmsDecisionCode.Replied, reply.DecisionCode);
        Assert.Equal(SmsReplyCode.PolicyNotVerified, reply.ReplyCode);
        Assert.Null(reply.PolicyStatusCode);
        Assert.Null(reply.PolicyStatusLabel);
    }

    [Theory]
    [InlineData(SmsReplyCode.ServiceUnavailable, SmsDecisionCode.Error)]
    [InlineData(SmsReplyCode.InvalidInputMissingFields, SmsDecisionCode.Rejected)]
    [InlineData(SmsReplyCode.InvalidPolicyNumberFormat, SmsDecisionCode.Rejected)]
    [InlineData(SmsReplyCode.InvalidPeselFormat, SmsDecisionCode.Rejected)]
    [InlineData(SmsReplyCode.SmsRateLimited, SmsDecisionCode.RateLimited)]
    public void FactoryMethods_MapToExpectedDecisionCode(SmsReplyCode replyCode, SmsDecisionCode expectedDecision)
    {
        var requestId = Guid.NewGuid();

        var reply = replyCode switch
        {
            SmsReplyCode.ServiceUnavailable => PolicyStatusReplyMapper.ServiceUnavailable(requestId),
            SmsReplyCode.InvalidInputMissingFields => PolicyStatusReplyMapper.MissingFields(requestId),
            SmsReplyCode.InvalidPolicyNumberFormat => PolicyStatusReplyMapper.InvalidPolicyNumberFormat(requestId),
            SmsReplyCode.InvalidPeselFormat => PolicyStatusReplyMapper.InvalidPeselFormat(requestId),
            SmsReplyCode.SmsRateLimited => PolicyStatusReplyMapper.RateLimited(requestId),
            _ => throw new ArgumentOutOfRangeException(nameof(replyCode)),
        };

        Assert.Equal(requestId, reply.RequestId);
        Assert.Equal(expectedDecision, reply.DecisionCode);
        Assert.Equal(replyCode, reply.ReplyCode);
        Assert.Null(reply.PolicyStatusCode);
        Assert.Null(reply.PolicyStatusLabel);
    }

    [Theory]
    [InlineData(SmsReplyCode.PolicyStatusFound)]
    [InlineData(SmsReplyCode.PolicyNotVerified)]
    [InlineData(SmsReplyCode.InvalidInputMissingFields)]
    [InlineData(SmsReplyCode.InvalidPolicyNumberFormat)]
    [InlineData(SmsReplyCode.InvalidPeselFormat)]
    [InlineData(SmsReplyCode.SmsRateLimited)]
    [InlineData(SmsReplyCode.ServiceUnavailable)]
    public void ReplyText_EveryReplyCode_ReturnsNonEmptyText(SmsReplyCode replyCode)
    {
        var text = PolicyStatusReplyMapper.ReplyText(replyCode);

        Assert.False(string.IsNullOrWhiteSpace(text));
    }

    [Theory]
    [InlineData(PolicyStatus.Active, "Aktywna")]
    [InlineData(PolicyStatus.Expired, "Wygasla")]
    [InlineData(PolicyStatus.Cancelled, "Anulowana")]
    public void StatusLabel_DisclosableStatuses_ReturnsExpectedLabel(PolicyStatus status, string expectedLabel)
    {
        Assert.Equal(expectedLabel, PolicyStatusReplyMapper.StatusLabel(status));
    }

    [Fact]
    public void StatusLabel_Draft_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PolicyStatusReplyMapper.StatusLabel(PolicyStatus.Draft));
    }
}
