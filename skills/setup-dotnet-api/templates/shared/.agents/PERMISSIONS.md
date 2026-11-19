<!-- @file-module:auth -->
# Authentication and permissions

Read this file for a task that touches authentication, authorization, permissions, roles, accounts,
sign-in or tokens. The code is the source of truth. This file says where things are and why.

One service owns the accounts and issues the access tokens. In the paths below, `<Auth>` is its name:
<!-- @each-service -->
<!-- @module:auth -->
`ServiceName`.
<!-- @end -->
<!-- @end -->
All services check the tokens with `AppName.Api.Authentication` and the same key, issuer and audience.
<!-- @module:trust -->
A trust service has no accounts. It cannot read the revocation list of the auth service, so a
signed-out access token stays valid there until it expires.
<!-- @end -->
<!-- @module:permissions -->

## Model

- A **permission** is a stable code in `AppName.<Auth>.Domain/Accounts/Permissions.cs`. The code never changes,
  because the database and the access tokens store it.
- A **role** is a named set of permissions. An admin edits roles through the admin API.
- An **account** has one role or none (`Account.RoleId`), plus **direct grants** (`Account.Grants`).
- What an account may do = the permissions of its role + its direct grants (`EffectivePermissions`).
  A direct grant only adds. A stored code that is unknown or admin-only gives nothing.
- The **Admin** role has a fixed id (`Role.AdminId`). It has every permission, also the ones added later,
  so it stores none. Nobody can edit or delete it.
- The first account of an empty database gets the Admin role (`IAccountRepository.EnsureAdminExistsAsync`).
  A self-registered account has no role.

| Code | Allows | Grantable |
|---|---|---|
| `accounts.manage` | Accounts, roles, grants | no (admin only) |

Every signed-in account may also read `/accounts/me`, change its own password and sign out.
<!-- @end -->

## Token

- Sign-in returns an access token (JWT) in the body and a refresh token in the HttpOnly cookie
  `refresh_token` (SameSite=Strict). The refresh token is never in the body.
<!-- @module:permissions -->
- Sign-in and refresh read the role and the grants (`AccountAccessResolver`) and write one `perm` claim
  for each permission. An admin token has the `Admin` role claim and no `perm` claims.
- The access token lives 60 s, so a changed role or grant applies at the next refresh.
<!-- @end -->
- The database stores only the SHA-256 hash of a refresh token (`RefreshTokenHash`). A refresh rotates
  the token in one transaction (`IRefreshTokenRepository.RotateAsync`).
- The token has `typ: at+jwt`. The server accepts only HS256 and only this type.
- The signing key comes from `Jwt__Secret` (base64, at least 32 bytes). Without it, `JwtSecretFile`
  makes a key on the first start and saves it in `Jwt:SecretFile`. Later starts read that file. An
  invalid key stops the server before it changes the database.
- Sign-out revokes the access token id (`ITokenManager`, a distributed cache) and deletes the refresh
  token. `TokenRevocationMiddleware` refuses a revoked access token with 401 `token_revoked`.
<!-- @module:permissions -->
- The client reads `is_admin` and `permissions` from the token response, and the account id and username
  from `/accounts/me`. It does not decode the JWT (RFC 9068 section 6).
<!-- @end -->

## Enforcement

- Every route has exactly one rule from `AppName.<Service>.Api/Infrastructure/Extensions/AuthorizationEndpointExtensions.cs`
  or `AllowAnonymous()`. Put the rule on the route, not on the group.
<!-- @module:permissions -->
  - `RequirePermission(code)` / `RequireAnyPermission(a, b, ...)`: a signed-in account with one of the permissions.
<!-- @end -->
  - `AllowAnySignedInUser()`: any signed-in account.
  - `AllowAnonymous()`: no sign-in.
