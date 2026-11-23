namespace Adimarip.Infrastructure.Persistence.Repositories;

internal static class LikePattern
{
    /// <summary>Шаблон «содержит» для ILIKE; символы %, _ и \ из ввода экранируются.</summary>
    public static string Contains(string term) =>
        $"%{term.Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_")}%";
}
