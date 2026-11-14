using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using HeatSafe.Api.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace HeatSafe.Api.Security;

public sealed class JwtTokenService(IOptions<JwtOptions> options, TimeProvider timeProvider)
{
    private readonly JwtOptions _options = options.Value;

    public (string Token, DateTime ExpiresAtUtc) Create(string subjectId, string role, string username, string? deviceCode = null)
    {
        var now = timeProvider.GetUtcNow();
        var expires = now.AddMinutes(_options.ExpirationMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, subjectId),
            new(JwtRegisteredClaimNames.UniqueName, username),
            new(ClaimTypes.Role, role)
        };
        if (!string.IsNullOrWhiteSpace(deviceCode)) claims.Add(new Claim("device_code", deviceCode));
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(_options.Issuer, _options.Audience, claims,
            now.UtcDateTime, expires.UtcDateTime, credentials);
        return (new JwtSecurityTokenHandler().WriteToken(token), expires.UtcDateTime);
    }
}
