using Adimarip.Application.Access;
using Adimarip.Application.Notices;
using Adimarip.Domain.Access;
using Adimarip.Domain.Common;
using Adimarip.Domain.Notices;
using Adimarip.Tests.Fakes;
using Xunit;

namespace Adimarip.Tests.Application;

public sealed class NoticeServiceTests
{
    private readonly InMemoryStore _store = new();
    private NoticeService Service => new(_store, _store, _store, _store, new FixedClock(TestData.Now));

    private static CreateNoticeInput Input(int recipientId = 1, string number = "AD-1") =>
        new(number, recipientId, 1, TestData.Today, TestData.Today.AddDays(2), "Причина", null);

    [Fact]
    public async Task Create_rejects_archived_recipient_and_duplicate_number()
    {
        _store.Recipients.Add(TestData.Recipient(1));
        var archived = TestData.Recipient(2);
        archived.Archive("Списан", "manager", TestData.Now);
        _store.Recipients.Add(archived);
        var operatorAccess = TestData.Access(SystemRoles.Operator);

        Assert.True((await Service.CreateAsync(operatorAccess, Input(), CancellationToken.None)).IsSuccess);
        Assert.Equal(ErrorKind.Conflict, (await Service.CreateAsync(operatorAccess, Input(), CancellationToken.None)).Error!.Kind);
        Assert.Equal(ErrorKind.Conflict, (await Service.CreateAsync(operatorAccess, Input(2, "AD-2"), CancellationToken.None)).Error!.Kind);
        Assert.Equal(ErrorKind.NotFound, (await Service.CreateAsync(operatorAccess, Input(9, "AD-3"), CancellationToken.None)).Error!.Kind);
    }

    [Fact]
    public async Task Observer_sees_only_own_notice_without_history()
    {
        _store.Notices.Add(TestData.Notice(id: 1, recipientId: 7));
        _store.Notices.Add(TestData.Notice(id: 2, recipientId: 8));
        var observer = TestData.Access(SystemRoles.Observer, recipientId: 7);
        _store.Recipients.Add(TestData.Recipient(7));
        foreach (var n in _store.Notices) SetNavigation(n);

        var own = await Service.GetAsync(observer, 1, CancellationToken.None);
        var foreign = await Service.GetAsync(observer, 2, CancellationToken.None);

        Assert.True(own.IsSuccess);
        Assert.Empty(own.Value.History);
        Assert.Null(own.Value.Audit);
        Assert.Empty(own.Value.AllowedTransitions);
        Assert.True(own.Value.CanAcknowledge);
        Assert.Equal(ErrorKind.NotFound, foreign.Error!.Kind);
    }

    [Fact]
    public async Task Manager_sees_audit_and_transitions()
    {
        var notice = TestData.Notice(NoticeStatus.Sent);
        SetNavigation(notice);
        _store.Notices.Add(notice);

        var detail = (await Service.GetAsync(TestData.Access(SystemRoles.Manager), notice.Id, CancellationToken.None)).Value;

        Assert.NotNull(detail.Audit);
        Assert.NotEmpty(detail.History);
        Assert.Equal(new[] { "Delivered", "Failed", "Cancelled" }, detail.AllowedTransitions.ToArray());
    }

    [Fact]
    public async Task Observer_without_binding_is_forbidden()
    {
        var result = await Service.SearchAsync(TestData.Access(SystemRoles.Observer), new NoticeSearchInput(null, null, null,
                null, null), CancellationToken.None);
        Assert.Equal(ErrorKind.Forbidden, result.Error!.Kind);
    }

    [Fact]
    public async Task Acknowledge_works_only_for_own_notice()
    {
        _store.Notices.Add(TestData.Notice(NoticeStatus.Delivered, id: 1, recipientId: 7));

        var foreign = await Service.AcknowledgeAsync(TestData.Access(SystemRoles.Observer, recipientId: 8), 1, CancellationToken.None);
        var own = await Service.AcknowledgeAsync(TestData.Access(SystemRoles.Observer, recipientId: 7), 1, CancellationToken.None);

        Assert.Equal(ErrorKind.NotFound, foreign.Error!.Kind);
        Assert.Equal("Read", own.Value.CurrentStatus);
    }

