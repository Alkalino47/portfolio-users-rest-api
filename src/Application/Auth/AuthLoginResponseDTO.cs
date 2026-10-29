namespace portfolio_users_rest_api.Application.Auth;

public class AuthLoginResponseDTO
{
    public int UserId { get; set; }

    public string UserName { get; set; } = null!;

    public string UserRole { get; set; } = null!;

    public bool IsActive { get; set; }

    public AuthTokenDTO Token { get; set; } = null!;
}
