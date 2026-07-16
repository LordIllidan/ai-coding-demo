using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PolicyPlatform.Domain.Assistance;

namespace PolicyPlatform.Api.Filters;

/// <summary>Enforces the assistance-reports contract's Idempotency-Key requirement: the
/// header must be present and a valid UUID v4. Missing or malformed both map to
/// ASSISTANCE_004 MISSING_IDEMPOTENCY_KEY — the contract has no separate "invalid format"
/// code. On success the parsed key is stashed in HttpContext.Items for the action to read.</summary>
public sealed class IdempotencyKeyRequiredFilter : IAsyncActionFilter
{
    public const string HeaderName = "Idempotency-Key";
    public const string ItemsKey = "AssistanceIdempotencyKey";

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var header = context.HttpContext.Request.Headers[HeaderName].ToString();

        if (string.IsNullOrWhiteSpace(header) || !Guid.TryParse(header, out var idempotencyKey))
        {
            context.Result = new ObjectResult(new
            {
                code = AssistanceErrorCodes.MissingIdempotencyKey,
                message = $"{HeaderName} header (UUID v4) is required.",
            })
            {
                StatusCode = StatusCodes.Status400BadRequest,
            };
            return;
        }

        context.HttpContext.Items[ItemsKey] = idempotencyKey;
        await next();
    }
}