- The fallback policy needs a signed-in user, so a route without a rule is never public by mistake.
- `RouteMap` in `tests/AppName.<Service>.Api.IntegrationTests/Security/EndpointAuthorizationCoverageTests.cs` is the
  approved route map. A new or changed route fails that test until you add it to the map on purpose.
<!-- @module:permissions -->
- `PermissionAuthorizeAttribute` (`AuthorizeAttribute` + `IAuthorizationRequirementData`) carries the
  requirement. `PermissionAuthorizationHandler` passes it for a matching `perm` claim or the Admin role.
  It never calls `Fail()`, so a later handler can still run.
- A check that depends on the request data is in the handler, with
  `AccountAccessResolver.HasPermissionAsync` (it reads the database).
- A missing permission gives 403 ProblemDetails `InsufficientPermissions` with `details.requiredAnyOf`
  (`PermissionDeniedResultHandler`), and a `SecurityEvent AuthorizationDenied` log line.
<!-- @end -->
- Sign-in and password change share the `AccountCredentials` rate limit (`CredentialsRateLimit`
  settings, per client address). A refused request gets 429 ProblemDetails.
<!-- @module:permissions -->
  Account setup with a setup code shares it too.
<!-- @end -->
- `ForwardedHeaders` lists the trusted proxies, so the rate limit sees the client address.

## Accounts API

- `POST /v1/accounts`: self-registration. The first account is always allowed. Later accounts need
  `AccountSettings:AllowUserRegistration`.
- `POST /v1/accounts/signin`, `POST /v1/accounts/token/refresh` (cookie), `POST /v1/accounts/signout`.
- `GET /v1/accounts/me`, `PUT /v1/accounts/me/password` (needs the current password, ends the other sessions).
<!-- @module:permissions -->
- `POST /v1/accounts/setup`: the user sets a password with a setup code.

## Account management API

All routes need `accounts.manage` (admin only):

- `/v1/admin/accounts`: list, get, create, `PUT {id}/role`, `PUT {id}/permissions`,
  `POST {id}/sessions/revoke`, `POST {id}/setup-code`.
- `/v1/admin/roles`: list, get, create, update, delete. `/v1/admin/permissions`: the catalog.

Rules:

- An admin cannot change their own role (403).
- The last admin keeps the Admin role (409). `TrySetRoleAsync` checks this in the same atomic statement as
  the update, so two parallel demotions cannot both win.
- A role that an account has cannot be deleted (409). The Admin role cannot be changed (403).
- A role or a grant cannot have an admin-only permission (400).

Setup codes:

- A new account has an unknown random password and a one-time **setup code** (128 bits, 24 h). The server
  stores only its SHA-256 hash and shows the code once.
- The user sets a password with `POST /v1/accounts/setup`. The code works once, and every session of the
  account ends. A new code (`POST {id}/setup-code`) replaces the old one.
- A password change deletes the open setup code of the account.

## Add a permission

1. Add the constant and its `PermissionDefinition` to `Permissions.cs`. Decide if it is grantable.
2. Put it on the routes, and update `RouteMap` in the coverage test.
3. Existing roles do not get it. Admin gets it at once.

To rename or remove a code, add a data migration that changes the stored rows.
<!-- @end -->

## Audit

Each security change logs one line `SecurityEvent {Event}: ...` with a message template, for example
PasswordChanged.
<!-- @module:permissions -->
The events are AccountCreated, RoleAssigned, GrantsReplaced, SessionsRevoked, SetupCodeIssued,
AccountSetupCompleted, PasswordChanged, RoleCreated, RoleUpdated, RoleDeleted and AuthorizationDenied.
<!-- @end -->
Today the only sink is the console.

## Known gaps

These are not done yet. Each one is a separate task:

- No account lockout after failed sign-ins (only the per-address rate limit).
- An account cannot be disabled or deleted.
- The token revocation list is an in-memory cache. Use a shared cache when the API runs on more than one instance.
- Anonymous OpenAPI and Scalar docs also in production. Restrict them when the API is public.
