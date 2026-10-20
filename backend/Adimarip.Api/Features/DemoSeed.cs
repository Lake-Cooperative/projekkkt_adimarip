using Adimarip.Data;
using Adimarip.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Adimarip.Api.Features;

public static class DemoSeed
{
    public static async Task ApplyAsync(IServiceProvider services, IConfiguration config, CancellationToken ct = default)
    {
        if (!config.GetValue<bool>("Demo:Enabled")) return;
        var password = config["Demo:Password"] ?? throw new InvalidOperationException("Set Demo:Password to use demo seeding.");
        if (password.Length < 10) throw new InvalidOperationException("Demo:Password must have at least 10 characters.");
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AdimaripDbContext>();
        if (!await db.Offices.AnyAsync(ct))
        {
            db.Offices.AddRange(new Office { Name = "Северный отдел" }, new Office { Name = "Центральное управление" });
            await db.SaveChangesAsync(ct);
        }
        var person = await db.Recipients.FirstOrDefaultAsync(r => r.ExternalIdentifier == "DEMO-001", ct);
        if (person is null)
        {
            person = new Recipient
            {
                ExternalIdentifier = "DEMO-001", FullName = "Примеров Алексей Сергеевич", DateOfBirth = new DateOnly(2001, 4, 12),
                Email = "demo-recipient@example.invalid", Phone = "+70000000000",
                Address = new Address { Region = "Тестовый регион", City = "Учебный город", Street = "Примерная", House = "1" }
            };
            db.Recipients.Add(person);
            await db.SaveChangesAsync(ct);
        }
        if (!await db.Notices.AnyAsync(ct))
        {
            var officeId = await db.Offices.OrderBy(x => x.Id).Select(x => x.Id).FirstAsync(ct);
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            for (var i = 1; i <= 3; i++)
            {
                var notice = new Notice
                {
                    Number = $"DEMO-{i:0000}", RecipientId = person.Id, OfficeId = officeId,
                    AuthorUserId = "seed", CreatedAt = today.AddDays(-i), ProcessingDeadline = today.AddDays(7 + i),
                    Reason = i == 1 ? "Тестовое уведомление о посещении подразделения" : "Демонстрационное оповещение для проверки реестра",
                    Comment = "Только учебные данные", CurrentStatus = i == 3 ? "Sent" : "Created"
                };
                notice.StatusHistory.Add(new NoticeStatusHistory { Status = notice.CurrentStatus, ChangedAt = DateTimeOffset.UtcNow, ChangedByUserId = "seed" });
                notice.AuditEvents.Add(new AuditEvent { Action = "NoticeSeeded", UserId = "seed", OccurredAt = DateTimeOffset.UtcNow });
                db.Notices.Add(notice);
            }
            await db.SaveChangesAsync(ct);
        }
        var store = scope.ServiceProvider.GetRequiredService<UserStore>();
        await store.CreateAsync("operator", "Секретарь (демо)", "Operator", null, password, ct);
        await store.CreateAsync("manager", "Управитель (демо)", "Manager", null, password, ct);
        await store.CreateAsync("observer", "Служащий (демо)", "Observer", person.Id, password, ct);
        await store.CreateAsync("engineer", "Инженер (демо)", "AutomationEngineer", null, password, ct);
    }
}
