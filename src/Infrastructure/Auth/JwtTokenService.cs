using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using portfolio_users_rest_api.Application.Auth;
using portfolio_users_rest_api.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace portfolio_users_rest_api.Infrastructure.Auth;

public class JwtTokenService(IOptions<JwtOptions> jwtOptions) : IJwtTokenService
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;

    public AuthTokenDTO CreateAccessToken(User user)
    {
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(_jwtOptions.ExpiresMinutes);
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SigningKey));
        var signingCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var audiences = _jwtOptions.Audiences
            .Where(audience => !string.IsNullOrWhiteSpace(audience))
            .ToArray();

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName),
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim("userRoleId", user.UserRoleId.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };
        claims.AddRange(audiences.Select(audience => new Claim(JwtRegisteredClaimNames.Aud, audience)));

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: null,
            claims: claims,
            expires: expiresAtUtc,
            signingCredentials: signingCredentials);

        return new AuthTokenDTO
        {
            AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
            ExpiresAtUtc = expiresAtUtc
        };
    }
}
