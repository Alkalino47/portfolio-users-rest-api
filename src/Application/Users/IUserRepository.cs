using portfolio_users_rest_api.Domain.Entities;

namespace portfolio_users_rest_api.Application.Users;

public interface IUserRepository
{
    Task<IReadOnlyList<User>> GetUsersAsync(UserPaginationQuery query, int skip, int take, CancellationToken cancellationToken);

    Task<int> CountUsersAsync(UserPaginationQuery query, CancellationToken cancellationToken);

    Task<User?> GetUserAsync(int id, CancellationToken cancellationToken);

    Task<User?> GetUserByUserNameAsync(string userName, CancellationToken cancellationToken);

    Task<User?> GetUserForUpdateAsync(int id, CancellationToken cancellationToken);

    Task<bool> UserExistsAsync(int id, CancellationToken cancellationToken);

    Task AddUserAsync(User user, CancellationToken cancellationToken);

    void DeleteUser(User user);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
