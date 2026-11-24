using Adimarip.Domain.Access;
using Adimarip.Domain.Common;
using Adimarip.Tests.Fakes;
using Xunit;

namespace Adimarip.Tests.Domain;

public sealed class AccessTests
{
    [Fact]
    public void System_roles_keep_previous_access_rules()
    {
        var manager = SystemRoles.Get(SystemRoles.Manager);
        var operatorRole = SystemRoles.Get(SystemRoles.Operator);
        var observer = SystemRoles.Get(SystemRoles.Observer);
        var engineer = SystemRoles.Get(SystemRoles.AutomationEngineer);

        Assert.True(manager.Has(Permission.AuditView));
        Assert.False(operatorRole.Has(Permission.AuditView));
        Assert.True(operatorRole.Has(Permission.HistoryView));
        Assert.Equal(DataScope.Own, Role.ScopeOf(observer.PermissionSet));
        Assert.True(observer.RequiresRecipientBinding);
        Assert.False(observer.Has(Permission.NoticesCreate));
        Assert.Equal(DataScope.None, Role.ScopeOf(engineer.PermissionSet)); // инженер не видит бизнес-данные
        Assert.True(engineer.Has(Permission.RolesManage));
    }

    [Fact]
    public void Custom_role_is_created_from_permission_codes()
    {
        var role = Role.CreateCustom("DutyOfficer", "Дежурный", ["NoticesReadAll", "BroadcastsSend"], TestData.Now).Value;

        Assert.False(role.IsSystem);
        Assert.True(role.Has(Permission.BroadcastsSend));
        Assert.False(role.RequiresRecipientBinding);
    }

    [Theory]
    [InlineData("1abc")]
    [InlineData("ab")]
    [InlineData("has space")]
    public void Custom_role_code_is_validated(string name) =>
        Assert.True(Role.CreateCustom(name, "Название", ["NoticesReadAll"], TestData.Now).IsFailure);

    [Fact]
    public void Custom_role_rejects_unknown_or_empty_permissions()
    {
        Assert.True(Role.CreateCustom("Custom1", "Название", ["DropDatabase"], TestData.Now).IsFailure);
        Assert.True(Role.CreateCustom("Custom1", "Название", [], TestData.Now).IsFailure);
    }

    [Fact]
    public void Read_all_supersedes_read_own()
    {
        var role = Role.CreateCustom("Mixed", "Смешанная", ["NoticesReadAll", "NoticesReadOwn"], TestData.Now).Value;
        Assert.False(role.Has(Permission.NoticesReadOwn));
    }

    [Fact]
    public void Update_changes_only_the_difference()
    {
        var role = Role.CreateCustom("Custom1", "Название", ["NoticesReadAll", "HistoryView"], TestData.Now).Value;
        var kept = role.Permissions.Single(p => p.Permission == Permission.NoticesReadAll);

        role.Update("Новое название", ["NoticesReadAll", "AuditView"]);

        Assert.Equal("Новое название", role.DisplayName);
        Assert.Contains(kept, role.Permissions); // тот же объект — EF не удалит и не вставит строку заново
        Assert.False(role.Has(Permission.HistoryView));
        Assert.True(role.Has(Permission.AuditView));
    }

    [Fact]
    public void System_role_cannot_be_changed() =>
        Assert.Equal(ErrorKind.Conflict, SystemRoles.Get(SystemRoles.Manager).Update("x", ["AuditView"]).Error!.Kind);

    [Fact]
    public void Own_scope_account_requires_recipient()
    {
        var observer = SystemRoles.Get(SystemRoles.Observer);
        Assert.True(AppUser.Create("worker1", "Работник", observer, null, "hash", TestData.Now).IsFailure);
        Assert.True(AppUser.Create("worker1", "Работник", observer, 5, "hash", TestData.Now).IsSuccess);
    }

    [Fact]
    public void Recipient_filter_fails_closed_without_binding()
    {
        Assert.Null(TestData.Access(SystemRoles.Manager).RecipientFilter().Value);
        Assert.Equal(7, TestData.Access(SystemRoles.Observer, recipientId: 7).RecipientFilter().Value);
        Assert.Equal(ErrorKind.Forbidden, TestData.Access(SystemRoles.Observer).RecipientFilter().Error!.Kind);
        Assert.True(TestData.Access(SystemRoles.AutomationEngineer).RecipientFilter().IsFailure);
    }
}
