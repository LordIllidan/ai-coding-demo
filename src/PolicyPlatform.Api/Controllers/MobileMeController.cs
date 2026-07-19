using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PolicyPlatform.Api.Security;
using PolicyPlatform.Application.Identity;

namespace PolicyPlatform.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/mobile/me")]
public sealed class MobileMeController : ControllerBase
{
    private readonly LoginHistoryService _loginHistory;

    public MobileMeController(LoginHistoryService loginHistory) => _loginHistory = loginHistory;

    [HttpGet("login-history")]
    public async Task<ActionResult<LoginHistoryResponse>> GetLoginHistory(CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var response = await _loginHistory.GetForUserAsync(userId.Value, ct);
        return Ok(response);
    }
}
