using portfolio_users_rest_api.Application.Common;

namespace portfolio_users_rest_api.Application.Auth;

public interface IAuthService
{
    Task<Result<AuthLoginResponseDTO>> LoginAsync(
        AuthLoginRequestDTO loginRequest,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken);
}
