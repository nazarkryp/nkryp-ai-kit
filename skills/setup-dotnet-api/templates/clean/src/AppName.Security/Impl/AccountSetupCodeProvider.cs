// @file-module:permissions
using System.Buffers;
using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using AppName.Security.Abstractions;
using AppName.Security.Tokens;

namespace AppName.Security.Impl;

public sealed class AccountSetupCodeProvider : IAccountSetupCodeProvider
{
    private const int CodeByteCount = 16;

    private static readonly int CodeLength = Base64Url.GetEncodedLength(CodeByteCount);

    private static readonly SearchValues<char> Base64UrlCharacters =
        SearchValues.Create("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_");

    public AccountSetupCodeValue Generate()
    {
        var value = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(CodeByteCount));

        return new AccountSetupCodeValue(value, Hash(value));
    }

    // The code has 128 random bits, so a fast hash is enough: nobody can guess it from the hash.
    public string Hash(string code) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(code)));

    public bool IsFormat(string code) =>
        code.Length == CodeLength && !code.AsSpan().ContainsAnyExcept(Base64UrlCharacters);
}
