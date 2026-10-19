using System.Security.Claims;

namespace Adimarip.Api.Features;

/// <summary>
/// Resolve the observer's recipient from the authenticated account, never from
/// a request parameter. Fail closed if the session's binding is missing/stale.
/// </summary>
public static class ObserverAccess
{
    public static async Task<int?> GetVerifiedRecipientIdAsync(
        ClaimsPrincipal principal, UserStore accounts, CancellationToken ct)
    {
        if (!principal.IsInRole("Observer")) return null;

        var claimedId = principal.CurrentRecipientId();
        if (claimedId is null or <= 0) return null;

        var account = await accounts.FindAsync(principal.CurrentUserId(), ct);
        if (account is null || account.Role != "Observer" || account.RecipientId != claimedId)
            return null;

        return claimedId;
    }
}
