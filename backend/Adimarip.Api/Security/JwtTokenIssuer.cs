using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Adimarip.Application.Access;
using Microsoft.IdentityModel.Tokens;

namespace Adimarip.Api.Security;

public sealed class JwtOptions
{
    public const string Issuer = "adimarip-api";
    public const string Audience = "adimarip-ui";
    public static readonly TimeSpan Lifetime = TimeSpan.FromHours(2);

    public required SymmetricSecurityKey SigningKey { get; init; }

    public static JwtOptions FromConfiguration(IConfiguration config)
    {
        var secret = config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key must be configured.");
        if (Encoding.UTF8.GetByteCount(secret) < 32)
            throw new InvalidOperationException("Jwt:Key must contain at least 32 UTF-8 bytes.");
        return new JwtOptions { SigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)) };
    }
}

/// <summary>Ответ на успешный вход. Права нужны интерфейсу для меню; на сервере они перепроверяются по БД.</summary>
public sealed record SessionResponse(
    string Token,
    DateTime ExpiresAt,
    string Username,
    string DisplayName,
    string Role,
    string RoleDisplayName,
    int? RecipientId,
    string Surname,
    IReadOnlyList<string> Permissions);

public sealed class JwtTokenIssuer(JwtOptions options)
{
    public SessionResponse Issue(LoginResult login)
    {
        var access = login.Access;
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, access.Username),
            new(ClaimTypes.Name, access.DisplayName),
            new(ClaimTypes.Role, access.RoleName)
        };
        if (access.RecipientId is int recipientId)
            claims.Add(new Claim("recipientId", recipientId.ToString()));

        var expiresAt = DateTime.UtcNow.Add(JwtOptions.Lifetime);
        var token = new JwtSecurityToken(JwtOptions.Issuer, JwtOptions.Audience, claims, expires: expiresAt,
            signingCredentials: new SigningCredentials(options.SigningKey, SecurityAlgorithms.HmacSha256));
        return new SessionResponse(new JwtSecurityTokenHandler().WriteToken(token), expiresAt, access.Username,
            access.DisplayName, access.RoleName, access.RoleDisplayName, access.RecipientId, login.Surname,
            access.PermissionCodes);
    }
}
