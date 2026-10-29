using portfolio_users_rest_api.Application.LoginLogs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace portfolio_users_rest_api.Controllers;

[Route("api/v1/loginlogs")]
[ApiController]
[Authorize]
public class LoginLogsController(ILoginLogService loginLogService) : ControllerBase
{
    private readonly ILoginLogService _loginLogService = loginLogService;

    [HttpPost]
    [ProducesResponseType(typeof(LoginLogDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginLogDTO>> PostLoginLog(
        LoginLogForPostDTO loginLogForPostDTO,
        CancellationToken cancellationToken)
    {
        var loginLog = await _loginLogService.CreateLoginLogAsync(loginLogForPostDTO, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, loginLog);
    }
}
