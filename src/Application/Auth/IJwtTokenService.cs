using portfolio_users_rest_api.Domain.Entities;

namespace portfolio_users_rest_api.Application.Auth;

public interface IJwtTokenService
{
    AuthTokenDTO CreateAccessToken(User user);
}