    [Fact]
    public async Task Unknown_status_is_validation_error()
    {
        _store.Notices.Add(TestData.Notice());
        var result = await Service.ChangeStatusAsync(TestData.Access(SystemRoles.Manager), 1, new ChangeStatusInput("Teleported",
                null), CancellationToken.None);
        Assert.Equal(ErrorKind.Validation, result.Error!.Kind);
    }

    [Fact]
    public async Task Unknown_status_filter_returns_empty_page()
    {
        _store.Notices.Add(TestData.Notice());
        var page = await Service.SearchAsync(TestData.Access(SystemRoles.Manager), new NoticeSearchInput("Teleported", null, null,
                null, null), CancellationToken.None);
        Assert.Empty(page.Value.Items);
    }

    [Fact]
    public async Task Status_change_conflict_is_reported_as_409()
    {
        _store.Notices.Add(TestData.Notice());
        _store.FailSaveNumber = _ => true;

        var result = await Service.ChangeStatusAsync(TestData.Access(SystemRoles.Manager), 1, new ChangeStatusInput("Sent", null),
                CancellationToken.None);

        Assert.Equal(ErrorKind.Conflict, result.Error!.Kind);
    }

    private void SetNavigation(Notice notice)
    {
        typeof(Notice).GetProperty(nameof(Notice.Recipient))!.SetValue(notice, TestData.Recipient(notice.RecipientId));
        typeof(Notice).GetProperty(nameof(Notice.Office))!.SetValue(notice, _store.Offices[0]);
    }
}

public sealed class AccessServiceTests
{
    private readonly InMemoryStore _store = new();
    private readonly NoCacheRoles _snapshots;
    private readonly FixedClock _clock = new(TestData.Now);

    public AccessServiceTests() => _snapshots = new NoCacheRoles(_store);

    private RoleService Roles => new(_store, _store, _snapshots, _store, _clock);
    private UserAdminService Users => new(_store, _store, _store, _store, new PlainHasher(), _store, _clock);
    private static readonly UserAccess Engineer = TestData.Access(SystemRoles.AutomationEngineer, username: "engineer");

    [Fact]
    public async Task Engineer_creates_custom_role_and_assigns_it()
    {
        var created = await Roles.CreateAsync(new RoleInput("DutyOfficer", "Дежурный", ["NoticesReadAll", "BroadcastsSend"]),
                CancellationToken.None);
        var account = await Users.CreateAsync(new CreateAccountInput("duty1", "Дежурный Д.Д.", "DutyOfficer", null,
                "long-password-1"), CancellationToken.None);

        Assert.True(created.IsSuccess);
        Assert.Equal("All", created.Value.Scope);
        Assert.Equal("DutyOfficer", account.Value.Role);
        Assert.Null(account.Value.RecipientId);
    }

    [Fact]
    public async Task Own_scope_custom_role_creates_recipient_together_with_account()
    {
        await Roles.CreateAsync(new RoleInput("Contractor", "Подрядчик", ["NoticesReadOwn", "NoticesAcknowledgeOwn"]),
                CancellationToken.None);

        var account = await Users.CreateAsync(new CreateAccountInput("worker1", "Подрядов Пётр Петрович", "Contractor", null,
            "long-password-1", DateOfBirth: new DateOnly(1990, 5, 5), Email: "w@example.invalid"), CancellationToken.None);

        Assert.True(account.IsSuccess);
        var recipient = _store.Recipients.Single();
        Assert.Equal(recipient.Id, account.Value.RecipientId);
        Assert.Equal("EMP-WORKER1", recipient.ExternalIdentifier);
    }

    [Fact]
    public async Task Duplicate_login_is_conflict_regardless_of_case()
    {
        await Users.CreateAsync(new CreateAccountInput("Manager2", "Управитель", "Manager", null, "long-password-1"),
                CancellationToken.None);
        var duplicate = await Users.CreateAsync(new CreateAccountInput("manager2", "Управитель", "Manager", null,
                "long-password-1"), CancellationToken.None);
        Assert.Equal(ErrorKind.Conflict, duplicate.Error!.Kind);
    }

