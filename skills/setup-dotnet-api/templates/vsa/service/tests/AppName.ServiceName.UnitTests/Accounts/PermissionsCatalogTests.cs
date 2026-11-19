// @file-module:permissions
using System.Reflection;
using AppName.ServiceName.Domain.Accounts;

namespace AppName.ServiceName.UnitTests.Accounts;

public sealed class PermissionsCatalogTests
{
    [Fact]
    public void Every_code_constant_has_exactly_one_definition()
    {
        var constants = typeof(Permissions)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(x => x.IsLiteral)
            .Select(x => (string)x.GetRawConstantValue()!)
            .ToList();

        Permissions.All.Select(x => x.Code).Should().BeEquivalentTo(constants).And.OnlyHaveUniqueItems();
    }

    [Fact]
    public void Codes_have_the_resource_dot_operation_form()
    {
        Permissions.All.Should().AllSatisfy(x => x.Code.Should().MatchRegex("^[a-z]+\\.[a-z]+$"));
    }

    [Fact]
    public void Every_permission_has_a_description()
    {
        Permissions.All.Should().AllSatisfy(x => x.Description.Should().NotBeNullOrWhiteSpace());
    }

    /// <summary>The approved list of admin-only codes. Change it on purpose when a new admin-only code comes.</summary>
    [Fact]
    public void Only_the_admin_only_codes_cannot_be_granted()
    {
        Permissions.All.Where(x => !x.IsGrantable).Select(x => x.Code).Should().BeEquivalentTo(
            Permissions.AccountsManage);
    }

    [Fact]
    public void IsKnown_and_IsGrantable_compare_the_exact_code()
    {
        Permissions.IsKnown(Permissions.AccountsManage).Should().BeTrue();
        Permissions.IsKnown("Accounts.Manage").Should().BeFalse();
        Permissions.IsGrantable(Permissions.AccountsManage).Should().BeFalse();
        Permissions.IsGrantable("items.fly").Should().BeFalse();
    }
}
