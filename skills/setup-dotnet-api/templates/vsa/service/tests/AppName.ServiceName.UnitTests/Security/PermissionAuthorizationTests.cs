// @file-module:permissions
using System.Security.Claims;
using AppName.Api.Authentication;
using AppName.ServiceName.Domain.Accounts;
using AppName.ServiceName.Api.Infrastructure.Security.Authentication;
using AppName.ServiceName.Api.Infrastructure.Security.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace AppName.ServiceName.UnitTests.Security;

public sealed class PermissionAuthorizationTests
{
    // The handler compares claims only, so these codes do not have to be in the catalog.
    private const string ItemsRead = "items.read";
    private const string ItemsWrite = "items.write";

    private readonly PermissionAuthorizationHandler _handler = new();

    [Fact]
    public async Task A_user_without_the_permission_claim_does_not_pass()
    {
        var passed = await PassesAsync(SignedIn(), ItemsWrite);

        passed.Should().BeFalse();
    }

    [Fact]
    public async Task A_user_with_the_permission_claim_passes()
    {
        var passed = await PassesAsync(SignedIn(Permission(ItemsWrite)), ItemsWrite);

        passed.Should().BeTrue();
    }

    [Fact]
    public async Task A_user_with_another_permission_claim_does_not_pass()
    {
        var passed = await PassesAsync(SignedIn(Permission(ItemsRead)), ItemsWrite);

        passed.Should().BeFalse();
    }

    [Fact]
    public async Task One_permission_of_an_any_of_list_is_enough()
    {
        var user = SignedIn(Permission(ItemsRead));

        var passed = await PassesAsync(user, ItemsWrite, ItemsRead);

        passed.Should().BeTrue();
    }

    [Fact]
    public async Task An_admin_passes_without_permission_claims()
    {
        var passed = await PassesAsync(SignedIn(new Claim(ClaimTypes.Role, AccountRoles.Admin)), Permissions.AccountsManage);

        passed.Should().BeTrue();
    }

    [Fact]
    public async Task Another_role_does_not_pass()
    {
        var passed = await PassesAsync(SignedIn(new Claim(ClaimTypes.Role, "Editor")), Permissions.AccountsManage);

        passed.Should().BeFalse();
    }

    [Fact]
    public async Task An_unauthenticated_principal_does_not_pass_even_with_the_claim()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity([Permission(ItemsWrite)]));

        var passed = await PassesAsync(user, ItemsWrite);

        passed.Should().BeFalse();
    }

    [Fact]
    public async Task A_missing_permission_leaves_the_requirement_open_and_does_not_fail_the_context()
    {
        var requirement = new PermissionRequirement([ItemsWrite]);
        var context = new AuthorizationHandlerContext([requirement], SignedIn(), resource: null);

        await _handler.HandleAsync(context);

        context.HasFailed.Should().BeFalse();
        context.PendingRequirements.Should().Contain(requirement);
    }

    [Fact]
    public void The_attribute_keeps_the_codes_and_adds_one_requirement()
    {
        var attribute = new PermissionAuthorizeAttribute(Permissions.AccountsManage);

        attribute.Requirement.AnyOf.Should().Equal(Permissions.AccountsManage);
        attribute.GetRequirements().Should().ContainSingle().Which.Should().BeSameAs(attribute.Requirement);
        attribute.Policy.Should().BeNull();
        attribute.Roles.Should().BeNull();
    }

    public static TheoryData<string[]> InvalidCodeLists => new(
        Array.Empty<string>(),
        new[] { "items.fly" },
        new[] { Permissions.AccountsManage, Permissions.AccountsManage });

    [Theory]
    [MemberData(nameof(InvalidCodeLists))]
    public void The_attribute_refuses_an_empty_unknown_or_repeated_code(string[] codes)
    {
        var create = () => new PermissionAuthorizeAttribute(codes);

        create.Should().Throw<ArgumentException>();
    }

    private async Task<bool> PassesAsync(ClaimsPrincipal user, params string[] anyOf)
    {
        var requirement = new PermissionRequirement(anyOf);
        var context = new AuthorizationHandlerContext([requirement], user, resource: null);

        await _handler.HandleAsync(context);

        return context.HasSucceeded;
    }

    private static ClaimsPrincipal SignedIn(params Claim[] claims) =>
        new(new ClaimsIdentity(claims, authenticationType: "Test", nameType: ClaimTypes.Name, roleType: ClaimTypes.Role));

    private static Claim Permission(string code) => new(AccountTokenDefaults.PermissionClaimType, code);
}
