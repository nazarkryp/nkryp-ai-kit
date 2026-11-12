using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AppName.Api.IntegrationTests.Infrastructure;

/// <summary>
/// Hosts the real API in process. Each test class gets its own instance, so it also gets its own
/// temporary directory (and its own database, when the project has one): one class never sees the data
/// of another class.
/// <para>
/// A test class that needs other configuration values uses a subclass that overrides
/// <see cref="ConfigureSettings"/>.
/// </para>
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    // @module:permissions
    private readonly SemaphoreSlim _adminLock = new(1, 1);

    private bool _adminCreated;

    // @end
    public TestPaths Paths { get; } = TestPaths.Create();
    // @module:permissions

    /// <summary>
    /// The admin of this host: the first account that the tests create. The API gives the Admin role to
    /// the first account of a new database. Null on a host whose database already has an admin.
    /// </summary>
    public TestAccount? AdminAccount { get; private set; }
    // @end

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // The Development environment shows the exception message in a 500 response, which makes a
        // failed test easier to read.
        builder.UseEnvironment("Development");

        // app.UseHttpsRedirection() turns requests into 307s when an https port is configured.
        builder.UseSetting("https_port", string.Empty);

        var settings = TestConfiguration.Build(Paths);

        // @module:auth
        // [setup:persistence-test] The database step of the setup gives each test class its own database
        // here, for example a file in Paths.DataDirectory or a container, and sets its connection string
        // in settings. A plain file path goes in TestConfiguration.PathKeys. A connection string that
        // holds a file path is checked in CreateHost, after its file path is parsed.
        // @end

        ConfigureSettings(settings);

        // Program.cs reads builder.Configuration while it registers services, which happens before
        // ConfigureAppConfiguration runs. UseSetting values are already there at that point, so the
        // registrations see the test values.
        foreach (var (key, value) in settings)
        {
            builder.UseSetting(key, value);
        }

        // The same values again for everything that reads configuration after the host is built.
        builder.ConfigureAppConfiguration(configuration => configuration.AddInMemoryCollection(settings));
    }
    // @module:permissions

    /// <summary>Creates the admin of this host once, before any other test account.</summary>
    /// <param name="register">Registers an account and tells whether the API made it the admin.</param>
    internal async Task EnsureAdminAsync(
        Func<Task<(TestAccount Account, bool IsAdmin)>> register,
        CancellationToken cancellationToken)
    {
        await _adminLock.WaitAsync(cancellationToken);

        try
        {
            if (_adminCreated)
            {
                return;
            }

            var (account, isAdmin) = await register();

            AdminAccount = isAdmin ? account : null;
            _adminCreated = true;
        }
        finally
        {
            _adminLock.Release();
        }
    }
    // @end

    /// <summary>Changes the test configuration before the host reads it.</summary>
    protected virtual void ConfigureSettings(Dictionary<string, string?> settings)
    {
    }

    /// <summary>
    /// Fails the run if the host did not take the test configuration. The API project copies its
    /// appsettings files into the test output directory, and Program.cs reads them from the current
    /// directory. Without this check a configuration regression would quietly point the tests at
    /// real data.
    /// </summary>
    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        var configuration = host.Services.GetRequiredService<IConfiguration>();

        foreach (var key in TestConfiguration.PathKeys)
        {
            RequireTestPath(configuration[key], Paths.Root, key);
        }

        return host;
    }

    private static void RequireTestPath(string? value, string root, string key)
    {
        if (value is null || !Path.GetFullPath(value).StartsWith(root, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"The test host did not use the test configuration. {key} is '{value}', " +
                $"which is outside '{root}'.");
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            Paths.Delete();
        }
    }
}
