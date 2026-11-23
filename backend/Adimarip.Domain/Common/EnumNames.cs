namespace Adimarip.Domain.Common;

/// <summary>
/// Строгий разбор значений перечислений из внешнего ввода: только точное имя с учётом регистра.
/// Стандартный Enum.TryParse принимает числа («1» → второй элемент) и лишние пробелы — для API это ошибка.
/// </summary>
public static class EnumNames
{
    public static bool TryParse<TEnum>(string? value, out TEnum result) where TEnum : struct, Enum
    {
        foreach (var candidate in Enum.GetValues<TEnum>())
        {
            if (candidate.ToString() != value) continue;
            result = candidate;
            return true;
        }
        result = default;
        return false;
    }
}
