// @file-module:permissions
using FluentValidation;

namespace AppName.Api.Features.Admin.Roles.Shared;

internal static class RoleRules
{
    public const int MaxNameLength = 64;

    /// <summary>The handler saves the trimmed name, so the length check uses the trimmed name.</summary>
    public static IRuleBuilderOptions<T, string> ValidRoleName<T>(this IRuleBuilder<T, string> rule) =>
        rule
            .Must(name => !string.IsNullOrWhiteSpace(name))
                .WithErrorCode("RoleNameRequired")
                .WithMessage("Role name is required")
            .Must(name => name is null || name.Trim().Length <= MaxNameLength)
                .WithErrorCode("RoleNameTooLong")
                .WithMessage($"Role name must not exceed {MaxNameLength} characters");
}
