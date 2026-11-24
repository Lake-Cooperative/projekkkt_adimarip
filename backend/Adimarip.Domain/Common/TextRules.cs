namespace Adimarip.Domain.Common;

/// <summary>Единые правила нормализации и проверки строк, чтобы не дублировать Trim/Length по всему коду.</summary>
public static class TextRules
{
    public static string Clean(string? value) => value?.Trim() ?? string.Empty;

    public static string? CleanOptional(string? value)
    {
        var cleaned = value?.Trim();
        return string.IsNullOrEmpty(cleaned) ? null : cleaned;
    }

    public static bool HasLength(string value, int min, int max) => value.Length >= min && value.Length <= max;

    public static bool FitsOptional(string? value, int max) => value is null || value.Length <= max;
}
