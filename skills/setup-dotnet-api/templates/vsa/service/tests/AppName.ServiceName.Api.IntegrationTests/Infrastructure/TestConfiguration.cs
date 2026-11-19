namespace AppName.ServiceName.Api.IntegrationTests.Infrastructure;

/// <summary>
/// Every configuration key the API needs, pointed at the test's temporary directories.
/// <para>
/// This must stay complete. <c>AppConfiguration.Build</c> rebuilds configuration from the
/// current directory and layers in <c>appsettings.local.json</c>, which can hold machine
/// specific values. Setting each key here keeps the host away from them.
/// </para>
/// </summary>
public static class TestConfiguration
{
    // @module:auth
    /// <summary>Long enough that no token expires while a test runs.</summary>
    public const string AccessTokenLifetime = "00:30:00";

    // @end
    /// <summary>
    /// The keys whose value is a file path. <see cref="ApiFactory"/> stops the run when one of them
    /// points outside the temporary directory of the test host.
    /// </summary>
    public static readonly IReadOnlyList<string> PathKeys =
    [
        // @module:jwt
        "Jwt:SecretFile",
        // @end
    ];

    public static Dictionary<string, string?> Build(TestPaths paths) => new()
    {
        ["Cors:AllowedOrigins:0"] = "http://localhost",

        ["Collection:DefaultLimit"] = "24",
        ["Collection:MaxLimit"] = "100",
        // @module:trust

        // The key, issuer and audience of the auth service. TestTokens signs the tokens of the tests with them.
        ["Jwt:Secret"] = TestTokens.Secret,
        ["Jwt:SecretFile"] = Path.Combine(paths.DataDirectory, "jwt-secret"),
        ["Jwt:Issuer"] = TestTokens.Issuer,
        ["Jwt:Audience"] = TestTokens.Audience,
        // @end
        // @module:auth

        ["Jwt:KeyId"] = "appname-test-key",
        ["Jwt:Secret"] = "hjJlQPe9BU/DCMZj5za+vGR2rlo/afL3ezWO6ow6ig4=",
        ["Jwt:SecretFile"] = Path.Combine(paths.DataDirectory, "jwt-secret"),
        ["Jwt:Issuer"] = "https://localhost",
        ["Jwt:Audience"] = "appname-api",
        ["Jwt:TokenType"] = "Bearer",
        ["Jwt:AccessTokenExpiresIn"] = AccessTokenLifetime,
        ["Jwt:RefreshTokenExpiresIn"] = "7.00:00:00",

        // The tests sign in many times from one address. The limit itself has its own test host.
        ["CredentialsRateLimit:PermitLimit"] = "100000",
        ["CredentialsRateLimit:Window"] = "00:01:00",

        ["AccountSettings:MinUsernameLength"] = "3",
        ["AccountSettings:MaxUsernameLength"] = "20",
        ["AccountSettings:UsernameRegexPattern"] =
            "^(?:[a-zA-Z][a-zA-Z0-9]*(?:[._][a-zA-Z0-9]+)*|[0-9]+[a-zA-Z][a-zA-Z0-9]*(?:[._][a-zA-Z0-9]+)*)$",
        ["AccountSettings:EmailAddressPattern"] = "^[^@\\s]+@[^@\\s]+\\.[^@\\s]+$",
        // The tests register many accounts, also in a project without the signup module.
        // RegistrationTests checks the closed registration on its own host.
        ["AccountSettings:AllowUserRegistration"] = "true",
        ["AccountSettings:BlockedWords:0"] = "idiot",
        ["AccountSettings:SystemUsernames:0"] = "admin",
        ["AccountSettings:SystemUsernames:1"] = "root",

        ["PasswordSettings:MinPasswordLength"] = "8",
        ["PasswordSettings:MaxPasswordLength"] = "128",
        ["PasswordSettings:RequireUppercase"] = "true",
        ["PasswordSettings:RequireLowercase"] = "true",
        ["PasswordSettings:RequireDigit"] = "true",
        ["PasswordSettings:RequireSpecialCharacter"] = "true",
        ["PasswordSettings:SpecialCharacters"] = "!@#$%^&*()-_=+[]{}|;:,.<>?",
        ["PasswordSettings:DisallowUsernameInPassword"] = "true",
        ["PasswordSettings:DisallowEmailInPassword"] = "true",
        ["PasswordSettings:MaxConsecutiveCharacters"] = "3",
        ["PasswordSettings:AllowWhitespace"] = "false",
        ["PasswordSettings:BlockedPasswords:0"] = "password",
        // @end
    };
}
