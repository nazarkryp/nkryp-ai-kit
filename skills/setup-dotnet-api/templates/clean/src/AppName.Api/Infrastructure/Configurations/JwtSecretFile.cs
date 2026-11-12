// @file-module:auth
using System.Security.Cryptography;
using System.Text;

namespace AppName.Api.Infrastructure.Configurations;

/// <summary>
/// The JWT signing key when Jwt__Secret is not set. The server makes the key on the first start and
/// saves it in <c>Jwt:SecretFile</c>. Later starts read the same key, so a restart does not sign out
/// the users or break the media links.
/// </summary>
public static class JwtSecretFile
{
    private const int SecretBytes = 32;

    /// <summary>
    /// Returns <c>Jwt:Secret</c> when it is set. Otherwise returns the key from <c>Jwt:SecretFile</c>, and
    /// makes the file when it does not exist. AddAppSecurity validates the result.
    /// </summary>
    public static string GetOrCreateJwtSecret(this IConfiguration configuration)
    {
        var secret = configuration["Jwt:Secret"];

        if (!string.IsNullOrEmpty(secret))
        {
            return secret;
        }

        var path = configuration["Jwt:SecretFile"];

        if (string.IsNullOrWhiteSpace(path))
        {
            throw new InvalidOperationException("Jwt:SecretFile is required when Jwt:Secret is not set.");
        }

        if (!File.Exists(path))
        {
            Create(path);
        }

        return File.ReadAllText(path).Trim();
    }

    private static void Create(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);

        // The key goes to a temporary file first, so a crash cannot leave an empty or partial key file.
        var temporaryPath = path + ".tmp";
        var options = new FileStreamOptions { Mode = FileMode.Create, Access = FileAccess.Write };

        if (!OperatingSystem.IsWindows())
        {
            // Only the account that runs the server can read the key.
            options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
        }

        using (var stream = new FileStream(temporaryPath, options))
        {
            stream.Write(Encoding.ASCII.GetBytes(Convert.ToBase64String(RandomNumberGenerator.GetBytes(SecretBytes))));
            stream.Flush(flushToDisk: true);
        }

        File.Move(temporaryPath, path, overwrite: false);
    }
}
