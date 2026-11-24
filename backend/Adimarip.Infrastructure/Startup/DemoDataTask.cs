using Adimarip.Application.Abstractions;
using Adimarip.Domain.Access;
using Adimarip.Domain.Directory;
using Adimarip.Domain.Notices;
using Adimarip.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Adimarip.Infrastructure.Startup;

/// <summary>
/// Вымышленные данные для демонстрации (только при Demo:Enabled=true).
/// Каждый шаг идемпотентен: повторный запуск ничего не дублирует и не сбрасывает пароли.
/// </summary>
internal sealed class DemoDataTask(
    IConfiguration config,
    AdimaripDbContext db,
    AccountSeeding accounts,
    IUnitOfWork unitOfWork,
    IClock clock) : IStartupTask
{
    public const string DutyOfficerRole = "DutyOfficer";

    private static readonly string[] OfficeNames = ["Северный отдел", "Центральное управление"];

    // Работники для демонстрации массового оповещения: у кого-то нет телефона,
    // у кого-то нет контактов вовсе (сработает резервный канал «Портал»),
    // один адрес заведомо «сбойный» — чтобы был виден отчёт об ошибках.
    private static readonly (string Id, string Name, string? Email, string? Phone, int Office)[] Employees =
    [
        ("DEMO-002", "Иванова Мария Петровна", "ivanova@example.invalid", "+70000000002", 0),
        ("DEMO-003", "Петров Олег Николаевич", "petrov@example.invalid", null, 0),
        ("DEMO-004", "Сидорова Анна Викторовна", null, "+70000000004", 0),
        ("DEMO-005", "Кузнецов Илья Андреевич", "fail-mailbox@example.invalid", "+70000000005", 1),
        ("DEMO-006", "Смирнова Елена Игоревна", "smirnova@example.invalid", "+70000000006", 1),
        ("DEMO-007", "Волков Денис Сергеевич", null, null, 1),
        ("DEMO-008", "Морозова Ксения Олеговна", "morozova@example.invalid", "+70000000008", 1)
    ];

    public async Task ExecuteAsync(CancellationToken ct)
    {
        if (!config.GetValue<bool>("Demo:Enabled")) return;
        var password = config["Demo:Password"] ?? throw new InvalidOperationException("Set Demo:Password to use demo seeding.");
        if (password.Length < AppUser.MinPasswordLength)
            throw new InvalidOperationException("Demo:Password must have at least 10 characters.");

        var offices = await EnsureOfficesAsync(ct);
        var demoObserver = await EnsureEmployeesAsync(offices, ct);
        await EnsureNoticesAsync(demoObserver.Id, offices[0].Id, ct);
        await EnsureDutyOfficerRoleAsync(ct);
        await EnsureAccountsAsync(demoObserver.Id, password, ct);
    }

    /// <summary>Работники для показа рассылки; возвращает адресата, к которому привязан демо-служащий.</summary>
    private async Task<Recipient> EnsureEmployeesAsync(IReadOnlyList<Office> offices, CancellationToken ct)
    {
        var demoAddress = new AddressData(null, "Тестовый регион", "Учебный город", "Примерная", "1", null);
        var observer = await EnsureRecipientAsync("DEMO-001", "Примеров Алексей Сергеевич", "demo-recipient@example.invalid",
            "+70000000000", offices[0].Id, demoAddress, ct);
        foreach (var employee in Employees)
            await EnsureRecipientAsync(employee.Id, employee.Name, employee.Email, employee.Phone, offices[employee.Office].Id, null, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return observer;
    }

    private async Task EnsureAccountsAsync(int observerRecipientId, string password, CancellationToken ct)
    {
        await accounts.EnsureAsync("operator", "Секретарь (демо)", SystemRoles.Operator, null, password, ct);
        await accounts.EnsureAsync("manager", "Управитель (демо)", SystemRoles.Manager, null, password, ct);
        await accounts.EnsureAsync("observer", "Служащий (демо)", SystemRoles.Observer, observerRecipientId, password, ct);
        await accounts.EnsureAsync("engineer", "Инженер (демо)", SystemRoles.AutomationEngineer, null, password, ct);
        await accounts.EnsureAsync("duty", "Дежурный (демо)", DutyOfficerRole, null, password, ct);
        await unitOfWork.SaveChangesAsync(ct);
    }

    private async Task<IReadOnlyList<Office>> EnsureOfficesAsync(CancellationToken ct)
    {
        if (!await db.Offices.AnyAsync(ct))
        {
            db.Offices.AddRange(OfficeNames.Select(name => new Office(name)));
            await unitOfWork.SaveChangesAsync(ct);
        }
        var all = await db.Offices.OrderBy(o => o.Id).ToListAsync(ct);
        return all.Count >= 2 ? all : [all[0], all[0]];
    }

    private async Task<Recipient> EnsureRecipientAsync(string identifier, string fullName, string? email, string? phone,
        int officeId, AddressData? address, CancellationToken ct)
    {
        var existing = await db.Recipients.FirstOrDefaultAsync(r => r.ExternalIdentifier == identifier, ct);
        if (existing is not null)
        {
            existing.AssignOffice(existing.OfficeId ?? officeId); // старые демо-тома: привязываем к подразделению
            return existing;
        }
        var created = Recipient.Create(new RecipientProfile(identifier, fullName, new DateOnly(2001, 4, 12),
            email, phone, officeId, address), clock.Today).Value;
        db.Recipients.Add(created);
        await unitOfWork.SaveChangesAsync(ct);
        return created;
    }

    private async Task EnsureNoticesAsync(int recipientId, int officeId, CancellationToken ct)
    {
        if (await db.Notices.AnyAsync(ct)) return;
        var today = clock.Today;
        for (var i = 1; i <= 3; i++)
        {
            var draft = new NoticeDraft($"DEMO-{i:0000}", recipientId, officeId, today.AddDays(-i), today.AddDays(7 + i),
                i == 1 ? "Тестовое уведомление о посещении подразделения" : "Демонстрационное оповещение для проверки реестра",
                "Только учебные данные");
            db.Notices.Add(Notice.Seed(draft, i == 3 ? NoticeStatus.Sent : NoticeStatus.Created, clock.UtcNow));
        }
        await unitOfWork.SaveChangesAsync(ct);
    }

    /// <summary>Пример кастомной роли: дежурный видит реестр и запускает массовые оповещения.</summary>
    private async Task EnsureDutyOfficerRoleAsync(CancellationToken ct)
    {
        if (await db.Roles.AnyAsync(r => r.Name == DutyOfficerRole, ct)) return;
        var role = Role.CreateCustom(DutyOfficerRole, "Дежурный по безопасности",
        [
            nameof(Permission.NoticesReadAll), nameof(Permission.RecipientsRead), nameof(Permission.HistoryView),
            nameof(Permission.BroadcastsSend), nameof(Permission.BroadcastsView)
        ], clock.UtcNow).Value;
        db.Roles.Add(role);
        await unitOfWork.SaveChangesAsync(ct);
    }
}
