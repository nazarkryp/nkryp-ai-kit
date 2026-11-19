// @file-module:auth
using System.Net;
using AppName.ServiceName.Api.Features.Accounts.CreateAccount;
using Microsoft.Extensions.Configuration;

namespace AppName.ServiceName.Api.IntegrationTests.Accounts;

/// <summary>A host where self-registration is closed, as in a project without the signup module.</summary>
public sealed class ClosedRegistrationApiFactory : ApiFactory
{
    protected override void ConfigureSettings(Dictionary<string, string?> settings) =>
        settings["AccountSettings:AllowUserRegistration"] = "false";
}

/// <summary>
/// The first account of an empty database can always register. Later accounts need
/// <c>AccountSettings:AllowUserRegistration</c>.
/// </summary>
public sealed class ClosedRegistrationTests(ClosedRegistrationApiFactory factory)
    : IClassFixture<ClosedRegistrationApiFactory>
{
    private const string Password = "Sup3r!Secret";

    [Fact]
    public async Task Only_the_first_account_can_register()
    {
        using var client = factory.CreateClient();

        var first = await client.PostJsonAsync("/v1/accounts", NewAccount("firstuser"));
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var second = await client.PostJsonAsync("/v1/accounts", NewAccount("seconduser"));
        await second.ShouldBeProblemAsync(HttpStatusCode.Forbidden, "RegistrationDisabled");
    }

    private static CreateAccountRequest NewAccount(string username) => new()
    {
        Username = username,
        EmailAddress = $"{username}@example.test",
        Password = Password
    };
}

/// <summary>The appsettings.json that the project ships.</summary>
public sealed class ShippedRegistrationSettingTests
{
    // @module:signup
    [Fact]
    public void Self_registration_is_open()
    {
        ShippedSetting("AccountSettings:AllowUserRegistration").Should().BeTrue();
    }
    // @end
    // @module:!signup
    [Fact]
    public void Self_registration_is_closed()
    {
        ShippedSetting("AccountSettings:AllowUserRegistration").Should().BeFalse();
    }
    // @end

    /// <summary>The API project copies its appsettings.json into the test output directory.</summary>
    private static bool ShippedSetting(string key)
    {
        var value = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.json"), optional: false)
            .Build()[key];

        return bool.Parse(value ?? throw new InvalidOperationException($"appsettings.json has no {key}."));
    }
}
