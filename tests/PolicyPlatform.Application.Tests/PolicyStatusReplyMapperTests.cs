using PolicyPlatform.Application.Sms;
using PolicyPlatform.Domain.Policies;
using Xunit;

namespace PolicyPlatform.Application.Tests;

public class PolicyStatusReplyMapperTests
{
    [Fact]
    public void Found_ReturnsRepliedWithStatusAndLabel()
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
    public void NotVerified_ReturnsRepliedWithoutStatusDisclosure()
    {
        var requestId = Guid.NewGuid();

        var reply = PolicyStatusReplyMapper.NotVerified(requestId);

        Assert.Equal(requestId, reply.RequestId);
        Assert.Equal(SmsDecisionCode.Replied, reply.DecisionCode);
        Assert.Equal(SmsReplyCode.PolicyNotVerified, reply.ReplyCode);
        Assert.Null(reply.PolicyStatusCode);
        Assert.Null(reply.PolicyStatusLabel);
    }

    [Fact]
    public void ServiceUnavailable_ReturnsErrorWithoutStatusDisclosure()
    {
        var requestId = Guid.NewGuid();

        var reply = PolicyStatusReplyMapper.ServiceUnavailable(requestId);

        Assert.Equal(requestId, reply.RequestId);
        Assert.Equal(SmsDecisionCode.Error, reply.DecisionCode);
        Assert.Equal(SmsReplyCode.ServiceUnavailable, reply.ReplyCode);
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
    public void ReplyText_KnownReplyCode_ReturnsNonEmptyText(SmsReplyCode replyCode)
    {
        var text = PolicyStatusReplyMapper.ReplyText(replyCode);

        Assert.False(string.IsNullOrWhiteSpace(text));
    }

    [Fact]
    public void ReplyText_UnknownReplyCode_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PolicyStatusReplyMapper.ReplyText((SmsReplyCode)999));
    }

    [Theory]
    [InlineData(PolicyStatus.Active, "Aktywna")]
    [InlineData(PolicyStatus.Expired, "Wygasla")]
    [InlineData(PolicyStatus.Cancelled, "Anulowana")]
    public void StatusLabel_DisclosableStatus_ReturnsLocalizedLabel(PolicyStatus status, string expectedLabel)
    {
        var label = PolicyStatusReplyMapper.StatusLabel(status);

        Assert.Equal(expectedLabel, label);
    }

    [Fact]
    public void StatusLabel_DraftStatus_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PolicyStatusReplyMapper.StatusLabel(PolicyStatus.Draft));
    }
}
