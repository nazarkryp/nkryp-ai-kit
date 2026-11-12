namespace AppName.Api.Infrastructure.Configurations;

public static class AppConfiguration
{
    public static IConfiguration Build(string[]? args = null)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

        var configurationBuilder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddJsonFile("appsettings.local.json", optional: true)
            .AddEnvironmentVariables("DOTNET_")
            .AddEnvironmentVariables("ASPNETCORE_")
            .AddEnvironmentVariables();

        if (args != null && args.Any())
        {
            configurationBuilder.AddCommandLine(args);
        }

        return configurationBuilder.Build();
    }
}

public static class WebApplicationBuilderExtensions
{
    public static void ConfigureAppConfiguration(this WebApplicationBuilder webApplicationBuilder, string[]? args)
    {
        var configuration = AppConfiguration.Build(args);

        webApplicationBuilder.Configuration
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddConfiguration(configuration);
    }
}