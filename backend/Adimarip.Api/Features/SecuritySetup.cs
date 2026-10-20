using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Adimarip.Api.Features;

public static class SecuritySetup
{
    public const string Reader = "RegistryReader";
    public const string Writer = "RegistryWriter";
    public const string Engineer = "RegistryEngineer";

    public static IServiceCollection AddRegistrySecurity(this IServiceCollection services, IConfiguration config)
    {
        var secret = config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key must be configured.");
        if (Encoding.UTF8.GetByteCount(secret) < 32)
            throw new InvalidOperationException("Jwt:Key must contain at least 32 UTF-8 bytes.");
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options => options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = signingKey,
                ValidateIssuer = true,
                ValidIssuer = "adimarip-api",
                ValidateAudience = true,
                ValidAudience = "adimarip-ui",
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30)
            });
        services.AddAuthorization(options =>
        {
            options.AddPolicy(Reader, policy => policy.RequireRole("Operator", "Manager", "Observer"));
            options.AddPolicy(Writer, policy => policy.RequireRole("Operator", "Manager"));
            options.AddPolicy(Engineer, policy => policy.RequireRole("AutomationEngineer"));
        });
        services.AddScoped<UserStore>();
        return services;
    }

    public static object CreateToken(AppUser account, IConfiguration config, string? recipientFullName = null)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, account.Username),
            new(ClaimTypes.Name, account.DisplayName),
            new(ClaimTypes.Role, account.Role)
        };
        if (account.RecipientId is int recipientId)
            claims.Add(new Claim("recipientId", recipientId.ToString()));
        var expiry = DateTime.UtcNow.AddHours(2);
        var token = new JwtSecurityToken(
            issuer: "adimarip-api", audience: "adimarip-ui", claims: claims,
            expires: expiry,
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!)),
                SecurityAlgorithms.HmacSha256));
        return new
        {
            token = new JwtSecurityTokenHandler().WriteToken(token),
            expiresAt = expiry,
            username = account.Username,
            displayName = account.DisplayName,
            role = account.Role,
            recipientId = account.RecipientId,
            // Observer's surname comes from the bound recipient (first word of the full name, "Фамилия Имя Отчество").
            surname = SurnameOf(recipientFullName ?? account.DisplayName)
        };
    }

    public static string SurnameOf(string fullName) =>
        fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault() ?? fullName;

    public static int? CurrentRecipientId(this ClaimsPrincipal user)
    {
        return int.TryParse(user.FindFirstValue("recipientId"), out var id) ? id : null;
    }

    public static string CurrentUserId(this ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";
}
