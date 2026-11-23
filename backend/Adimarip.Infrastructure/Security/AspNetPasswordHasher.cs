using Adimarip.Application.Abstractions;
using Microsoft.AspNetCore.Identity;

namespace Adimarip.Infrastructure.Security;

/// <summary>
/// Адаптер к PasswordHasher из ASP.NET Core Identity (PBKDF2). Формат хеша не изменился,
/// поэтому пароли, созданные прежней версией, продолжают работать.
/// </summary>
internal sealed class AspNetPasswordHasher : IPasswordHasher
{
    private static readonly object Owner = new();
    private readonly PasswordHasher<object> _hasher = new();

    public string Hash(string password) => _hasher.HashPassword(Owner, password);

    public bool Verify(string hash, string password) =>
        _hasher.VerifyHashedPassword(Owner, hash, password) != PasswordVerificationResult.Failed;
}
