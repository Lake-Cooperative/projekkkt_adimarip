using System.Reflection;
using Adimarip.Application.Access;
using Adimarip.Domain.Access;
using Adimarip.Domain.Directory;
using Adimarip.Domain.Notices;

namespace Adimarip.Tests.Fakes;

/// <summary>Построители тестовых объектов.</summary>
internal static class TestData
{
    public static readonly DateTimeOffset Now = new(2026, 11, 19, 12, 0, 0, TimeSpan.Zero);
    public static DateOnly Today => DateOnly.FromDateTime(Now.UtcDateTime);

    /// <summary>Id назначает БД; в тестах выставляем его вместо неё.</summary>
    public static T WithId<T>(this T entity, int id)
    {
        typeof(T).GetProperty("Id", BindingFlags.Instance | BindingFlags.Public)!
            .SetValue(entity, id);
        return entity;
    }

    public static NoticeDraft Draft(string number = "AD-2026-001", int recipientId = 1, DateOnly? created = null,
        DateOnly? deadline = null, string reason = "Проверка") =>
        new(number, recipientId, 1, created ?? Today, deadline ?? Today.AddDays(5), reason, null);

    private static readonly Dictionary<NoticeStatus, NoticeStatus[]> PathTo = new()
    {
        [NoticeStatus.Created] = [],
        [NoticeStatus.Sent] = [NoticeStatus.Sent],
        [NoticeStatus.Delivered] = [NoticeStatus.Sent, NoticeStatus.Delivered],
        [NoticeStatus.Read] = [NoticeStatus.Sent, NoticeStatus.Delivered, NoticeStatus.Read],
        [NoticeStatus.Closed] = [NoticeStatus.Sent, NoticeStatus.Delivered, NoticeStatus.Closed],
        [NoticeStatus.Failed] = [NoticeStatus.Sent, NoticeStatus.Failed],
        [NoticeStatus.Cancelled] = [NoticeStatus.Cancelled]
    };

    /// <summary>Оповещение, доведённое до нужного статуса разрешёнными переходами.</summary>
    public static Notice Notice(NoticeStatus status = NoticeStatus.Created, int id = 1, int recipientId = 1)
    {
        var notice = global::Adimarip.Domain.Notices.Notice.Register(Draft($"N-{id:000}", recipientId), "tester", Now).Value.WithId(id);
        foreach (var step in PathTo[status])
            notice.ChangeStatus(step, "tester", null, Now);
        return notice;
    }

    public static UserAccess Access(string role, int? recipientId = null, string username = "user")
    {
        var definition = SystemRoles.Get(role);
        return new UserAccess(username, username, definition.Name, definition.DisplayName, definition.PermissionSet, recipientId);
    }

    public static UserAccess AccessWith(params Permission[] permissions) =>
        new("custom", "custom", "Custom", "Кастомная", permissions.ToHashSet(), null);

    public static Recipient Recipient(int id, string? email = "a@example.invalid", string? phone = "+70000000001") =>
        global::Adimarip.Domain.Directory.Recipient.Create(new RecipientProfile($"R-{id}", $"Тестов{id} Тест Тестович",
                new DateOnly(2000, 1, 1),
            email, phone, 1, null), Today).Value.WithId(id);
}
