using portfolio_users_rest_api.Application.Users;
using portfolio_users_rest_api.Application.Common;
using portfolio_users_rest_api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace portfolio_users_rest_api.Infrastructure.Persistence;

public class UserRepository(UsersDbContext context) : IUserRepository
{
    private readonly UsersDbContext _context = context;

    public async Task<IReadOnlyList<User>> GetUsersAsync(
        UserPaginationQuery query,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        return await ApplyUserFilters(query, _context.Users.AsNoTracking())
            .ApplyUserSorting(query)
            .Skip(skip)
            .Take(take)
            .Select(user => new User
            {
                UserId = user.UserId,
                UserName = user.UserName,
                UserPass = string.Empty,
                UserRoleId = user.UserRoleId,
                UserRole = user.UserRole == null
                    ? null
                    : new UserRole
                    {
                        UserRoleId = user.UserRole.UserRoleId,
                        RoleName = user.UserRole.RoleName
                    },
                DateCreated = user.DateCreated,
                DateModified = user.DateModified,
                IsActive = user.IsActive
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountUsersAsync(UserPaginationQuery query, CancellationToken cancellationToken)
    {
        return await ApplyUserFilters(query, _context.Users.AsNoTracking()).CountAsync(cancellationToken);
    }

    public async Task<User?> GetUserAsync(int id, CancellationToken cancellationToken)
    {
        return await _context.Users
            .AsNoTracking()
            .Select(user => new User
            {
                UserId = user.UserId,
                UserName = user.UserName,
                UserPass = string.Empty,
                UserRoleId = user.UserRoleId,
                UserRole = user.UserRole == null
                    ? null
                    : new UserRole
                    {
                        UserRoleId = user.UserRole.UserRoleId,
                        RoleName = user.UserRole.RoleName
                    },
                DateCreated = user.DateCreated,
                DateModified = user.DateModified,
                IsActive = user.IsActive
            })
            .SingleOrDefaultAsync(user => user.UserId == id, cancellationToken);
    }

    public async Task<User?> GetUserByUserNameAsync(string userName, CancellationToken cancellationToken)
    {
        return await _context.Users
            .AsNoTracking()
            .Include(user => user.UserRole)
            .SingleOrDefaultAsync(user => user.UserName == userName, cancellationToken);
    }

    public async Task<User?> GetUserForUpdateAsync(int id, CancellationToken cancellationToken)
    {
        return await _context.Users.FindAsync([id], cancellationToken);
    }

    public async Task<bool> UserExistsAsync(int id, CancellationToken cancellationToken)
    {
        return await _context.Users.AnyAsync(user => user.UserId == id && user.IsActive, cancellationToken);
    }

    public async Task AddUserAsync(User user, CancellationToken cancellationToken)
    {
        await _context.Users.AddAsync(user, cancellationToken);
    }

    public void DeleteUser(User user)
    {
        _context.Users.Remove(user);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }

    private static IQueryable<User> ApplyUserFilters(UserPaginationQuery query, IQueryable<User> users)
    {
        if (query.IsActive.HasValue)
        {
            users = users.Where(user => user.IsActive == query.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            users = users.Where(user => user.UserName.Contains(search));
        }

        return users;
    }
}

public static class UserRepositoryQueryExtensions
{
    public static IQueryable<User> ApplyUserSorting(this IQueryable<User> users, UserPaginationQuery query)
    {
        return query.SortBy?.Trim().ToLowerInvariant() switch
        {
            "username" => query.SortDirection == SortDirection.Desc
                ? users.OrderByDescending(user => user.UserName).ThenBy(user => user.UserId)
                : users.OrderBy(user => user.UserName).ThenBy(user => user.UserId),
            "datecreated" => query.SortDirection == SortDirection.Desc
                ? users.OrderByDescending(user => user.DateCreated).ThenBy(user => user.UserId)
                : users.OrderBy(user => user.DateCreated).ThenBy(user => user.UserId),
            "datemodified" => query.SortDirection == SortDirection.Desc
                ? users.OrderByDescending(user => user.DateModified).ThenBy(user => user.UserId)
                : users.OrderBy(user => user.DateModified).ThenBy(user => user.UserId),
            "isactive" => query.SortDirection == SortDirection.Desc
                ? users.OrderByDescending(user => user.IsActive).ThenBy(user => user.UserId)
                : users.OrderBy(user => user.IsActive).ThenBy(user => user.UserId),
            _ => query.SortDirection == SortDirection.Desc
                ? users.OrderByDescending(user => user.UserId)
                : users.OrderBy(user => user.UserId)
        };
    }
}
