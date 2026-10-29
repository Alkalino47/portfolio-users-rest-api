namespace portfolio_users_rest_api.Application.Auth;

public class AuthTokenDTO
{
    public string AccessToken { get; set; } = null!;

    public string TokenType { get; set; } = "Bearer";

    public DateTime ExpiresAtUtc { get; set; }
}
