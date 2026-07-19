using PolicyPlatform.Application.Sms;
using PolicyPlatform.Domain.Policies;
using Xunit;

namespace PolicyPlatform.Application.Tests;

public class SmsPolicyStatusResponseDtoTests
{
    [Fact]
    public void FromReply_Found_MapsPolicyStatusCodeAndLabel()
    {
        var requestId = Guid.NewGuid();
        var reply = PolicyStatusReplyMapper.Found(requestId, PolicyStatus.Active);

        var dto = SmsPolicyStatusResponseDto.FromReply(reply);

        Assert.Equal(requestId.ToString(), dto.RequestId);
        Assert.Equal("REPLIED", dto.DecisionCode);
        Assert.Equal("POLICY_STATUS_FOUND", dto.ReplyCode);
        Assert.Equal("ACTIVE", dto.PolicyStatusCode);
        Assert.Equal("Aktywna", dto.PolicyStatusLabel);
    }

    [Fact]
    public void FromReply_NotVerified_LeavesPolicyStatusFieldsNull()
    {
        var reply = PolicyStatusReplyMapper.NotVerified(Guid.NewGuid());

        var dto = SmsPolicyStatusResponseDto.FromReply(reply);

        Assert.Equal("REPLIED", dto.DecisionCode);
        Assert.Equal("POLICY_NOT_VERIFIED", dto.ReplyCode);
        Assert.Null(dto.PolicyStatusCode);
        Assert.Null(dto.PolicyStatusLabel);
    }

    [Theory]
    [InlineData(PolicyStatus.Active, "ACTIVE")]
    [InlineData(PolicyStatus.Expired, "EXPIRED")]
    [InlineData(PolicyStatus.Cancelled, "CANCELLED")]
    public void FromReply_DisclosableStatuses_MapToExpectedWireCode(PolicyStatus status, string expectedWireCode)
    {
        var reply = PolicyStatusReplyMapper.Found(Guid.NewGuid(), status);

        var dto = SmsPolicyStatusResponseDto.FromReply(reply);

        Assert.Equal(expectedWireCode, dto.PolicyStatusCode);
    }

    [Theory]
    [InlineData(SmsDecisionCode.Replied, "REPLIED")]
    [InlineData(SmsDecisionCode.Rejected, "REJECTED")]
    [InlineData(SmsDecisionCode.RateLimited, "RATE_LIMITED")]
    [InlineData(SmsDecisionCode.Error, "ERROR")]
    public void FromReply_EveryDecisionCode_MapsToExpectedWireValue(SmsDecisionCode decisionCode, string expectedWireValue)
    {
        var reply = new PolicyStatusReply(Guid.NewGuid(), decisionCode, SmsReplyCode.ServiceUnavailable, "text", null, null);

        var dto = SmsPolicyStatusResponseDto.FromReply(reply);

        Assert.Equal(expectedWireValue, dto.DecisionCode);
    }

    [Theory]
    [InlineData(SmsReplyCode.PolicyStatusFound, "POLICY_STATUS_FOUND")]
    [InlineData(SmsReplyCode.PolicyNotVerified, "POLICY_NOT_VERIFIED")]
    [InlineData(SmsReplyCode.InvalidInputMissingFields, "INVALID_INPUT_MISSING_FIELDS")]
    [InlineData(SmsReplyCode.InvalidPolicyNumberFormat, "INVALID_POLICY_NUMBER_FORMAT")]
    [InlineData(SmsReplyCode.InvalidPeselFormat, "INVALID_PESEL_FORMAT")]
    [InlineData(SmsReplyCode.SmsRateLimited, "SMS_RATE_LIMITED")]
    [InlineData(SmsReplyCode.ServiceUnavailable, "SERVICE_UNAVAILABLE")]
    public void FromReply_EveryReplyCode_MapsToExpectedWireValue(SmsReplyCode replyCode, string expectedWireValue)
    {
        var reply = new PolicyStatusReply(Guid.NewGuid(), SmsDecisionCode.Error, replyCode, "text", null, null);

        var dto = SmsPolicyStatusResponseDto.FromReply(reply);

        Assert.Equal(expectedWireValue, dto.ReplyCode);
    }

    [Fact]
    public void FromReply_DraftPolicyStatus_ThrowsRatherThanLeakingNonDisclosableStatus()
    {
        var reply = new PolicyStatusReply(
            Guid.NewGuid(), SmsDecisionCode.Replied, SmsReplyCode.PolicyStatusFound, "text", PolicyStatus.Draft, null);

        Assert.Throws<ArgumentOutOfRangeException>(() => SmsPolicyStatusResponseDto.FromReply(reply));
    }
}
