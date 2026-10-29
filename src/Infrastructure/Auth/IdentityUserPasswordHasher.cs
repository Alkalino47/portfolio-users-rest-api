using portfolio_users_rest_api.Application.Auth;
using portfolio_users_rest_api.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace portfolio_users_rest_api.Infrastructure.Auth;

public sealed class IdentityUserPasswordHasher(IPasswordHasher<User> passwordHasher) : IUserPasswordHasher
{
    public string HashPassword(User user, string password) => passwordHasher.HashPassword(user, password);

    public bool VerifyPassword(User user, string password) =>
        passwordHasher.VerifyHashedPassword(user, user.UserPass, password) != PasswordVerificationResult.Failed;
}
