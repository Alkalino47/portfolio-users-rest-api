using portfolio_users_rest_api.Application.Common;

namespace portfolio_users_rest_api.Application.Users;

public interface IUserService
{
    Task<PaginatedResult<UserDTO>> GetUsersAsync(UserPaginationQuery paginationQuery, CancellationToken cancellationToken);

    Task<Result<UserDTO>> GetUserAsync(int id, CancellationToken cancellationToken);

    Task<Result<UserDTO>> CreateUserAsync(UserForPostDTO userForPostDTO, CancellationToken cancellationToken);

    Task<Result<UserDTO>> UpdateUserAsync(int id, UserForPutDTO userForPutDTO, CancellationToken cancellationToken);

    Task<Result<UserDTO>> SetActiveStatusAsync(int id, UserActivePatchDTO activePatchDTO, CancellationToken cancellationToken);

    Task<Result> DeleteUserAsync(int id, CancellationToken cancellationToken);
}
