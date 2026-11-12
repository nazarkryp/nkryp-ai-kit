// @file-module:permissions
using FluentValidation;
using AppName.Domain.Accounts;

namespace AppName.Application.Accounts;

internal static class PermissionRules
{
    /// <summary>
    /// Every code must be a known permission that can be granted. An admin-only code is refused, so no
    /// role and no account gets it except through the Admin role.
    /// </summary>
    public static IRuleBuilderOptions<T, IReadOnlyList<string>> GrantablePermissions<T>(
        this IRuleBuilderInitial<T, IReadOnlyList<string>> rule) =>
        rule
            .Cascade(CascadeMode.Stop)
            .NotNull()
                .WithErrorCode("PermissionsRequired")
                .WithMessage("The permission list is required")
            // A null element must not reach IsKnown.
            .Must(codes => codes.All(code => !string.IsNullOrWhiteSpace(code)))
                .WithErrorCode("UnknownPermission")
                .WithMessage("A permission code must not be empty")
            .Must(codes => codes.All(Permissions.IsKnown))
                .WithErrorCode("UnknownPermission")
                .WithMessage((_, codes) => "Unknown permission: " + string.Join(", ", codes.Where(code => !Permissions.IsKnown(code))))
            .Must(codes => codes.Where(Permissions.IsKnown).All(Permissions.IsGrantable))
                .WithErrorCode("PermissionNotGrantable")
                .WithMessage((_, codes) => "Admin-only permission: " + string.Join(", ", codes.Where(code => Permissions.IsKnown(code) && !Permissions.IsGrantable(code))));
}
