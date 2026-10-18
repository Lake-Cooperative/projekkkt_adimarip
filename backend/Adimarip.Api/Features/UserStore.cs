using Adimarip.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Adimarip.Api.Features;

public sealed record AppUser(string Username, string DisplayName, string Role, int? RecipientId, string PasswordHash);

public sealed class UserStore(AdimaripDbContext db)
{
    private static readonly PasswordHasher<AppUser> Hasher = new();

    public async Task<AppUser?> FindAsync(string username, CancellationToken ct)
    {
        await db.Database.OpenConnectionAsync(ct);
        try
        {
            await using var command = db.Database.GetDbConnection().CreateCommand();
            command.CommandText = "SELECT \"Username\",\"DisplayName\",\"Role\",\"RecipientId\",\"PasswordHash\" FROM \"AppUsers\" WHERE \"Username\"=@username";
            AddParameter(command, "username", username);
            await using var reader = await command.ExecuteReaderAsync(ct);
            if (!await reader.ReadAsync(ct)) return null;
            return new AppUser(reader.GetString(0), reader.GetString(1), reader.GetString(2),
                reader.IsDBNull(3) ? null : reader.GetInt32(3), reader.GetString(4));
        }
        finally { await db.Database.CloseConnectionAsync(); }
    }

    public static bool Verify(AppUser user, string password) =>
        Hasher.VerifyHashedPassword(user, user.PasswordHash, password) != PasswordVerificationResult.Failed;

    public async Task<bool> CreateAsync(string username, string displayName, string role, int? recipientId, string password, CancellationToken ct)
    {
        var account = new AppUser(username, displayName, role, recipientId, "");
        var hash = Hasher.HashPassword(account, password);
        await db.Database.OpenConnectionAsync(ct);
        try
        {
            await using var command = db.Database.GetDbConnection().CreateCommand();
            command.CommandText = """
                INSERT INTO "AppUsers" ("Username", "DisplayName", "Role", "RecipientId", "PasswordHash")
                VALUES (@username, @displayName, @role, @recipientId, @hash)
                ON CONFLICT ("Username") DO NOTHING
                """;
            AddParameter(command, "username", username);
            AddParameter(command, "displayName", displayName);
            AddParameter(command, "role", role);
            AddParameter(command, "recipientId", recipientId is null ? DBNull.Value : recipientId.Value);
            AddParameter(command, "hash", hash);
            return await command.ExecuteNonQueryAsync(ct) == 1;
        }
        finally { await db.Database.CloseConnectionAsync(); }
    }

    private static void AddParameter(System.Data.Common.DbCommand cmd, string name, object value)
    {
        var p = cmd.CreateParameter();
        p.ParameterName = name;
        p.Value = value;
        cmd.Parameters.Add(p);
    }
}
