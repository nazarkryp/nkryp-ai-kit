// @file-module:permissions
using AppName.Domain.Accounts;

namespace AppName.UnitTests.Accounts;

public sealed class EffectivePermissionsTests
{
    /// <summary>Every code that a role or a grant can give, in catalog order.</summary>
    private static readonly string[] Grantable = Permissions.All.Where(x => x.IsGrantable).Select(x => x.Code).ToArray();

    [Fact]
    public void An_account_without_a_role_or_grants_has_no_permission()
    {
        var permissions = EffectivePermissions.For(role: null, grants: []);

        permissions.IsAdmin.Should().BeFalse();
        permissions.Codes.Should().BeEmpty();
        permissions.Has(Permissions.AccountsManage).Should().BeFalse();
    }

    [Fact]
    public void Role_permissions_and_grants_combine_in_catalog_order()
    {
        // The role has the second half of the catalog and the grants have the first half, both reversed.
        var half = Grantable.Length / 2;
        var role = NewRole(Enumerable.Reverse(Grantable[half..]).ToArray());

        var permissions = EffectivePermissions.For(role, Enumerable.Reverse(Grantable[..half]));

        permissions.IsAdmin.Should().BeFalse();
        permissions.Codes.Should().Equal(Grantable);
    }

    [Fact]
    public void Grants_alone_give_permissions()
    {
        var permissions = EffectivePermissions.For(role: null, Grantable);

        permissions.Codes.Should().Equal(Grantable);
        Grantable.Should().OnlyContain(code => permissions.Has(code));
    }

    [Fact]
    public void The_admin_role_has_every_permission_also_the_admin_only_ones()
    {
        var admin = new Role { Id = Role.AdminId, Name = Role.AdminName, Permissions = new HashSet<string>() };

        var permissions = EffectivePermissions.For(admin, grants: []);

        permissions.IsAdmin.Should().BeTrue();
        permissions.Codes.Should().Equal(Permissions.All.Select(x => x.Code));
        permissions.Has(Permissions.AccountsManage).Should().BeTrue();
    }

    [Fact]
    public void An_unknown_code_gives_nothing()
    {
        var permissions = EffectivePermissions.For(NewRole("items.fly"), ["Accounts.Manage"]);

        permissions.Codes.Should().BeEmpty();
    }

    [Fact]
    public void An_admin_only_code_in_a_role_or_a_grant_gives_nothing()
    {
        var adminOnly = Permissions.All.Where(x => !x.IsGrantable).Select(x => x.Code).ToArray();

        var permissions = EffectivePermissions.For(NewRole(adminOnly), adminOnly);

        permissions.Codes.Should().BeEmpty();
        adminOnly.Should().NotContain(code => permissions.Has(code));
    }

    private static Role NewRole(params string[] codes) => new()
    {
        Id = Guid.NewGuid(),
        Name = "Family",
        Permissions = codes.ToHashSet(StringComparer.Ordinal)
    };
}
