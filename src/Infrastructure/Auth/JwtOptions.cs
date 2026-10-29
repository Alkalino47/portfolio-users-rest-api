namespace portfolio_users_rest_api.Infrastructure.Auth;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = null!;

    public string[] Audiences { get; set; } = [];

    public string SigningKey { get; set; } = null!;

    public int ExpiresMinutes { get; set; } = 60;
}
