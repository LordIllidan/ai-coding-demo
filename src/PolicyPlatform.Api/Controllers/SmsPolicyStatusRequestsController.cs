using Microsoft.AspNetCore.Mvc;
using PolicyPlatform.Application.Sms;

namespace PolicyPlatform.Api.Controllers;

[ApiController]
[Route("api/v1/sms/policy-status-requests")]
public sealed class SmsPolicyStatusRequestsController : ControllerBase
{
    private readonly IPolicyStatusRequestHandler _handler;

    public SmsPolicyStatusRequestsController(IPolicyStatusRequestHandler handler) => _handler = handler;

    [HttpPost]
    public async Task<ActionResult<SmsPolicyStatusResponseDto>> Create(
        SmsPolicyStatusRequestDto request, CancellationToken ct)
    {
        var requestId = Guid.NewGuid();
        var validation = PolicyStatusRequestValidator.Validate(request, out var normalizedPolicyNumber);

        var reply = validation switch
        {
            PolicyStatusRequestValidationResult.MissingFields =>
                PolicyStatusReplyMapper.MissingFields(requestId),
            PolicyStatusRequestValidationResult.InvalidPolicyNumberFormat =>
                PolicyStatusReplyMapper.InvalidPolicyNumberFormat(requestId),
            PolicyStatusRequestValidationResult.InvalidPeselFormat =>
                PolicyStatusReplyMapper.InvalidPeselFormat(requestId),
            _ => null,
        };

        // Rate limiting (5/15min per senderMsisdn, AISDLC-84/87) is not wired in yet — only
        // input-shape validation and the resulting HTTP mapping are in scope here.
        reply ??= await _handler.HandleAsync(new PolicyStatusRequest(normalizedPolicyNumber, request.Pesel!), ct);

        return StatusCode(HttpStatusFor(reply), SmsPolicyStatusResponseDto.FromReply(reply));
    }

    private static int HttpStatusFor(PolicyStatusReply reply) => reply.ReplyCode switch
    {
        SmsReplyCode.InvalidInputMissingFields => StatusCodes.Status400BadRequest,
        SmsReplyCode.InvalidPolicyNumberFormat => StatusCodes.Status422UnprocessableEntity,
        SmsReplyCode.InvalidPeselFormat => StatusCodes.Status422UnprocessableEntity,
        SmsReplyCode.SmsRateLimited => StatusCodes.Status429TooManyRequests,
        SmsReplyCode.ServiceUnavailable => StatusCodes.Status503ServiceUnavailable,
        _ => StatusCodes.Status200OK,
    };
}
