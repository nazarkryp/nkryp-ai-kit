// @file-module:auth
using AppName.Security.Tokens;

namespace AppName.Security.Abstractions;

public interface ITokenManager
{
    ValueTask RevokeTokenAsync(string tokenId, DateTime expiresOn, CancellationToken cancellationToken = default);

    ValueTask<bool> ValidateTokenAsync(string tokenId, CancellationToken cancellationToken = default);
}