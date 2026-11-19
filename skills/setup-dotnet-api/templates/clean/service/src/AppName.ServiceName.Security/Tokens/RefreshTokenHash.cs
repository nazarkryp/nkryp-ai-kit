// @file-module:auth
using System.Security.Cryptography;
using System.Text;

namespace AppName.ServiceName.Security.Tokens;

/// <summary>
/// The hash under which the database stores a refresh token. The token has 512 random bits, so a fast
/// hash is enough: nobody can find the token from the hash.
/// </summary>
public static class RefreshTokenHash
{
    public static string Compute(string refreshToken) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));
}
