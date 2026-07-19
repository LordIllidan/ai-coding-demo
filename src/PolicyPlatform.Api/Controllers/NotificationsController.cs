using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PolicyPlatform.Application.Notifications;

namespace PolicyPlatform.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/mobile/v1/notifications")]
public sealed class NotificationsController : ControllerBase
{
    private static readonly HashSet<string> AllowedListQueryParams = new(StringComparer.OrdinalIgnoreCase)
    {
        "read", "limit", "cursor",
    };

    private readonly NotificationService _notifications;

    public NotificationsController(NotificationService notifications) => _notifications = notifications;

    [HttpGet("counter")]
    public async Task<ActionResult<NotificationCounterDto>> GetCounter(CancellationToken ct)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return UnauthenticatedError();
        }

        return Ok(await _notifications.GetCounterAsync(userId, ct));
    }

    [HttpGet]
    public async Task<ActionResult<NotificationListDto>> GetUnread(
        [FromQuery] string? read, [FromQuery] int limit = 50, [FromQuery] string? cursor = null, CancellationToken ct = default)
    {
        foreach (var key in Request.Query.Keys)
        {
            if (!AllowedListQueryParams.Contains(key))
            {
                return ValidationError($"Unsupported query parameter '{key}'.");
            }
        }

        if (read is not null && read != "false")
        {
            return ValidationError("Query parameter 'read' only supports 'false'.");
        }

        if (limit <= 0)
        {
            return ValidationError("Query parameter 'limit' must be a positive integer.");
        }

        if (!TryGetCurrentUserId(out var userId))
        {
            return UnauthenticatedError();
        }

        return Ok(await _notifications.GetUnreadAsync(userId, limit, cursor, ct));
    }

    [HttpPatch("{notificationId}/read")]
    public async Task<ActionResult<NotificationReadResultDto>> MarkAsRead(string notificationId, CancellationToken ct)
    {
        if (!Guid.TryParse(notificationId, out var parsedId))
        {
            return ValidationError("Path parameter 'notificationId' must be a valid UUID.");
        }

        if (!TryGetCurrentUserId(out var userId))
        {
            return UnauthenticatedError();
        }

        try
        {
            return Ok(await _notifications.MarkAsReadAsync(userId, parsedId, ct));
        }
        catch (NotificationAccessException ex) when (ex.Error == NotificationAccessError.NotFound)
        {
            return NotFoundError("Notification was not found.");
        }
        catch (NotificationAccessException ex) when (ex.Error == NotificationAccessError.Forbidden)
        {
            return ForbiddenError("Notification does not belong to the current user.");
        }
    }

    private bool TryGetCurrentUserId(out Guid userId)
    {
        var sub = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        return Guid.TryParse(sub, out userId);
    }

    private ObjectResult UnauthenticatedError()
        => ErrorResult(StatusCodes.Status401Unauthorized, "UNAUTHENTICATED", "Bearer token is missing a valid subject claim.");

    private ObjectResult ValidationError(string message)
        => ErrorResult(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", message);

    private ObjectResult NotFoundError(string message)
        => ErrorResult(StatusCodes.Status404NotFound, "NOTIFICATION_NOT_FOUND", message);

    private ObjectResult ForbiddenError(string message)
        => ErrorResult(StatusCodes.Status403Forbidden, "FORBIDDEN", message);

    private ObjectResult ErrorResult(int statusCode, string code, string message)
        => new(new { code, message }) { StatusCode = statusCode };
}