    [Fact]
    public async Task Unknown_role_or_short_password_is_rejected()
    {
        Assert.Equal(ErrorKind.Validation, (await Users.CreateAsync(new CreateAccountInput("user1", "Имя", "Ghost", null,
                "long-password-1"), CancellationToken.None)).Error!.Kind);
        Assert.Equal(ErrorKind.Validation, (await Users.CreateAsync(new CreateAccountInput("user1", "Имя", "Manager", null,
                "short"), CancellationToken.None)).Error!.Kind);
    }

    [Fact]
    public async Task Role_in_use_or_system_role_cannot_be_deleted()
    {
        await Roles.CreateAsync(new RoleInput("DutyOfficer", "Дежурный", ["NoticesReadAll"]), CancellationToken.None);
        await Users.CreateAsync(new CreateAccountInput("duty1", "Дежурный", "DutyOfficer", null, "long-password-1"),
                CancellationToken.None);

        Assert.Equal(ErrorKind.Conflict, (await Roles.DeleteAsync("DutyOfficer", CancellationToken.None)).Error!.Kind);
        Assert.Equal(ErrorKind.Conflict, (await Roles.DeleteAsync("Manager", CancellationToken.None)).Error!.Kind);
    }

    [Fact]
    public async Task Role_list_shows_user_counts()
    {
        await Users.CreateAsync(new CreateAccountInput("op1", "Секретарь 1", "Operator", null, "long-password-1"), CancellationToken.None);
        await Users.CreateAsync(new CreateAccountInput("op2", "Секретарь 2", "Operator", null, "long-password-1"), CancellationToken.None);

        var roles = await Roles.ListAsync(CancellationToken.None);

        Assert.Equal(2, roles.Single(r => r.Name == "Operator").UsersCount);
        Assert.Equal(0, roles.Single(r => r.Name == "Manager").UsersCount);
    }

    [Fact]
    public async Task Role_update_invalidates_cache()
    {
        await Roles.CreateAsync(new RoleInput("DutyOfficer", "Дежурный", ["NoticesReadAll"]), CancellationToken.None);

        var updated = await Roles.UpdateAsync(Engineer, "DutyOfficer", new RoleInput(null, "Дежурный смены", ["NoticesReadAll",
                "AuditView"]), CancellationToken.None);

        Assert.True(updated.IsSuccess);
        Assert.Contains("DutyOfficer", _snapshots.Invalidated);
    }

    [Fact]
    public async Task Engineer_cannot_lock_himself_out()
    {
        await Users.CreateAsync(new CreateAccountInput("engineer", "Инженер", "AutomationEngineer", null, "long-password-1"),
                CancellationToken.None);

        var result = await Users.ChangeRoleAsync(Engineer, "engineer", new ChangeRoleInput("Manager"), CancellationToken.None);

        Assert.Equal(ErrorKind.Conflict, result.Error!.Kind);
    }

    [Fact]
    public async Task Login_returns_permissions_and_surname()
    {
        await Users.CreateAsync(new CreateAccountInput("Observer1", "Петров Пётр Петрович", "Observer", null, "long-password-1",
            DateOfBirth: new DateOnly(1990, 1, 1)), CancellationToken.None);
        var auth = new AuthService(_store, _store, new PlainHasher(), new AccessResolver(_store, _snapshots));

        var wrong = await auth.LoginAsync(new LoginInput("observer1", "bad-password"), CancellationToken.None);
        var ok = await auth.LoginAsync(new LoginInput("OBSERVER1", "long-password-1"), CancellationToken.None);

        Assert.Equal(ErrorKind.Unauthorized, wrong.Error!.Kind);
        Assert.Equal("Петров", ok.Value.Surname);
        Assert.Contains("NoticesReadOwn", ok.Value.Access.PermissionCodes);
    }
}
