using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PeopleGrid.Application.Abstractions;
using PeopleGrid.Domain.Entities;

namespace PeopleGrid.Infrastructure.Security;

public sealed class JwtTokenService(IOptions<JwtOptions> options) : IJwtTokenService
{
    public string GenerateAccessToken(User user, IEnumerable<string> roles, IEnumerable<string> permissions, string tenantCode)
    {
        var jwt = options.Value;
        var claims = new List<Claim>
        {
            new("UserId", user.Id.ToString()),
            new("TenantCode", tenantCode),
            new("Email", user.Email)
        };
        claims.AddRange(roles.Select(role => new Claim("Roles", role)));
        claims.AddRange(permissions.Select(permission => new Claim("Permissions", permission)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(jwt.Issuer, jwt.Audience, claims, expires: DateTime.UtcNow.AddMinutes(jwt.AccessTokenMinutes), signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }
}
