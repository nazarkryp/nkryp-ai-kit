// @file-module:permissions
namespace AppName.ServiceName.Api.Contracts.Accounts;

/// <param name="RequiredAnyOf">The route needs one of these permission codes.</param>
public sealed record InsufficientPermissionsDetails(IReadOnlyList<string> RequiredAnyOf);
