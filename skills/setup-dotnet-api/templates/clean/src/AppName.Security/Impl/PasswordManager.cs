// @file-module:auth
using System.Security.Cryptography;
using AppName.Security.Abstractions;

namespace AppName.Security.Impl;

public sealed class PasswordManager : IPasswordManager
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 600_000;

    private static readonly HashAlgorithmName AlgorithmName = HashAlgorithmName.SHA512;

    public string CreatePasswordHash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, AlgorithmName, HashSize);

        return $"pbkdf2-{AlgorithmName.Name!.ToLowerInvariant()}" +
               $"${Iterations}" +
               $"${Convert.ToHexString(salt)}" +
               $"${Convert.ToHexString(hash)}";
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        var parts = passwordHash.Split('$');
        if (parts.Length != 4)
        {
            return false;
        }

        if (!int.TryParse(parts[1], out var iterations))
        {
            return false;
        }

        var algorithmName = new HashAlgorithmName(parts[0].Split('-')[1].ToUpperInvariant());
        var salt = Convert.FromHexString(parts[2]);
        var hash = Convert.FromHexString(parts[3]);

        var inputHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, algorithmName, hash.Length);

        return CryptographicOperations.FixedTimeEquals(hash, inputHash);
    }
}
