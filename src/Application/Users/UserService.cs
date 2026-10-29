using portfolio_users_rest_api.Application.Auth;
using portfolio_users_rest_api.Application.Common;
using portfolio_users_rest_api.Domain.Entities;

namespace portfolio_users_rest_api.Application.Users;

public class UserService(
    IUserRepository userRepository,
    TimeProvider timeProvider,
    IUserPasswordHasher passwordHasher) : IUserService
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly IUserPasswordHasher _passwordHasher = passwordHasher;

    public async Task<PaginatedResult<UserDTO>> GetUsersAsync(
        UserPaginationQuery paginationQuery,
        CancellationToken cancellationToken)
    {
        var skip = (paginationQuery.PageNumber - 1) * paginationQuery.PageSize;
        var totalCount = await _userRepository.CountUsersAsync(paginationQuery, cancellationToken);
        var users = await _userRepository.GetUsersAsync(paginationQuery, skip, paginationQuery.PageSize, cancellationToken);

        return new PaginatedResult<UserDTO>
        {
            Pagination = new PaginationMetadata
            {
                PageNumber = paginationQuery.PageNumber,
                PageSize = paginationQuery.PageSize,
                TotalCount = totalCount
            },
            Items = users.Select(UserToDTO).ToList()
        };
    }

    public async Task<Result<UserDTO>> GetUserAsync(int id, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetUserAsync(id, cancellationToken);

        return user == null
            ? Result<UserDTO>.Failure(Errors.NotFound, $"User {id} was not found.")
            : Result<UserDTO>.Success(UserToDTO(user));
    }

    public async Task<Result<UserDTO>> CreateUserAsync(UserForPostDTO userForPostDTO, CancellationToken cancellationToken)
    {
        var now = GetLocalNow();
        var user = new User
        {
            UserName = userForPostDTO.UserName,
            UserRoleId = userForPostDTO.UserRoleId,
            DateCreated = now,
            DateModified = now,
            IsActive = userForPostDTO.IsActive
        };
        user.UserPass = _passwordHasher.HashPassword(user, userForPostDTO.UserPass);

        await _userRepository.AddUserAsync(user, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);

        var createdUser = await _userRepository.GetUserAsync(user.UserId, cancellationToken);

        return Result<UserDTO>.Success(UserToDTO(createdUser ?? user));
    }

    public async Task<Result<UserDTO>> UpdateUserAsync(
        int id,
        UserForPutDTO userForPutDTO,
        CancellationToken cancellationToken)
    {
        if (id != userForPutDTO.UserId)
        {
            return Result<UserDTO>.Failure(Errors.Validation, "Route user id must match the request body UserId.");
        }

        var user = await _userRepository.GetUserForUpdateAsync(id, cancellationToken);
        if (user == null)
        {
            return Result<UserDTO>.Failure(Errors.NotFound, $"User {id} was not found.");
        }

        user.UserName = userForPutDTO.UserName;
        user.UserPass = _passwordHasher.HashPassword(user, userForPutDTO.UserPass);
        user.UserRoleId = userForPutDTO.UserRoleId;
        user.IsActive = userForPutDTO.IsActive;
        user.DateModified = GetLocalNow();

        await _userRepository.SaveChangesAsync(cancellationToken);

        var updatedUser = await _userRepository.GetUserAsync(id, cancellationToken);

        return Result<UserDTO>.Success(UserToDTO(updatedUser ?? user));
    }

    public async Task<Result> DeleteUserAsync(int id, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetUserForUpdateAsync(id, cancellationToken);
        if (user == null)
        {
            return Result.Failure(Errors.NotFound, $"User {id} was not found.");
        }

        _userRepository.DeleteUser(user);
        await _userRepository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result<UserDTO>> SetActiveStatusAsync(
        int id,
        UserActivePatchDTO activePatchDTO,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetUserForUpdateAsync(id, cancellationToken);
        if (user == null)
        {
            return Result<UserDTO>.Failure(Errors.NotFound, $"User {id} was not found.");
        }

        user.IsActive = activePatchDTO.IsActive;
        user.DateModified = GetLocalNow();

        await _userRepository.SaveChangesAsync(cancellationToken);

        var updatedUser = await _userRepository.GetUserAsync(id, cancellationToken);

        return Result<UserDTO>.Success(UserToDTO(updatedUser ?? user));
    }

    private DateTime GetLocalNow()
    {
        return _timeProvider.GetLocalNow().DateTime;
    }

    private static UserDTO UserToDTO(User user) =>
        new()
        {
            UserId = user.UserId,
            UserName = user.UserName,
            UserRoleId = user.UserRoleId,
            UserRole = user.UserRole?.RoleName ?? string.Empty,
            DateCreated = user.DateCreated,
            DateModified = user.DateModified,
            IsActive = user.IsActive
        };
}
