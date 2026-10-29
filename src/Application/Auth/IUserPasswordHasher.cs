using portfolio_users_rest_api.Domain.Entities;

namespace portfolio_users_rest_api.Application.Auth;

public interface IUserPasswordHasher
{
    string HashPassword(User user, string password);

    bool VerifyPassword(User user, string password);
}
