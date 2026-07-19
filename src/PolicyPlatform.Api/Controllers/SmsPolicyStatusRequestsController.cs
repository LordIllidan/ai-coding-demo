using Microsoft.AspNetCore.Mvc;
using PolicyPlatform.Application.Sms;

namespace PolicyPlatform.Api.Controllers;

/// <summary>Handles unauthenticated SMS requests for policy status (AISDLC-78 contract). Runs
/// input-shape validation and maps the outcome to the response's HTTP status; the actual
/// policy/PESEL decision is delegated to <see cref="IPolicyStatusRequestHandler"/>.</summary>
[ApiController]
[Route("api/v1/sms/policy-status-requests")]
public sealed class SmsPolicyStatusRequestsController : ControllerBase
{
    private readonly IPolicyStatusRequestHandler _handler;

    /// <summary>Creates the controller with its decision-handling dependency.</summary>
    /// <param name="handler">Use-case port that resolves the business outcome for a validated request.</param>
    public SmsPolicyStatusRequestsController(IPolicyStatusRequestHandler handler) => _handler = handler;

    /// <summary>Validates and processes an incoming SMS policy-status request.</summary>
    /// <param name="request">Wire-level request body (messageId, senderMsisdn, policyNumber, pesel, receivedAt).</param>
    /// <param name="ct">Cancellation token for the downstream decision lookup.</param>
    /// <returns>200 with the outcome on success; 400/422 on input validation failure; 429/503 as mapped
    /// from the decision outcome's <see cref="SmsReplyCode"/>.</returns>
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
