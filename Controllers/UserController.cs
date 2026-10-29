using portfolio_users_rest_api.Application.Common;
using portfolio_users_rest_api.Application.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace portfolio_users_rest_api.Controllers;

[Route("api/v1/[controller]")]
[ApiController]
[Authorize]
public class UserController(IUserService userService) : ControllerBase
{
    private readonly IUserService _userService = userService;

    [HttpGet]
    public async Task<ActionResult<PaginatedResult<UserDTO>>> GetUsers(
        [FromQuery] UserPaginationQuery paginationQuery,
        CancellationToken cancellationToken)
    {
        var users = await _userService.GetUsersAsync(paginationQuery, cancellationToken);

        return Ok(users);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserDTO>> GetUser(int id, CancellationToken cancellationToken)
    {
        var user = await _userService.GetUserAsync(id, cancellationToken);

        return this.ToActionResult(user);
    }

    [HttpPost]
    public async Task<ActionResult<UserDTO>> PostUser(
        UserForPostDTO userForPostDTO,
        CancellationToken cancellationToken)
    {
        var user = await _userService.CreateUserAsync(userForPostDTO, cancellationToken);
        if (!user.Succeeded || user.Value == null)
        {
            return this.ToActionResult(user);
        }

        return CreatedAtAction(
            nameof(GetUser),
            new { id = user.Value.UserId },
            user.Value);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<UserDTO>> PutUser(
        int id,
        UserForPutDTO userForPutDTO,
        CancellationToken cancellationToken)
    {
        var user = await _userService.UpdateUserAsync(id, userForPutDTO, cancellationToken);

        return this.ToActionResult(user);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteUser(int id, CancellationToken cancellationToken)
    {
        var deleted = await _userService.DeleteUserAsync(id, cancellationToken);

        return this.ToActionResult(deleted);
    }

    [HttpPatch("{id:int}/active")]
    [ProducesResponseType(typeof(UserDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDTO>> PatchUserActiveStatus(
        int id,
        UserActivePatchDTO activePatchDTO,
        CancellationToken cancellationToken)
    {
        var user = await _userService.SetActiveStatusAsync(id, activePatchDTO, cancellationToken);

        return this.ToActionResult(user);
    }
}
